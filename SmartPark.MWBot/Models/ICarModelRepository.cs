using System.Collections.Generic;
using System.Threading.Tasks;

namespace SmartPark.MWBot.Models
{
    // Repository contract per l'entità CarModel (catalogo dei modelli auto).
    // Scopo: incapsulare l'accesso ai dati (EF Core nel progetto concreto) e
    // offrire metodi chiari per le operazioni tipiche.
    public interface ICarModelRepository
    {
        // Restituisce un CarModel per chiave primaria (o null se non trovato).
        Task<CarModel?> GetByIdAsync(int id);

        // Elenca tutti i CarModel (tipicamente in sola lettura nella UI).
        Task<List<CarModel>> ListAsync();

        // Aggiunge un nuovo CarModel al change tracker.
        // N.B. la persistenza effettiva avviene con SaveChangesAsync().
        Task AddAsync(CarModel entity);

        // Marca l'entità come modificata.
        // N.B. richiede che l'entità sia tracciata o venga attaccata al contesto.
        void Update(CarModel entity);

        // Marca l'entità per la rimozione.
        void Remove(CarModel entity);

        // Persiste tutte le modifiche pendenti nel contesto sottostante.
        Task<int> SaveChangesAsync();
    }
}
