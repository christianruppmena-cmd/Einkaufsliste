using Einkaufsliste.Interfaces;
using Einkaufsliste.Models;


namespace Einkaufsliste.Services
{
    public class ArtikelService : DBConnection, IArtikelService
    {
        private readonly IDBBackupService _backupService;
        private readonly IPreisService _preisService;

        public ArtikelService(IDBBackupService backupService, IPreisService preisService)
        {
            _backupService = backupService;
            _preisService = preisService;
        }

        public async Task<List<Einkaufsartikel>> AlleArtikelLadenAsync()
        {
            return await Datenbank.Table<Einkaufsartikel>().ToListAsync();
        }

        public async Task ArtikelSpeichernAsync(Einkaufsartikel artikel)
        {
            int ergebnis;

            if (artikel.Id == 0)
            {
                ergebnis = await Datenbank.InsertAsync(artikel);
            }
            else
            {
                ergebnis = await Datenbank.UpdateAsync(artikel);
            }

            System.Diagnostics.Debug.WriteLine(
                $"SQLite-Speichern: ID={artikel.Id}, " +
                $"Menge={artikel.Menge}, Einheit={artikel.Einheit}, " +
                $"Ergebnis={ergebnis}");

            await _backupService.LokaleSicherungErstellenAsync();
        }

        public async Task EinkaufsartikelHaendlerAktualisierenAsync(int produktId, int? haendlerId)
        {
            List<Einkaufsartikel> artikel = await Datenbank
                .Table<Einkaufsartikel>()
                .Where(a => a.ProduktId == produktId && !a.IstErledigt)
                .ToListAsync();

            Produktpreis? aktuellerPreis = null;

            if (haendlerId.HasValue)
            {
                aktuellerPreis = await _preisService.GuenstigstenAktuellenPreisLadenAsync(
                    produktId,
                    haendlerId.Value);
            }

            foreach (Einkaufsartikel eintrag in artikel)
            {
                eintrag.AusgewaehlterHaendlerId = haendlerId ?? 0;
                eintrag.HaendlerAnzeige = haendlerId.HasValue
                    ? (await Datenbank.Table<Haendler>()
                        .Where(h => h.Id == haendlerId.Value)
                        .FirstOrDefaultAsync())?.Name ?? string.Empty
                    : string.Empty;

                eintrag.Stueckpreis = aktuellerPreis?.PreisEuro ?? 0m;

                await Datenbank.UpdateAsync(eintrag);
            }

            await _backupService.LokaleSicherungErstellenAsync();
        }

        public async Task ArtikelLoeschenAsync(Einkaufsartikel artikel)
        {
            await Datenbank.DeleteAsync(artikel);
            await _backupService.LokaleSicherungErstellenAsync();
        }
    }
}
