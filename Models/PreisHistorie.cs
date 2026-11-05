
using SQLite;

namespace Einkaufsliste.Models;

public class PreisHistorie
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public int ProduktpreisId { get; set; }

    public int Versionsnummer { get; set; }
    
    public int ProduktId { get; set; }

    public int HaendlerId { get; set; }

    // Gespeicherter Preis in Cent
    public int PreisInCent { get; set; }

    // Normalpreis oder Angebot
    [NotNull]
    public string Preisart { get; set; } = "Normalpreis";

    // Zeitpunkt der Erfassung
    [NotNull]
    public string ErfasstAm { get; set; } = string.Empty;

    public string GueltigVon { get; set; } = string.Empty;

    public string? GueltigBis { get; set; }

    [Ignore]
    public decimal PreisEuro => PreisInCent / 100m;
}
