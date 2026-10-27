using SQLite;

namespace Einkaufsliste.Models;

public class Produkt
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    [Unique]
    public string Name { get; set; } = string.Empty;
    public string Kategorie { get; set; } = "Sonstiges";
}