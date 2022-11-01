using DataFlareClient;
using MicroCBuilder.Models;
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

        public delegate void CreateBuildEventHandler(object sender, ProductList list);
        public event CreateBuildEventHandler OnCreateBuild;

        public delegate void CreateChecklistEventHandler(object sender, Checklist checklist);
        public event CreateChecklistEventHandler OnCreateChecklist;

        public ObservableCollection<FlareInfo> Flares { get; set; }
        public Command UpdateNetworkFlares { get; }

        public Command<FlareInfo> PrintBuildCommand { get; }
        public Command<FlareInfo> OutputSignsCommand { get; }

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
                foreach (var f in toAdd)
                {
                    AddBuildFlare(f);
                }

                var toRemove = Flares.Where(f => flares.All(updated => updated.ShortCode != f.Flare.ShortCode)).ToList();
                foreach (var f in toRemove)
                {
                    var index = Flares.ToList().FindIndex(check => check.Flare.ShortCode == f.Flare.ShortCode);
                    if (index >= 0 && index < Flares.Count)
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

            PrintBuildCommand = new Command<FlareInfo>(async (info) =>
            {
                await Views.BuildPage.DoPrintQuote(info.Components);
            });

            OutputSignsCommand = new Command<FlareInfo>(async (info) =>
            {
                BuildPageViewModel.DoSaveSigns(info.Components, info.Flare.Title);
            });

            FlareHubManager.Subscribe($"micro-c-{Settings.StoreID()}");
            FlareHubManager.Subscribe($"micro-c-checklist-{Settings.StoreID()}");
            
            FlareHubManager.OnFlareReceived += async (flare) =>
            {
                var dispatcher = MainWindow.Current.DispatcherQueue;
                dispatcher.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Normal, () =>
                {
                    AddBuildFlare(flare);
                });
            };

        }

        private void AddBuildFlare(Flare flare)
        {
            if (flare.Tag != $"micro-c-{Settings.StoreID()}")
            {
                return;
            }

            var sharedPassword = Settings.SharedPassword();

            AesInfo? aesInfo = null;
            if (!string.IsNullOrWhiteSpace(sharedPassword))
            {
                aesInfo = AesInfo.FromPassword(sharedPassword);
            }

            if (aesInfo != null)
            {
                var (data, encrypted) = flare.TryDecrypt<List<BuildComponent>>(aesInfo);
                if (data != null)
                {
                    Flares.Insert(0, new FlareInfo(flare));
                }
            }
            else
            {
                try
                {
                    var data = flare.Value(typeof(List<BuildComponent>));
                    if (data != null)
                    {
                        Flares.Insert(0, new FlareInfo(flare));
                    }
                }
                catch (Exception ex)
                {
                    //flare is likely encrypted (or possibly malformed)
                }
            }
        }

        private ProductList GetInfo(string name)
        {
            var template = BuildTemplates.FirstOrDefault(b => b.Name == name);
            return new ProductList()
            {
                Name = template.Name,
                Components = template.Components,
            };
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
            if (string.IsNullOrWhiteSpace(password))
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