using SQLite;

namespace Einkaufsliste.Models;

public class Kategorie
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Unique]
    public string Name { get; set; } = string.Empty;
}