using Einkaufsliste.Interfaces;

namespace Einkaufsliste.Services;

public partial class DBBackupService : IDBBackupService
{
    private SQLiteAsyncConnection datenbank;
    private readonly string datenbankPfad =
        Path.Combine(FileSystem.AppDataDirectory, "einkaufliste.db3");
    private readonly string sicherungsOrdner =
    Path.Combine(FileSystem.AppDataDirectory, "Sicherungen");
    public DBBackupService()
    {
        datenbank = new SQLiteAsyncConnection(datenbankPfad);
    }
}
