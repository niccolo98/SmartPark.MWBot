

namespace SmartPark.MWBot.Models
{
    // Contratto repository per i job di ricarica (ChargeJob), cioè gli elementi della coda MWBot.
    // Espone query tipiche per monitoraggio e orchestrazione, oltre a CRUD basilari.
    public interface IChargeJobRepository
    {
        // Dettaglio job per Id.
        Task<ChargeJob?> GetByIdAsync(int id);

        // Elenco completo dei job (tipicamente read-only in UI admin).
        Task<List<ChargeJob>> ListAsync();

        // Elenco dei job legati a una specifica ChargeRequest.
        Task<List<ChargeJob>> ListByRequestAsync(int chargeRequestId);

        // Elenco dei job attualmente in coda (stato Queued).
        Task<List<ChargeJob>> ListQueuedAsync();

        // Prossimo job da avviare secondo la policy (es. FIFO).
        Task<ChargeJob?> NextQueuedAsync();

        // Job attualmente in esecuzione (se presente).
        Task<ChargeJob?> GetRunningAsync();

        // CRUD: inserimento (persistenza con SaveChangesAsync).
        Task AddAsync(ChargeJob entity);

        // CRUD: aggiornamento (richiede entity tracciata/attaccata).
        void Update(ChargeJob entity);

        // CRUD: rimozione.
        void Remove(ChargeJob entity);

        // Commit delle modifiche pendenti nel contesto dati.
        Task<int> SaveChangesAsync();
    }
}
