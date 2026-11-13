using Einkaufsliste.Interfaces;
using Microsoft.Maui.Controls.Shapes;

namespace Einkaufsliste.Application.Pages.Datenbank;

public partial class DatenbanksicherungPage : ContentPage
{
    private readonly IDBBackupService _backupService;

    public DatenbanksicherungPage(IDBBackupService dBBackupService)
    {
        InitializeComponent();

        _backupService = dBBackupService;
    }
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await SicherungenAnzeigenAsync();
    }
    private async Task SicherungenAnzeigenAsync()
    {
        SicherungenListe.Children.Clear();

        var sicherungen = await _backupService.LokaleSicherungenLadenAsync();

        KeineSicherungenLabel.IsVisible = sicherungen.Count == 0;

        foreach (var sicherung in sicherungen)
        {
            var karte = new Border
            {
                BackgroundColor = Color.FromArgb("#F5F8F5"),
                Stroke = Color.FromArgb("#D8E5DC"),
                StrokeShape = new RoundRectangle
                {
                    CornerRadius = 10
                },
                Padding = 12,
                Content = new VerticalStackLayout
                {
                    Spacing = 5,
                    Children =
                    {
                        new Label
                        {
                            Text = sicherung.LastWriteTime.ToString("dd.MM.yyyy - HH:mm 'Uhr'"),
                            FontAttributes = FontAttributes.Bold,
                            TextColor = Color.FromArgb("#245C43"),
                            FontSize = 16
                        },
                        new Label
                        {
                            Text = $"Dateigröße: {Math.Round(sicherung.Length / 1024.0, 1)} KB",
                            FontSize = 13,
                            TextColor = Color.FromArgb("#555555")
                        },
                        new Button
                        {
                            Text = "Diese Sicherung wiederherstellen",
                            BackgroundColor = Color.FromArgb("#777777"),
                            TextColor = Colors.White,
                            CornerRadius = 8,
                            HeightRequest = 42,
                            FontSize = 14,
                            Command = new Command(async () => await LokaleSicherungenWiederherstellenAsync(sicherung))
                        }

                    }
                }
            };
            SicherungenListe.Children.Add(karte);
        }
    }
    private async Task LokaleSicherungenWiederherstellenAsync(FileInfo sicherung)
    {
        bool bestaetigt = await DisplayAlertAsync(
            "Sicherung wiederherstellen",
            "Möchtest du diese Sicherung vom {sicherung.LastWriteTime:dd.MM.yyyy - HH:mm} Uhr wiederherstellen?",
            "Ja, wiederherstellen",
            "Abbrechen");
        if (!bestaetigt)
            return;
        try
        {
            await _backupService.DatenbankImportierenAsync(sicherung.FullName);
            StatusLabel.Text = "Lokale Sicherung erfolgreich wiederhergestellt.";

            await DisplayAlertAsync(
                "Erfolgreich",
                "Die Datenbank und Produktbilder wurden wiederhergestellt. Bitte starte die App neu.",
                "OK");

        }
        catch (Exception ex)
        {
            await DisplayAlertAsync(
                "Wiederherstellungsfehler",
                $"Die Sicherung konnte nicht wiederhergestellt werden: {ex.Message}",
                "OK");
        }
    }
    private async void OnExportierenClicked(object sender, EventArgs e)
    {
        try
        {
            string exportPfad = await _backupService.ExportDateiErstellenAsync();

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
        string importPfad = null;

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
                                new[] { "Application/zip" }
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
            importPfad = System.IO.Path.Combine(
                FileSystem.CacheDirectory,
                $"import_{Guid.NewGuid()}.zip");

            using (Stream quelle = await datei.OpenReadAsync())
            using (FileStream ziel = File.Create(importPfad))
            {
                await quelle.CopyToAsync(ziel);
            }

            // Datenbank und Produktbilder importieren
            await _backupService.DatenbankImportierenAsync(importPfad);

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