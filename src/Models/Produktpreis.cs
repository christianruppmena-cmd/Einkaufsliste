namespace Einkaufsliste.Models;

public class Produktpreis
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    public int ProduktId { get; set; }
    public int HaendlerId { get; set; }
    // Preis in Cent, z. B. 299 = 2,99 €
    public int PreisInCent { get; set; }
    // Normalpreis oder Angebot
    [NotNull]
    public string Preisart { get; set; } = "Normalpreis";
    // Datum der Erfassung
    [NotNull]
    public string ErfasstAm { get; set; } = string.Empty;
    // Gültigkeitszeitraum im Format yyyy-MM-dd
    [NotNull]
    public string GueltigVon { get; set; } = string.Empty;
    public string GueltigBis { get; set; }
    public bool IstAktiv { get; set; } = true;
    [Ignore]
    public decimal PreisEuro => PreisInCent / 100m;
}
