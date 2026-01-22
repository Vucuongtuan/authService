using authModule.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;


//https://learn.microsoft.com/vi-vn/aspnet/identity/overview/getting-started/

namespace authModule.DataContext;

public class ApplicationDbContext : IdentityDbContext<User, IdentityRole<Guid>, Guid>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<Client> Clients { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    public DbSet<AuthorizationCode> AuthorizationCodes { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
    }
}