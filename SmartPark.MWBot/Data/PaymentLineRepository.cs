using Microsoft.EntityFrameworkCore;
using SmartPark.MWBot.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SmartPark.MWBot.Data
{
    // Repository per le righe di pagamento (PaymentLine).
    // Strutturato per permettere:
    //  - recupero singola riga,
    //  - elenco righe per un Payment (sosta/ricarica),
    //  - CRUD,
    // con SaveChangesAsync delegato al DbContext.
    public class PaymentLineRepository : IPaymentLineRepository
    {
        private readonly ApplicationDbContext _db;
        public PaymentLineRepository(ApplicationDbContext db) => _db = db;

        // Restituisce una PaymentLine per Id (tracking abilitato: utile in scenari di update).
        public Task<PaymentLine?> GetByIdAsync(int id)
            => _db.PaymentLines.FirstOrDefaultAsync(l => l.Id == id);

        // Elenca tutte le PaymentLine associate a un Payment specifico (read-only).
        // AsNoTracking migliora le performance perché non si intende modificare i risultati.
        public Task<List<PaymentLine>> ListByPaymentAsync(int paymentId)
            => _db.PaymentLines.AsNoTracking()
                               .Where(l => l.PaymentId == paymentId)
                               .ToListAsync();

        // Inserisce una singola riga 
        public Task AddAsync(PaymentLine entity) { _db.PaymentLines.Add(entity); return Task.CompletedTask; }

        // Inserisce più righe in blocco (utile al checkout per Parking + Charging).
        public Task AddRangeAsync(IEnumerable<PaymentLine> entities) { _db.PaymentLines.AddRange(entities); return Task.CompletedTask; }

        // Aggiorna una riga esistente 
        public void Update(PaymentLine entity) => _db.PaymentLines.Update(entity);

        // Rimuove una riga esistente
        public void Remove(PaymentLine entity) => _db.PaymentLines.Remove(entity);

        // Commit delle modifiche pendenti nel DbContext.
        public Task<int> SaveChangesAsync() => _db.SaveChangesAsync();
    }
}
