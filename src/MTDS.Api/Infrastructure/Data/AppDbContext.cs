using Microsoft.EntityFrameworkCore;
using MTDS.Api.Infrastructure.Entities;

namespace MTDS.Api.Infrastructure.Data;

class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }


    public DbSet<User> Users { get; set; }
}