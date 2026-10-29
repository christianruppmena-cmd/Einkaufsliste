using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using SQLite;

namespace Einkaufsliste.Models
{
    public class Einkaufsartikel : INotifyPropertyChanged
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        private string name = string.Empty;
        private bool istErledigt;
        public string Name
        {
            get => name;
            set
            {
                if (name == value)
                    return;

                name = value;
                OnPropertyChanged();

            }
            
        }
        public bool IstErledigt
        {
            get => istErledigt;
            set
            {
                if (istErledigt == value)
                    return;
                istErledigt = value;
                OnPropertyChanged();
            }
        }
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(propertyName));
        }
        public string Kategorie { get; set; } = "Sonstiges";
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
                OnPropertyChanged(nameof(Gesamtpreis));
                OnPropertyChanged(nameof(DetailsAnzeige));
            }
        }
        private string einheit = "Stück";
        public string Einheit
        {
            get => einheit;
            set
            {
                if (einheit == value)
                    return;

                einheit = value;
                OnPropertyChanged(nameof(Einheit));
                OnPropertyChanged(nameof(DetailsAnzeige));
            }
        }
        private string hinweis = string.Empty;
        public string Hinweis
        {
            get => hinweis;
            set
            {
                if (hinweis == value)
                    return;

                hinweis = value;
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
