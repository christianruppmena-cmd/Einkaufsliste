using Einkaufsliste.Models;
using Einkaufsliste.Interfaces;

namespace Einkaufsliste.Services
{
    public class KategorienService : DBConnection, IKategorienService
    {
        private readonly IDBBackupService _backupService;

        public KategorienService(IDBBackupService backupService)
        {
            _backupService = backupService;
        }

        public async Task StandardkategorienAnlegenAsync()
        {
            string[] standardkategorien =
            {
            "Obst",
            "Gemüse",
            "Milchprodukte",
            "Fleisch & Fisch",
            "Backwaren",
            "Getränke",
            "Tiefkühlprodukte",
            "Süßigkeiten & Snacks",
            "Haushalt",
            "Drogerie",
            "Sonstiges"
        };

            foreach (string name in standardkategorien)
            {
                Kategorie? vorhanden = await Datenbank
                    .Table<Kategorie>()
                    .Where(k => k.Name == name)
                    .FirstOrDefaultAsync();

                if (vorhanden == null)
                {
                    await Datenbank.InsertAsync(new Kategorie
                    {
                        Name = name
                    });
                }
            }
        }
        public async Task<List<Kategorie>> AlleKategorieEintraegeLadenAsync()
        {
            return await Datenbank
                .Table<Kategorie>()
                .OrderBy(k => k.Name)
                .ToListAsync();
        }
        public async Task KategorieSpeichernAsync(Kategorie kategorie)
        {
          
            if (kategorie.Id == 0)
            {
                await Datenbank.InsertAsync(kategorie);

            }
            else
            {
                await Datenbank.UpdateAsync(kategorie);
            }
            await _backupService.LokaleSicherungErstellenAsync();
        }
        public async Task<bool> KategorieUmbenennenAsync(Kategorie kategorie, string neuerName)
        {
            string alterName = kategorie.Name.Trim();
            neuerName = neuerName.Trim();

            if (string.IsNullOrWhiteSpace(neuerName))
                return false;

            // Prüfen ob der neue Name bereits vergene ist

            List<Kategorie> kategorien =
                await Datenbank.Table<Kategorie>().ToListAsync();

            bool existiertBereits = kategorien.Any(k =>
            k.Id != kategorie.Id &&
            string.Equals(
                k.Name.Trim(),
                neuerName,
                StringComparison.OrdinalIgnoreCase));

            if (existiertBereits)
                return false;

            // Alle betroffenen Artikel aktualisieren

            List<Einkaufsartikel> artikel =
                await Datenbank.Table<Einkaufsartikel>().ToListAsync();

            List<Produkt> produkte =
                await Datenbank.Table<Produkt>().ToListAsync();

            await Datenbank.RunInTransactionAsync(conn =>
            {
                foreach (Einkaufsartikel eintrag in artikel.Where(a =>
                string.Equals(
                    a.Kategorie?.Trim(),
                    alterName,
                    StringComparison.OrdinalIgnoreCase)))
                {
                    eintrag.Kategorie = neuerName;
                    conn.Update(eintrag);
                }
                foreach (Produkt produkt in produkte.Where(p =>
                string.Equals(
                    p.Kategorie?.Trim(),
                    alterName,
                    StringComparison.OrdinalIgnoreCase)))
                {
                    produkt.Kategorie = neuerName;
                    conn.Update(produkt);
                }

                kategorie.Name = neuerName;
                conn.Update(kategorie);
            });

            await _backupService.LokaleSicherungErstellenAsync();
            return true;
        }
    }
}
