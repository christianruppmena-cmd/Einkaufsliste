using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using Einkaufsliste.Models;

namespace Einkaufsliste;

public class Artikelgruppe : ObservableCollection<Einkaufsartikel>
{
    public string Kategorie { get; }

    public string Ueberschrift => $"{Kategorie.ToUpper()} ({Count})";

    public Artikelgruppe(string kategorie)
    {
        Kategorie = kategorie;

        CollectionChanged += BeiArtikelAenderung;
    }

    private void BeiArtikelAenderung(object? sender, NotifyCollectionChangedEventArgs e)
    {
        OnPropertyChanged(
            new PropertyChangedEventArgs(nameof(Ueberschrift)));
    }
}
