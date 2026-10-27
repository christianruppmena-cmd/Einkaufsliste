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

        artikel.Name = name;
        artikel.Menge = string.IsNullOrWhiteSpace(MengeEingabe.Text) ? "1" : MengeEingabe.Text.Trim();
        artikel.Einheit = EinheitAuswahl.SelectedItem?.ToString() ?? "Stück";
        artikel.Hinweis = HinweisEingabe.Text?.Trim() ?? string.Empty;
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