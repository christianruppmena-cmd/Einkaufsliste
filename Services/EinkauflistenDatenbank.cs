
using Einkaufsliste.Models;
using SQLite;
using System.IO.Compression;
using System.Diagnostics;

namespace Einkaufsliste.Services;

public class EinkauflistenDatenbank
{
    private SQLiteAsyncConnection datenbank;
    private readonly string datenbankPfad =
    Path.Combine(FileSystem.AppDataDirectory, "einkaufliste.db3");

    private readonly string sicherungsOrdner =
        Path.Combine(FileSystem.AppDataDirectory, "Sicherungen");
    public EinkauflistenDatenbank()
    {
        datenbank = new SQLiteAsyncConnection(datenbankPfad);
    }
    private async Task SicherungsOrdnerErstellenAsync()
    {
        await Task.Run(() =>
        {
            Directory.CreateDirectory(sicherungsOrdner);
            
        });
    }
   
    private async Task<bool> DatenbankEnthaeltDatenAsync()
    {
        var produkte = await datenbank
            .Table<Produkt>()
            .CountAsync();

        var artikel = await datenbank
            .Table<Einkaufsartikel>()
            .CountAsync();

        return produkte > 0 || artikel > 0;
    }
    public async Task InitialisierenAsync()
    {
        await SicherungsOrdnerErstellenAsync();


        // Einkaufslisten
        await datenbank.CreateTableAsync<Einkaufsartikel>();
        await datenbank.CreateTableAsync<Kategorie>();

        // Produktverwaltung
        await datenbank.CreateTableAsync<Produkt>();
        // Migration: Bevorzugten Händler beim Produkt ergänzen
        var produktSpalten =
            await datenbank.GetTableInfoAsync("Produkt");

        if (!produktSpalten.Any(s => s.Name == "BevorzugterHaendlerId"))
        {
            await datenbank.ExecuteAsync(
                "ALTER TABLE Produkt ADD COLUMN BevorzugterHaendlerId INTEGER NULL");
        }

        // Migration: Ausgewählten Händler beim Einkaufsartikel ergänzen
        var artikelSpalten =
            await datenbank.GetTableInfoAsync("Einkaufsartikel");

        if (!artikelSpalten.Any(s => s.Name == "AusgewaehlterHaendlerId"))
        {
            await datenbank.ExecuteAsync(
                "ALTER TABLE Einkaufsartikel ADD COLUMN AusgewaehlterHaendlerId INTEGER NULL");
        }

        // Preisvergleich
        await datenbank.CreateTableAsync<Haendler>();
        await datenbank.CreateTableAsync<Produktpreis>();
        await datenbank.CreateTableAsync<PreisHistorie>();

        // Verknüpfung zwischen Einkaufsartikel und Produkt
        var spalten = await datenbank.GetTableInfoAsync("Einkaufsartikel");

        if (!spalten.Any(s => s.Name == "ProduktId"))
        {
            await datenbank.ExecuteAsync(
                "ALTER TABLE Einkaufsartikel ADD COLUMN ProduktId INTEGER NULL");
        }
        // Migration PreisHistorie: Versionsnummer ergänzen
        var historieSpalten =
            await datenbank.GetTableInfoAsync("PreisHistorie");

        if (!historieSpalten.Any(s => s.Name == "Versionsnummer"))
        {
            await datenbank.ExecuteAsync(
                "ALTER TABLE PreisHistorie ADD COLUMN Versionsnummer INTEGER NOT NULL DEFAULT 0");
        }


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
    public async Task StandardHaendlerAnlegenAsync()
    {
        string[] haendlerNamen =
        {
        "ALDI SÜD",
        "Lidl",
        "REWE",
        "EDEKA",
        "Penny",
        "Netto",
        "dm",
        "Rossmann",
        "Kaufland"
    };

        for (int i = 0; i < haendlerNamen.Length; i++)
        {
            string name = haendlerNamen[i];

            var vorhanden = await datenbank
                .Table<Haendler>()
                .Where(h => h.Name == name)
                .FirstOrDefaultAsync();

            if (vorhanden == null)
            {
                await datenbank.InsertAsync(new Haendler
                {
                    Name = name,
                    IstAktiv = true,
                    Sortierung = i + 1
                });
            }
        }
    }
    public async Task<List<Haendler>> AlleHaendlerLadenAsync()
    {
        await InitialisierenAsync();

        return await datenbank
            .Table<Haendler>()
            .Where(h => h.IstAktiv)
            .OrderBy(h => h.Sortierung)
            .ToListAsync();
    }
    public async Task<List<Produktpreis>> AlleProduktpreiseLadenAsync(int produktId)
    {
        await InitialisierenAsync();

        return await datenbank
            .Table<Produktpreis>()
            .Where(p => p.ProduktId == produktId && p.IstAktiv)
            .ToListAsync();
    }
    public async Task<Produktpreis?> AktuellenProduktpreisLadenAsync(int produktId, int haendlerId, string preisart)
    {
        await InitialisierenAsync();

        string heute = DateTime.Now.ToString("yyyy-MM-dd");

        List<Produktpreis> preise = await datenbank
            .Table<Produktpreis>()
            .Where(p =>
                p.ProduktId == produktId &&
                p.HaendlerId == haendlerId &&
                p.Preisart == preisart &&
                p.IstAktiv)
            .ToListAsync();

        return preise
            .Where(p =>
                string.CompareOrdinal(p.GueltigVon, heute) <= 0 &&
                (string.IsNullOrEmpty(p.GueltigBis) ||
                 string.CompareOrdinal(p.GueltigBis, heute) >= 0))
            .OrderByDescending(p => p.GueltigVon)
            .FirstOrDefault();
    }
    public async Task<Produktpreis?> GuenstigstenAktuellenPreisLadenAsync(int produktId, int haendlerId)
    {
        Produktpreis? angebot = await AktuellenProduktpreisLadenAsync(
            produktId,
            haendlerId,
            "Angebot");

        if (angebot != null)
            return angebot;

        return await AktuellenProduktpreisLadenAsync(
            produktId,
            haendlerId,
            "Normalpreis");
    }
    private static void PreisInHistorieUebernehmen(SQLiteConnection conn, Produktpreis preis)
    {
        int naechsteVersion = conn.Table<PreisHistorie>()
            .Where(h => h.ProduktpreisId == preis.Id)
            .ToList()
            .Select(h => h.Versionsnummer)
            .DefaultIfEmpty(0)
            .Max() + 1;

        conn.Insert(new PreisHistorie
        {
            ProduktpreisId = preis.Id,
            Versionsnummer = naechsteVersion,
            ProduktId = preis.ProduktId,
            HaendlerId = preis.HaendlerId,
            PreisInCent = preis.PreisInCent,
            Preisart = preis.Preisart,
            ErfasstAm = preis.ErfasstAm,
            GueltigVon = preis.GueltigVon,
            GueltigBis = preis.GueltigBis
        });
    }
    public async Task ProduktpreisSpeichernAsync(Produktpreis produktpreis)
    {
        await InitialisierenAsync();

        if (produktpreis.ProduktId <= 0 ||
            produktpreis.HaendlerId <= 0 ||
            produktpreis.PreisInCent <= 0)
        {
            throw new ArgumentException(
                "Bitte gültige Preisangaben machen.");
        }

        if (produktpreis.Preisart != "Normalpreis" &&
            produktpreis.Preisart != "Angebot")
        {
            throw new ArgumentException(
                "Die Preisart muss Normalpreis oder Angebot sein.");
        }

        if (!DateTime.TryParseExact(
            produktpreis.GueltigVon,
            "yyyy-MM-dd",
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.None,
            out DateTime neuerStart))
        {
            throw new ArgumentException(
                "Das Gültig-ab-Datum ist ungültig.");
        }

        if (!string.IsNullOrWhiteSpace(produktpreis.GueltigBis))
        {
            if (!DateTime.TryParseExact(
                produktpreis.GueltigBis,
                "yyyy-MM-dd",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None,
                out DateTime neuesEnde) ||
                neuesEnde < neuerStart)
            {
                throw new ArgumentException(
                    "Das Gültig-bis-Datum ist ungültig.");
            }
        }

        produktpreis.ErfasstAm =
            DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        await datenbank.RunInTransactionAsync(conn =>
        {
            // Alten Stand beim Bearbeiten sichern
            if (produktpreis.Id > 0)
            {
                Produktpreis? alterPreis = conn.Find<Produktpreis>(
                    produktpreis.Id);

                if (alterPreis != null)
                {
                    PreisInHistorieUebernehmen(conn, alterPreis);
                }
            }

            // Bisherige Preise derselben Art abschließen
            var vorhandenePreise = conn.Table<Produktpreis>()
                .Where(p =>
                    p.ProduktId == produktpreis.ProduktId &&
                    p.HaendlerId == produktpreis.HaendlerId &&
                    p.Preisart == produktpreis.Preisart &&
                    p.IstAktiv &&
                    p.Id != produktpreis.Id)
                .ToList();

            foreach (var vorhanden in vorhandenePreise)
            {
                DateTime alterStart = DateTime.ParseExact(
                    vorhanden.GueltigVon,
                    "yyyy-MM-dd",
                    System.Globalization.CultureInfo.InvariantCulture);

                if (alterStart < neuerStart &&
                    (string.IsNullOrEmpty(vorhanden.GueltigBis) ||
                     vorhanden.GueltigBis.CompareTo(
                         neuerStart.ToString("yyyy-MM-dd")) >= 0))
                {
                    vorhanden.GueltigBis =
                        neuerStart.AddDays(-1).ToString("yyyy-MM-dd");

                    conn.Update(vorhanden);

                    PreisInHistorieUebernehmen(conn, vorhanden);
                }
            }

            // Neuen oder bearbeiteten Preis speichern
            if (produktpreis.Id == 0)
            {
                conn.Insert(produktpreis);
            }
            else
            {
                conn.Update(produktpreis);
            }

            // Neuen Preisstand historisch sichern
            PreisInHistorieUebernehmen(conn, produktpreis);
        });
        await LokaleSicherungErstellenAsync();
    }
    public async Task ProduktpreisDeaktivierenAsync(int produktpreisId)
    {
        await InitialisierenAsync();

        if (produktpreisId <= 0)
            return;

        await datenbank.RunInTransactionAsync(conn =>
        {
            Produktpreis? preis = conn.Find<Produktpreis>(produktpreisId);

            if (preis == null || !preis.IstAktiv)
                return;

            // Bisherigen Preisstand in der Historie sichern
            PreisInHistorieUebernehmen(conn, preis);

            // Angebot deaktivieren
            preis.IstAktiv = false;
            preis.ErfasstAm = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            conn.Update(preis);
        });

        await LokaleSicherungErstellenAsync();
    }
    public async Task<List<PreisHistorie>> PreisHistorieLadenAsync(int produktId, int haendlerId)
    {
        await InitialisierenAsync();

        return await datenbank
            .Table<PreisHistorie>()
            .Where(h =>
                h.ProduktId == produktId &&
                h.HaendlerId == haendlerId)
            .OrderByDescending(h => h.ErfasstAm)
            .ToListAsync();
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
            Kategorie? vorhanden = await datenbank
                .Table<Kategorie>()
                .Where(k => k.Name == name)
                .FirstOrDefaultAsync();

            if (vorhanden == null)
            {
                await datenbank.InsertAsync(new Kategorie
                {
                    Name = name
                });
            }
        }
    }

    public async Task<List<Kategorie>> AlleKategorieEintraegeLadenAsync()
    {
        await InitialisierenAsync();

        return await datenbank
            .Table<Kategorie>()
            .OrderBy(k => k.Name)
            .ToListAsync();
    }

    public async Task KategorieSpeichernAsync(Kategorie kategorie)
    {
        await InitialisierenAsync();

        if (kategorie.Id == 0)
        {
            await datenbank.InsertAsync(kategorie);

        }
        else
        {
            await datenbank.UpdateAsync(kategorie);
        }
        await LokaleSicherungErstellenAsync();
    }

    public async Task<bool> KategorieUmbenennenAsync(Kategorie kategorie, string neuerName)
    {
        await InitialisierenAsync();

        string alterName = kategorie.Name.Trim();
        neuerName = neuerName.Trim();

        if (string.IsNullOrWhiteSpace(neuerName))
            return false;

        // Prüfen ob der neue Name bereits vergene ist

        List<Kategorie> kategorien =
            await datenbank.Table<Kategorie>().ToListAsync();

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
            await datenbank.Table<Einkaufsartikel>().ToListAsync();

        List<Produkt> produkte =
            await datenbank.Table<Produkt>().ToListAsync();

        await datenbank.RunInTransactionAsync(conn =>
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

        await LokaleSicherungErstellenAsync();
        return true;
    }

    public async Task<List<Einkaufsartikel>> AlleArtikelLadenAsync()
    {
        return await datenbank.Table<Einkaufsartikel>().ToListAsync();
    }

    public async Task ArtikelSpeichernAsync(Einkaufsartikel artikel)
    {
        int ergebnis;

        if (artikel.Id == 0)
        {
            ergebnis = await datenbank.InsertAsync(artikel);
        }
        else
        {
            ergebnis = await datenbank.UpdateAsync(artikel);
        }

        System.Diagnostics.Debug.WriteLine(
            $"SQLite-Speichern: ID={artikel.Id}, " +
            $"Menge={artikel.Menge}, Einheit={artikel.Einheit}, " +
            $"Ergebnis={ergebnis}");

        await LokaleSicherungErstellenAsync();
    }

    public async Task EinkaufsartikelHaendlerAktualisierenAsync(int produktId,int? haendlerId)
    {
        List<Einkaufsartikel> artikel = await datenbank
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
            eintrag.AusgewaehlterHaendlerId = haendlerId;
            eintrag.HaendlerAnzeige = haendlerId.HasValue
                ? (await datenbank.Table<Haendler>()
                    .Where(h => h.Id == haendlerId.Value)
                    .FirstOrDefaultAsync())?.Name ?? string.Empty
                : string.Empty;

            eintrag.Stueckpreis = aktuellerPreis?.PreisEuro ?? 0m;

            await datenbank.UpdateAsync(eintrag);
        }

        await LokaleSicherungErstellenAsync();
    }

    public async Task ArtikelLoeschenAsync(Einkaufsartikel artikel)
    {
        await datenbank.DeleteAsync(artikel);
        await LokaleSicherungErstellenAsync();
    }

    public async Task<List<Produkt>> AlleProdukteLadenAsync()
    {
        return await datenbank.Table<Produkt>().ToListAsync();

    }

    public async Task<List<string>> AlleKategorienLadenAsync()
    {
        await InitialisierenAsync();

        List<Produkt> produkte = await datenbank.Table<Produkt>().ToListAsync();

        return produkte
            .Select(p => p.Kategorie)
            .Where(k => !string.IsNullOrWhiteSpace(k))
            .Distinct()
            .OrderBy(k => k)
            .ToList();
    }

    public async Task<Produkt?> ProduktFindenAsync(string name)
    {

        static string Normalisieren(string text)
        {
            return string.Concat(
                text.Where(c => !char.IsWhiteSpace(c))).ToLowerInvariant();
        }


        string suchname = Normalisieren(name);

        List<Produkt> produkte =
            await datenbank.Table<Produkt>().ToListAsync();

        // Den vollständigen Namen vergleichen

        Produkt? direktErgebnis = produkte.FirstOrDefault(
            p => Normalisieren(p.Name) == suchname);

        if (direktErgebnis != null)
            return direktErgebnis;

        // Einfache Wortendungen brücksichtigen

        string grundform = suchname;

        if (grundform.EndsWith("en"))
            grundform = grundform[..^2];
        else if (grundform.EndsWith("e"))
            grundform = grundform[..^1];
        else if (grundform.EndsWith("n"))
            grundform = grundform[..^1];

        return produkte.FirstOrDefault(
            p => Normalisieren(p.Name) == grundform);
    }

    public async Task ProduktSpeichernAsync(Produkt produkt)
    {
        if (string.IsNullOrWhiteSpace(produkt.Name))
        {
            throw new ArgumentException("Der Produktname darf nicht leer sein.");
        }

        string Normalisieren(string? wert)
        {
            return string.IsNullOrWhiteSpace(wert)
                ? string.Empty
                : wert.Trim().ToLowerInvariant();
        }

        string name = Normalisieren(produkt.Name);
        string marke = Normalisieren(produkt.Marke);
        string variante = Normalisieren(produkt.Variante);
        string packung = produkt.Packungsgroesse?.ToString(
            "0.################",
            System.Globalization.CultureInfo.InvariantCulture) ?? "";
        string einheit = Normalisieren(produkt.Basiseinheit);

        produkt.ProduktSchluessel =
            string.Join("|", name, marke, variante, packung, einheit);

        var vorhandenesProdukt = await datenbank
            .Table<Produkt>()
            .Where(p => p.ProduktSchluessel == produkt.ProduktSchluessel)
            .FirstOrDefaultAsync();

        if (vorhandenesProdukt != null &&
            vorhandenesProdukt.Id != produkt.Id)
        {
            throw new InvalidOperationException(
                "Dieses Produkt existiert bereits.");
        }

        if (produkt.Id == 0)
        {
            await datenbank.InsertAsync(produkt);
        }
        else
        {
            await datenbank.UpdateAsync(produkt);
        }
        await LokaleSicherungErstellenAsync();
    }

    public async Task<Einkaufsartikel?> ArtikelLadenNachIdAsync(int id)
    {
        return await datenbank
            .Table<Einkaufsartikel>()
            .Where(a => a.Id == id)
            .FirstOrDefaultAsync();
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
}
