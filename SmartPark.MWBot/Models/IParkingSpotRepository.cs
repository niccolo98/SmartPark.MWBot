using System.Threading.Tasks;
using System.Collections.Generic;

namespace SmartPark.MWBot.Models
{
    // Contratto repository per i posti auto (ParkingSpot).
    // Incapsula operazioni di accesso, ricerca e modifica.
    public interface IParkingSpotRepository
    {
        // Ricerca per chiave primaria.
        Task<ParkingSpot?> GetByIdAsync(int id);

        // Ricerca per "codice" del posto (es. "P01").
        Task<ParkingSpot?> GetByCodeAsync(string code);

        // Elenco completo (read-only).
        Task<List<ParkingSpot>> ListAsync();

        // CRUD: inserimento (persistenza con SaveChangesAsync).
        Task AddAsync(ParkingSpot entity);

        // CRUD: aggiornamento.
        void Update(ParkingSpot entity);

        // CRUD: cancellazione.
        void Remove(ParkingSpot entity);

        // Commit modifiche pendenti nel contesto dati.
        Task<int> SaveChangesAsync();
    }
}
