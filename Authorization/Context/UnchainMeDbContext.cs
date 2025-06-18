using Authorization.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Authorization.Context;

public class UnchainMeDbContext(DbContextOptions<UnchainMeDbContext> options)
    : IdentityDbContext<User>(options)
{
    public DbSet<LoginRequest> LoginRequests { get; set; }
    
    public DbSet<Company> Companies { get; set; }
    public DbSet<CompanyUser> CompanyUsers { get; set; }
    public DbSet<Invitation> Invitations { get; set; }
    public DbSet<TaskItem> Tasks { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    public DbSet<Application> Applications { get; set; }
    
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<CompanyUser>()
            .HasKey(cu => new { cu.CompanyId, cu.UserId });

        builder.Entity<CompanyUser>()
            .HasOne(cu => cu.Company)
            .WithMany(c => c.CompanyUsers)
            .HasForeignKey(cu => cu.CompanyId);

        builder.Entity<CompanyUser>()
            .HasOne(cu => cu.User)
            .WithMany()
            .HasForeignKey(cu => cu.UserId);

        builder.Entity<Invitation>()
            .HasOne(i => i.Company)
            .WithMany(c => c.Invitations)
            .HasForeignKey(i => i.CompanyId);

        builder.Entity<TaskItem>()
            .HasOne(t => t.Company)
            .WithMany(c => c.Tasks)
            .HasForeignKey(t => t.CompanyId);

        builder.Entity<TaskItem>()
            .HasOne(t => t.CreatedBy)
            .WithMany()
            .HasForeignKey(t => t.CreatedById)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<TaskItem>()
            .HasOne(t => t.AssignedTo)
            .WithMany()
            .HasForeignKey(t => t.AssignedToId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}