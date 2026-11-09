using Einkaufsliste.Services;
using Einkaufsliste.Models;
using System.ComponentModel;
using System.Globalization;

using Einkaufsliste;
using Einkaufsliste.Application;
using Einkaufsliste.Application.Pages;
using Einkaufsliste.Application.Pages.Produkte;

namespace Einkaufsliste.Application.Pages.Produkte;

public partial class ProduktpreisePage : ContentPage
{
    private readonly EinkauflistenDatenbank datenbank;
    private readonly Produkt produkt;

    public ProduktpreisePage(EinkauflistenDatenbank datenbank, Produkt produkt)
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
        List<Haendler> haendler =
            await datenbank.AlleHaendlerLadenAsync();

        List<HaendlerPreisEintrag> anzeige = new();

        foreach (Haendler h in haendler)
        {
            Produktpreis normalpreis =
                await datenbank.AktuellenProduktpreisLadenAsync(
                    produkt.Id,
                    h.Id,
                    "Normalpreis");

            Produktpreis angebot =
                await datenbank.AktuellenProduktpreisLadenAsync(
                    produkt.Id,
                    h.Id,
                    "Angebot");

            anzeige.Add(new HaendlerPreisEintrag
            {
                HaendlerId = h.Id,
                Haendler = h.Name,
                NormalpreisId = normalpreis?.Id ?? 0,
                AngebotId = angebot?.Id ?? 0,

                NormalpreisText = normalpreis?.PreisEuro.ToString(
                    "0.00",
                    CultureInfo.CurrentCulture) ?? "",

                AngebotText = angebot?.PreisEuro.ToString(
                    "0.00",
                    CultureInfo.CurrentCulture) ?? "",

                GueltigBisAktiviert =
                    angebot != null &&
                    !string.IsNullOrWhiteSpace(angebot.GueltigBis),

                GueltigBis =
                    DateTime.TryParse(
                        angebot?.GueltigBis,
                        CultureInfo.InvariantCulture,
                        out DateTime datum)
                        ? datum
                        : DateTime.Today
            });
        }

        HaendlerPreisListe.ItemsSource = anzeige;
    }

    private async void OnHaendlerPreisSpeichernClicked(object sender, EventArgs e)
    {
        if (sender is not Button button ||
            button.BindingContext is not HaendlerPreisEintrag eintrag)
        {
            return;
        }

        bool normalpreisVorhanden =
            !string.IsNullOrWhiteSpace(eintrag.NormalpreisText);

        bool angebotVorhanden =
            !string.IsNullOrWhiteSpace(eintrag.AngebotText);

        bool angebotDeaktivieren = angebotVorhanden && decimal.TryParse(
                eintrag.AngebotText,
                NumberStyles.Number,
                CultureInfo.CurrentCulture,
                out decimal angebotEingabe) &&
            angebotEingabe == 0;

        if (!normalpreisVorhanden && !angebotVorhanden)
        {
            await DisplayAlertAsync(
                "Keine Preise",
                "Bitte gib mindestens einen Preis ein.",
                "OK");
            return;
        }

        if (normalpreisVorhanden &&
            (!decimal.TryParse(
                eintrag.NormalpreisText,
                NumberStyles.Number,
                CultureInfo.CurrentCulture,
                out decimal normalpreisPruefung) ||
             normalpreisPruefung <= 0))
        {
            await DisplayAlertAsync(
                "Ungültiger Normalpreis",
                "Bitte gib einen gültigen Normalpreis ein.",
                "OK");
            return;
        }

        if (angebotVorhanden &&
            (!decimal.TryParse(
                eintrag.AngebotText,
                NumberStyles.Number,
                CultureInfo.CurrentCulture,
                out decimal angebotPruefung) ||
             angebotPruefung < 0))
        {
            await DisplayAlertAsync(
                "Ungültiger Angebotspreis",
                "Bitte gib einen gültigen Angebotspreis ein.",
                "OK");
            return;
        }

        try
        {
            string heute = DateTime.Today.ToString(
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture);

            if (normalpreisVorhanden)
            {
                decimal.TryParse(
                    eintrag.NormalpreisText,
                    NumberStyles.Number,
                    CultureInfo.CurrentCulture,
                    out decimal normalpreis);

                var neuerPreis = new Produktpreis
                {
                    Id = eintrag.NormalpreisId,
                    ProduktId = produkt.Id,
                    HaendlerId = eintrag.HaendlerId,
                    PreisInCent = (int)Math.Round(
                        normalpreis * 100,
                        MidpointRounding.AwayFromZero),
                    Preisart = "Normalpreis",
                    GueltigVon = heute,
                    GueltigBis = null,
                    IstAktiv = true
                };

                await datenbank.ProduktpreisSpeichernAsync(neuerPreis);
            }

            if (angebotDeaktivieren)
            {
                if (eintrag.AngebotId > 0)
                {
                    await datenbank.ProduktpreisDeaktivierenAsync(
                        eintrag.AngebotId);
                }
            }
            else if (angebotVorhanden)
            {
                decimal.TryParse(
                    eintrag.AngebotText,
                    NumberStyles.Number,
                    CultureInfo.CurrentCulture,
                    out decimal angebot);

                var neuerPreis = new Produktpreis
                {
                    Id = eintrag.AngebotId,
                    ProduktId = produkt.Id,
                    HaendlerId = eintrag.HaendlerId,
                    PreisInCent = (int)Math.Round(
                        angebot * 100,
                        MidpointRounding.AwayFromZero),
                    Preisart = "Angebot",
                    GueltigVon = heute,
                    GueltigBis = eintrag.GueltigBisAktiviert
                        ? eintrag.GueltigBis.ToString(
                            "yyyy-MM-dd",
                            CultureInfo.InvariantCulture)
                        : null,
                    IstAktiv = true
                };

                await datenbank.ProduktpreisSpeichernAsync(neuerPreis);
            }

            if (produkt.BevorzugterHaendlerId == eintrag.HaendlerId)
            {
                await datenbank.EinkaufsartikelHaendlerAktualisierenAsync(
                    produkt.Id,
                    eintrag.HaendlerId);
            }

            await LadePreiseAsync();

            await DisplayAlertAsync(
                "Gespeichert",
                $"Die Preise für {eintrag.Haendler} wurden gespeichert.",
                "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync(
                "Fehler",
                $"Die Preise konnten nicht gespeichert werden:\n{ex.Message}",
                "OK");
        }
    }
}

public class HaendlerPreisEintrag : System.ComponentModel.INotifyPropertyChanged
{
    public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;

    private bool gueltigBisAktiviert;

    public int HaendlerId { get; set; }

    public string Haendler { get; set; } = "";

    public int NormalpreisId { get; set; }

    public int AngebotId { get; set; }

    public string NormalpreisText { get; set; } = "";

    public string AngebotText { get; set; } = "";

    public bool GueltigBisAktiviert
    {
        get => gueltigBisAktiviert;
        set
        {
            if (gueltigBisAktiviert == value)
                return;

            gueltigBisAktiviert = value;

            PropertyChanged?.Invoke(
                this,
                new System.ComponentModel.PropertyChangedEventArgs(
                    nameof(GueltigBisAktiviert)));
        }
    }

    public DateTime GueltigBis { get; set; } = DateTime.Today;
}