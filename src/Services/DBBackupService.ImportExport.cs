using Einkaufsliste.Interfaces;
using Einkaufsliste.Models;
using System.IO.Compression;

namespace Einkaufsliste.Services
{
    public partial class DBBackupService
    {
        public async Task DatenbankImportierenAsync(string importPfad)
        {
            if (!File.Exists(importPfad))
            {
                throw new FileNotFoundException(
                    "Die Sicherungsdatei wurde nicht gefunden.");
            }

            string pruefPfad = Path.Combine(
                FileSystem.CacheDirectory,
                $"import_pruefung_{Guid.NewGuid()}.db3");

            string importBildOrdner = Path.Combine(
                FileSystem.CacheDirectory,
                $"Produktbilder_{Guid.NewGuid()}");

            string rueckfallPfad = Path.Combine(
                FileSystem.CacheDirectory,
                $"datenbank_vor_import_{Guid.NewGuid()}.db3");

            string rueckfallBildOrdner = Path.Combine(
                FileSystem.CacheDirectory,
                $"Produktbilder_vor_import_{Guid.NewGuid()}");

            Directory.CreateDirectory(importBildOrdner);

            try
            {
                SicherungsdatenbankExtrahierenUndPruefen(importPfad, pruefPfad);

                ProduktbilderTemporaerExtrahieren(importPfad, importBildOrdner);

                // Aktuelle Datenbank als Rückfallsicherung erstellen
                await datenbank.ExecuteAsync(
                    "VACUUM INTO ?",
                    rueckfallPfad);

                // Zusätzliche lokale Sicherung erstellen
                await LokaleSicherungErstellenAsync();

                ProduktbilderAlsRueckfallSichern(rueckfallBildOrdner);

                // Verbindung erst nach erfolgreicher Sicherung schließen
                await datenbank.CloseAsync();

                try
                {
                    ImportierteDatenbankUebernehmen(pruefPfad);
                    
                    ProduktbilderErsetzen(importBildOrdner);

                    // Rückfallsicherungen nach erfolgreichem Import löschen
                    if (File.Exists(rueckfallPfad))
                        File.Delete(rueckfallPfad);

                    if (Directory.Exists(rueckfallBildOrdner))
                    {
                        Directory.Delete(
                            rueckfallBildOrdner,
                            recursive: true);
                    }
                }
                catch
                {
                    await UrsprungsdatenbankWiederherstellenAsync(rueckfallPfad, rueckfallBildOrdner);
                    throw;
                }
            }
            finally
            {
                ImportdateienAufraeumen(pruefPfad, importBildOrdner, rueckfallPfad, rueckfallBildOrdner);
            }
        }
        private static void SicherungsdatenbankExtrahierenUndPruefen(string importPfad, string pruefPfad)
        {
            // ZIP-Sicherung öffnen und Datenbank extrahieren
            using (var archiv = ZipFile.OpenRead(importPfad))
            {
                var datenbankEintrag =
                    archiv.GetEntry("einkaufliste.db3");

                if (datenbankEintrag == null)
                {
                    throw new Exception(
                        "Die ZIP-Sicherung enthält keine Datenbank.");
                }

                datenbankEintrag.ExtractToFile(
                    pruefPfad,
                    overwrite: true);
            }

            // Datenbank auf Integrität prüfen
            using (var pruefDatenbank =
                new SQLite.SQLiteConnection(pruefPfad))
            {
                string ergebnis =
                    pruefDatenbank.ExecuteScalar<string>(
                        "PRAGMA integrity_check;");

                if (ergebnis != "ok")
                {
                    throw new Exception(
                        "Die Sicherungsdatei ist beschädigt.");
                }

                var tabellen =
                    pruefDatenbank.GetTableInfo("Produkt");

                if (tabellen.Count == 0)
                {
                    throw new Exception(
                        "Die Sicherungsdatei enthält keine gültige Produkt-Tabelle.");
                }
            }
        }
        private static void ProduktbilderTemporaerExtrahieren(string importPfad, string importBildOrdner)
        {
            // Produktbilder zunächst temporär extrahieren
            using (var archiv = ZipFile.OpenRead(importPfad))
            {
                foreach (var eintrag in archiv.Entries)
                {
                    if (!eintrag.FullName.StartsWith(
                        "Produktbilder/",
                        StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    string dateiname =
                        Path.GetFileName(eintrag.FullName);

                    if (string.IsNullOrWhiteSpace(dateiname))
                    {
                        continue;
                    }

                    string endung =
                        Path.GetExtension(dateiname).ToLowerInvariant();

                    if (endung != ".jpg" &&
                        endung != ".jpeg" &&
                        endung != ".png" &&
                        endung != ".webp")
                    {
                        continue;
                    }

                    string zielpfad = Path.Combine(
                        importBildOrdner,
                        dateiname);

                    eintrag.ExtractToFile(
                        zielpfad,
                        overwrite: true);
                }
            }
        }
        private static void ProduktbilderAlsRueckfallSichern(string rueckfallBildOrdner)
        {
            // Bisherige Produktbilder für einen möglichen Rollback sichern
            Directory.CreateDirectory(rueckfallBildOrdner);

            foreach (string datei in Directory.GetFiles(
                FileSystem.AppDataDirectory))
            {
                string endung =
                    Path.GetExtension(datei).ToLowerInvariant();

                if (endung == ".jpg" ||
                    endung == ".jpeg" ||
                    endung == ".png" ||
                    endung == ".webp")
                {
                    File.Copy(
                        datei,
                        Path.Combine(
                            rueckfallBildOrdner,
                            Path.GetFileName(datei)),
                        overwrite: true);
                }
            }
        }
        private static void ProduktbilderErsetzen(string importBildOrdner)
        {
            // Vorhandene Produktbilder entfernen
            string bildOrdner = FileSystem.AppDataDirectory;

            foreach (string datei in Directory.GetFiles(bildOrdner))
            {
                string dateiname = Path.GetFileName(datei);

                string endung =
                    Path.GetExtension(datei).ToLowerInvariant();

                if (endung != ".jpg" &&
                    endung != ".jpeg" &&
                    endung != ".png" &&
                    endung != ".webp")
                {
                    continue;
                }

                if (File.Exists(
                    Path.Combine(importBildOrdner, dateiname)))
                {
                    continue;
                }

                File.Delete(datei);
            }

            // Neue Produktbilder übernehmen
            foreach (string bild in Directory.GetFiles(importBildOrdner))
            {
                string zielpfad = Path.Combine(
                    bildOrdner,
                    Path.GetFileName(bild));

                File.Copy(
                    bild,
                    zielpfad,
                    overwrite: true);
            }
        }
        private void ImportierteDatenbankUebernehmen(string pruefPfad)
        {
            // Alte SQLite-Begleitdateien entfernen
            if (File.Exists(datenbankPfad + "-wal"))
                File.Delete(datenbankPfad + "-wal");

            if (File.Exists(datenbankPfad + "-shm"))
                File.Delete(datenbankPfad + "-shm");

            // Neue Datenbank übernehmen
            File.Copy(
                pruefPfad,
                datenbankPfad,
                overwrite: true);

            // Neue Verbindung herstellen
            datenbank = new SQLiteAsyncConnection(datenbankPfad);
        }
        private async Task UrsprungsdatenbankWiederherstellenAsync(string rueckfallPfad,string rueckfallBildOrdner)
        {
            // Fehler beim Import: ursprüngliche Datenbank wiederherstellen
            try
            {
                await datenbank.CloseAsync();
            }
            catch
            {
                // Verbindung ist möglicherweise bereits geschlossen
            }

            if (File.Exists(rueckfallPfad))
            {
                File.Copy(
                    rueckfallPfad,
                    datenbankPfad,
                    overwrite: true);
            }

            // Ursprüngliche Datenbankverbindung wiederherstellen
            datenbank = new SQLiteAsyncConnection(datenbankPfad);

            // Importierte Produktbilder entfernen
            foreach (string datei in Directory.GetFiles(
                FileSystem.AppDataDirectory))
            {
                string endung =
                    Path.GetExtension(datei).ToLowerInvariant();

                if (endung == ".jpg" ||
                    endung == ".jpeg" ||
                    endung == ".png" ||
                    endung == ".webp")
                {
                    File.Delete(datei);
                }
            }

            // Ursprüngliche Produktbilder zurückkopieren
            if (Directory.Exists(rueckfallBildOrdner))
            {
                foreach (string bild in Directory.GetFiles(
                    rueckfallBildOrdner))
                {
                    string zielpfad = Path.Combine(
                        FileSystem.AppDataDirectory,
                        Path.GetFileName(bild));

                    File.Copy(
                        bild,
                        zielpfad,
                        overwrite: true);
                }
            }
        }
        private static void ImportdateienAufraeumen(string pruefPfad, string importBildOrdner,string rueckfallPfad,string rueckfallBildOrdner)
        {
            // Temporäre Dateien und Ordner aufräumen
            if (File.Exists(pruefPfad))
                File.Delete(pruefPfad);

            if (Directory.Exists(importBildOrdner))
            {
                Directory.Delete(
                    importBildOrdner,
                    recursive: true);
            }

            if (File.Exists(rueckfallPfad))
                File.Delete(rueckfallPfad);

            if (Directory.Exists(rueckfallBildOrdner))
            {
                Directory.Delete(
                    rueckfallBildOrdner,
                    recursive: true);
            }
        }
    }
}
