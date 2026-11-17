
using Einkaufsliste.Models;

namespace Einkaufsliste.Interfaces
{
    public interface IKategorienService
    {
        public Task StandardkategorienAnlegenAsync();
        public Task<List<Kategorie>> AlleKategorieEintraegeLadenAsync();
        public Task KategorieSpeichernAsync(Kategorie kategorie);
        public Task<bool> KategorieUmbenennenAsync(Kategorie kategorie, string neuerName);

    }
}
