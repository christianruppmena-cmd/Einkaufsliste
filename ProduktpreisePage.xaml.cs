using Einkaufsliste.Models;
using Einkaufsliste.Services;

namespace Einkaufsliste;

public partial class ProduktpreisePage : ContentPage
{
    private readonly EinkauflistenDatenbank datenbank;
    private readonly Produkt produkt;

    public ProduktpreisePage(EinkauflistenDatenbank datenbank,Produkt produkt)
    {
        InitializeComponent();

        this.datenbank = datenbank;
        this.produkt = produkt;

        ProduktNameLabel.Text = produkt.Name;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        await LadePreiseAsync();
    }

    private async Task LadePreiseAsync()
    {
        List<Produktpreis> preise =
            await datenbank.AlleProduktpreiseLadenAsync(produkt.Id);

        List<Haendler> haendler =
            await datenbank.AlleHaendlerLadenAsync();

        HaendlerPicker.ItemsSource = haendler;
        HaendlerPicker.ItemDisplayBinding = new Binding(nameof(Haendler.Name));

        var anzeige = haendler.Select(h =>
        {
            var haendlerPreise = preise
                .Where(p => p.HaendlerId == h.Id)
                .ToList();

            return new
            {
                Haendler = h.Name,
                Normalpreis = haendlerPreise
                    .FirstOrDefault(p => p.Preisart == "Normalpreis")
                    ?.PreisEuro.ToString("F2") + " €",

                Angebot = haendlerPreise
                    .FirstOrDefault(p => p.Preisart == "Angebot")
                    ?.PreisEuro.ToString("F2") + " €"
            };
        }).ToList();

        HaendlerPreisListe.ItemsSource = anzeige;
    }
    private void OnGueltigBisAktivierenChanged(object sender, CheckedChangedEventArgs e)
    {
        GueltigBisPicker.IsEnabled = e.Value;
    }
    private async void OnPreisSpeichernClicked(object sender,EventArgs e)
    {
        if (HaendlerPicker.SelectedItem is not Haendler haendler)
        {
            await DisplayAlertAsync(
                "Hinweis",
                "Bitte wähle einen Händler aus.",
                "OK");
            return;
        }

        if (PreisartPicker.SelectedItem is not string preisart)
        {
            await DisplayAlertAsync(
                "Hinweis",
                "Bitte wähle eine Preisart aus.",
                "OK");
            return;
        }

        if (!decimal.TryParse(
            PreisEntry.Text,
            System.Globalization.NumberStyles.Number,
            System.Globalization.CultureInfo.CurrentCulture,
            out decimal preisEuro) || preisEuro <= 0)
        {
            await DisplayAlertAsync(
                "Ungültiger Preis",
                "Bitte gib einen gültigen Preis ein.",
                "OK");
            return;
        }

        string? gueltigBis = GueltigBisAktivieren.IsChecked? string.Format(System.Globalization.CultureInfo.InvariantCulture,"{0:yyyy-MM-dd}", GueltigBisPicker.Date) : null;

        var neuerPreis = new Produktpreis
        {
            ProduktId = produkt.Id,
            HaendlerId = haendler.Id,
            PreisInCent = (int)Math.Round(
                preisEuro * 100,
                MidpointRounding.AwayFromZero),
            Preisart = preisart,
            GueltigVon = string.Format(System.Globalization.CultureInfo.InvariantCulture,"{0:yyyy-MM-dd}", GueltigVonPicker.Date),
            GueltigBis = gueltigBis,
            IstAktiv = true
        };

        await datenbank.ProduktpreisSpeichernAsync(neuerPreis);
        

        PreisEntry.Text = string.Empty;
        await LadePreiseAsync();

        await DisplayAlertAsync(
            "Gespeichert",
            "Der Preis wurde erfolgreich gespeichert.",
            "OK");
    }


}