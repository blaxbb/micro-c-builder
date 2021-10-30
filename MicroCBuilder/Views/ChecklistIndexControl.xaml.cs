using MicroCBuilder.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Microsoft.Toolkit.Uwp.UI;
using MicroCBuilder.ViewModels;
using System.Threading;
using System.Threading.Tasks;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace MicroCBuilder.Views
{
    public sealed partial class ChecklistIndexControl : UserControl
    {
        public delegate void CreateChecklistEventHandler(object sender, Checklist checklist);
        public event CreateChecklistEventHandler OnCreateChecklist;
        private CancellationTokenSource tokenSource;

        public ChecklistIndexControl()
        {
            tokenSource = new CancellationTokenSource();

            this.InitializeComponent();
            this.Unloaded += (sender, args) => {
                tokenSource.Cancel();
                if (DataContext is ChecklistIndexControlViewModel vm)
                {
                    vm.Unloaded();
                }
            };

            if(DataContext is ChecklistIndexControlViewModel vm)
            {
                vm.OnCreateChecklist += CreateChecklist;
                var token = tokenSource.Token;
                Task.Run(async () =>
                {
                    while (!tokenSource.IsCancellationRequested)
                    {
                        await Task.Delay(60 * 1000);
                        vm.CleanOldEntries();
                    }
                }, token);
            }
        }

        private void CreateChecklist(object sender, Checklist checklist)
        {
            //this function also called from viewmodel via event
            MainPage.Instance.CreateChecklist(checklist);
        }

        private void ChecklistFlareDoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            if (e.OriginalSource is FrameworkElement ele && ele.DataContext is Checklist checklist)
            {
                var landingPageParent = this.FindAscendant<LandingPage>();
                if (landingPageParent != null)
                {
                    CreateChecklist(sender, checklist);
                }

                var checklistPageParent = this.FindAscendant<ChecklistPage>();
                if(checklistPageParent != null && checklistPageParent.DataContext is ChecklistPageViewModel vm)
                {
                    vm.Checklist = checklist;
                }
            }
        }

        private void FavoriteChecklistClick(object sender, RoutedEventArgs e)
        {
            if (sender is Button b && b.DataContext is Checklist checklist)
            {
                if (checklist.IsFavorited)
                {
                    ChecklistFavoriteCache.Current?.RemoveItem(checklist);
                    checklist.IsFavorited = false;
                }
                else
                {
                    ChecklistFavoriteCache.Current?.AddItem(checklist);
                    checklist.IsFavorited = true;
                }
            }
        }
    }
}
