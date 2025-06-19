using Microsoft.EntityFrameworkCore;
using navajo_dotnet.Domain;

namespace navajo_dotnet.Data;

public class AppDbContext : DbContext
{
    public DbSet<Secret> Secrets { get; set; }
    
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
}