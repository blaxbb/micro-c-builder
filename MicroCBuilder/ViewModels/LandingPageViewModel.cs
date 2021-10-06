using DataFlareClient;
using MicroCLib.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace MicroCBuilder.ViewModels
{
    public class LandingPageViewModel : BaseViewModel
    {
        public Command<string> NewBuildCommand { get; }
        public ObservableCollection<BuildInfo> BuildTemplates { get; }

        public delegate void CreateBuildEventHandler(object sender, BuildInfo name);
        public event CreateBuildEventHandler OnCreateBuild;

        public delegate void CreateChecklistEventHandler(object sender, EventArgs e);
        public event CreateChecklistEventHandler OnCreateChecklist;

        public ObservableCollection<FlareInfo> Flares { get; set; }
        public Command UpdateNetworkFlares { get; }

        public ObservableCollection<Flare> ChecklistItems { get; set; }
        public Command UpdateNetworkChecklistFlares { get; }
        public Command NewChecklistCommand { get; }

        public LandingPageViewModel()
        {
            BuildTemplates = new ObservableCollection<BuildInfo>();
            NewBuildCommand = new Command<string>((name) => OnCreateBuild?.Invoke(this, GetInfo(name)));

            Flares = new ObservableCollection<FlareInfo>();

            UpdateNetworkFlares = new Command(async (o) =>
            {
                var sharedPassword = Settings.SharedPassword();
                AesInfo? aesInfo = null;
                if (!string.IsNullOrWhiteSpace(sharedPassword))
                {
                    aesInfo = AesInfo.FromPassword(sharedPassword);
                }

                var flares = await Flare.GetTag("https://dataflare.bbarrett.me/api/Flare", $"micro-c-{Settings.StoreID()}");
                var toAdd = flares.Where(f => Flares.All(existing => existing.Flare.ShortCode != f.ShortCode)).OrderBy(f => f.Created).ToList();
                foreach(var f in toAdd)
                {
                    if (aesInfo != null)
                    {
                        var (data, encrypted) = f.TryDecrypt<List<BuildComponent>>(aesInfo);
                        if (data != null)
                        {
                            Flares.Insert(0, new FlareInfo(f));
                        }
                    }
                    else
                    {
                        try
                        {
                            var data = f.Value(typeof(List<BuildComponent>));
                            if (data != null)
                            {
                                Flares.Insert(0, new FlareInfo(f));
                            }
                        }
                        catch(Exception ex)
                        {
                            //flare is likely encrypted (or possibly malformed)
                        }
                    }
                }

                var toRemove = Flares.Where(f => flares.All(updated => updated.ShortCode != f.Flare.ShortCode)).ToList();
                foreach(var f in toRemove)
                {
                    var index = Flares.ToList().FindIndex(check => check.Flare.ShortCode == f.Flare.ShortCode);
                    if(index >= 0 && index < Flares.Count)
                    {
                        Flares.RemoveAt(index);
                    }
                }

                if (toAdd.Count > 0 || toRemove.Count > 0)
                {
                    OnPropertyChanged(nameof(Flares));
                }
            });
            UpdateNetworkFlares.Execute(null);

            UpdateNetworkChecklistFlares = new Command(async (o) =>
            {
                var flares = await Flare.GetTag("https://dataflare.bbarrett.me/api/Flare", $"micro-c-checklists-{Settings.StoreID()}");
                var latestItems = flares.GroupBy(f => f.Title).SelectMany(g => g).OrderByDescending(f => f.Created).ToList();

                var toRemove = ChecklistItems.Where(f => !latestItems.Any(g => f.Tag == g.Tag));
                foreach(var f in toRemove)
                {
                    ChecklistItems.Remove(f);
                }

                foreach(var newFlare in latestItems)
                {
                    if(!ChecklistItems.Any(c => c.Tag == newFlare.Tag))
                    {
                        ChecklistItems.Add(newFlare);
                    }
                }
            });

            NewChecklistCommand = new Command((o) =>
            {
                OnCreateChecklist?.Invoke(this, new EventArgs());
            });

        }


        private BuildInfo GetInfo(string name)
        {
            return BuildTemplates.FirstOrDefault(b => b.Name == name);
        }
    }
    public class BuildInfo
    {
        public string Name { get; set; }
        public List<BuildComponent> Components { get; set; }
        public float Total => Components.Sum(c => c?.Item.Price ?? 0 * c?.Item.Quantity ?? 0);
    }

    public class FlareInfo
    {
        public Flare Flare { get; set; }
        public List<BuildComponent> Components { get; }
        public float Total => Components?.Sum(c => c?.Item.Price ?? 0 * c?.Item.Quantity ?? 0) ?? 0f;
        public bool IsEncrypted { get; }

        public FlareInfo(Flare flare)
        {
            Flare = flare;
            var password = Settings.SharedPassword();
            if(string.IsNullOrWhiteSpace(password))
            {
                Components = (List<BuildComponent>)flare.Value(typeof(List<BuildComponent>));
                IsEncrypted = false;
            }
            else
            {
                (Components, IsEncrypted) = Flare.TryDecrypt<List<BuildComponent>>(AesInfo.FromPassword(password));
            }
        }
    }
}
