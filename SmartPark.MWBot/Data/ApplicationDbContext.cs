using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SmartPark.MWBot.Models;

namespace SmartPark.MWBot.Data
{
    // DbContext dell’applicazione.
    // Eredita da IdentityDbContext<ApplicationUser> per includere automaticamente le tabelle Identity
    // (AspNetUsers, AspNetRoles, ecc.) e usare ApplicationUser come utente.
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        // SET di entità (tabelle)
        // Posti auto con stato di occupazione.
        public DbSet<ParkingSpot> ParkingSpots => Set<ParkingSpot>();
        // Catalogo modelli auto (marca/modello/capacità batteria).
        public DbSet<CarModel> CarModels => Set<CarModel>();
        // Auto registrate dagli utenti (targa + riferimento al CarModel).
        public DbSet<Car> Cars => Set<Car>();
        // Sessioni di sosta (aperta/chiusa) legate a utente, auto e posto.
        public DbSet<ParkingSession> ParkingSessions => Set<ParkingSession>();
        // Richieste di ricarica (Proposed/Pending/InProgress/Completed/Cancelled).
        public DbSet<ChargeRequest> ChargeRequests => Set<ChargeRequest>();
        // Job di ricarica per la coda del MWBot (Queued/Running/Finished/Aborted).
        public DbSet<ChargeJob> ChargeJobs => Set<ChargeJob>();
        // Stato del MWBot (singolo record nel sistema).
        public DbSet<MWBotModel> MWBots => Set<MWBotModel>();
        // Tariffe (parking €/h, energy €/kWh) con data di inizio validità.
        public DbSet<Tariff> Tariffs => Set<Tariff>();
        // Pagamenti testa (totale, utente, sessione).
        public DbSet<Payment> Payments => Set<Payment>();
        // Righe pagamento (sosta/ricarica) collegate a Payment.
        public DbSet<PaymentLine> PaymentLines => Set<PaymentLine>();

        // CONFIGURAZIONE MODELLO + SEED DATI STATICI        
        protected override void OnModelCreating(ModelBuilder builder)
        {
            // OnModelCreating per lasciare a Identity il tempo di configurare le proprie entità/mapping/tabella.
            base.OnModelCreating(builder);

            // Seed posti auto (20 fissi)
            var spots = new List<ParkingSpot>();
            for (int i = 1; i <= 20; i++)
            {
                spots.Add(new ParkingSpot
                {
                    Id = i, // Id fissi
                    Code = $"P{i:D2}",
                    IsOccupied = false,
                    SensorLastUpdateUtc = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) // valore statico
                });
            }
            builder.Entity<ParkingSpot>().HasData(spots);

            // -------------------------
            // Tariffa base (seed)
            // -------------------------
            builder.Entity<Tariff>().HasData(new Tariff
            {
                Id = 1,
                ParkingPerHour = 2.0,
                EnergyPerKWh = 0.4,
                ValidFromUtc = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            });

            // -------------------------
            // Stato iniziale MWBot (seed)
            // -------------------------
            builder.Entity<MWBotModel>().HasData(new MWBotModel
            {
                Id = 1,
                BatteryPercent = 100,
                MaxPowerKW = 22,
                IsBusy = false,
                LastUpdateUtc = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            });

            // -------------------------
            // Catalogo modelli auto (seed)
            // -------------------------
            // Id fissi per semplificare riferimenti e test.
            builder.Entity<CarModel>().HasData(
            new CarModel { Id = 1, Make = "Tesla", Model = "Model 3", BatteryCapacityKWh = 57.5 },
            new CarModel { Id = 2, Make = "VW", Model = "ID.3", BatteryCapacityKWh = 58 },
            new CarModel { Id = 3, Make = "Nissan", Model = "Leaf", BatteryCapacityKWh = 40 },
            new CarModel { Id = 4, Make = "Renault", Model = "Zoe", BatteryCapacityKWh = 52 },
            new CarModel { Id = 5, Make = "Fiat", Model = "500e", BatteryCapacityKWh = 42 }
            );

            
        }

    }
}
