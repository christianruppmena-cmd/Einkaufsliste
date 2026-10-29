using Einkaufsliste.Models;

namespace Einkaufsliste;

public partial class ArtikeldetailsPage : ContentPage
{
    private readonly Einkaufsartikel artikel;
    private readonly TaskCompletionSource<bool> ergebnis = new TaskCompletionSource<bool>();
    public Task<bool> ErgebnisTask => ergebnis.Task;

    public bool Gespeichert { get; private set; }

    public ArtikeldetailsPage(Einkaufsartikel artikel)
    {
        InitializeComponent();

        this.artikel = artikel;

        NameEingabe.Text = artikel.Name;
        MengeEingabe.Text = artikel.Menge;
        HinweisEingabe.Text = artikel.Hinweis;
        StueckpreisEingabe.Text = artikel.Stueckpreis.ToString(System.Globalization.CultureInfo.CurrentCulture);

        int einheitIndex = EinheitAuswahl.Items.IndexOf(artikel.Einheit);

        if (einheitIndex >= 0)
        {
            EinheitAuswahl.SelectedIndex = einheitIndex;

        }
        else
        {
            EinheitAuswahl.SelectedItem = "Stück";
        }
    }

    private async void OnSpeichernClicked(object sender, EventArgs e)
  
        {
            string name = NameEingabe.Text?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(name))
            {
                await DisplayAlertAsync("Fehlender Artikelname", "Bitte gib einen Artikelnamen ein.", "OK");
                return;

            }

        string preisText = StueckpreisEingabe.Text?.Trim() ?? string.Empty;
        decimal stueckpreis = 0m;

        if (!string.IsNullOrWhiteSpace(preisText) && !decimal.TryParse(
            preisText,
            System.Globalization.NumberStyles.Number,
            System.Globalization.CultureInfo.CurrentCulture,
            out stueckpreis))
        {
            await DisplayAlertAsync(
                "Ungültiger Preis",
                "Bitte einen gültigen Preis eingeben, z. B. 1,29.",
                "OK");
            return;
        }

        if (stueckpreis <0)
        {
            await DisplayAlertAsync(
                "Ungültiger Preis",
                "Der Preis darf nicht negativ sein.",
                "OK");

        }
        artikel.Name = name;
        artikel.Menge = string.IsNullOrWhiteSpace(MengeEingabe.Text) ? "1" : MengeEingabe.Text.Trim();
        artikel.Einheit = EinheitAuswahl.SelectedItem?.ToString() ?? "Stück";
        
        artikel.Hinweis = HinweisEingabe.Text?.Trim() ?? string.Empty;
        artikel.Stueckpreis = stueckpreis;
        Gespeichert = true;
        await Navigation.PopModalAsync();
        ergebnis.TrySetResult(true);
    }

    private async void OnAbbrechenClicked(object sender, EventArgs e)
    {
        Gespeichert = false;
        await Navigation.PopModalAsync();

        ergebnis.TrySetResult(false);
    }
}