
using Einkaufsliste.Models;

namespace Einkaufsliste.Interfaces
{
    public interface IArtikelService
    {
        public Task<List<Einkaufsartikel>> AlleArtikelLadenAsync();
        public Task ArtikelSpeichernAsync(Einkaufsartikel artikel);
        public Task EinkaufsartikelHaendlerAktualisierenAsync(int produktId, int? haendlerId);
        public Task ArtikelLoeschenAsync(Einkaufsartikel artikel);
    }
}
