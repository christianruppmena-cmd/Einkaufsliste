using Einkaufsliste.Services;

namespace Einkaufsliste;

public partial class DatenbanksicherungPage : ContentPage
{
    private readonly EinkauflistenDatenbank datenbank;

    public DatenbanksicherungPage(EinkauflistenDatenbank datenbank)
    {
        InitializeComponent();

        this.datenbank = datenbank;
    }

    private async void OnExportierenClicked(object sender, EventArgs e)
    {
        try
        {
            string exportPfad =
                await datenbank.ExportDateiErstellenAsync();

            await Share.Default.RequestAsync(
                new ShareFileRequest
                {
                    Title = "Einkaufsliste sichern",
                    File = new ShareFile(exportPfad)
                });

            StatusLabel.Text =
                "Datenbanksicherung wurde vorbereitet.";
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync(
                "Exportfehler",
                ex.Message,
                "OK");
        }
    }

    private async void OnImportierenClicked(object sender, EventArgs e)
    {
        string? importPfad = null;

        try
        {
            var datei = await FilePicker.Default.PickAsync(
                new PickOptions
                {
                    PickerTitle = "ZIP-Datenbanksicherung auswählen",
                    FileTypes = new FilePickerFileType(
                        new Dictionary<DevicePlatform, IEnumerable<string>>
                        {
                            {
                                DevicePlatform.Android,
                                new[] { "application/zip" }
                            },
                            {
                                DevicePlatform.WinUI,
                                new[] { ".zip" }
                            }
                        })
                });

            if (datei == null)
                return;

            bool bestaetigt = await DisplayAlertAsync(
                "Datenbank wiederherstellen",
                "Die aktuelle Datenbank wird durch die ausgewählte Sicherung ersetzt. Möchtest du fortfahren?",
                "Ja",
                "Abbrechen");

            if (!bestaetigt)
                return;

            // ZIP-Datei temporär speichern
            importPfad = Path.Combine(
                FileSystem.CacheDirectory,
                $"import_{Guid.NewGuid()}.zip");

            using (Stream quelle = await datei.OpenReadAsync())
            using (FileStream ziel = File.Create(importPfad))
            {
                await quelle.CopyToAsync(ziel);
            }

            // Datenbank und Produktbilder importieren
            await datenbank.DatenbankImportierenAsync(importPfad);

            StatusLabel.Text =
                "Datenbanksicherung erfolgreich wiederhergestellt.";

            await DisplayAlertAsync(
                "Erfolgreich",
                "Die Datenbank und Produktbilder wurden wiederhergestellt. Bitte starte die App neu.",
                "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync(
                "Importfehler",
                ex.Message,
                "OK");
        }
        finally
        {
            // Temporäre ZIP-Datei löschen
            if (importPfad != null && File.Exists(importPfad))
            {
                File.Delete(importPfad);
            }
        }
    }
}