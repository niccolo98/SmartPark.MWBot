using System.Threading.Tasks;
using System.Collections.Generic;

namespace SmartPark.MWBot.Models
{
    // Contratto repository per l'entità ParkingSession (sessioni di sosta).
    // Centralizza le query/operazioni tipiche usate dalla UI e dalla logica applicativa.
    public interface IParkingSessionRepository
    {
        // Dettaglio sessione per Id (null se non trovata).
        Task<ParkingSession?> GetByIdAsync(int id);

        // Restituisce l'eventuale sessione APERTA su uno specifico posto.
        Task<ParkingSession?> GetOpenBySpotAsync(int spotId);

        // Elenco completo delle sessioni (tipicamente read-only in admin).
        Task<List<ParkingSession>> ListAsync();

        // Elenco delle sessioni APERTE per un determinato utente (usata nella pagina "Le mie sessioni").
        Task<List<ParkingSession>> ListOpenByUserAsync(string userId);

        // Verifica se esiste almeno una sessione APERTA per la specifica auto.
        // N.B. Se si volesse bloccare p.es. l'eliminazione dell'auto anche in presenza di sessioni CHIUSE,
        // andrebbe aggiunto un ulteriore metodo o rimosso il filtro sullo stato lato implementazione.
        Task<bool> AnyByCarAsync(int carId);


        // CRUD: inserimento (persistenza con SaveChangesAsync a carico del chiamante).
        Task AddAsync(ParkingSession entity);

        // CRUD: aggiornamento (richiede entity tracciata o attaccata).
        void Update(ParkingSession entity);

        // CRUD: rimozione.
        void Remove(ParkingSession entity);

        // Commit delle modifiche pendenti nel contesto dati.
        Task<int> SaveChangesAsync();
    }
}
