using Einkaufsliste.Interfaces;

namespace Einkaufsliste.Services;

public partial class DBBackupService : DBConnection, IDBBackupService
{
    private readonly string sicherungsOrdner =
    Path.Combine(FileSystem.AppDataDirectory, "Sicherungen");

}
