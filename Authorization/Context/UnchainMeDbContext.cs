using Authorization.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Authorization.Context;

public class UnchainMeDbContext(DbContextOptions<UnchainMeDbContext> options)
    : IdentityDbContext<User>(options)
{
    public DbSet<LoginRequest> LoginRequests { get; set; }
}