using Einkaufsliste.Interfaces;
using Einkaufsliste.Models;
using System.IO.Compression;

namespace Einkaufsliste.Services
{
    public partial class DBBackupService
    {
        public async Task<string> ExportDateiErstellenAsync()
        {
            string tempDatenbank = Path.Combine(
                FileSystem.CacheDirectory,
                "einkaufliste_export_temp.db3");

            string exportPfad = Path.Combine(
                FileSystem.CacheDirectory,
                $"einkaufliste_export_{DateTime.Now:yyyyMMdd_HHmmss}.zip");

            try
            {
                // Aktuelle Datenbank erstellen
                await datenbank.ExecuteAsync(
                    "VACUUM INTO ?",
                    tempDatenbank);

                // ZIP-Archiv erstellen
                using (var datei = new FileStream(
                    exportPfad, FileMode.Create))
                using (var archiv = new ZipArchive(
                    datei, ZipArchiveMode.Create))
                {
                    // Datenbank hinzufügen
                    var datenbankEintrag = archiv.CreateEntry(
                        "einkaufliste.db3",
                        CompressionLevel.Optimal);

                    await using (var quelle = File.OpenRead(tempDatenbank))
                    await using (var ziel = datenbankEintrag.Open())
                    {
                        await quelle.CopyToAsync(ziel);
                    }

                    // Produktbilder laden
                    var produkte = await datenbank
                        .Table<Produkt>()
                        .ToListAsync();

                    foreach (var produkt in produkte)
                    {
                        if (string.IsNullOrWhiteSpace(produkt.BildPfad))
                            continue;

                        if (!File.Exists(produkt.BildPfad))
                            continue;

                        string bildname = Path.GetFileName(produkt.BildPfad);

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

                return exportPfad;
            }
            finally
            {
                if (File.Exists(tempDatenbank))
                    File.Delete(tempDatenbank);
            }
        }
    }
}
