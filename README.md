# InvestCore

**Plataforma Distribuída de Gestão de Investimentos · Microserviços · Arquitetura Event-Driven**

![.NET 9](https://img.shields.io/badge/.NET-9.0-512BD4?style=flat-square&logo=dotnet)
![C# 12](https://img.shields.io/badge/C%23-12-239120?style=flat-square&logo=csharp)
![Docker](https://img.shields.io/badge/Docker-Compose-2496ED?style=flat-square&logo=docker)
![RabbitMQ](https://img.shields.io/badge/RabbitMQ-MassTransit-FF6600?style=flat-square&logo=rabbitmq)
![Redis](https://img.shields.io/badge/Redis-Cache-DC382D?style=flat-square&logo=redis)
![xUnit](https://img.shields.io/badge/xUnit-Testes-512BD4?style=flat-square)

---

## Visão Geral

O **InvestCore** é uma plataforma distribuída de alta performance que simula os fluxos críticos de uma gestora de investimentos de médio porte — o tipo de sistema que roda por trás de corretoras como **XP Investimentos**, **BTG Pactual** e **Banco Safra**.

O projeto cobre o ciclo completo: onboarding com verificação KYC, classificação de perfil de investidor (suitability CVM), execução de ordens de investimento, custódia de ativos e log imutável de compliance — tudo em uma arquitetura de microserviços desacoplados, comunicando-se via barramento de eventos.

> **Por que esse domínio importa?** Sistemas financeiros são dos mais exigentes em produção: requerem consistência eventual com rastreabilidade total, ordenação de eventos sem perda e separação estrita entre leitura e escrita de posições. Cada padrão adotado aqui foi escolhido para refletir os desafios reais desse mercado.

---

## Arquitetura

```
                    ┌──────────────────────────────────────────────┐
                    │           API GATEWAY (YARP)                 │
                    │     Porta :5050 · Swagger Unificado          │
                    └───────┬──────────────┬───────────────────────┘
                            │              │
          ┌─────────────────▼──┐     ┌─────▼──────────────────────┐
          │   IdentityService  │     │      ClientService          │
          │  JWT · BCrypt · KYC│     │  Suitability · Perfil CVM  │
          │     Porta :5001    │     │      Porta :5002            │
          └────────────────────┘     └────────────────────────────┘
                                                  │
                            ┌─────────────────────▼──────────────┐
                            │           OrderService              │
                            │   Ciclo de Ordens · Outbox Pattern  │
                            │           Porta :5003               │
                            └──────────────┬─────────────────────┘
                                           │
                              ┌────────────▼────────────┐
                              │   RabbitMQ · MassTransit │
                              │  Barramento de Eventos   │
                              └──┬──────────┬────────────┘
                                 │          │
          ┌──────────────────────▼──┐  ┌────▼──────────────────────┐
          │     PortfolioService    │  │      AuditService          │
          │  Posições · Redis Cache │  │  Log Imutável · Compliance │
          │       Porta :5004       │  │       Porta :5005          │
          └─────────────────────────┘  └───────────────────────────┘
                                                  │
                            ┌─────────────────────▼──────────────┐
                            │        NotificationService          │
                            │     Alertas · Webhooks              │
                            │           Porta :5006               │
                            └────────────────────────────────────┘

  [ SQL Server ]  [ SQL Server ]  [ SQL Server ]  [ SQL Server ]  [ SQL Server ]
  Identity DB      Client DB       Order DB        Portfolio DB    Audit DB
  (Isolado)        (Isolado)       (Isolado)       (Isolado)       (Isolado)
                                                        +
                                                   [ Redis ]
                                                  Portfolio Cache
```

### Decisões Arquiteturais

| Decisão | Solução Adotada | Justificativa |
|---|---|---|
| Garantia de entrega de eventos | Outbox Pattern (EF Core) | Atomicidade entre banco e mensageria sem 2PC |
| Leitura de posições em tempo real | Redis (CQRS read model) | Latência sub-ms sem pressionar o SQL transacional |
| Compliance regulatório | Suitability isolado no ClientService | Bounded Context independente, regra isolada |
| Rastreabilidade distribuída | OpenTelemetry + Jaeger | Correlation ID atravessa todos os serviços |
| Comunicação assíncrona | RabbitMQ + MassTransit | Desacoplamento e resiliência a falhas parciais |
| Isolamento de dados | Database-per-Service | Bounded Contexts sem acoplamento de schema |
| Resiliência HTTP | Polly (retry + circuit breaker) | Tolerância a falhas transitórias entre serviços |
| Saúde dos serviços | ASP.NET Health Checks | Startup ordering correto no Docker Compose |

---

## Microserviços

### IdentityService — Autenticação e Controle de Acesso
Gerencia o ciclo de vida de identidades e sessões. Implementa autenticação JWT com hashing seguro via BCrypt. O diferencial em relação a um sistema genérico: o token carrega a **flag de KYC** (Know Your Customer). Nenhum serviço de negócio aceita requisição de um cliente sem KYC verificado — exigência real de qualquer banco regulado pelo Bacen.

### ClientService — Perfil e Suitability CVM
O coração regulatório da plataforma. Gerencia o perfil de investidor seguindo as regras da **Comissão de Valores Mobiliários (CVM)**: coleta o questionário de perfil de risco, classifica o cliente (`Conservador`, `Moderado`, `Arrojado`) e publica o evento `SuitabilityCompleted`. Nenhuma ordem pode ser executada para um produto incompatível com o perfil do cliente.

### OrderService — Execução de Ordens
Núcleo de execução da plataforma. Orquestra o ciclo completo de uma ordem de investimento:

```
Criada → Validada → Enviada → Executada / Cancelada
```

Implementa o **Outbox Pattern**: o evento é gravado na mesma transação SQL que a entidade, eliminando a possibilidade de inconsistência entre banco e mensageria — mesmo que o serviço caia entre os dois passos.

### PortfolioService — Custódia e Posições
Fonte de verdade das posições dos clientes. Consome eventos `OrderExecuted` e atualiza posições de forma assíncrona. Demonstra **CQRS em sua forma mais pura**:
- **Escritas** → SQL Server via EF Core (consistência garantida)
- **Leituras** → Redis (sub-milissegundo, sem pressionar o banco transacional)

### NotificationService — Alertas em Tempo Real
Consome eventos do barramento e dispara notificações para o cliente (e-mail, webhook). Demonstra um subscriber simples e stateless — ideal para entender o padrão Pub/Sub de forma isolada.

### AuditService — Log Imutável de Compliance
Toda ação crítica (criação de cliente, execução de ordem, alteração de perfil) gera um registro append-only. **Nenhum registro pode ser editado ou deletado** — exigência real de qualquer sistema regulado pela CVM e pelo Bacen. Os repositórios de infraestrutura deste serviço não implementam os métodos `Update` e `Delete`.

---

## Stack Tecnológica

```
Backend
├── Runtime          → .NET 9 / C# 12
├── API              → ASP.NET Core Minimal APIs
├── ORM              → Entity Framework Core (Code First + Migrations)
├── Mensageria       → RabbitMQ + MassTransit (Publisher/Subscriber)
├── CQRS             → MediatR (Commands, Queries, Notifications)
├── Auth             → JWT Bearer + BCrypt.Net (KYC claim)
├── Gateway          → YARP (Yet Another Reverse Proxy)
└── Cache            → Redis (read model do PortfolioService)

Resiliência
├── Outbox Pattern   → EF Core (atomicidade banco + mensageria)
├── Polly            → Retry + Circuit Breaker em chamadas HTTP
└── Health Checks    → ASP.NET Core Health Checks por serviço

Observabilidade
├── Traces           → OpenTelemetry → Jaeger
├── Métricas         → OpenTelemetry → Prometheus → Grafana
└── Logs             → Serilog (structured logging · correlation ID)

Persistência
├── Write store      → SQL Server (instâncias isoladas por serviço)
├── Read cache       → Redis (PortfolioService)
└── Padrão           → Repository Pattern + Unit of Work

Qualidade
├── Testes           → xUnit + FluentAssertions + NSubstitute
├── Test DB          → EF Core In-Memory (testes de infraestrutura)
├── Cobertura        → Domain · Application · Infrastructure layers
└── Contract Tests   → Compatibilidade de schemas de eventos

Infraestrutura
├── Containers       → Docker + Docker Compose
├── Network          → Docker network interno entre serviços
├── Config           → Options Pattern + Environment Variables
└── CI/CD            → GitHub Actions

Cloud (Nível Introdutório)
├── Banco            → Azure SQL Database
├── Mensageria       → Azure Service Bus (substituto do RabbitMQ)
└── Deploy           → Azure Container Apps
```

---

## Fluxo de Execução de uma Ordem (Event-Driven)

```
Cliente              OrderService          RabbitMQ           PortfolioService
  │                       │                   │                      │
  │──POST /orders/────────▶│                   │                      │
  │                       │── grava Outbox ──▶ │                      │
  │◀── 202 Accepted ───────│                   │                      │
  │                       │                   │──OrderCreated ───────▶│
  │                       │                   │                      │── atualiza posição
  │                       │                   │◀── PositionUpdated ───│
  │                       │                   │                       │
  │                       │                   │──────────────────────▶ AuditService
  │                       │                   │                      (persiste log)
  │                       │                   │──────────────────────▶ NotificationService
  │                       │                   │                      (alerta ao cliente)
```

> O cliente recebe `202 Accepted` imediatamente. O portfólio é atualizado de forma assíncrona — esse é o modelo de **consistência eventual** que sistemas de alta escala adotam.

---

## Estratégia de Testes

A cobertura protege três camadas com abordagens distintas para cada uma:

**Domain Tests** — sem nenhuma dependência externa. Validam as invariantes de negócio puras:
- Uma ordem não pode ser executada para produto incompatível com o perfil do cliente
- O estado `Executed` não pode regredir para `Pending`
- Um portfólio não pode ter posição negativa em renda variável

**Application Tests** — usam `NSubstitute` para mockar repositórios e barramento. Validam que os handlers de comando chamam os repositórios corretos, publicam os eventos corretos e retornam os DTOs corretos. Sem banco, sem Docker, execução em milissegundos.

**Infrastructure Tests** — usam `EF Core In-Memory` para validar mapeamentos ORM, comportamento de queries e que o Outbox persiste o evento na mesma transação que a entidade.

**Contract Tests** — validam que o schema dos eventos publicados pelo `OrderService` é compatível com o schema esperado pelo `PortfolioService`. Garante que uma mudança em um serviço não quebra silenciosamente outro.

```bash
# Executar todos os testes
dotnet test

# Com relatório detalhado
dotnet test --logger "console;verbosity=detailed"
```

---

## Estrutura do Repositório

```
InvestCore/
├── src/
│   ├── ApiGateway/
│   │   └── ApiGateway.Api/
│   ├── IdentityService/
│   │   ├── IdentityService.Api/
│   │   ├── IdentityService.Application/
│   │   ├── IdentityService.Domain/
│   │   └── IdentityService.Infrastructure/
│   ├── ClientService/
│   │   ├── ClientService.Api/
│   │   ├── ClientService.Application/
│   │   ├── ClientService.Domain/
│   │   └── ClientService.Infrastructure/
│   ├── OrderService/
│   │   ├── OrderService.Api/
│   │   ├── OrderService.Application/
│   │   ├── OrderService.Domain/
│   │   └── OrderService.Infrastructure/
│   ├── PortfolioService/
│   │   ├── PortfolioService.Api/
│   │   ├── PortfolioService.Application/
│   │   ├── PortfolioService.Domain/
│   │   └── PortfolioService.Infrastructure/
│   ├── NotificationService/
│   │   ├── NotificationService.Api/
│   │   ├── NotificationService.Application/
│   │   └── NotificationService.Infrastructure/
│   └── AuditService/
│       ├── AuditService.Api/
│       ├── AuditService.Domain/
│       └── AuditService.Infrastructure/
├── tests/
│   ├── OrderService.Domain.Tests/
│   ├── OrderService.Application.Tests/
│   ├── PortfolioService.Infrastructure.Tests/
│   └── ContractTests/
├── infra/
│   ├── docker-compose.yml
│   ├── otel-collector.yml
│   ├── prometheus.yml
│   └── grafana/
│       └── dashboards/
├── .github/
│   └── workflows/
│       └── ci.yml
├── InvestCore.sln
└── README.md
```

---

## Getting Started

### Pré-requisitos

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (Apple Silicon para Mac M3)
- [Git](https://git-scm.com/)

> **Mac M3:** todas as imagens Docker utilizadas neste projeto possuem versão ARM64 nativa. O SQL Server usa a imagem `mcr.microsoft.com/azure-sql-edge` (ARM64 nativa) em vez da imagem padrão `mssql/server` (amd64).

### Setup em 4 comandos

```bash
# 1. Clone o repositório
git clone https://github.com/seu-usuario/InvestCore.git && cd InvestCore

# 2. Suba todo o ecossistema (build + migrations automáticas incluídas)
docker compose up -d --build

# 3. Acesse o portal de documentação unificado
open http://localhost:5050

# 4. Acesse o painel de rastreamento distribuído
open http://localhost:16686
```

### Endpoints Principais

| Serviço | URL | Descrição |
|---|---|---|
| API Gateway / Swagger Hub | http://localhost:5050 | Documentação unificada |
| IdentityService | http://localhost:5001 | Autenticação e KYC |
| ClientService | http://localhost:5002 | Perfil e suitability |
| OrderService | http://localhost:5003 | Execução de ordens |
| PortfolioService | http://localhost:5004 | Posições e custódia |
| NotificationService | http://localhost:5005 | Alertas |
| AuditService | http://localhost:5006 | Log de compliance |
| RabbitMQ Management | http://localhost:15672 | Painel do message broker |
| Jaeger UI | http://localhost:16686 | Traces distribuídos |
| Grafana | http://localhost:3000 | Métricas e dashboards |

---

## Princípios e Padrões Aplicados

| Princípio | Aplicação no Projeto |
|---|---|
| Single Responsibility | Cada microserviço possui uma única responsabilidade de domínio |
| Bounded Contexts (DDD) | Cada serviço é dono do seu schema e modelo de domínio |
| Eventual Consistency | Fluxo de execução assíncrono via eventos no barramento |
| Outbox Pattern | Eventos nunca se perdem, mesmo em falha parcial do serviço |
| CQRS | Leituras e escritas segregadas no PortfolioService (Redis + SQL) |
| Fail Fast | Resiliência configurada com Polly para falhas transitórias |
| Clean Architecture | Separação estrita entre Domain, Application e Infrastructure |
| Immutable Audit Log | Eventos de auditoria são append-only, nunca alterados |
| Observabilidade | Correlation ID em todos os traces via OpenTelemetry |

---

## Observabilidade

Cada requisição recebe um `TraceId` que atravessa todos os microserviços. No **Jaeger**, é possível visualizar o trace completo — desde a entrada no API Gateway até o registro no AuditService, passando por todos os eventos publicados no barramento.

```
Jaeger UI     → http://localhost:16686   (traces distribuídos)
Grafana       → http://localhost:3000    (métricas e dashboards)
Prometheus    → http://localhost:9090    (coleta de métricas)
```

Todos os logs são estruturados com **Serilog** e incluem o `CorrelationId` para rastreamento cross-service.

---

## CI/CD com GitHub Actions

O pipeline executa automaticamente a cada push:

```yaml
# .github/workflows/ci.yml
# 1. Restore de dependências
# 2. Build da solution completa
# 3. Execução de todos os testes
# 4. (opcional) Deploy para Azure Container Apps
```

---

## Ambiente de Desenvolvimento Recomendado

| Ferramenta | Versão | Observação |
|---|---|---|
| macOS | Apple Silicon (M3) | ARM64 nativo para toda a stack |
| .NET SDK | 9.0 | `brew install dotnet` |
| Docker Desktop | 4.x+ | Versão Apple Silicon |
| VS Code | Latest | Extensão C# Dev Kit |
| Rider | 2024.x | Alternativa ao VS Code |
| Git | Latest | `brew install git` |

---

## Conceitos de Negócio Financeiro

| Conceito | Implementação |
|---|---|
| **KYC** (Know Your Customer) | Flag obrigatória no token JWT — sem ela, nenhuma operação é autorizada |
| **Suitability CVM** | Cliente conservador não pode comprar renda variável — validado no OrderService |
| **Custódia** | PortfolioService é a fonte de verdade das posições — Redis para leitura rápida |
| **Compliance** | AuditService nunca permite `Update` ou `Delete` — exigência regulatória real |

---

## Roadmap

- [x] Arquitetura base com Docker Compose
- [x] IdentityService com KYC claim
- [x] ClientService com suitability CVM
- [x] OrderService com Outbox Pattern
- [x] PortfolioService com CQRS + Redis
- [x] AuditService append-only
- [x] NotificationService Pub/Sub
- [x] API Gateway com YARP
- [x] Observabilidade com OpenTelemetry + Jaeger + Grafana
- [x] Testes de domínio, aplicação e infraestrutura
- [x] Contract Tests para eventos do barramento
- [ ] Deploy no Azure Container Apps
- [ ] Azure SQL Database em produção
- [ ] Azure Service Bus (substituto do RabbitMQ em cloud)
- [ ] GitHub Actions com deploy automatizado

---

## Licença

Este projeto está licenciado sob a [MIT License](LICENSE).

---

<div align="center">
  <sub>Desenvolvido para fins educacionais · Inspirado em arquiteturas reais do mercado financeiro brasileiro</sub>
</div>
