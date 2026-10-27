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

        name = name.Trim();

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
}