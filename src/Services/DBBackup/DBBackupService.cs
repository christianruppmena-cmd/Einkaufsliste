using Einkaufsliste.Interfaces;

namespace Einkaufsliste.Services;

public partial class DBBackupService : DBConnection, IDBBackupService
{
    private readonly IInitialisierungsService _initialisierungsService;
    private readonly string sicherungsOrdner =
    Path.Combine(FileSystem.AppDataDirectory, "Sicherungen");

    public DBBackupService(IInitialisierungsService initialisierungservice)
    {
        _initialisierungsService = initialisierungservice;
    }

}
