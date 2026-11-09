using Einkaufsliste.Models;
using Einkaufsliste.Interfaces;

namespace Einkaufsliste.Services
{
    public partial class EinkauflistenDatenbank
    {
        public async Task<List<Einkaufsartikel>> AlleArtikelLadenAsync()
        {
            return await _datenbank.Table<Einkaufsartikel>().ToListAsync();
        }
        public async Task ArtikelSpeichernAsync(Einkaufsartikel artikel)
        {
            int ergebnis;

            if (artikel.Id == 0)
            {
                ergebnis = await _datenbank.InsertAsync(artikel);
            }
            else
            {
                ergebnis = await _datenbank.UpdateAsync(artikel);
            }

            System.Diagnostics.Debug.WriteLine(
                $"SQLite-Speichern: ID={artikel.Id}, " +
                $"Menge={artikel.Menge}, Einheit={artikel.Einheit}, " +
                $"Ergebnis={ergebnis}");

            await _backupService.LokaleSicherungErstellenAsync();
        }
        public async Task EinkaufsartikelHaendlerAktualisierenAsync(int produktId, int? haendlerId)
        {
            List<Einkaufsartikel> artikel = await _datenbank
                .Table<Einkaufsartikel>()
                .Where(a => a.ProduktId == produktId && !a.IstErledigt)
                .ToListAsync();

            Produktpreis? aktuellerPreis = null;

            if (haendlerId.HasValue)
            {
                aktuellerPreis = await GuenstigstenAktuellenPreisLadenAsync(
                    produktId,
                    haendlerId.Value);
            }

            foreach (Einkaufsartikel eintrag in artikel)
            {
                eintrag.AusgewaehlterHaendlerId = haendlerId ?? 0;
                eintrag.HaendlerAnzeige = haendlerId.HasValue
                    ? (await _datenbank.Table<Haendler>()
                        .Where(h => h.Id == haendlerId.Value)
                        .FirstOrDefaultAsync())?.Name ?? string.Empty
                    : string.Empty;

                eintrag.Stueckpreis = aktuellerPreis?.PreisEuro ?? 0m;

                await _datenbank.UpdateAsync(eintrag);
            }

            await _backupService.LokaleSicherungErstellenAsync();
        }
        public async Task ArtikelLoeschenAsync(Einkaufsartikel artikel)
        {
            await _datenbank.DeleteAsync(artikel);
            await _backupService.LokaleSicherungErstellenAsync();
        }
        public async Task<Einkaufsartikel?> ArtikelLadenNachIdAsync(int id)
        {
            return await _datenbank
                .Table<Einkaufsartikel>()
                .Where(a => a.Id == id)
                .FirstOrDefaultAsync();
        }
    }
}
