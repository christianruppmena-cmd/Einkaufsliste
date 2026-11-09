using Einkaufsliste.Models;
using Einkaufsliste.Interfaces;

namespace Einkaufsliste.Services;

public partial class EinkauflistenDatenbank
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

    

    

    

    
   
}
