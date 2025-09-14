

namespace SmartPark.MWBot.Models
{
    // Contratto repository per l'entità ChargeRequest (richieste di ricarica).
    // Fornisce operazioni di lettura/filtraggio e CRUD basilari.
    // Nota: la repo concreta include spesso la ParkingSession/Car tramite Include dove serve.
    public interface IChargeRequestRepository
    {
        // Dettaglio richiesta per Id (o null se non trovata).
        Task<ChargeRequest?> GetByIdAsync(int id);

        // Elenco completo (tipicamente read-only).
        Task<List<ChargeRequest>> ListAsync();

        // Richieste in stato "Pending" (quelle accettate dall'utente e in attesa di job/avvio).
        Task<List<ChargeRequest>> ListPendingAsync();

        // Richieste legate a una specifica sessione (utile per blocco duplicati e report).
        Task<List<ChargeRequest>> ListBySessionAsync(int sessionId);

        // CRUD: inserimento (persistenza con SaveChangesAsync lato chiamante).
        Task AddAsync(ChargeRequest entity);

        // CRUD: aggiornamento (richiede entity tracciata o attaccata).
        void Update(ChargeRequest entity);

        // CRUD: rimozione.
        void Remove(ChargeRequest entity);

        // Commit modifiche pendenti sul contesto dati.
        Task<int> SaveChangesAsync();
    }
}
