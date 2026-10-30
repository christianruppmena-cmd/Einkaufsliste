
using SQLite;

namespace Einkaufsliste.Models;

public class Produkt
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [NotNull]
    public string Name { get; set; } = string.Empty;

    public string Marke { get; set; } = string.Empty;

    public string Variante { get; set; } = string.Empty;

    public string Kategorie { get; set; } = "Sonstiges";

    public decimal? Packungsgroesse { get; set; }

    public string Basiseinheit { get; set; } = string.Empty;

    // Lokaler Dateipfad des Produktbildes
    public string? BildPfad { get; set; }

    public bool IstAktiv { get; set; } = true;
}
