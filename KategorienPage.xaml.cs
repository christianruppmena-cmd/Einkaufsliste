using Einkaufsliste.Models;
using Einkaufsliste.Services;

namespace Einkaufsliste;

public partial class KategorienPage : ContentPage
{
    private readonly EinkauflistenDatenbank datenbank = new();

    public KategorienPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        await KategorienLadenAsync();
    }

    private async Task KategorienLadenAsync()
    {
        await datenbank.InitialisierenAsync();

        List<Kategorie> kategorien =
            await datenbank.AlleKategorieEintraegeLadenAsync();

        KategorienListe.ItemsSource = kategorien;
    }

    private async void OnNeueKategorieClicked(object sender, EventArgs e)
    {
        string? name = await DisplayPromptAsync(
            "Neue Kategorie",
            "Wie soll die Kategorie heißen?");

        if (string.IsNullOrWhiteSpace(name))
            return;

        name = TextHelfer.Normalisieren(name);

        List<Kategorie> kategorien = await datenbank.AlleKategorieEintraegeLadenAsync();

        bool existiert = kategorien.Any(k => string.Equals(k.Name, name, StringComparison.OrdinalIgnoreCase));

        if(existiert)
        {
            await DisplayAlertAsync("Hinweis", "Diese Kategorie existiert bereits.", "OK");
            return;
        }
        await datenbank.KategorieSpeichernAsync(new Kategorie { Name = name });
        await KategorienLadenAsync();
    }

    private async void OnKategorieBearbeitenClicked(object sender, EventArgs e)
    {
        if (sender is not Button button || button.CommandParameter is not Kategorie kategorie)
            return;

        string? neuerName = await DisplayPromptAsync(
            "Kategorie bearbeiten",
            "Neuer Name für die Kategorie: ",
            initialValue: kategorie.Name,
            accept: "Speichern",
            cancel: "Abbrechen");

        if (string.IsNullOrWhiteSpace(neuerName))
            return;

        neuerName = TextHelfer.Normalisieren(neuerName);

        if (string.Equals(
            kategorie.Name,
            neuerName,
            StringComparison.Ordinal))
            return;

        bool gespeichert = await datenbank.KategorieUmbenennenAsync(
            kategorie, neuerName);

        if(!gespeichert)
        {
            await DisplayAlertAsync(
                "Hinweis",
                "Diese Kategorie exisitert bereits oder der Name ist ungültig.",
                "OK");
            return;
        }

        await KategorienLadenAsync();

        await DisplayAlertAsync(
            "Erfolgreich",
            "Die Kategorie wurde umbenannt.",
            "OK");
    }

}