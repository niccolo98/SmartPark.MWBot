

namespace SmartPark.MWBot.Models
{
    // Contratto repository per l'entità MWBotModel (lo "stato" del robot mobile di ricarica).
    // In questa soluzione si prevede un SOLO bot; per questo c'è anche GetSingletonAsync().
    // Le operazioni CRUD non eseguono il salvataggio: la persistenza avviene con SaveChangesAsync() lato chiamante.
    public interface IMWBotModelRepository
    {
        // Dettaglio per chiave primaria.
        Task<MWBotModel?> GetByIdAsync(int id);

        // Ritorna l'unico bot presente nel sistema (o null se non ancora creato).
        Task<MWBotModel?> GetSingletonAsync(); // il nostro unico bot

        // Elenco completo (nella pratica: 0 o 1 record).
        Task<List<MWBotModel>> ListAsync();

        // Inserisce un nuovo record (richiede SaveChangesAsync per persistere).
        Task AddAsync(MWBotModel entity);

        // Marca l'entità come modificata (tracking/attach gestiti dall'implementazione concreta).
        void Update(MWBotModel entity);

        // Marca l'entità per la rimozione.
        void Remove(MWBotModel entity);

        // Commit delle modifiche pendenti nel contesto dati.
        Task<int> SaveChangesAsync();
    }
}
