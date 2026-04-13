using Microsoft.EntityFrameworkCore;
using AuditService.Domain.Entities;

namespace AuditService.Infrastructure.Data;
public class AuditContext : DbContext
{
    public AuditContext(DbContextOptions<AuditContext> options) : base(options) { }

    public DbSet<AuditLog> AuditLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.EventName).IsRequired();
            entity.Property(e => e.EventData).IsRequired();
        });
    }
}