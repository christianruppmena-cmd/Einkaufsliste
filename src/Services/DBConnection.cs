
namespace Einkaufsliste.Services
{
    public abstract class DBConnection
    {
        protected SQLiteAsyncConnection Datenbank;
        private readonly string _datenbankPfad =
            Path.Combine(FileSystem.AppDataDirectory, "einkaufliste.db3");
        public DBConnection()
        {
            Datenbank = new SQLiteAsyncConnection(_datenbankPfad);
        }
        public void CloseConnection()
        {
            Datenbank.CloseAsync().Wait();
        }

        public void OpenConnection()
        {
            try
            {
                this.CloseConnection();            
            }
            catch (Exception ex)
            {              
                
            }
            Datenbank = new SQLiteAsyncConnection(_datenbankPfad);
        }
    }

}
