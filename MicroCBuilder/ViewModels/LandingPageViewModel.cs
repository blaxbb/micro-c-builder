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

        public delegate void CreateBuildEventHandler(object sender, BuildInfo name);
        public event CreateBuildEventHandler OnCreateBuild;

        public delegate void CreateChecklistEventHandler(object sender, Checklist checklist);
        public event CreateChecklistEventHandler OnCreateChecklist;

        public ObservableCollection<FlareInfo> Flares { get; set; }
        public Command UpdateNetworkFlares { get; }

        public ObservableCollection<Checklist> ChecklistItems { get; set; }
        public Command UpdateNetworkChecklistFlares { get; }
        public Command NewChecklistCommand { get; }

        public Command<FlareInfo> PrintBuildCommand { get; }
        public Command<FlareInfo> OutputSignsCommand { get; }

        public LandingPageViewModel()
        {
            BuildTemplates = new ObservableCollection<BuildInfo>();
            NewBuildCommand = new Command<string>((name) => OnCreateBuild?.Invoke(this, GetInfo(name)));

            Flares = new ObservableCollection<FlareInfo>();
            ChecklistItems = new ObservableCollection<Checklist>();

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
                    AddBuildFlare(f);
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
                var flares = await Flare.GetTag("https://dataflare.bbarrett.me/api/Flare", $"micro-c-checklist-{Settings.StoreID()}");
                var sharedPassword = Settings.SharedPassword();
                var aesInfo = AesInfo.FromPassword(sharedPassword);
                var latestItems = flares.Select(f =>
                {
                    try
                    {
                        var checklist = f.TryDecrypt<Checklist>(aesInfo);
                        checklist.data.Created = f.Created;
                        checklist.data.UseEncryption = checklist.encrypted;
                        return checklist.data;
                    }
                    catch (Exception e)
                    {
                        return default;
                    }
                }).ToList();
                Console.WriteLine(latestItems);
                var toRemove = ChecklistItems.Where(checklist => !latestItems.Any(c => checklist.Id == c.Id));
                foreach (var f in toRemove)
                {
                    ChecklistItems.Remove(f);
                }

                foreach (var newChecklist in latestItems)
                {
                    var existing = ChecklistItems.FirstOrDefault(c => c.Id == newChecklist.Id);
                    if(existing != null && existing.Created < newChecklist.Created)
                    {
                        existing.Created = newChecklist.Created;
                        existing.Items = newChecklist.Items;
                        existing.Name = newChecklist.Name;
                    }
                    else
                    {
                        ChecklistItems.Add(newChecklist);
                    }
                }
            });
            UpdateNetworkChecklistFlares.Execute(null);

            NewChecklistCommand = new Command((o) =>
            {
                OnCreateChecklist?.Invoke(this, new Checklist());
            });

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
            var dispatcher = Windows.UI.Xaml.Window.Current.Dispatcher;
            FlareHubManager.OnFlareReceived += async (flare) =>
            {
                await dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    AddBuildFlare(flare);
                    AddChecklistFlare(flare);
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

        private void AddChecklistFlare(Flare flare)
        {
            if (flare.Tag != $"micro-c-checklist-{Settings.StoreID()}")
            {
                return;
            }


            var sharedPassword = Settings.SharedPassword();

            AesInfo? aesInfo = null;
            if (!string.IsNullOrWhiteSpace(sharedPassword))
            {
                aesInfo = AesInfo.FromPassword(sharedPassword);
            }

            Checklist newChecklist = null;

            if (aesInfo != null)
            {
                var (data, encrypted) = flare.TryDecrypt<Checklist>(aesInfo);
                newChecklist = data;
            }
            else
            {
                try
                {
                    newChecklist = (Checklist)flare.Value(typeof(Checklist));
                }
                catch (Exception ex)
                {
                    //flare is likely encrypted (or possibly malformed)
                }
            }

            if (newChecklist == null)
            {
                return;
            }
            newChecklist.Created = flare.Created;

            var existing = ChecklistItems.FirstOrDefault(c => c.Id == newChecklist.Id);
            if (existing != null && existing.Created < newChecklist.Created)
            {
                var index = ChecklistItems.IndexOf(existing);

                ChecklistItems.Remove(existing);
                ChecklistItems.Insert(index, newChecklist);
            }
            else
            {
                ChecklistItems.Add(newChecklist);
            }
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
