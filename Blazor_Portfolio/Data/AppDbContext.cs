using Microsoft.EntityFrameworkCore;

namespace Blazor_Portfolio.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<AppUser> Users => Set<AppUser>();

    public DbSet<ContactMessage> ContactMessages => Set<ContactMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var user = modelBuilder.Entity<AppUser>();

        user.ToTable("Users");
        user.HasKey(u => u.Id);
        user.Property(u => u.Email).HasMaxLength(256).IsRequired();
        user.Property(u => u.DisplayName).HasMaxLength(128).IsRequired();
        user.Property(u => u.PasswordHash).HasMaxLength(512).IsRequired();
        user.Property(u => u.IsActive).HasDefaultValue(true);
        user.Property(u => u.CreatedUtc).IsRequired();

        // Login looks users up by email, so the index doubles as the lookup path.
        user.HasIndex(u => u.Email).IsUnique();

        var message = modelBuilder.Entity<ContactMessage>();

        message.ToTable("ContactMessages");
        message.HasKey(m => m.Id);
        message.Property(m => m.Name).HasMaxLength(120).IsRequired();
        message.Property(m => m.Email).HasMaxLength(256).IsRequired();
        message.Property(m => m.Company).HasMaxLength(160);
        message.Property(m => m.Message).HasMaxLength(4000).IsRequired();
        message.Property(m => m.CreatedUtc).IsRequired();
        message.Property(m => m.IsHandled).HasDefaultValue(false);
        message.Property(m => m.IpAddress).HasMaxLength(45);

        // Triaging the inbox means "newest first, unhandled first".
        message.HasIndex(m => new { m.IsHandled, m.CreatedUtc });
    }
}