using Einkaufsliste.Models;


namespace Einkaufsliste.Interfaces
{
    public interface IProdukteService
    {
        public Task<List<Produkt>> AlleProdukteLadenAsync();
        public Task<Produkt?> ProduktFindenAsync(string name);
        public Task ProduktSpeichernAsync(Produkt produkt);
        public Task ProduktZusammenfuehrenAsync(int quellProduktId, int zielProduktId);
        public Task<Produkt?> ProduktMitGleichemSchluesselLadenAsync(Produkt produkt);
    }
}
