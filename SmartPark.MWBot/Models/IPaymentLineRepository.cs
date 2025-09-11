using System.Collections.Generic;
using System.Threading.Tasks;

namespace SmartPark.MWBot.Models
{
    // Contratto repository per le righe di pagamento (PaymentLine).
    // Consente di recuperare righe per Payment, oltre a CRUD basilari.
    // Nota: Add/AddRange aggiungono solo al change tracker; la persistenza effettiva
    // avviene quando il chiamante invoca SaveChangesAsync().
    public interface IPaymentLineRepository
    {
        // Restituisce una singola riga per Id (o null se non trovata).
        Task<PaymentLine?> GetByIdAsync(int id);

        // Elenca tutte le righe associate a un dato pagamento.
        Task<List<PaymentLine>> ListByPaymentAsync(int paymentId);

        // Inserimento di una singola riga.
        Task AddAsync(PaymentLine entity);

        // Inserimento in blocco (utile al checkout: Parking + Charging).
        Task AddRangeAsync(IEnumerable<PaymentLine> entities);

        // Aggiornamento riga esistente.
        void Update(PaymentLine entity);

        // Rimozione riga esistente.
        void Remove(PaymentLine entity);

        // Commit delle modifiche pendenti nel contesto dati (inserimenti/aggiornamenti/cancellazioni).
        Task<int> SaveChangesAsync();
    }
}
