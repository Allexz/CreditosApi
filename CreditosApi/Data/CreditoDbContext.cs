using CreditosApi.Data.Configuration;
using CreditosApi.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace CreditosApi.Data;

public class CreditoDbContext : DbContext
{

    public DbSet<CreditoIntegracao> Creditos { get; set; }

    public CreditoDbContext(DbContextOptions<CreditoDbContext> options) : base(options)
    {

    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        //modelBuilder.ApplyConfigurationsFromAssembly(typeof(CreditoDbContext).Assembly);
        modelBuilder.ApplyConfiguration(new CreditoConfiguration());
        ConfigureGlobalSettings(modelBuilder);
        ConfigurePostgreSQL(modelBuilder);
    }
    public async Task<bool> CanConnectAsync()
    {
        return await Database.CanConnectAsync();
    }

    #region Configuracoes Globais e PostgreSQL
    private void ConfigureGlobalSettings(ModelBuilder modelBuilder)
    {
        foreach (var relationship in modelBuilder.Model.GetEntityTypes()
            .SelectMany(e => e.GetForeignKeys()))
        {
            relationship.DeleteBehavior = DeleteBehavior.Restrict;
        }

        foreach (var property in modelBuilder.Model.GetEntityTypes()
            .SelectMany(t => t.GetProperties())
            .Where(p => p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?)))
        {
            property.SetColumnType("decimal(18,2)");
        }

        foreach (var property in modelBuilder.Model.GetEntityTypes()
            .SelectMany(t => t.GetProperties())
            .Where(p => p.ClrType == typeof(string) && p.GetMaxLength() == null))
        {
            property.SetMaxLength(500);
        }
    }

    /// <summary>
    /// Configuracoes especificas para PostgreSQL (lowercase)
    /// </summary>
    /// <param name="modelBuilder"></param>
    private void ConfigurePostgreSQL(ModelBuilder modelBuilder)
    {
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            entity.SetTableName(entity.GetTableName()?.ToLower());

            foreach (var property in entity.GetProperties())
            {
                property.SetColumnName(property.GetColumnName().ToLower());
            }

            foreach (var key in entity.GetKeys())
            {
                key.SetName(key.GetName()?.ToLower());
            }

            foreach (var fk in entity.GetForeignKeys())
            {
                fk.SetConstraintName(fk.GetConstraintName()?.ToLower());
            }

            foreach (var index in entity.GetIndexes())
            {
                index.SetDatabaseName(index.GetDatabaseName()?.ToLower());
            }
        }
    }
    #endregion

    #region Overrides de SaveChanges para timestamps e trimming
    public override int SaveChanges()
    {
        OnBeforeSaving();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        OnBeforeSaving();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void OnBeforeSaving()
    {
        var entries = ChangeTracker.Entries();
        var now = DateTime.UtcNow;

        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                var createdAtProperty = entry.Properties.FirstOrDefault(p => p.Metadata.Name == "CreatedAt");
                if (createdAtProperty != null && createdAtProperty.Metadata.ClrType == typeof(DateTime))
                {
                    createdAtProperty.CurrentValue = now;
                }
            }

            if (entry.State == EntityState.Modified)
            {
                var updatedAtProperty = entry.Properties.FirstOrDefault(p => p.Metadata.Name == "UpdatedAt");
                if (updatedAtProperty != null && updatedAtProperty.Metadata.ClrType == typeof(DateTime))
                {
                    updatedAtProperty.CurrentValue = now;
                }
            }

            foreach (var property in entry.Properties.Where(p => p.Metadata.ClrType == typeof(string)))
            {
                if (property.CurrentValue is string value && !string.IsNullOrEmpty(value))
                {
                    property.CurrentValue = value.Trim();
                }
            }
        }
    }
    #endregion
}