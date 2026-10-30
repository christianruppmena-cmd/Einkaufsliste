
using SQLite;

namespace Einkaufsliste.Models;

public class Haendler
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Unique, NotNull]
    public string Name { get; set; } = string.Empty;

    public bool IstAktiv { get; set; } = true;

    public int Sortierung { get; set; }
}
