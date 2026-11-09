using Einkaufsliste.Models;
using Einkaufsliste.Interfaces;

namespace Einkaufsliste.Services;

public class EinkauflistenDatenbank
{
    private SQLiteAsyncConnection _datenbank;
    private readonly string datenbankPfad =
        Path.Combine(FileSystem.AppDataDirectory, "einkaufliste.db3");
    private readonly IDBBackupService _backupService;

    
    public EinkauflistenDatenbank(IDBBackupService backupService)
    {
        _datenbank = new SQLiteAsyncConnection(datenbankPfad);
        _backupService = backupService;
    }
   
     public async Task InitialisierenAsync()
    {
        // Einkaufslisten
        await _datenbank.CreateTableAsync<Einkaufsartikel>();
        await _datenbank.CreateTableAsync<Kategorie>();

        // Produktverwaltung
        await _datenbank.CreateTableAsync<Produkt>();
        // Migration: Bevorzugten Händler beim Produkt ergänzen
        var produktSpalten =
            await _datenbank.GetTableInfoAsync("Produkt");

        if (!produktSpalten.Any(s => s.Name == "BevorzugterHaendlerId"))
        {
            await _datenbank.ExecuteAsync(
                "ALTER TABLE Produkt ADD COLUMN BevorzugterHaendlerId INTEGER NULL");
        }

        // Migration: Ausgewählten Händler beim Einkaufsartikel ergänzen
        var artikelSpalten =
            await _datenbank.GetTableInfoAsync("Einkaufsartikel");

        if (!artikelSpalten.Any(s => s.Name == "AusgewaehlterHaendlerId"))
        {
            await _datenbank.ExecuteAsync(
                "ALTER TABLE Einkaufsartikel ADD COLUMN AusgewaehlterHaendlerId INTEGER NULL");
        }

        // Preisvergleich
        await _datenbank.CreateTableAsync<Haendler>();
        await _datenbank.CreateTableAsync<Produktpreis>();
        await _datenbank.CreateTableAsync<PreisHistorie>();

        // Verknüpfung zwischen Einkaufsartikel und Produkt
        var spalten = await _datenbank.GetTableInfoAsync("Einkaufsartikel");

        if (!spalten.Any(s => s.Name == "ProduktId"))
        {
            await _datenbank.ExecuteAsync(
                "ALTER TABLE Einkaufsartikel ADD COLUMN ProduktId INTEGER NULL");
        }
        // Migration PreisHistorie: Versionsnummer ergänzen
        var historieSpalten =
            await _datenbank.GetTableInfoAsync("PreisHistorie");

        if (!historieSpalten.Any(s => s.Name == "Versionsnummer"))
        {
            await _datenbank.ExecuteAsync(
                "ALTER TABLE PreisHistorie ADD COLUMN Versionsnummer INTEGER NOT NULL DEFAULT 0");
        }
    }

    public async Task HaendlerSpeichernAsync(Haendler haendler)
    {
        await InitialisierenAsync();

        if (haendler == null)
            return;

        haendler.Name = haendler.Name.Trim();

        if (string.IsNullOrWhiteSpace(haendler.Name))
            return;

        if (haendler.Id == 0)
            await _datenbank.InsertAsync(haendler);
        else
            await _datenbank.UpdateAsync(haendler);

        await _backupService.LokaleSicherungErstellenAsync();
    }

    public async Task<List<Haendler>> AlleHaendlerVerwaltenLadenAsync()
    {
        await InitialisierenAsync();

        return await _datenbank.QueryAsync<Haendler>(
            "SELECT * FROM Haendler ORDER BY name COLLATE NOCASE ASC");
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

            var vorhanden = await _datenbank
                .Table<Haendler>()
                .Where(h => h.Name == name)
                .FirstOrDefaultAsync();

            if (vorhanden == null)
            {
                await _datenbank.InsertAsync(new Haendler
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

        return await _datenbank
            .Table<Haendler>()
            .Where(h => h.IstAktiv)
            .OrderBy(h => h.Sortierung)
            .ToListAsync();
    }
    public async Task<List<Produktpreis>> AlleProduktpreiseLadenAsync(int produktId)
    {
        await InitialisierenAsync();

        return await _datenbank
            .Table<Produktpreis>()
            .Where(p => p.ProduktId == produktId && p.IstAktiv)
            .ToListAsync();
    }
    public async Task<Produktpreis?> AktuellenProduktpreisLadenAsync(int produktId, int haendlerId, string preisart)
    {
        await InitialisierenAsync();

        string heute = DateTime.Now.ToString("yyyy-MM-dd");

        List<Produktpreis> preise = await _datenbank
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

        await _datenbank.RunInTransactionAsync(conn =>
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
        await _backupService.LokaleSicherungErstellenAsync();
    }
    public async Task ProduktpreisDeaktivierenAsync(int produktpreisId)
    {
        await InitialisierenAsync();

        if (produktpreisId <= 0)
            return;

        await _datenbank.RunInTransactionAsync(conn =>
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

        await _backupService.LokaleSicherungErstellenAsync();
    }
    public async Task<List<PreisHistorie>> PreisHistorieLadenAsync(int produktId, int haendlerId)
    {
        await InitialisierenAsync();

        return await _datenbank
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
            Kategorie? vorhanden = await _datenbank
                .Table<Kategorie>()
                .Where(k => k.Name == name)
                .FirstOrDefaultAsync();

            if (vorhanden == null)
            {
                await _datenbank.InsertAsync(new Kategorie
                {
                    Name = name
                });
            }
        }
    }

    public async Task<List<Kategorie>> AlleKategorieEintraegeLadenAsync()
    {
        await InitialisierenAsync();

        return await _datenbank
            .Table<Kategorie>()
            .OrderBy(k => k.Name)
            .ToListAsync();
    }

    public async Task KategorieSpeichernAsync(Kategorie kategorie)
    {
        await InitialisierenAsync();

        if (kategorie.Id == 0)
        {
            await _datenbank.InsertAsync(kategorie);

        }
        else
        {
            await _datenbank.UpdateAsync(kategorie);
        }
        await _backupService.LokaleSicherungErstellenAsync();
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
            await _datenbank.Table<Kategorie>().ToListAsync();

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
            await _datenbank.Table<Einkaufsartikel>().ToListAsync();

        List<Produkt> produkte =
            await _datenbank.Table<Produkt>().ToListAsync();

        await _datenbank.RunInTransactionAsync(conn =>
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

    public async Task EinkaufsartikelHaendlerAktualisierenAsync(int produktId,int? haendlerId)
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

    public async Task<List<Produkt>> AlleProdukteLadenAsync()
    {
        return await _datenbank.Table<Produkt>().ToListAsync();

    }

    public async Task<List<string>> AlleKategorienLadenAsync()
    {
        await InitialisierenAsync();

        List<Produkt> produkte = await _datenbank.Table<Produkt>().ToListAsync();

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
            await _datenbank.Table<Produkt>().ToListAsync();

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

       produkt.ProduktSchluessel = ProduktSchluesselErstellen(produkt);

        var vorhandenesProdukt = await _datenbank
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
            await _datenbank.InsertAsync(produkt);
        }
        else
        {
            await _datenbank.UpdateAsync(produkt);
        }
        await _backupService.LokaleSicherungErstellenAsync();
    }
    public async Task ProduktZusammenfuehrenAsync(int quellProduktId,int zielProduktId)
    {
        if (quellProduktId == zielProduktId)
        {
            throw new ArgumentException(
                "Quell- und Zielprodukt müssen verschieden sein.");
        }

        await InitialisierenAsync();

        await _datenbank.RunInTransactionAsync(verbindung =>
        {
            Produkt quellProdukt =
                verbindung.Find<Produkt>(quellProduktId);

            Produkt zielProdukt =
                verbindung.Find<Produkt>(zielProduktId);

            if (quellProdukt == null || zielProdukt == null)
            {
                throw new InvalidOperationException(
                    "Quell- oder Zielprodukt wurde nicht gefunden.");
            }

            foreach (Einkaufsartikel artikel in verbindung
                .Table<Einkaufsartikel>()
                .Where(a => a.ProduktId == quellProduktId)
                .ToList())
            {
                artikel.ProduktId = zielProduktId;
                artikel.Name = zielProdukt.Name;
                artikel.Kategorie = zielProdukt.Kategorie;
                verbindung.Update(artikel);
            }

            foreach (Produktpreis preis in verbindung
                .Table<Produktpreis>()
                .Where(p => p.ProduktId == quellProduktId)
                .ToList())
            {
                preis.ProduktId = zielProduktId;
                verbindung.Update(preis);
            }

            foreach (PreisHistorie historie in verbindung
                .Table<PreisHistorie>()
                .Where(h => h.ProduktId == quellProduktId)
                .ToList())
            {
                historie.ProduktId = zielProduktId;
                verbindung.Update(historie);
            }

            zielProdukt.IstAktiv = true;
            verbindung.Update(zielProdukt);

            quellProdukt.IstAktiv = false;
            verbindung.Update(quellProdukt);
        });

        await _backupService.LokaleSicherungErstellenAsync();
    }
    public async Task<Einkaufsartikel?> ArtikelLadenNachIdAsync(int id)
    {
        return await _datenbank
            .Table<Einkaufsartikel>()
            .Where(a => a.Id == id)
            .FirstOrDefaultAsync();
    }
    public async Task<Produkt?> ProduktMitGleichemSchluesselLadenAsync(Produkt produkt)
    {
        string schluessel = ProduktSchluesselErstellen(produkt);

        return await _datenbank
            .Table<Produkt>()
            .Where(p =>
                p.Id != produkt.Id &&
                p.ProduktSchluessel == schluessel)
            .FirstOrDefaultAsync();
    }
    private static string ProduktSchluesselErstellen(Produkt produkt)
    {
        static string Normalisieren(string? wert)
        {
            return string.IsNullOrWhiteSpace(wert)
                ? string.Empty
                : wert.Trim().ToLowerInvariant();
        }

        string name = Normalisieren(produkt.Name);
        string marke = Normalisieren(produkt.Marke);
        string variante = Normalisieren(produkt.Variante);
        string packung = produkt.Packungsgroesse.ToString(
            "0.################",
            System.Globalization.CultureInfo.InvariantCulture);
        string einheit = Normalisieren(produkt.Basiseinheit);

        return string.Join("|", name, marke, variante, packung, einheit);
    }
}
