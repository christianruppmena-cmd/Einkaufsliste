using Einkaufsliste.Services;
using Einkaufsliste.Models;
using System.Globalization;

namespace Einkaufsliste;

public partial class ProduktverwaltungPage : ContentPage
{
    private readonly EinkauflistenDatenbank datenbank;
    private List<Produkt> alleProdukte = new();
    private string? ausgewaehlterBildPfad;
    private Produkt? produktZumBearbeiten;
    private bool bildEntfernen;

    public ProduktverwaltungPage(EinkauflistenDatenbank datenbank)
    {
        InitializeComponent();

        this.datenbank = datenbank;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        await LadeKategorienAsync();
        await LadeProdukteAsync();
    }

    private async Task LadeKategorienAsync()
    {
        List<Kategorie> kategorien =
            await datenbank.AlleKategorieEintraegeLadenAsync();

        KategoriePicker.ItemsSource = kategorien
            .Select(k => k.Name)
            .ToList();
    }

    private async void OnProduktSpeichernClicked(object sender, EventArgs e)
    {
        string name = NameEntry.Text?.Trim() ?? "";
        string marke = MarkeEntry.Text?.Trim() ?? "";
        string variante = VarianteEntry.Text?.Trim() ?? "";

        string? kategorie = KategoriePicker.SelectedItem?.ToString();
        string? einheit = BasiseinheitPicker.SelectedItem?.ToString();

        if (string.IsNullOrWhiteSpace(name))
        {
            await DisplayAlertAsync("Eingabe fehlt",
                "Bitte gib einen Produktnamen ein.", "OK");
            return;
        }

        if (string.IsNullOrWhiteSpace(kategorie))
        {
            await DisplayAlertAsync("Eingabe fehlt",
                "Bitte wähle eine Kategorie aus.", "OK");
            return;
        }

        decimal? packungsgroesse = null;

        string packungText =
            PackungsgroesseEntry.Text?.Trim().Replace(',', '.') ?? "";

        if (!string.IsNullOrWhiteSpace(packungText))
        {
            if (!decimal.TryParse(
                packungText,
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out decimal packung) || packung <= 0)
            {
                await DisplayAlertAsync("Ungültige Packungsgröße",
                    "Bitte gib eine gültige Zahl größer als 0 ein.", "OK");
                return;
            }

            if (string.IsNullOrWhiteSpace(einheit))
            {
                await DisplayAlertAsync("Einheit fehlt",
                    "Bitte wähle eine Basiseinheit aus.", "OK");
                return;
            }

            packungsgroesse = packung;
        }

        string? altesBild = produktZumBearbeiten?.BildPfad;

        Produkt zuSpeicherndesProdukt =
            produktZumBearbeiten ?? new Produkt();

        zuSpeicherndesProdukt.Name = name;
        zuSpeicherndesProdukt.Marke = marke;
        zuSpeicherndesProdukt.Variante = variante;
        zuSpeicherndesProdukt.Kategorie = kategorie;
        zuSpeicherndesProdukt.Packungsgroesse = packungsgroesse;
        zuSpeicherndesProdukt.Basiseinheit = einheit ?? "";

        if (bildEntfernen)
        {
            zuSpeicherndesProdukt.BildPfad = null;
        }
        else if (ausgewaehlterBildPfad != null)
        {
            zuSpeicherndesProdukt.BildPfad = ausgewaehlterBildPfad;
        }

        try
        {
            await datenbank.ProduktSpeichernAsync(zuSpeicherndesProdukt);

            produktZumBearbeiten = null;
            ausgewaehlterBildPfad = null;
            bildEntfernen = false;

            await LadeProdukteAsync();

            await AltesProduktbildLoeschenAsync(
                altesBild,
                zuSpeicherndesProdukt.BildPfad);

            await DisplayAlertAsync(
                "Gespeichert",
                $"{name} wurde erfolgreich gespeichert.",
                "OK");

            NameEntry.Text = "";
            MarkeEntry.Text = "";
            VarianteEntry.Text = "";
            KategoriePicker.SelectedItem = null;
            PackungsgroesseEntry.Text = "";
            BasiseinheitPicker.SelectedItem = null;
        }
        catch (InvalidOperationException ex)
        {
            await DisplayAlertAsync(
                "Produkt bereits vorhanden", ex.Message, "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync(
                "Speicherfehler", ex.Message, "OK");
        }
    }

    private async Task LadeProdukteAsync()
    {
        alleProdukte = await datenbank.AlleProdukteLadenAsync();

        ProduktListe.ItemsSource = alleProdukte.Where(p => p.IstAktiv).OrderBy(p => p.Name).ToList();
    }

    private void OnProduktBearbeitenClicked(object sender, EventArgs e)
    {
        if (sender is not Button button ||
            button.BindingContext is not Produkt produkt)
        {
            return;
        }

        produktZumBearbeiten = produkt;
        bildEntfernen = false;

        NameEntry.Text = produkt.Name;
        MarkeEntry.Text = produkt.Marke;
        VarianteEntry.Text = produkt.Variante;

        KategoriePicker.SelectedItem = produkt.Kategorie;

        PackungsgroesseEntry.Text =
            produkt.Packungsgroesse?.ToString(
                System.Globalization.CultureInfo.CurrentCulture) ?? "";

        BasiseinheitPicker.SelectedItem = produkt.Basiseinheit;
    }

    private async void OnProduktbildAuswaehlenClicked(object sender, EventArgs e)
    {
        try
        {
            FileResult? foto = await MediaPicker.Default.PickPhotoAsync();

            if (foto == null)
                return;

            string? vorherigesBild = ausgewaehlterBildPfad;

            string dateiendung = Path.GetExtension(foto.FileName);

            string dateiname = $"{Guid.NewGuid()}{dateiendung}";

            string zielpfad = Path.Combine(
                FileSystem.AppDataDirectory,
                dateiname);

            await using Stream quelle = await foto.OpenReadAsync();
            await using FileStream ziel = File.Create(zielpfad);

            await quelle.CopyToAsync(ziel);

            ausgewaehlterBildPfad = zielpfad;
            bildEntfernen = false;

            // Vorheriges, noch nicht gespeichertes Bild aufräumen
            await AltesProduktbildLoeschenAsync(
                vorherigesBild,
                zielpfad);

            await DisplayAlertAsync(
                "Bild ausgewählt",
                "Das Produktbild wurde erfolgreich übernommen.",
                "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync(
                "Fehler",
                $"Das Bild konnte nicht geladen werden:\n{ex.Message}",
                "OK");
        }
    }

    private async void OnProduktbildVerwaltenClicked(object sender, EventArgs e)
    {
        string? auswahl = await DisplayActionSheetAsync(
            "Produktbild verwalten",
            "Abbrechen",
            null,
            "Bild aus Galerie auswählen",
            "Foto mit Kamera aufnehmen",
            "Produktbild entfernen");

        switch (auswahl)
        {
            case "Bild aus Galerie auswählen":
                OnProduktbildAuswaehlenClicked(sender, e);
                break;

            case "Foto mit Kamera aufnehmen":
                await ProduktbildMitKameraAufnehmenAsync();
                break;

            case "Produktbild entfernen":
                ausgewaehlterBildPfad = null;
                bildEntfernen = true;

                await DisplayAlertAsync(
                    "Bild entfernt",
                    "Das Produktbild wird beim Speichern entfernt.",
                    "OK");
                break;
        }
    }

    private async Task ProduktbildMitKameraAufnehmenAsync()
    {
        try
        {
            if (!MediaPicker.Default.IsCaptureSupported)
            {
                await DisplayAlertAsync(
                    "Nicht verfügbar",
                    "Die Kamera wird auf diesem Gerät nicht unterstützt.",
                    "OK");
                return;
            }

            FileResult? foto =
                await MediaPicker.Default.CapturePhotoAsync();

            if (foto == null)
                return;

            string? vorherigesBild = ausgewaehlterBildPfad;

            string dateiendung = Path.GetExtension(foto.FileName);
            string dateiname = $"{Guid.NewGuid()}{dateiendung}";

            string zielpfad = Path.Combine(
                FileSystem.AppDataDirectory,
                dateiname);

            await using Stream quelle = await foto.OpenReadAsync();
            await using FileStream ziel = File.Create(zielpfad);

            await quelle.CopyToAsync(ziel);

            ausgewaehlterBildPfad = zielpfad;
            bildEntfernen = false;

            await AltesProduktbildLoeschenAsync(
                vorherigesBild,
                zielpfad);

            await DisplayAlertAsync(
                "Foto aufgenommen",
                "Das Produktfoto wurde übernommen.",
                "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync(
                "Kamerafehler",
                ex.Message,
                "OK");
        }
    }
    private async Task AltesProduktbildLoeschenAsync(string? altesBild,string? neuesBild)
    {
        if (string.IsNullOrWhiteSpace(altesBild))
            return;

        if (string.Equals(
            altesBild,
            neuesBild,
            StringComparison.OrdinalIgnoreCase))
            return;

        List<Produkt> produkte =
            await datenbank.AlleProdukteLadenAsync();

        bool wirdNochVerwendet = produkte.Any(p =>
            string.Equals(
                p.BildPfad,
                altesBild,
                StringComparison.OrdinalIgnoreCase));

        if (wirdNochVerwendet)
            return;

        string speicherOrdner =
            Path.GetFullPath(FileSystem.AppDataDirectory)
            + Path.DirectorySeparatorChar;

        string bildPfad = Path.GetFullPath(altesBild);

        if (!bildPfad.StartsWith(
            speicherOrdner,
            StringComparison.Ordinal))
            return;

        if (File.Exists(bildPfad))
            File.Delete(bildPfad);
    }

}