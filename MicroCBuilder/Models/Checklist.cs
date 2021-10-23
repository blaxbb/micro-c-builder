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
        private string name;
        private DateTime created;
        private ObservableCollection<ChecklistItem> items;
        private bool isFavorited;
        private bool useEncryption;

        public Guid Id { get; set; }
        public ObservableCollection<ChecklistItem> Items { get => items; set => SetProperty(ref items, value); }
        public string Name { get => name; set => SetProperty(ref name, value); }

        [JsonIgnore]
        public int Completed => Items.Count(i => i != null && i.Complete);
        [JsonIgnore]
        public int Percentage => (int)(100 * Completed / (float)Items.Count());
        [JsonIgnore]
        public DateTime Created { get => created; set => SetProperty(ref created, value); }
        [JsonIgnore]
        public bool UseEncryption { get => useEncryption; set => SetProperty(ref useEncryption, value); }
        [JsonIgnore]
        public bool IsFavorited { get => isFavorited; set => SetProperty(ref isFavorited, value); }

        public Checklist()
        {
            Id = Guid.NewGuid();
            Items = new ObservableCollection<ChecklistItem>();
        }

        public Checklist Clone()
        {
            var ret = new Checklist()
            {
                Id = Id,
                Name = Name,
                Created = Created,
                IsFavorited = IsFavorited,
                UseEncryption = UseEncryption
            };
            foreach(var item in Items)
            {
                ret.Items.Add(new ChecklistItem()
                {
                    Name = item.Name,
                    Id= item.Id,
                });
            }

            return ret;
        }


        public override bool Equals(object obj)
        {
            if (obj is Checklist checklist)
            {
                var eq = Id.Equals(checklist.Id) &&
                       Items.Count == checklist.Items.Count &&
                       Name == checklist.Name;

                if (!eq)
                {
                    return false;
                }

                for (int i = 0; i < Items.Count; i++)
                {
                    var a = Items[i];
                    var b = checklist.Items[i];
                    if (a.Id == b.Id &&
                        a.Name == b.Name &&
                        a.Assigned == b.Assigned &&
                        a.Complete == b.Complete)
                    {
                        continue;
                    }
                    return false;
                }
                return true;
            }

            return false;
        }

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged([CallerMemberName] string propertyName = "")
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
