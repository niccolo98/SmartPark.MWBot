using Microsoft.EntityFrameworkCore;
using SmartPark.MWBot.Models;


namespace SmartPark.MWBot.Data
{
    // Repository per l'entità ParkingSpot (posti auto).
    public class ParkingSpotRepository : IParkingSpotRepository
    {
        private readonly ApplicationDbContext _db;
        public ParkingSpotRepository(ApplicationDbContext db) => _db = db;

        // Recupera un posto per Id usando FindAsync.
        public Task<ParkingSpot?> GetByIdAsync(int id) => _db.ParkingSpots.FindAsync(id).AsTask();

        // Recupera un posto per codice (es. "P01").
        public Task<ParkingSpot?> GetByCodeAsync(string code)
            => _db.ParkingSpots.FirstOrDefaultAsync(p => p.Code == code);

        // Elenco completo dei posti in sola lettura.
        public Task<List<ParkingSpot>> ListAsync()
            => _db.ParkingSpots.AsNoTracking().ToListAsync();

        // Inserimento
        public Task AddAsync(ParkingSpot entity) { _db.ParkingSpots.Add(entity); return Task.CompletedTask; }

        // Aggiornamento
        public void Update(ParkingSpot entity) => _db.ParkingSpots.Update(entity);

        // Cancellazione
        public void Remove(ParkingSpot entity) => _db.ParkingSpots.Remove(entity);

        // Commit delle modifiche pendenti sul DbContext (insert/update/delete).
        public Task<int> SaveChangesAsync() => _db.SaveChangesAsync();
    }
}
