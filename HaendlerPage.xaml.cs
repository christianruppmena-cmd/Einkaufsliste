using Einkaufsliste.Models;
using Einkaufsliste.Services;
using System.Collections.ObjectModel;

namespace Einkaufsliste;

public partial class HaendlerPage : ContentPage
{
    private readonly EinkauflistenDatenbank datenbank;

    private readonly string logoOrdner = Path.Combine(
        FileSystem.AppDataDirectory,
        "Haendlerlogos");

    public ObservableCollection<Haendler> HaendlerListeItems { get; set; } = new();

    public HaendlerPage(EinkauflistenDatenbank datenbank)
    {
        InitializeComponent();

        this.datenbank = datenbank;

        HaendlerListe.ItemsSource = HaendlerListeItems;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        await HaendlerLadenAsync();
    }

    private async Task HaendlerLadenAsync()
    {
        HaendlerListeItems.Clear();

        var haendler =
            await datenbank.AlleHaendlerVerwaltenLadenAsync();

        foreach (var eintrag in haendler)
        {
            HaendlerListeItems.Add(eintrag);
        }
    }

    private async void OnHaendlerAngeklickt(object sender, TappedEventArgs e)
    {
        if (sender is not BindableObject element ||
            element.BindingContext is not Haendler haendler)
        {
            return;
        }

        string auswahl = await DisplayActionSheetAsync(
            haendler.Name,
            "Abbrechen",
            null,
            "Name ändern",
            "Logo ändern",
            haendler.IstAktiv ? "Deaktivieren" : "Aktivieren");

        switch (auswahl)
        {
            case "Name ändern":

                string neuerName = await DisplayPromptAsync(
                    "Händler bearbeiten",
                    "Neuer Händlername:",
                    "Speichern",
                    "Abbrechen",
                    initialValue: haendler.Name);

                if (string.IsNullOrWhiteSpace(neuerName))
                    return;

                neuerName = neuerName.Trim();

                var alleHaendler =
                    await datenbank.AlleHaendlerVerwaltenLadenAsync();

                if (alleHaendler.Any(h =>
                    h.Id != haendler.Id &&
                    h.Name.Equals(neuerName, StringComparison.OrdinalIgnoreCase)))
                {
                    await DisplayAlertAsync(
                        "Hinweis",
                        "Dieser Händlername existiert bereits.",
                        "OK");

                    return;
                }

                haendler.Name = neuerName;

                await datenbank.HaendlerSpeichernAsync(haendler);

                await HaendlerLadenAsync();

                break;

            case "Logo ändern":

                string? neuesLogo = await LogoAuswaehlenAsync();

                if (neuesLogo == null)
                    return;

                string? altesLogo = haendler.LogoPfad;

                haendler.LogoPfad = neuesLogo;

                try
                {
                    await datenbank.HaendlerSpeichernAsync(haendler);

                    // Altes Logo erst nach erfolgreichem Speichern löschen
                    if (!string.IsNullOrWhiteSpace(altesLogo) &&
                        File.Exists(altesLogo))
                    {
                        File.Delete(altesLogo);
                    }

                    await HaendlerLadenAsync();
                }
                catch (Exception ex)
                {
                    // Bei einem Speicherfehler das neue Logo entfernen
                    if (File.Exists(neuesLogo))
                        File.Delete(neuesLogo);

                    haendler.LogoPfad = altesLogo;

                    await DisplayAlertAsync(
                        "Fehler",
                        $"Das Logo konnte nicht gespeichert werden:\n{ex.Message}",
                        "OK");
                }

                break;

            case "Deaktivieren":
            case "Aktivieren":

                bool neuerStatus = !haendler.IstAktiv;

                bool bestaetigt = await DisplayAlertAsync(
                    neuerStatus ? "Händler aktivieren" : "Händler deaktivieren",
                    neuerStatus
                        ? $"Möchtest du {haendler.Name} wieder aktivieren?"
                        : $"Möchtest du {haendler.Name} deaktivieren?",
                    "Ja",
                    "Nein");

                if (!bestaetigt)
                    return;

                haendler.IstAktiv = neuerStatus;

                await datenbank.HaendlerSpeichernAsync(haendler);

                await HaendlerLadenAsync();

                break;
        }
    }
   
    private async Task<string?> LogoAuswaehlenAsync()
    {
        try
        {
            var datei = await FilePicker.Default.PickAsync(
                new PickOptions
                {
                    PickerTitle = "Händlerlogo auswählen",
                    FileTypes = FilePickerFileType.Images
                });

            if (datei == null)
                return null;

            Directory.CreateDirectory(logoOrdner);

            string dateiendung = Path.GetExtension(datei.FileName);

            string dateiname = $"{Guid.NewGuid()}{dateiendung}";

            string zielPfad = Path.Combine(logoOrdner, dateiname);

            using var quelle = await datei.OpenReadAsync();
            using var ziel = File.Create(zielPfad);

            await quelle.CopyToAsync(ziel);

            return zielPfad;
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync(
                "Fehler",
                $"Das Logo konnte nicht ausgewählt werden:\n{ex.Message}",
                "OK");

            return null;
        }
    }

    private async void OnHaendlerHinzufuegenClicked(object sender, EventArgs e)
    {
        string name = await DisplayPromptAsync(
            "Händler hinzufügen",
            "Wie heißt der Händler?",
            "Hinzufügen",
            "Abbrechen",
            "Händlername eingeben");

        if (string.IsNullOrWhiteSpace(name))
            return;

        name = name.Trim();

        var vorhandeneHaendler =
            await datenbank.AlleHaendlerVerwaltenLadenAsync();

        if (vorhandeneHaendler.Any(h =>
            h.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
        {
            await DisplayAlertAsync(
                "Hinweis",
                "Dieser Händler existiert bereits.",
                "OK");

            return;
        }

        string? logoPfad = await LogoAuswaehlenAsync();

        var neuerHaendler = new Haendler
        {
            Name = name,
            LogoPfad = logoPfad,
            IstAktiv = true,
            Sortierung = vorhandeneHaendler.Count + 1
        };

        await datenbank.HaendlerSpeichernAsync(neuerHaendler);

        await HaendlerLadenAsync();

        await DisplayAlertAsync(
            "Erfolgreich",
            $"Der Händler „{name}“ wurde hinzugefügt.",
            "OK");
    }
}