
using Einkaufsliste.Models;

namespace Einkaufsliste;

public partial class ArtikelEingabePage : ContentPage
{
    private readonly TaskCompletionSource<Einkaufsartikel?> ergebnisQuelle = new();

    public Task<Einkaufsartikel?> ErgebnisTask => ergebnisQuelle.Task;

    public ArtikelEingabePage(string artikelName)
    {
        InitializeComponent();

        ArtikelNameEingabe.Text = artikelName;
        EinheitAuswahl.SelectedItem = "Stück";
    }

    private async void OnHinzufuegenClicked(object sender, EventArgs e)
    {
        string name = ArtikelNameEingabe.Text?.Trim() ?? string.Empty;
        string menge = MengeEingabe.Text?.Trim() ?? string.Empty;
        string? einheit = EinheitAuswahl.SelectedItem?.ToString();
        string details = DetailsEingabe.Text?.Trim() ?? string.Empty;
        string preisText = StueckpreisEingabe.Text?.Trim() ?? string.Empty;

        decimal stueckpreis = 0m;

        if (!string.IsNullOrWhiteSpace(preisText) && !decimal.TryParse(preisText, System.Globalization.NumberStyles.Number,System.Globalization.CultureInfo.CurrentCulture, out stueckpreis))
        {
            await DisplayAlertAsync(
                "Ungültiger Preis"
                , "Bitte einen gültigen Preis eingeben, z. B. 1,29.",
                "OK");
            return;
        }

        if(stueckpreis < 0)
        {
            await DisplayAlertAsync(
                "Ungültiger Preis",
                "Der Preis darf nicht negativ sein.",
                "OK");
            return;
        }



        if (string.IsNullOrWhiteSpace(name))
        {
            await DisplayAlertAsync("Hinweis", "Bitte einen Artikelnamen eingeben.", "OK");
            return;
        }

        if (string.IsNullOrWhiteSpace(menge) ||
            !decimal.TryParse(
                menge,
                System.Globalization.NumberStyles.Number,
                System.Globalization.CultureInfo.CurrentCulture,
                out decimal mengeWert) ||
            mengeWert <= 0)
        {
            await DisplayAlertAsync("Hinweis", "Bitte eine gültige Menge eingeben.", "OK");
            return;
        }

        if (string.IsNullOrWhiteSpace(einheit))
        {
            await DisplayAlertAsync("Hinweis", "Bitte eine Einheit auswählen.", "OK");
            return;
        }

        Einkaufsartikel artikel = new()
        {
            Name = name,
            Menge = menge,
            Einheit = einheit,
            Hinweis = details,
            Stueckpreis = stueckpreis,
            IstErledigt = false
        };

        ergebnisQuelle.TrySetResult(artikel);
        await Navigation.PopModalAsync();
    }

    private async void OnAbbrechenClicked(object sender, EventArgs e)
    {
        ergebnisQuelle.TrySetResult(null);
        await Navigation.PopModalAsync();
    }
}
