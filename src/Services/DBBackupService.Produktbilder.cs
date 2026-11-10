using Einkaufsliste.Models;
using System.IO.Compression;

namespace Einkaufsliste.Services
{
    public partial class DBBackupService
    {
        private async Task ProduktbilderZumArchivHinzufuegenAsync(
            ZipArchive archiv)
        {
            var produkte = await datenbank
                .Table<Produkt>()
                .ToListAsync();

            var bereitsGespeicherteBilder =
                new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var produkt in produkte)
            {
                if (string.IsNullOrWhiteSpace(produkt.BildPfad))
                    continue;

                if (!File.Exists(produkt.BildPfad))
                    continue;

                string bildname = Path.GetFileName(produkt.BildPfad);

                if (!bereitsGespeicherteBilder.Add(bildname))
                    continue;

                var bildEintrag = archiv.CreateEntry(
                    $"Produktbilder/{bildname}",
                    CompressionLevel.Optimal);

                await using (var quelle = File.OpenRead(produkt.BildPfad))
                await using (var ziel = bildEintrag.Open())
                {
                    await quelle.CopyToAsync(ziel);
                }
            }
        }
    }
}