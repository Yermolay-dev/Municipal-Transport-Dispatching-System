using Microsoft.EntityFrameworkCore;
using MTDS.Api.Infrastructure.Entities;

namespace MTDS.Api.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        var dispatcherId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        modelBuilder.Entity<User>().HasData(new User
        {
            Id = dispatcherId,
            Role = "Dispatcher",
            Name = "Ермолай",
            SurName = "Лукинов",
            Email = "admin@mtds.ru",
            PasswordHash = "$2a$11$E9P.h40f51W37E2U22C.7uC4.vFv2.R/f4PjC2j78K1hG8E/w0C7a",
            CreatedAt = new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc),
            LastEntranceAt = new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc)
        });

        modelBuilder.Entity<TransportEntity>().HasData(new TransportEntity
        {
            Id = 105,
            NumberPlate = "A154AA150",
            Capacity = 50,
            Status = "in_garage",
            Type = "bus"
        });

        modelBuilder.Entity<RouteEntity>().HasData(new RouteEntity
        {
            Id = 1,
            PathNumber = "49a",
            TransportType = "bus",
            RouteLineJson = "[[55.4311, 37.5458], [55.4280, 37.5490], [55.4200, 37.5500]]"
        });

        modelBuilder.Entity<Stop>().HasData(
            new Stop { Id = 1, Name = "Автостанция Подольск", Lat = 55.4311, Lon = 37.5458 },
            new Stop { Id = 2, Name = "Площадь Ленина", Lat = 55.4280, Lon = 37.5490 },
            new Stop { Id = 3, Name = "Рынок", Lat = 55.4200, Lon = 37.5500 }
        );

        modelBuilder.Entity<RouteStop>().HasData(
            new RouteStop { Id = 1, RouteId = 1, StopId = 1, StopOrder = 1, MinutesFromStart = 0 },
            new RouteStop { Id = 2, RouteId = 1, StopId = 2, StopOrder = 2, MinutesFromStart = 5 },
            new RouteStop { Id = 3, RouteId = 1, StopId = 3, StopOrder = 3, MinutesFromStart = 17 }
        );
    }


    public DbSet<User> Users { get; set; }
    public DbSet<TransportEntity> Transports { get; set; }
    public DbSet<TransportAssigment> TransportAssigments { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }
    public DbSet<RouteEntity> Routes { get; set; }
    public DbSet<Stop> Stops { get; set; }
    public DbSet<RouteStop> RouteStops { get; set; }
}