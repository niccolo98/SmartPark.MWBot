

namespace SmartPark.MWBot.Models
{
    // Contratto repository per l'entità Car (auto registrate dagli utenti).
    // Incapsula operazioni di accesso e modifica, inclusi metodi di query frequenti.
    public interface ICarRepository
    {
        // Dettaglio per Id (può includere CarModel nella repo concreta).
        Task<Car?> GetByIdAsync(int id);

        // Ricerca per targa. Suggerita normalizzazione (ToUpperInvariant) lato chiamante.
        Task<Car?> GetByPlateAsync(string plate);

        // Elenco completo (read-only).
        Task<List<Car>> ListAsync();

        // Elenco delle auto di uno specifico utente (read-only).
        Task<List<Car>> ListByUserAsync(string userId);

        // Inserimento: aggiunge al change tracker (persistenza con SaveChangesAsync).
        Task AddAsync(Car entity);

        // Aggiornamento: marca l'entità come Modified.
        void Update(Car entity);

        // Cancellazione: marca l'entità per la rimozione.
        void Remove(Car entity);

        // Commit delle modifiche pendenti.
        Task<int> SaveChangesAsync();
    }
}
