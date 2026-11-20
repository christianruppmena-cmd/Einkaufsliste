using Einkaufsliste.Models;


namespace Einkaufsliste.Interfaces
{
    public interface IHaendlerService
    {
        public Task HaendlerSpeichernAsync(Haendler haendler);
        public Task<List<Haendler>> AlleHaendlerVerwaltenLadenAsync();
        public Task StandardHaendlerAnlegenAsync();
        public Task<List<Haendler>> AlleHaendlerLadenAsync();
    }
}
