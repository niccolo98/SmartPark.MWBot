using Microsoft.EntityFrameworkCore;
using SmartPark.MWBot.Models;

namespace SmartPark.MWBot.Data
{
    // Repository specifico per il catalogo dei modelli auto (CarModel).
    // Operazioni CRUD e query di sola lettura.
    public class CarModelRepository : ICarModelRepository
    {
        private readonly ApplicationDbContext _db;
        public CarModelRepository(ApplicationDbContext db) => _db = db;

        // Recupera un CarModel per Id (tracking abilitato: utile se poi lo si modifica).
        public Task<CarModel?> GetByIdAsync(int id)
            => _db.CarModels.FirstOrDefaultAsync(m => m.Id == id);

        // Elenco di tutti i modelli in sola lettura, ordinati per Marca quindi Modello.
        public Task<List<CarModel>> ListAsync()
            => _db.CarModels.AsNoTracking().OrderBy(m => m.Make).ThenBy(m => m.Model).ToListAsync();

        // Inserimento.
        public Task AddAsync(CarModel entity) { _db.CarModels.Add(entity); return Task.CompletedTask; }

        // Aggiornamento.
        public void Update(CarModel entity) => _db.CarModels.Update(entity);

        // Cancellazione.
        public void Remove(CarModel entity) => _db.CarModels.Remove(entity);

        // Commit unitario delle modifiche pendenti nel DbContext.
        public Task<int> SaveChangesAsync() => _db.SaveChangesAsync();
    }
}

