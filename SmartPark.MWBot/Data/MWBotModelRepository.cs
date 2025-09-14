using Microsoft.EntityFrameworkCore;
using SmartPark.MWBot.Models;


namespace SmartPark.MWBot.Data
{
    // Repository per l'entità MWBotModel (stato/config del robot di ricarica).
    // In questa soluzione è previsto un SOLO bot
    public class MWBotModelRepository : IMWBotModelRepository
    {
        private readonly ApplicationDbContext _db;
        public MWBotModelRepository(ApplicationDbContext db) => _db = db;

        // Dettaglio per Id (tracking abilitato: utile se poi si aggiorna l'entità).
        public Task<MWBotModel?> GetByIdAsync(int id)
            => _db.MWBots.FirstOrDefaultAsync(b => b.Id == id);

        // Abbiamo un solo bot: ritorna il primo (o null se non esiste).
        // Comodo nelle pagine Admin/MWBot per mostrare e aggiornare lo stato corrente.
        public Task<MWBotModel?> GetSingletonAsync()
            => _db.MWBots.FirstOrDefaultAsync();

        // Elenco dei bot (read-only). In pratica avrà 0 o 1 elementi nell'app.
        public Task<List<MWBotModel>> ListAsync()
            => _db.MWBots.AsNoTracking().OrderBy(b => b.Id).ToListAsync();

        // CRUD basilari: Add/Update/Remove delegano al DbContext.
        public Task AddAsync(MWBotModel entity) { _db.MWBots.Add(entity); return Task.CompletedTask; }
        public void Update(MWBotModel entity) => _db.MWBots.Update(entity);
        public void Remove(MWBotModel entity) => _db.MWBots.Remove(entity);
        public Task<int> SaveChangesAsync() => _db.SaveChangesAsync();
    }
}
