using DataFlareClient;
using MicroCBuilder.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MicroCBuilder.ViewModels
{
    public class ChecklistIndexControlViewModel : BaseViewModel
    {
        public ObservableCollection<Checklist> Items { get; }
        public Command UpdateNetworkChecklistFlares { get; }
        public Command NewChecklistCommand { get; }

        public delegate void CreateChecklistEventHandler(object sender, Checklist checklist);
        public event CreateChecklistEventHandler OnCreateChecklist;

        static Windows.UI.Core.CoreDispatcher dispatcher;

        public ChecklistIndexControlViewModel()
        {
            Items = new ObservableCollection<Checklist>();

            ChecklistFavoriteCache.OnChecklistFavoritesUpdated += async (favorites) =>
            {
                foreach (var checklist in Items)
                {
                    checklist.IsFavorited = favorites.IsFavorited(checklist);
                }

                SetFavorites(favorites.Favorites);
            };

            UpdateNetworkChecklistFlares = new Command(async (o) =>
            {
                var flares = await Flare.GetTag("https://dataflare.bbarrett.me/api/Flare", $"micro-c-checklist-{Settings.StoreID()}");
                if (flares.Count > 0)
                {
                    await ProcessFlares(flares);
                }

            });
            UpdateNetworkChecklistFlares.Execute(null);

            NewChecklistCommand = new Command((o) =>
            {
                var encrypted = !string.IsNullOrWhiteSpace(Settings.SharedPassword());
                OnCreateChecklist?.Invoke(this, new Checklist()
                {
                    UseEncryption = encrypted
                });
            });

            FlareHubManager.Subscribe($"micro-c-checklist-{Settings.StoreID()}");
            FlareHubManager.OnFlareReceived += ProcessFlareFromThread;
        }

        internal void Unloaded()
        {
            FlareHubManager.OnFlareReceived -= ProcessFlareFromThread;
        }

        private async void ProcessFlareFromThread(Flare flare)
        {
            await dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
            {
                await ProcessFlares(new List<Flare>() { flare });
            });
        }

        private async Task ProcessFlares(List<Flare> flares)
        {
            var sharedPassword = Settings.SharedPassword();
            var aesInfo = AesInfo.FromPassword(sharedPassword);
            var checklists = flares.Select(f =>
            {
                try
                {
                    var checklist = f.TryDecrypt<Checklist>(aesInfo);
                    checklist.data.Created = f.Created;
                    checklist.data.UseEncryption = checklist.encrypted;
                    checklist.data.IsFavorited = ChecklistFavoriteCache.Current?.IsFavorited(checklist.data) ?? false;
                    return checklist.data;
                }
                catch (Exception e)
                {
                    return default;
                }
            }).ToList();

            foreach (var newChecklist in checklists)
            {
                if (newChecklist == null || newChecklist.Created.DayOfYear != DateTime.Now.DayOfYear)
                {
                    continue;
                }
                if (newChecklist.IsFavorited)
                {
                    await ChecklistFavoriteCache.Current.AddItem(newChecklist);
                }

                var existing = Items.FirstOrDefault(c => c.Id == newChecklist.Id);
                if (existing != null)
                {
                    if (existing.Created <= newChecklist.Created)
                    {
                        var index = Items.IndexOf(existing);
                        Items.Remove(existing);
                        Items.Insert(index, newChecklist);
                    }
                }
                else
                {
                    Items?.Add(newChecklist);
                }
            }

            SetFavorites(ChecklistFavoriteCache.Current?.Favorites?.ToList());
        }

        private void SetFavorites(List<Checklist> favorites)
        {
            foreach (var fav in favorites)
            {
                if (!Items.Any(c => c.Id == fav.Id))
                {
                    Items.Add(fav.Clone());
                }
            }
        }

        public void CleanOldEntries()
        {
            var toRemove = Items?.Where(i => i.Created.DayOfYear != DateTime.Now.DayOfYear).ToList();
            toRemove.ForEach(i => Items.Remove(i));
            SetFavorites(ChecklistFavoriteCache.Current.Favorites.ToList());
        }
    }
}