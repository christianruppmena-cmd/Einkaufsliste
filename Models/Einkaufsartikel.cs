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

        protected void OnPropertyChanged(
            [CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(propertyName));
        }

        public string Kategorie { get; set; } = "Sonstiges";
        public string Menge { get; set; } = "1";
        public string Einheit { get; set; } = "Stück";
        public string Hinweis { get; set; } = string.Empty;


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
