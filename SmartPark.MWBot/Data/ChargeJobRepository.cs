using Microsoft.EntityFrameworkCore;
using SmartPark.MWBot.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SmartPark.MWBot.Data
{
    // Repository per i job di ricarica (coda del MWBot).
    // Query di monitoraggio (lista completa, in coda, in esecuzione)
    // e le CRUD essenziali. Quando serve, include la ChargeRequest e la ParkingSession
    // per mostrare rapidamente le info correlate in UI (Admin/MWBot).
    public class ChargeJobRepository : IChargeJobRepository
    {
        private readonly ApplicationDbContext _db;
        public ChargeJobRepository(ApplicationDbContext db) => _db = db;

        // Restituisce un job per Id, includendo la richiesta e la sessione:
        // utile nelle pagine dove occorrono i dettagli (posto, auto, ecc.).
        public Task<ChargeJob?> GetByIdAsync(int id)
            => _db.ChargeJobs
                  .Include(j => j.ChargeRequest)
                    .ThenInclude(r => r.ParkingSession)
                  .FirstOrDefaultAsync(j => j.Id == id);

        // Lista job (read-only) con richiesta+sessione, ordinata dal più recente avviato.
        public Task<List<ChargeJob>> ListAsync()
            => _db.ChargeJobs
                  .AsNoTracking()
                  .Include(j => j.ChargeRequest)
                    .ThenInclude(r => r.ParkingSession)
                  .OrderByDescending(j => j.StartUtc)
                  .ThenByDescending(j => j.Id)
                  .ToListAsync();

        // Tutti i job associati a una specifica richiesta (read-only).
        public Task<List<ChargeJob>> ListByRequestAsync(int chargeRequestId)
            => _db.ChargeJobs
                  .AsNoTracking()
                  .Where(j => j.ChargeRequestId == chargeRequestId)
                  .OrderBy(j => j.Id)
                  .ToListAsync();

        // Tutti i job in coda (Queued), lettura ottimizzata.
        public Task<List<ChargeJob>> ListQueuedAsync()
            => _db.ChargeJobs
                  .AsNoTracking()
                  .Where(j => j.Status == ChargeJobStatus.Queued)
                  .OrderBy(j => j.Id)
                  .ToListAsync();

        // Prossimo job da avviare:
        // - Ordina per data richiesta della ChargeRequest (FIFO "per richiesta"),
        //   poi per Id come tie-breaker.
        // - Include la richiesta per avere subito gli agganci in pagina admin.
        public Task<ChargeJob?> NextQueuedAsync()
            => _db.ChargeJobs
                  .Include(j => j.ChargeRequest)
                  .Where(j => j.Status == ChargeJobStatus.Queued)
                  .OrderBy(j => j.ChargeRequest!.RequestedAtUtc)  // priorità: prima richiesta
                  .ThenBy(j => j.Id)
                  .FirstOrDefaultAsync();

        // Restituisce l’eventuale job attualmente in esecuzione (Running),
        // preferendo quello più recente in caso di anomalie.
        public Task<ChargeJob?> GetRunningAsync()
            => _db.ChargeJobs
                  .Include(j => j.ChargeRequest)
                  .Where(j => j.Status == ChargeJobStatus.Running)
                  .OrderByDescending(j => j.StartUtc)
                  .FirstOrDefaultAsync();

        // CRUD basilari: Add/Update/Remove + SaveChangesAsync delegato al DbContext.
        public Task AddAsync(ChargeJob entity) { _db.ChargeJobs.Add(entity); return Task.CompletedTask; }
        public void Update(ChargeJob entity) => _db.ChargeJobs.Update(entity);
        public void Remove(ChargeJob entity) => _db.ChargeJobs.Remove(entity);
        public Task<int> SaveChangesAsync() => _db.SaveChangesAsync();
    }
}
