using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace MicroCBuilder.Models
{
    public class Checklist : INotifyPropertyChanged
    {
        public Guid Id { get; set; }
        public ObservableCollection<ChecklistItem> Items { get; set; }
        public string Name { get; set; }

        [JsonIgnore]
        public int Completed => Items.Count(i => i != null && i.Complete);
        [JsonIgnore]
        public int Percentage => 100 * (int)(Completed / (float)Items.Count());
        [JsonIgnore]
        public DateTime Created { get; set; }

        public Checklist()
        {
            Id = Guid.NewGuid();
            Items = new ObservableCollection<ChecklistItem>();
        }
        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            var changed = PropertyChanged;
            if (changed == null)
                return;

            changed.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T backingStore, T value, [CallerMemberName] string propertyName = "", Action? onChanged = null)
        {
            if (EqualityComparer<T>.Default.Equals(backingStore, value))
                return false;

            backingStore = value;
            onChanged?.Invoke();
            OnPropertyChanged(propertyName);
            return true;
        }
        #endregion
    }
}
