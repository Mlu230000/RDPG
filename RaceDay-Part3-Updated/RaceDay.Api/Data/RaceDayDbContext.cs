using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Models;

namespace RaceDay.Api.Data
{
    public class RaceDayDbContext : DbContext
    {
        public RaceDayDbContext(DbContextOptions<RaceDayDbContext> options) : base(options)
        {
        }

        public DbSet<Role> Roles { get; set; } = null!;
        public DbSet<User> Users { get; set; } = null!;
        public DbSet<EventType> EventTypes { get; set; } = null!;
        public DbSet<Event> Events { get; set; } = null!;
        public DbSet<Category> Categories { get; set; } = null!;
        public DbSet<Route> Routes { get; set; } = null!;
        public DbSet<Enrolment> Enrolments { get; set; } = null!;
        public DbSet<Result> Results { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 1. Roles
            modelBuilder.Entity<Role>(entity =>
            {
                entity.ToTable("Roles");
                entity.HasKey(e => e.RoleId).HasName("PK_Roles");
                entity.HasIndex(e => e.RoleName).IsUnique().HasDatabaseName("UQ_Roles_Name");
            });

            // 2. Users
            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("Users");
                entity.HasKey(e => e.UserId).HasName("PK_Users");
                entity.HasIndex(e => e.Email).IsUnique().HasDatabaseName("UQ_Users_Email");

                entity.HasOne(e => e.Role)
                    .WithMany(r => r.Users)
                    .HasForeignKey(e => e.RoleId)
                    .OnDelete(DeleteBehavior.Restrict)
                    .HasConstraintName("FK_Users_Role");
            });

            // 3. EventTypes
            modelBuilder.Entity<EventType>(entity =>
            {
                entity.ToTable("EventTypes");
                entity.HasKey(e => e.EventTypeId).HasName("PK_EventTypes");
                entity.HasIndex(e => e.TypeName).IsUnique().HasDatabaseName("UQ_EventTypes_Name");
            });

            // 4. Events
            modelBuilder.Entity<Event>(entity =>
            {
                entity.ToTable("Events");
                entity.HasKey(e => e.EventId).HasName("PK_Events");

                entity.HasOne(e => e.Organiser)
                    .WithMany(u => u.OrganisedEvents)
                    .HasForeignKey(e => e.OrganiserId)
                    .OnDelete(DeleteBehavior.Restrict)
                    .HasConstraintName("FK_Events_Organiser");

                entity.HasOne(e => e.EventType)
                    .WithMany(t => t.Events)
                    .HasForeignKey(e => e.EventTypeId)
                    .OnDelete(DeleteBehavior.Restrict)
                    .HasConstraintName("FK_Events_EventType");
            });

            // 5. Categories
            modelBuilder.Entity<Category>(entity =>
            {
                entity.ToTable("Categories");
                entity.HasKey(e => e.CategoryId).HasName("PK_Categories");

                entity.HasOne(e => e.Event)
                    .WithMany(ev => ev.Categories)
                    .HasForeignKey(e => e.EventId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("FK_Categories_Event");
            });

            // 6. Routes (1:1 with Category)
            modelBuilder.Entity<Route>(entity =>
            {
                entity.ToTable("Routes");
                entity.HasKey(e => e.RouteId).HasName("PK_Routes");
                entity.HasIndex(e => e.CategoryId).IsUnique().HasDatabaseName("UQ_Routes_Category");

                entity.HasOne(e => e.Category)
                    .WithOne(c => c.Route)
                    .HasForeignKey<Route>(e => e.CategoryId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("FK_Routes_Category");
            });

            // 7. Enrolments (Unique Participant per Category)
            modelBuilder.Entity<Enrolment>(entity =>
            {
                entity.ToTable("Enrolments");
                entity.HasKey(e => e.EnrolmentId).HasName("PK_Enrolments");
                entity.HasIndex(e => new { e.ParticipantId, e.CategoryId }).IsUnique().HasDatabaseName("UQ_Enrolments_Participant_Category");

                entity.HasOne(e => e.Participant)
                    .WithMany(u => u.Enrolments)
                    .HasForeignKey(e => e.ParticipantId)
                    .OnDelete(DeleteBehavior.Restrict)
                    .HasConstraintName("FK_Enrolments_Participant");

                entity.HasOne(e => e.Category)
                    .WithMany(c => c.Enrolments)
                    .HasForeignKey(e => e.CategoryId)
                    .OnDelete(DeleteBehavior.Restrict)
                    .HasConstraintName("FK_Enrolments_Category");
            });

            // 8. Results (1:1 with Enrolment)
            modelBuilder.Entity<Result>(entity =>
            {
                entity.ToTable("Results");
                entity.HasKey(e => e.ResultId).HasName("PK_Results");
                entity.HasIndex(e => e.EnrolmentId).IsUnique().HasDatabaseName("UQ_Results_Enrolment");

                entity.HasOne(e => e.Enrolment)
                    .WithOne(enr => enr.Result)
                    .HasForeignKey<Result>(e => e.EnrolmentId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("FK_Results_Enrolment");
            });

            // Seed Initial Data (Matching raceday_schema.sql)
            SeedData(modelBuilder);
        }

        private static void SeedData(ModelBuilder modelBuilder)
        {
            // Roles
            modelBuilder.Entity<Role>().HasData(
                new Role { RoleId = 1, RoleName = "Organiser", Description = "Creates and manages events, categories and results." },
                new Role { RoleId = 2, RoleName = "Participant", Description = "Browses events, enrols in categories and views own results." }
            );

            // EventTypes
            modelBuilder.Entity<EventType>().HasData(
                new EventType { EventTypeId = 1, TypeName = "Running", Description = "Road or trail running events." },
                new EventType { EventTypeId = 2, TypeName = "Cycling", Description = "Road or mountain bike events." },
                new EventType { EventTypeId = 3, TypeName = "Walking", Description = "Walks and charity walks." }
            );

            // Seed Users with BCrypt hashed passwords
            // "password123" -> hashed
            var defaultHash = "$2a$11$N9qo8uLOickgx2ZMRZoMye.I5tZ8eQc2eA7LzLzK3XbO5i/nFjZeq";

            modelBuilder.Entity<User>().HasData(
                new User { UserId = 1, RoleId = 1, FullName = "Thandiwe Mokoena", Email = "thandiwe@raceday.co.za", PasswordHash = defaultHash, Phone = "0821234567", CreatedAt = new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc) },
                new User { UserId = 2, RoleId = 1, FullName = "Johan van der Merwe", Email = "johan@raceday.co.za", PasswordHash = defaultHash, Phone = "0837654321", CreatedAt = new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc) },
                new User { UserId = 3, RoleId = 2, FullName = "Lindiwe Dlamini", Email = "lindiwe@gmail.com", PasswordHash = defaultHash, Phone = "0721112222", CreatedAt = new DateTime(2026, 8, 5, 0, 0, 0, DateTimeKind.Utc) },
                new User { UserId = 4, RoleId = 2, FullName = "Pieter Botha", Email = "pieter@gmail.com", PasswordHash = defaultHash, Phone = "0733334444", CreatedAt = new DateTime(2026, 8, 6, 0, 0, 0, DateTimeKind.Utc) }
            );

            // Seed Events
            modelBuilder.Entity<Event>().HasData(
                new Event { EventId = 1, OrganiserId = 1, EventTypeId = 1, EventName = "Soweto Marathon", Description = "Annual road running event through the historic streets of Soweto.", EventDate = new DateTime(2026, 11, 1), StartTime = new TimeSpan(6, 0, 0), Location = "Soweto, Johannesburg", Province = "Gauteng", Status = "Open", CreatedAt = new DateTime(2026, 8, 10, 0, 0, 0, DateTimeKind.Utc) },
                new Event { EventId = 2, OrganiserId = 2, EventTypeId = 2, EventName = "Cape Town Cycle Tour", Description = "One of the world's largest timed cycling events.", EventDate = new DateTime(2027, 3, 8), StartTime = new TimeSpan(6, 30, 0), Location = "Cape Town", Province = "Western Cape", Status = "Open", CreatedAt = new DateTime(2026, 8, 12, 0, 0, 0, DateTimeKind.Utc) },
                new Event { EventId = 3, OrganiserId = 1, EventTypeId = 3, EventName = "Park Run Charity Walk", Description = "Community charity walk raising funds for local schools.", EventDate = new DateTime(2026, 9, 20), StartTime = new TimeSpan(7, 30, 0), Location = "Pretoria", Province = "Gauteng", Status = "Open", CreatedAt = new DateTime(2026, 8, 14, 0, 0, 0, DateTimeKind.Utc) }
            );

            // Seed Categories
            modelBuilder.Entity<Category>().HasData(
                new Category { CategoryId = 1, EventId = 1, CategoryName = "10km Fun Run", DistanceKm = 10.00m, MinAge = 12, EntryFee = 150.00m, MaxParticipants = 2000 },
                new Category { CategoryId = 2, EventId = 1, CategoryName = "42km Marathon", DistanceKm = 42.20m, MinAge = 18, EntryFee = 350.00m, MaxParticipants = 5000 },
                new Category { CategoryId = 3, EventId = 2, CategoryName = "109km Cycle Tour", DistanceKm = 109.00m, MinAge = 16, EntryFee = 550.00m, MaxParticipants = 15000 },
                new Category { CategoryId = 4, EventId = 2, CategoryName = "56km Cycle Tour", DistanceKm = 56.00m, MinAge = 14, EntryFee = 400.00m, MaxParticipants = 8000 },
                new Category { CategoryId = 5, EventId = 3, CategoryName = "5km Charity Walk", DistanceKm = 5.00m, EntryFee = 0.00m, MaxParticipants = 1000 }
            );

            // Seed Routes
            modelBuilder.Entity<Route>().HasData(
                new Route { RouteId = 1, CategoryId = 1, RouteName = "Soweto 10km Route", StartPoint = "FNB Stadium", EndPoint = "Orlando Stadium", ElevationGainM = 120, RouteMapUrl = "https://raceday.co.za/routes/soweto-10km" },
                new Route { RouteId = 2, CategoryId = 2, RouteName = "Soweto 42km Route", StartPoint = "FNB Stadium", EndPoint = "FNB Stadium", ElevationGainM = 410, RouteMapUrl = "https://raceday.co.za/routes/soweto-42km" },
                new Route { RouteId = 3, CategoryId = 3, RouteName = "Cape Town 109km Route", StartPoint = "Hertzog Boulevard", EndPoint = "Green Point", ElevationGainM = 980, RouteMapUrl = "https://raceday.co.za/routes/cpt-109km" },
                new Route { RouteId = 4, CategoryId = 4, RouteName = "Cape Town 56km Route", StartPoint = "Hertzog Boulevard", EndPoint = "Camps Bay", ElevationGainM = 520, RouteMapUrl = "https://raceday.co.za/routes/cpt-56km" },
                new Route { RouteId = 5, CategoryId = 5, RouteName = "Pretoria 5km Route", StartPoint = "Union Buildings", EndPoint = "Church Square", ElevationGainM = 45, RouteMapUrl = "https://raceday.co.za/routes/pta-5km" }
            );

            // Seed Enrolments
            modelBuilder.Entity<Enrolment>().HasData(
                new Enrolment { EnrolmentId = 1, ParticipantId = 3, CategoryId = 1, RaceNumber = "A1023", EnrolmentDate = new DateTime(2026, 8, 15, 0, 0, 0, DateTimeKind.Utc), PaymentStatus = "Paid" },
                new Enrolment { EnrolmentId = 2, ParticipantId = 4, CategoryId = 2, RaceNumber = "A2044", EnrolmentDate = new DateTime(2026, 8, 16, 0, 0, 0, DateTimeKind.Utc), PaymentStatus = "Paid" },
                new Enrolment { EnrolmentId = 3, ParticipantId = 3, CategoryId = 3, RaceNumber = "C3067", EnrolmentDate = new DateTime(2026, 8, 18, 0, 0, 0, DateTimeKind.Utc), PaymentStatus = "Pending" },
                new Enrolment { EnrolmentId = 4, ParticipantId = 4, CategoryId = 5, RaceNumber = "W1002", EnrolmentDate = new DateTime(2026, 8, 19, 0, 0, 0, DateTimeKind.Utc), PaymentStatus = "Paid" }
            );

            // Seed Results
            modelBuilder.Entity<Result>().HasData(
                new Result { ResultId = 1, EnrolmentId = 1, FinishTime = new TimeSpan(0, 52, 14), OverallPosition = 34, CategoryPosition = 12, Status = "Finished" },
                new Result { ResultId = 2, EnrolmentId = 2, FinishTime = new TimeSpan(4, 12, 3), OverallPosition = 210, CategoryPosition = 88, Status = "Finished" }
            );
        }
    }
}
