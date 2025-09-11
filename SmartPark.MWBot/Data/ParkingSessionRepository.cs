using Microsoft.EntityFrameworkCore;
using SmartPark.MWBot.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SmartPark.MWBot.Data
{
    // Repository per l'entità ParkingSession (sessioni di sosta).
    public class ParkingSessionRepository : IParkingSessionRepository
    {
        private readonly ApplicationDbContext _db;
        public ParkingSessionRepository(ApplicationDbContext db) => _db = db;

        // Dettaglio sessione per Id, con include del posto e dell'auto (e relativo modello).
        public Task<ParkingSession?> GetByIdAsync(int id)
            => _db.ParkingSessions
                  .Include(s => s.ParkingSpot)
                  .Include(s => s.Car).ThenInclude(c => c.CarModel)
                  .FirstOrDefaultAsync(s => s.Id == id);

        // Restituisce l'eventuale sessione aperta su un dato posto (se presente).
        // Include il posto e l'auto per mostrare info in UI.
        public Task<ParkingSession?> GetOpenBySpotAsync(int spotId)
            => _db.ParkingSessions
                  .Include(s => s.ParkingSpot)
                  .Include(s => s.Car)
                  .FirstOrDefaultAsync(s => s.ParkingSpotId == spotId && s.Status == ParkingSessionStatus.Open);

        // Elenco di tutte le sessioni (read-only), ordinate per data di inizio decrescente,
        // con include di posto e auto+modello per ridurre roundtrip in UI.
        public Task<List<ParkingSession>> ListAsync()
            => _db.ParkingSessions
                  .AsNoTracking()
                  .Include(s => s.ParkingSpot)
                  .Include(s => s.Car).ThenInclude(c => c.CarModel)
                  .OrderByDescending(s => s.StartUtc)
                  .ToListAsync();

        // Elenco delle sessioni APERTE di un determinato utente (read-only),
        // utile per la pagina "Le mie sessioni".
        public Task<List<ParkingSession>> ListOpenByUserAsync(string userId)
            => _db.ParkingSessions
                  .AsNoTracking()
                  .Include(s => s.ParkingSpot)
                  .Include(s => s.Car).ThenInclude(c => c.CarModel)
                  .Where(s => s.UserId == userId && s.Status == ParkingSessionStatus.Open)
                  .OrderByDescending(s => s.StartUtc)
                  .ToListAsync();

        // CRUD: inserimento
        public Task AddAsync(ParkingSession entity) { _db.ParkingSessions.Add(entity); return Task.CompletedTask; }
        // CRUD: aggiornamento 
        public void Update(ParkingSession entity) => _db.ParkingSessions.Update(entity);
        // CRUD: rimozione 
        public void Remove(ParkingSession entity) => _db.ParkingSessions.Remove(entity);
        // Commit delle modifiche pendenti.
        public Task<int> SaveChangesAsync() => _db.SaveChangesAsync();

        // Verifica se esiste almeno UNA sessione APERTA per la specifica auto.
        public Task<bool> AnyByCarAsync(int carId)
             => _db.ParkingSessions.AnyAsync(s => s.CarId == carId && s.Status == ParkingSessionStatus.Open);
    }
}
