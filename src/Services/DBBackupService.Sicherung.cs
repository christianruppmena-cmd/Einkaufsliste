using Einkaufsliste.Models;
using System.IO.Compression;

namespace Einkaufsliste.Services
{
    public partial class DBBackupService
    {
        public async Task LokaleSicherungErstellenAsync()
        {
            await SicherungsOrdnerErstellenAsync();

            if (!await DatenbankEnthaeltDatenAsync())
            {
                return;
            }

            // Temporäre Datenbank erstellen
            string tempDatenbank = Path.Combine(
                FileSystem.CacheDirectory,
                $"sicherung_temp_{Guid.NewGuid()}.db3");

            // ZIP-Sicherungsdatei erstellen
            string dateiname =
                $"einkaufliste_{DateTime.Now:yyyyMMdd_HHmmss_fff}.zip";

            string sicherungsPfad = Path.Combine(
                sicherungsOrdner,
                dateiname);

            bool erfolgreich = false;

            try
            {
                // Aktuelle Datenbank sichern
                string sichererPfad = tempDatenbank.Replace("'", "''");
                await datenbank.ExecuteAsync(
                    "VACUUM INTO ?",
                    sichererPfad);

                // ZIP-Archiv erstellen
                using (var datei = new FileStream(
                    sicherungsPfad,
                    FileMode.Create))
                using (var archiv = new ZipArchive(
                    datei,
                    ZipArchiveMode.Create))
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

                erfolgreich = true;

                // Nur die 10 neuesten ZIP-Sicherungen behalten
                var sicherungen = new DirectoryInfo(sicherungsOrdner)
                    .GetFiles("einkaufliste_*.zip")
                    .OrderByDescending(f => f.CreationTimeUtc)
                    .ToList();

                foreach (var alteSicherung in sicherungen.Skip(10))
                {
                    alteSicherung.Delete();
                }
            }
            finally
            {
                // Temporäre Datenbank löschen
                if (File.Exists(tempDatenbank))
                {
                    File.Delete(tempDatenbank);
                }

                // Unvollständige ZIP-Datei bei Fehler entfernen
                if (!erfolgreich && File.Exists(sicherungsPfad))
                {
                    File.Delete(sicherungsPfad);
                }
            }
        }
        public async Task<List<FileInfo>> LokaleSicherungenLadenAsync()
        {
            await SicherungsOrdnerErstellenAsync();

            return new DirectoryInfo(sicherungsOrdner)
                .GetFiles("einkaufliste_*.zip")
                .OrderByDescending(f => f.CreationTimeUtc)
                .Take(10)
                .ToList();
        }
        private async Task<bool> DatenbankEnthaeltDatenAsync()
        {
            int anzahlProdukte = await datenbank.Table<Produkt>().CountAsync();
            return anzahlProdukte > 0;
        }
        private async Task SicherungsOrdnerErstellenAsync()
        {
            await Task.Run(() =>
            {
                Directory.CreateDirectory(sicherungsOrdner);

            });
        }
    }
}
