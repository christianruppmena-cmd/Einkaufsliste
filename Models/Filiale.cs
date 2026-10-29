
using SQLite;

namespace Einkaufsliste.Models;

public class Filiale
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public string Haendler { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Adresse { get; set; } = string.Empty;

    public bool IstAktiv { get; set; } = true;
}
