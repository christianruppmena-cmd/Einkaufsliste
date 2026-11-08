using Einkaufsliste.Interfaces;
using Einkaufsliste.Models;

namespace Einkaufsliste.Services;

public class DBBackupService : IDBBackupService
{
    private SQLiteAsyncConnection datenbank;
    private readonly string datenbankPfad =
        Path.Combine(FileSystem.AppDataDirectory, "einkaufliste.db3");

    public DBBackupService()
    {
        datenbank = new SQLiteAsyncConnection(datenbankPfad);
    }

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

                // Produkte laden
                var produkte = await datenbank
                    .Table<Produkt>()
                    .ToListAsync();

                // Produktbilder hinzufügen
                var bereitsGespeicherteBilder =
                    new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (var produkt in produkte)
                {
                    if (string.IsNullOrWhiteSpace(produkt.BildPfad))
                        continue;

                    if (!File.Exists(produkt.BildPfad))
                        continue;

                    string bildname =
                        Path.GetFileName(produkt.BildPfad);

                    // Doppelte Bildeinträge vermeiden
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

            // Aktuelle Datenbank als Rückfallsicherung erstellen
            await datenbank.ExecuteAsync(
                "VACUUM INTO ?",
                rueckfallPfad);

            // Zusätzliche lokale Sicherung erstellen
            await LokaleSicherungErstellenAsync();

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

            // Verbindung erst nach erfolgreicher Sicherung schließen
            await datenbank.CloseAsync();

            try
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

                throw;
            }
        }
        finally
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

    private async Task SicherungsOrdnerErstellenAsync()
    {
        await Task.Run(() =>
        {
            Directory.CreateDirectory(sicherungsOrdner);

        });
    }
}
