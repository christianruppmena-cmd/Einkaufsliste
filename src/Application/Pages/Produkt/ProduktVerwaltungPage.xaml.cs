using Einkaufsliste.Services;
using System.Globalization;
using Einkaufsliste.Models;

namespace Einkaufsliste.Application.Pages.Produkte;

public partial class ProduktVerwaltungPage : ContentPage
{
    private readonly EinkauflistenDatenbank datenbank;
    private List<Produkt> alleProdukte = new();
    private List<Haendler> alleHaendler = new();
    private string ausgewaehlterBildPfad;
    private Produkt produktZumBearbeiten;
    private bool bildEntfernen;
    private readonly bool nurNeuesProduktFormular;


    public ProduktVerwaltungPage(EinkauflistenDatenbank datenbank, bool nurNeuesProduktFormular = false)
    {
        InitializeComponent();

        this.datenbank = datenbank;
        this.nurNeuesProduktFormular = nurNeuesProduktFormular;

        if(nurNeuesProduktFormular)
        {
            GespeicherteProdukteBereich.IsVisible = false;
            ProduktFormularBereich.IsVisible = true;
            FormularTitel.Text = "Neues Produkt";
        }
        else
        {
            GespeicherteProdukteBereich.IsVisible = true;
            ProduktFormularBereich.IsVisible = false;
        }
    }
    private void OnNeuesProduktClicked(object sender, EventArgs e)
    {
        produktZumBearbeiten = null;
        ausgewaehlterBildPfad = null;
        bildEntfernen = false;

        NameEntry.Text = "";
        MarkeEntry.Text = "";
        VarianteEntry.Text = "";
        KategoriePicker.SelectedItem = null;
        BevorzugterHaendlerPicker.SelectedIndex = 0;
        PackungsgroesseEntry.Text = "";
        BasiseinheitPicker.SelectedItem = null;

        FormularTitel.Text = "Neues Produkt";
        ProduktLoeschenButton.IsVisible = false;

        GespeicherteProdukteBereich.IsVisible = false;
        ProduktFormularBereich.IsVisible = true;
    }
    private void OnFormularAbbrechenClicked(object sender, EventArgs e)
    {
        produktZumBearbeiten = null;
        ausgewaehlterBildPfad = null;
        bildEntfernen = false;

        ProduktFormularBereich.IsVisible = false;
        GespeicherteProdukteBereich.IsVisible = true;
    }
    private void ZurProduktlisteZurueckkehren()
    {
        produktZumBearbeiten = null;
        ausgewaehlterBildPfad = null;
        bildEntfernen = false;

        ProduktFormularBereich.IsVisible = false;
        GespeicherteProdukteBereich.IsVisible = true;
    }
    protected override async void OnAppearing()
    {
        base.OnAppearing();

        await LadeKategorienAsync();
        await LadeProdukteAsync();
        await LadeHaendlerAsync();
    }

    private async Task LadeKategorienAsync()
    {
        List<Kategorie> kategorien =
            await datenbank.AlleKategorieEintraegeLadenAsync();

        KategoriePicker.ItemsSource = kategorien
            .Select(k => k.Name)
            .ToList();
    }

    private async Task LadeHaendlerAsync()
    {
        alleHaendler = await datenbank.AlleHaendlerLadenAsync();

        var haendlerNamen = new List<string>
    {
        "Kein bevorzugter Markt"
    };

        haendlerNamen.AddRange(
            alleHaendler.Select(h => h.Name));

        BevorzugterHaendlerPicker.ItemsSource = haendlerNamen;
        BevorzugterHaendlerPicker.SelectedIndex = 0;
    }
    private async void OnProduktSpeichernClicked(object sender, EventArgs e)
    {
        string name = NameEntry.Text?.Trim() ?? "";
        string marke = MarkeEntry.Text?.Trim() ?? "";
        string variante = VarianteEntry.Text?.Trim() ?? "";

        string kategorie = KategoriePicker.SelectedItem?.ToString();
        string einheit = BasiseinheitPicker.SelectedItem?.ToString();
        string ausgewaehlterHaendler = BevorzugterHaendlerPicker.SelectedItem?.ToString();
        int bevorzugterHaendlerId = alleHaendler.FirstOrDefault(h => h.Name == ausgewaehlterHaendler)?.Id ?? 0;

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

        decimal packungsgroesse = 0;

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

        string altesBild = produktZumBearbeiten?.BildPfad;

        string urspruenglicherName = produktZumBearbeiten?.Name ?? name;

        Produkt zuSpeicherndesProdukt =
            produktZumBearbeiten ?? new Produkt();

        zuSpeicherndesProdukt.Name = name;
        zuSpeicherndesProdukt.Marke = marke;
        zuSpeicherndesProdukt.Variante = variante;
        zuSpeicherndesProdukt.Kategorie = kategorie;
        zuSpeicherndesProdukt.BevorzugterHaendlerId = bevorzugterHaendlerId;
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
            Produkt? vorhandenesProdukt =
                        await datenbank.ProduktMitGleichemSchluesselLadenAsync(zuSpeicherndesProdukt);

            bool wurdeZusammengefuehrt = false;

            if (vorhandenesProdukt != null)
            {
                if (zuSpeicherndesProdukt.Id == 0)
                {
                    await DisplayAlertAsync(
                        "Produkt bereits vorhanden",
                        $"„{vorhandenesProdukt.Name}“ existiert bereits.",
                        "OK");
                    return;
                }

                bool bestaetigen = await DisplayAlertAsync(
                    "Produkt bereits vorhanden",
                    $"„{vorhandenesProdukt.Name}“ existiert bereits. " +
                    $"Sollen Einkaufsartikel, Preise und Preisverläufe von " +
                    $"„{urspruenglicherName}“ übernommen werden? " +
                    "Das vorhandene Produkt behält seine Stammdaten.",
                    "Ja, übernehmen",
                    "Nein");

                if (!bestaetigen)
                    return;

                await datenbank.ProduktZusammenfuehrenAsync(
                    zuSpeicherndesProdukt.Id,
                    vorhandenesProdukt.Id);

                wurdeZusammengefuehrt = true;

                // Das vorhandene Produkt behält sein Bild.
                if (!string.IsNullOrWhiteSpace(ausgewaehlterBildPfad) &&
                    File.Exists(ausgewaehlterBildPfad))
                {
                    File.Delete(ausgewaehlterBildPfad);
                }
            }
            else
            {
                await datenbank.ProduktSpeichernAsync(zuSpeicherndesProdukt);

                await datenbank.EinkaufsartikelHaendlerAktualisierenAsync(
                    zuSpeicherndesProdukt.Id,
                    zuSpeicherndesProdukt.BevorzugterHaendlerId);
            }

            produktZumBearbeiten = null;
            ausgewaehlterBildPfad = null;
            bildEntfernen = false;

            await LadeProdukteAsync();

            await AltesProduktbildLoeschenAsync(
                altesBild,
                zuSpeicherndesProdukt.BildPfad);

            await DisplayAlertAsync(
                wurdeZusammengefuehrt
                    ? "Produkte zusammengeführt"
                    : "Gespeichert",
                wurdeZusammengefuehrt
                    ? $"Die Daten von „{urspruenglicherName}“ wurden in „{vorhandenesProdukt!.Name}“ übernommen."
                    : $"{name} wurde erfolgreich gespeichert.",
                "OK");
            // Bei direktem Aufruf über den Plus-Button zurückkehren
            if (nurNeuesProduktFormular)
            {
                await Navigation.PopAsync();
                return;
            }
            ZurProduktlisteZurueckkehren();
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
        ausgewaehlterBildPfad = null;

        FormularTitel.Text = "Produkt bearbeiten";
        ProduktLoeschenButton.IsVisible = true;

        NameEntry.Text = produkt.Name;
        MarkeEntry.Text = produkt.Marke;
        VarianteEntry.Text = produkt.Variante;

        KategoriePicker.SelectedItem = produkt.Kategorie;

        if (produkt.BevorzugterHaendlerId > 0)
        {
            var bevorzugterHaendler = alleHaendler.FirstOrDefault(
                h => h.Id == produkt.BevorzugterHaendlerId);

            BevorzugterHaendlerPicker.SelectedItem =
                bevorzugterHaendler?.Name ?? "Kein bevorzugter Markt";
        }
        else
        {
            BevorzugterHaendlerPicker.SelectedIndex = 0;
        }

        PackungsgroesseEntry.Text =
            produkt.Packungsgroesse.ToString(
                CultureInfo.CurrentCulture) ?? "";

        BasiseinheitPicker.SelectedItem = produkt.Basiseinheit;

        GespeicherteProdukteBereich.IsVisible = false;
        ProduktFormularBereich.IsVisible = true;
    }
    private async void OnProduktLoeschenClicked(object sender, EventArgs e)
    {
        if (produktZumBearbeiten == null)
            return;

        bool bestaetigen = await DisplayAlertAsync(
            "Produkt löschen",
            $"Möchtest du „{produktZumBearbeiten.Name}“ wirklich aus der Produktverwaltung entfernen?\n\nBereits vorhandene Einkaufsartikel bleiben erhalten.",
            "Löschen",
            "Abbrechen");

        if (!bestaetigen)
            return;

        try
        {
            produktZumBearbeiten.IstAktiv = false;

            await datenbank.ProduktSpeichernAsync(produktZumBearbeiten);
            await LadeProdukteAsync();

            ZurProduktlisteZurueckkehren();

            await DisplayAlertAsync(
                "Produkt entfernt",
                "Das Produkt wurde aus der Produktverwaltung entfernt. Vorhandene Einkaufsartikel bleiben erhalten.",
                "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync(
                "Fehler",
                $"Das Produkt konnte nicht entfernt werden:\n{ex.Message}",
                "OK");
        }
    }
    private async void OnProduktbildAuswaehlenClicked(object sender, EventArgs e)
    {
        try
        {
            FileResult foto = await MediaPicker.Default.PickPhotoAsync();

            if (foto == null)
                return;

            string vorherigesBild = ausgewaehlterBildPfad;

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
        string auswahl = await DisplayActionSheetAsync(
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

            FileResult foto =
                await MediaPicker.Default.CapturePhotoAsync();

            if (foto == null)
                return;

            string vorherigesBild = ausgewaehlterBildPfad;

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
    private async Task AltesProduktbildLoeschenAsync(string altesBild,string neuesBild)
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

    private async void OnPreiseVerwaltenClicked( object sender,EventArgs e)
    {
        if (sender is not Button button ||
            button.BindingContext is not Produkt produkt)
        {
            return;
        }

        await Navigation.PushAsync(
            new ProduktpreisePage(datenbank, produkt));
    }






}