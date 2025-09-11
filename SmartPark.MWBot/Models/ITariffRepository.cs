using System;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace SmartPark.MWBot.Models
{
    // Contratto repository per la gestione delle Tariffe.
    // Espone:
    //  - recupero per Id,
    //  - "tariffa corrente" in un dato istante UTC,
    //  - elenco completo,
    //  - CRUD basilari con persistenza esplicita via SaveChangesAsync().
    //
    // Nota: i metodi accettano/ritornano DateTime in UTC per coerenza con Tariff.ValidFromUtc/ValidToUtc.
    public interface ITariffRepository
    {
        // Recupera una tariffa per chiave primaria (null se non trovata).
        Task<Tariff?> GetByIdAsync(int id);

        // Restituisce la tariffa "in vigore" all'istante specificato (UTC).
        // Criteri tipici: ValidFromUtc <= utcNow && (ValidToUtc == null || ValidToUtc >= utcNow).
        Task<Tariff?> GetCurrentAsync(DateTime utcNow);

        // Elenco completo (tipicamente read-only in UI Admin).
        Task<List<Tariff>> ListAsync();

        // CRUD: inserimento (l'entità viene aggiunta al change tracker; salvare con SaveChangesAsync).
        Task AddAsync(Tariff entity);

        // CRUD: aggiornamento (richiede entità tracciata o attaccata al contesto).
        void Update(Tariff entity);

        // CRUD: rimozione.
        void Remove(Tariff entity);

        // Commit delle modifiche pendenti nel contesto dati.
        Task<int> SaveChangesAsync();
    }
}
