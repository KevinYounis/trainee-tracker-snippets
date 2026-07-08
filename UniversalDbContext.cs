using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TraineeTracker.Web.Models;

namespace TraineeTracker.Web.Data
{
    public class UniversalDbContext : IdentityDbContext<ApplicationUser>
    {
        private readonly bool _useInMemory;

        // Standard-Konstruktor 
        public UniversalDbContext() { }

        // Konstruktor für flexible Konfiguration (aus Tutorial_Project)
        public UniversalDbContext(bool useInMemory)
        {
            _useInMemory = useInMemory;
        }

        // Standard-Konstruktor für die Web-Injektion (wichtig für die laufende Website)
        public UniversalDbContext(DbContextOptions<UniversalDbContext> options) : base(options) { }

        // Hier bitte eure DbSet-Zeilen einfügen!
        public DbSet<TraineeProfile> TraineeProfiles { get; set; } = null!;
        public DbSet<MentorProfile> MentorProfiles { get; set; } = null!;
        public DbSet<TraineeLektion> TraineeLektionen { get; set; } = null!;
        public DbSet<Lektion> Lektions { get; set; } = null!;
        public DbSet<Feedback> Feedbacks { get; set; } = null!;

        // Entscheidet, wo die Daten physisch landen (aus Tutorial_Project)
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                if (_useInMemory)
                {
                    optionsBuilder.UseInMemoryDatabase("TraineeTrackerTestDb");
                }
                else
                {
                    optionsBuilder.UseSqlite("Data Source=Data/traineetracker.db");
                }
            }
        }

        // Regeln, wie die Tabellen verknüpft sind
        // Erweitert bitte diese Methode mit euren Verknüpfungsregeln und kommentiert sie!
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // 1:1 Beziehung: ApplicationUser <-> TraineeProfile
            builder.Entity<ApplicationUser>()
                .HasOne(u => u.TraineeProfile)
                .WithOne(t => t.ApplicationUser)
                .HasForeignKey<TraineeProfile>(t => t.ApplicationUserId);

            // 1:1 Beziehung: ApplicationUser <-> MentorProfile
            builder.Entity<ApplicationUser>()
                .HasOne(u => u.MentorProfile)
                .WithOne(m => m.ApplicationUser)
                .HasForeignKey<MentorProfile>(m => m.ApplicationUserId);

            // n:m Beziehung: TraineeProfile <-> MentorProfile
            builder.Entity<TraineeProfile>()
                .HasMany(t => t.Mentoren)
                .WithMany(m => m.Trainees)
                .UsingEntity(j => j.ToTable("MentorTraineeZuweisungen"));

            // 1:n Beziehung: TraineeProfile -> Trainee_Lektionen
            builder.Entity<TraineeProfile>()
                .HasMany(t => t.Trainee_Lektionen)
                .WithOne(tl => tl.TraineeProfile)
                .HasForeignKey(tl => tl.TraineeId)
                .OnDelete(DeleteBehavior.Cascade);

            // 1:n Beziehung: Lektion <-> TraineeLektion
            builder.Entity<TraineeLektion>()
                .HasOne(tl => tl.Lektion)
                .WithMany()
                .HasForeignKey(tl => tl.LektionId);

            // 1:1 Beziehung: Feedback <-> TraineeLektion
            builder.Entity<Feedback>()
                .HasIndex(f => f.TraineeLektionId)
                .IsUnique();

            builder.Entity<Feedback>()
                .HasOne(f => f.TraineeLektion)
                .WithOne()
                .HasForeignKey<Feedback>(f => f.TraineeLektionId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
