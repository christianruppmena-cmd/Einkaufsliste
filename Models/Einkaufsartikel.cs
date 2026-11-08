namespace Einkaufsliste.Models
{
    public class Einkaufsartikel : INotifyPropertyChanged
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public int ProduktId { get; set; }
        public int AusgewaehlterHaendlerId { get; set; }

        public string Kategorie { get; set; } = "Sonstiges";

        private string _haendlerAnzeige = string.Empty;
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

        private string _name = string.Empty;
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

        private bool _istErledigt;
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

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(
            [CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(propertyName));
        }


        private decimal stueckpreis;
        public decimal Stueckpreis
        {
            get => stueckpreis;
            set
            {
                if (stueckpreis == value)
                    return;

                stueckpreis = value;
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

        private string menge = "1";
        public string Menge
        {
            get => menge;
            set
            {
                if (menge == value)
                    return;

                menge = value;
                OnPropertyChanged(nameof(Menge));
                OnPropertyChanged(nameof(DetailsAnzeige));
                OnPropertyChanged(nameof(Gesamtpreis));
            }
        }

        private string _einheit = "Stück";
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

        private string _hinweis = string.Empty;
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
    }
}
