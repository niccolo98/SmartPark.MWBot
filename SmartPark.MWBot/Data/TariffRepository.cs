using Microsoft.EntityFrameworkCore;
using SmartPark.MWBot.Models;


namespace SmartPark.MWBot.Data
{
    // Repository per la gestione delle tariffe.
    // Espone:
    //  - GetCurrentAsync(utcNow): seleziona la tariffa "in vigore" in un dato istante UTC
    //  - ListAsync(): elenco cronologico decrescente delle tariffe
    //  - CRUD basilari (Add/Update/Remove + SaveChangesAsync)
    //
    //  - Se esistono più tariffe che coprono lo stesso istante, si prende quella con ValidFromUtc più recente.
    public class TariffRepository : ITariffRepository
    {
        private readonly ApplicationDbContext _db;
        public TariffRepository(ApplicationDbContext db) => _db = db;

        // Recupera una tariffa per Id.
        public Task<Tariff?> GetByIdAsync(int id) => _db.Tariffs.FindAsync(id).AsTask();

        // Ritorna la tariffa "corrente" per l'istante passato (utcNow).
        // Criteri: ValidFromUtc <= now && (ValidToUtc assente oppure >= now).
        // In caso di sovrapposizioni, sceglie quella con ValidFromUtc più recente.
        public Task<Tariff?> GetCurrentAsync(DateTime utcNow)
            => _db.Tariffs
                .Where(t => t.ValidFromUtc <= utcNow && (t.ValidToUtc == null || t.ValidToUtc >= utcNow))
                .OrderByDescending(t => t.ValidFromUtc)
                .FirstOrDefaultAsync();

        // Elenco read-only di tutte le tariffe, ordinate dalla più recente alla più vecchia.
        public Task<List<Tariff>> ListAsync()
            => _db.Tariffs.AsNoTracking().OrderByDescending(t => t.ValidFromUtc).ToListAsync();

        // CRUD: inserimento 
        public Task AddAsync(Tariff entity) { _db.Tariffs.Add(entity); return Task.CompletedTask; }

        // CRUD: aggiornamento 
        public void Update(Tariff entity) => _db.Tariffs.Update(entity);

        // CRUD: rimozione
        public void Remove(Tariff entity) => _db.Tariffs.Remove(entity);

        // Commit delle modifiche pendenti nel DbContext.
        public Task<int> SaveChangesAsync() => _db.SaveChangesAsync();
    }
}
