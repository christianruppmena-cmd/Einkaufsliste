namespace Einkaufsliste.Interfaces
{
    public interface IDBBackupService
    {
        public Task DatenbankImportierenAsync(string importPfad);
        public Task<string> ExportDateiErstellenAsync();
        public Task LokaleSicherungErstellenAsync();
        public Task<List<FileInfo>> LokaleSicherungenLadenAsync();
    }
}
