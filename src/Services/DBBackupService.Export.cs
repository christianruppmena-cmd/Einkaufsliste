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

                    await ProduktbilderZumArchivHinzufuegenAsync(archiv);
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
