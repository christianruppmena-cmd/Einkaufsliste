namespace Einkaufsliste.Models
{
    public class Einkaufsartikel : INotifyPropertyChanged
    {
        private string _haendlerAnzeige = string.Empty;
        private string _name = string.Empty;
        private bool _istErledigt;
        private decimal _stueckpreis;
        private string _menge = "1";
        private string _einheit = "Stück";
        private string _hinweis = string.Empty;

        public event PropertyChangedEventHandler PropertyChanged;

        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public int ProduktId { get; set; }
        public int AusgewaehlterHaendlerId { get; set; }
        public string Kategorie { get; set; } = "Sonstiges";
        
        [Ignore]
        public string HaendlerAnzeige
        {
            get => _haendlerAnzeige;
            set
            {
                if (_haendlerAnzeige == value)
                    return;

                _haendlerAnzeige = value;
                OnPropertyChanged(nameof(HaendlerAnzeige));
            }
        }
        public string Name
        {
            get => _name;
            set
            {
                if (_name == value)
                    return;

                _name = value;
                OnPropertyChanged();
            }
        }
        public bool IstErledigt
        {
            get => _istErledigt;
            set
            {
                if (_istErledigt == value)
                    return;
                _istErledigt = value;
                OnPropertyChanged();
            }
        }
        public decimal Stueckpreis
        {
            get => _stueckpreis;
            set
            {
                if (_stueckpreis == value)
                    return;

                _stueckpreis = value;
                OnPropertyChanged(nameof(Stueckpreis));
                OnPropertyChanged(nameof(Gesamtpreis));
            }
        }
        public decimal Gesamtpreis
        {
            get
            {
                string mengeText = Menge.Trim().Replace(',', '.');

                if(decimal.TryParse(
                    mengeText,
                    System.Globalization.NumberStyles.Number,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out decimal mengeZahl))
                {
                    return mengeZahl * Stueckpreis;
                }
                return Stueckpreis;
            }
        }
        public string Menge
        {
            get => _menge;
            set
            {
                if (_menge == value)
                    return;

                _menge = value;
                OnPropertyChanged(nameof(Menge));
                OnPropertyChanged(nameof(DetailsAnzeige));
                OnPropertyChanged(nameof(Gesamtpreis));
            }
        }
        public string Einheit
        {
            get => _einheit;
            set
            {
                if (_einheit == value)
                    return;

                _einheit = value;
                OnPropertyChanged(nameof(Einheit));
                OnPropertyChanged(nameof(DetailsAnzeige));
            }
        }
        public string Hinweis
        {
            get => _hinweis;
            set
            {
                if (_hinweis == value)
                    return;

                _hinweis = value;
                OnPropertyChanged(nameof(Hinweis));
                OnPropertyChanged(nameof(DetailsAnzeige));
            }
        }
        public string DetailsAnzeige
        {
            get
            {
                string mengeEinheit = $"{Menge} {Einheit}".Trim();

                if (string.IsNullOrWhiteSpace(Hinweis))
                    return mengeEinheit;

                return $"{mengeEinheit} - {Hinweis}";
            }
        }
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(propertyName));
        }
    }
}
