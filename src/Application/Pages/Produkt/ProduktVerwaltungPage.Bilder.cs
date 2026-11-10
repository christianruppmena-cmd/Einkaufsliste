using Einkaufsliste.Services;
using System.Globalization;
using Einkaufsliste.Models;

namespace Einkaufsliste.Application.Pages.Produkte;

public partial class ProduktVerwaltungPage
{
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
                await ProduktbildAuswaehlenAsync();
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
    private async Task AltesProduktbildLoeschenAsync(string altesBild, string neuesBild)
    {
        if (string.IsNullOrWhiteSpace(altesBild))
            return;

        if (string.Equals(
            altesBild,
            neuesBild,
            StringComparison.OrdinalIgnoreCase))
            return;

        List<Produkt> produkte =
            await _datenbank.AlleProdukteLadenAsync();

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
    private async Task ProduktbildAuswaehlenAsync()
    {
        try
        {
            List<FileResult> fotos =
                await MediaPicker.Default.PickPhotosAsync(
                    new MediaPickerOptions
                    {
                        SelectionLimit = 1
                    });

            FileResult? foto = fotos.FirstOrDefault();

            if (foto == null)
                return;

            if (foto == null)
                return;

            string vorherigesBild = ausgewaehlterBildPfad;
            string zielpfad = await ProduktbildLokalSpeichernAsync(foto);


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
    private static async Task<string> ProduktbildLokalSpeichernAsync(FileResult foto)
    {
        string dateiendung = Path.GetExtension(foto.FileName);
        string dateiname = $"{Guid.NewGuid()}{dateiendung}";
        string zielpfad = Path.Combine(
            FileSystem.AppDataDirectory,
            dateiname);

        await using Stream quelle = await foto.OpenReadAsync();
        await using FileStream ziel = File.Create(zielpfad);

        await quelle.CopyToAsync(ziel);
        return zielpfad;
    }
}
