using Microsoft.EntityFrameworkCore;
using MTDS.Api.Infrastructure.Entities;

namespace MTDS.Api.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }


    public DbSet<User> Users { get; set; }
    public DbSet<TransportEntity> Transports { get; set; }
    public DbSet<TransportAssigment> TransportAssigments { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }
    public DbSet<RouteEntity> Routes { get; set; }
    public DbSet<Stop> Stops { get; set; }
    public DbSet<RouteStop> RouteStops { get; set; }
}