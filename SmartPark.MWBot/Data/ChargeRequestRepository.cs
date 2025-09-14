using Microsoft.EntityFrameworkCore;
using SmartPark.MWBot.Models;


namespace SmartPark.MWBot.Data
{
    // Repository per le ChargeRequest (richieste di ricarica).
    public class ChargeRequestRepository : IChargeRequestRepository
    {
        private readonly ApplicationDbContext _db;
        public ChargeRequestRepository(ApplicationDbContext db) => _db = db;

        // Dettaglio richiesta per Id, includendo:
        // - ParkingSession (navigazione)
        // - Car (dalla sessione) per visualizzare targa/modello nelle view.
        public Task<ChargeRequest?> GetByIdAsync(int id)
            => _db.ChargeRequests
                  .Include(r => r.ParkingSession)
                  .ThenInclude(s => s.Car)
                  .FirstOrDefaultAsync(r => r.Id == id);

        // Elenco richieste in sola lettura, ordinate per data richiesta (cronologico),
        // includendo la sessione e l'auto per renderle in UI senza query aggiuntive.
        public Task<List<ChargeRequest>> ListAsync()
            => _db.ChargeRequests
                  .AsNoTracking()
                  .Include(r => r.ParkingSession).ThenInclude(s => s.Car)
                  .OrderBy(r => r.RequestedAtUtc)
                  .ToListAsync();

        // Tutte le richieste ancora "attive" in attesa di accodamento/avvio (Pending) in sola lettura.
        public Task<List<ChargeRequest>> ListPendingAsync()
            => _db.ChargeRequests
                  .AsNoTracking()
                  .Where(r => r.Status == ChargeRequestStatus.Pending)
                  .OrderBy(r => r.RequestedAtUtc)
                  .ToListAsync();

        // Richieste per una specifica sessione (read-only), ordinate cronologicamente.
        // Utile per:
        // - bloccare richieste duplicate (Proposed/Pending/InProgress)
        // - sommare kWh dei job collegati alla sessione
        public Task<List<ChargeRequest>> ListBySessionAsync(int sessionId) => _db.ChargeRequests
          .AsNoTracking()
          .Where(r => r.ParkingSessionId == sessionId)
          .OrderBy(r => r.RequestedAtUtc)
          .ToListAsync();

        // CRUD basilari: Add/Update/Remove + SaveChangesAsync delegato al DbContext.
        public Task AddAsync(ChargeRequest entity) { _db.ChargeRequests.Add(entity); return Task.CompletedTask; }
        public void Update(ChargeRequest entity) => _db.ChargeRequests.Update(entity);
        public void Remove(ChargeRequest entity) => _db.ChargeRequests.Remove(entity);
        public Task<int> SaveChangesAsync() => _db.SaveChangesAsync();
    }
}
