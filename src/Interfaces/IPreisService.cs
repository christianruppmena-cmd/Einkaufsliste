using Einkaufsliste.Models;

namespace Einkaufsliste.Interfaces
{
    public interface IPreisService
    {
        public Task<Produktpreis?> AktuellenProduktpreisLadenAsync(int produktId, int haendlerId, string preisart);
        public Task<Produktpreis?> GuenstigstenAktuellenPreisLadenAsync(int produktId, int haendlerId);
        public Task ProduktpreisSpeichernAsync(Produktpreis produktpreis);
        public Task ProduktpreisDeaktivierenAsync(int produktpreisId);
    }
}
