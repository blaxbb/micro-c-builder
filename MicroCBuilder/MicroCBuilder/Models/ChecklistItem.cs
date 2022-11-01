using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace MicroCBuilder.Models
{
    public class ChecklistItem : INotifyPropertyChanged
    {
        private string name;
        private string assigned;
        private bool complete;
        private DateTime completeDate;

        public Guid Id { get; set; }
        public string Name { get => name; set => SetProperty(ref name, value); }
        public string Assigned { get => assigned; set => SetProperty(ref assigned, value); }
        public bool Complete { get => complete; set { SetProperty(ref complete, value); CompleteDate = complete ? DateTime.Now : default; } }
        public DateTime CompleteDate { get => completeDate; set => SetProperty(ref completeDate, value); }
        public ChecklistItem()
        {
            Id = Guid.NewGuid();
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
