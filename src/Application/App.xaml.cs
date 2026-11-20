
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
        private readonly AppShell _shell;

        public App( AppShell shell, IInitialisierungsService initialisierungService, IHaendlerService haendlerService, IKategorienService kategorienService)
        {
            InitializeComponent();

            _shell = shell;
            _initialisierungsService = initialisierungService;
            _haendlerService = haendlerService;
            _kategorienService = kategorienService;
        }
        protected override Window CreateWindow(IActivationState? activationState)
        {
            var shell = _shell;

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

            return new Window(_shell);
        }
    }
}
