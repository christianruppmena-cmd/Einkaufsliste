using SQLite;

namespace Einkaufsliste.Models;

public class Produktpreis
{
    [PrimaryKey, AutoIncrement]

    public int Id { get; set; }

    // Produkt und Filiale

    public int ProduktId { get; set; }
    public int FilialeId { get; set; }

    // Preis in Euro
    public decimal Preis { get; set; }
    // Normalpreis oder Angebot
    public string Preisart { get; set; } = "Normalpreis";
    // Manuell oder aus angebot übernommen
    public string Quelle { get; set; } = "Manuell";
    // Optionale Angaben zur Packungsgöße
    public decimal? Packungsgroesse { get; set; }

    public string Basiseinheit { get; set; } = "Stück";

    // Zeitpunkt der erfassung
    public DateTime ErfasstAm { get; set; }
    // Gültigkeit des Preises
    public DateTime? GueltigVon { get; set; }
    public DateTime? GueltigBis { get; set; }

    // Kennzeichnet Produktzuordnung
    public bool ZuordnunBestaetigt { get; set; } = true;
}