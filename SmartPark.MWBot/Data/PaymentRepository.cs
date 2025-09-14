using Microsoft.EntityFrameworkCore;
using SmartPark.MWBot.Models;


namespace SmartPark.MWBot.Data
{
    // Repository per l'entità Payment.
    // Nota: le "righe" del pagamento sono in PaymentLineRepository.
    public class PaymentRepository : IPaymentRepository
    {
        private readonly ApplicationDbContext _db;
        public PaymentRepository(ApplicationDbContext db) => _db = db;

        // Dettaglio pagamento per Id, includendo la ParkingSession collegata.
        public Task<Payment?> GetByIdAsync(int id)
            => _db.Payments
                  .Include(p => p.ParkingSession)
                  .FirstOrDefaultAsync(p => p.Id == id);

        // Elenco pagamenti in sola lettura, ordinati dal più recente (CreatedUtc desc),
        // includendo la sessione per mostrare info correlate (es. spot/car) nelle viste.
        public Task<List<Payment>> ListAsync()
            => _db.Payments
                  .AsNoTracking()
                  .Include(p => p.ParkingSession)
                  .OrderByDescending(p => p.CreatedUtc)
                  .ToListAsync();

        // Pagamenti in un intervallo temporale [fromUtc, toUtc] (estremi inclusi),
        // in sola lettura e ordinati cronologicamente (asc).
        public Task<List<Payment>> ListByRangeAsync(DateTime fromUtc, DateTime toUtc)
            => _db.Payments
                  .AsNoTracking()
                  .Include(p => p.ParkingSession)
                  .Where(p => p.CreatedUtc >= fromUtc && p.CreatedUtc <= toUtc)
                  .OrderBy(p => p.CreatedUtc)
                  .ToListAsync();

        // CRUD: inserimento 
        public Task AddAsync(Payment entity) { _db.Payments.Add(entity); return Task.CompletedTask; }

        // CRUD: aggiornamento 
        public void Update(Payment entity) => _db.Payments.Update(entity);

        // CRUD: rimozione
        public void Remove(Payment entity) => _db.Payments.Remove(entity);

        // Commit unitario delle modifiche pendenti nel DbContext.
        public Task<int> SaveChangesAsync() => _db.SaveChangesAsync();
    }
}
