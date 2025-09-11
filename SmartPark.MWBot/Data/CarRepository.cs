using Microsoft.EntityFrameworkCore;
using SmartPark.MWBot.Models;


namespace SmartPark.MWBot.Data
{
    // Repository per l'entità Car (auto registrate dagli utenti).
    public class CarRepository : ICarRepository
    {
        private readonly ApplicationDbContext _db;
        public CarRepository(ApplicationDbContext db) => _db = db;

        // Recupera un'auto per Id, includendo il modello associato.
        public Task<Car?> GetByIdAsync(int id)
            => _db.Cars.Include(c => c.CarModel).FirstOrDefaultAsync(c => c.Id == id);

        // Recupera un'auto per targa, includendo il modello associato.
        public Task<Car?> GetByPlateAsync(string plate)
            => _db.Cars.Include(c => c.CarModel).FirstOrDefaultAsync(c => c.Plate == plate);

        // Elenco completo delle auto (read-only), includendo il modello.
        public Task<List<Car>> ListAsync()
            => _db.Cars.AsNoTracking().Include(c => c.CarModel).ToListAsync();

        // Elenco read-only delle auto appartenenti a uno specifico utente.
        // Include CarModel per visualizzare marca/modello senza query aggiuntive.
        public Task<List<Car>> ListByUserAsync(string userId)
            => _db.Cars.AsNoTracking().Include(c => c.CarModel).Where(c => c.UserId == userId).ToListAsync();

        // Crea una nuova auto 
        public Task AddAsync(Car entity) { _db.Cars.Add(entity); return Task.CompletedTask; }

        // Aggiorna un'auto esistente
        public void Update(Car entity) => _db.Cars.Update(entity);

        // Rimuove un'auto
        public void Remove(Car entity) => _db.Cars.Remove(entity);

        // Salva le modifiche pendenti nel DbContext (inserimenti/aggiornamenti/cancellazioni).
        public Task<int> SaveChangesAsync() => _db.SaveChangesAsync();
    }
}
