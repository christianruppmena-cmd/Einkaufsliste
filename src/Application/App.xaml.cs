
using Einkaufsliste.Interfaces;

namespace Einkaufsliste.Application
{
    public partial class App : Microsoft.Maui.Controls.Application
    {
        private readonly IDBBackupService _backupService;
        private readonly IKategorienService _kategorienService;
        private readonly IProdukteService _produkteService;
        private readonly IHaendlerService _haendlerService;
        private readonly IPreisService _preisService;
        private readonly IArtikelService _artikelService;
        private readonly IInitialisierungsService _initialisierungsService;


        public App()
        {
            InitializeComponent();
            
            _backupService = Handler!.MauiContext.Services!.GetService<IDBBackupService>();
            _kategorienService = Handler!.MauiContext.Services!.GetService<IKategorienService>();
            _produkteService = Handler!.MauiContext.Services!.GetService<IProdukteService>();
            _haendlerService = Handler!.MauiContext.Services!.GetService<IHaendlerService>();
            _preisService = Handler!.MauiContext.Services!.GetService<IPreisService>();
            _artikelService = Handler!.MauiContext.Services!.GetService<IArtikelService>();
            _initialisierungsService = Handler!.MauiContext.Services!.GetService<IInitialisierungsService>();
        }
        protected override Window CreateWindow(IActivationState? activationState)
        {
            var shell = new AppShell();

            MainThread.BeginInvokeOnMainThread(async () =>
            {
                try
                {
                    await _initialisierungsService.InitialisierenAsync();
                    await _haendlerService.StandardHaendlerAnlegenAsync();
                    await _kategorienService.StandardkategorienAnlegenAsync();

                    shell.StartseiteAnzeigen();
                }
                catch (Exception ex)
                {
                    await shell.DisplayAlertAsync(
                            "Datenbankfehler",
                            ex.Message,
                            "OK");
                }
            });

            return new Window(shell);
        }
    }
}
