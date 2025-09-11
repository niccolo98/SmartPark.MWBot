using System;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace SmartPark.MWBot.Models
{
    // Contratto repository per l'entità Payment (testa del pagamento).
    // Espone operazioni di lettura (singolo, lista, per intervallo temporale) e CRUD basilari.
    // Nota: la persistenza effettiva delle modifiche avviene con SaveChangesAsync()
    //       invocato dal chiamante (PageModel/servizio applicativo).
    public interface IPaymentRepository
    {
        // Dettaglio pagamento per Id (o null se non trovato).
        Task<Payment?> GetByIdAsync(int id);

        // Elenco completo dei pagamenti (tipicamente read-only).
        Task<List<Payment>> ListAsync();

        // Elenco dei pagamenti nell'intervallo [fromUtc, toUtc] (estremi inclusi).
        // I parametri sono attesi in UTC, coerenti con Payment.CreatedUtc.
        Task<List<Payment>> ListByRangeAsync(DateTime fromUtc, DateTime toUtc);

        // Inserimento di un nuovo Payment nel change tracker
        // (richiede SaveChangesAsync per la persistenza).
        Task AddAsync(Payment entity);

        // Aggiornamento di un Payment esistente (richiede entity tracciata/attaccata).
        void Update(Payment entity);

        // Rimozione di un Payment (attenzione alle FK con PaymentLine).
        void Remove(Payment entity);

        // Commit delle modifiche pendenti nel contesto dati.
        Task<int> SaveChangesAsync();
    }
}
