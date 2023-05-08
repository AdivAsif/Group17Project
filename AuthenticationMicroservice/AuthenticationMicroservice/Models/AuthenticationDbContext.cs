namespace AuthenticationMicroservice.Models;

using System.ComponentModel.DataAnnotations.Schema;
using BaseObjects;
using Entities;
using Microsoft.EntityFrameworkCore;

public class AuthenticationDbContext : DbContext
{
    public static string ConnectionStringName = "DbConnectionString";

    public AuthenticationDbContext(DbContextOptions<AuthenticationDbContext> options) : base(options)
    {
    }

    public DbSet<User> User { get; set; } = null!;
    public DbSet<RefreshToken> RefreshToken { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>().HasIndex(u => u.EmailAddress).IsUnique();

        modelBuilder.Entity<RefreshToken>().HasIndex(r => r.UserId);
        modelBuilder.Entity<RefreshToken>().HasIndex(r => new {r.CreatedAt, r.Deleted});

        foreach (var relationship in modelBuilder.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys()))
            relationship.DeleteBehavior = DeleteBehavior.Restrict;

        foreach (var entity in modelBuilder.Model.GetEntityTypes().Where(t =>
                     t.ClrType.GetProperties().Any(p =>
                         p.CustomAttributes.Any(a => a.AttributeType == typeof(DatabaseGeneratedAttribute)))))
        {
            foreach (var property in entity.ClrType.GetProperties().Where(p =>
                         p.PropertyType == typeof(Guid) && p.CustomAttributes.Any(a =>
                             a.AttributeType == typeof(DatabaseGeneratedAttribute))))
                modelBuilder.Entity(entity.ClrType).Property(property.Name).HasDefaultValueSql("newid()");
            foreach (var property in entity.ClrType.GetProperties().Where(p =>
                         p.PropertyType == typeof(DateTimeOffset) &&
                         p.CustomAttributes.Any(a => a.AttributeType == typeof(DatabaseGeneratedAttribute))))
                modelBuilder.Entity(entity.ClrType).Property(property.Name).HasDefaultValueSql("SYSDATETIMEOFFSET()");
        }
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        UpdateTimestamps();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override int SaveChanges()
    {
        UpdateTimestamps();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        UpdateTimestamps();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void UpdateTimestamps()
    {
        var trackableModified = ChangeTracker.Entries()
            .Where(t => t.State == EntityState.Modified && t.Entity is ITrackable)
            .Select(t => t.Entity)
            .ToList();

        foreach (var t in trackableModified)
            if (t is ITrackable trackable)
                trackable.UpdatedAt = DateTimeOffset.UtcNow;
    }
}