﻿using FuzzySharp.SimilarityRatio;
using FuzzySharp.SimilarityRatio.Scorer.StrategySensitive;
using MicroCBuilder.ViewModels;
using MicroCLib.Models;
using MicroCLib.Models.Reference;
using CommunityToolkit.WinUI.UI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Windows.Input;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace MicroCBuilder.Views
{
    public sealed partial class BuildComponentControl : UserControl, INotifyPropertyChanged
    {
        public BuildComponent Component
        {
            get { return (BuildComponent)GetValue(ComponentProperty); }
            set
            {
                SetValue(ComponentProperty, value);
            }
        }

        public static readonly DependencyProperty ComponentProperty =
            DependencyProperty.Register("Component", typeof(BuildComponent), typeof(BuildComponentControl), new PropertyMetadata(new BuildComponent(), new PropertyChangedCallback(ComponentChanged)));

        private static void ComponentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is BuildComponentControl control)
            {
                if (e.OldValue is BuildComponent oldItem)
                {
                    oldItem.PropertyChanged -= control.UpdateFields;
                }
                if (e.NewValue is BuildComponent newItem)
                {
                    newItem.PropertyChanged += control.UpdateFields;
                    control.UpdateFields(control, new PropertyChangedEventArgs(e.Property.ToString()));
                }

            }
        }

        void UpdateFields(object sender, PropertyChangedEventArgs e)
        {
            OnPropertyChanged(nameof(Price));
            OnPropertyChanged(nameof(Quantity));
        }

        public float Price { get => Component?.Item?.Price ?? 0; set { if (Component.Item != null) { Component.Item.Price = value; ValuesUpdated?.Execute(null); } } }
        public int Quantity { get => Component?.Item?.Quantity ?? 1; set { if (Component.Item != null) { Component.Item.Quantity = value; ValuesUpdated?.Execute(null); } } }

        public List<Item> Items => BuildComponentCache.Current.FromType(Component.Type);

        public ICommand QueryChanged
        {
            get { return (ICommand)GetValue(QueryChangedProperty); }
            set { SetValue(QueryChangedProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Query.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty QueryChangedProperty =
            DependencyProperty.Register("QueryChanged", typeof(ICommand), typeof(BuildComponentControl), new PropertyMetadata(null));

        public delegate void QueryUpdatedEventHandler(BuildComponentControl sender, string query);
        public event QueryUpdatedEventHandler QueryUpdated;

        public event QueryUpdatedEventHandler QuerySubmitted;



        public ICommand RemoveCommand
        {
            get { return (ICommand)GetValue(RemoveCommandProperty); }
            set { SetValue(RemoveCommandProperty, value); }
        }

        // Using a DependencyProperty as the backing store for RemoveCommand.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty RemoveCommandProperty =
            DependencyProperty.Register("RemoveCommand", typeof(ICommand), typeof(BuildComponentControl), new PropertyMetadata(null));



        public ICommand AddEmptyCommand
        {
            get { return (ICommand)GetValue(AddEmptyCommandProperty); }
            set { SetValue(AddEmptyCommandProperty, value); }
        }

        // Using a DependencyProperty as the backing store for AddAnotherCommand.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty AddEmptyCommandProperty =
            DependencyProperty.Register("AddEmptyCommand", typeof(ICommand), typeof(BuildComponentControl), new PropertyMetadata(null));
        public ICommand AddDuplicateCommand
        {
            get { return (ICommand)GetValue(AddDuplicateCommandProperty); }
            set { SetValue(AddDuplicateCommandProperty, value); }
        }

        // Using a DependencyProperty as the backing store for AddAnotherCommand.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty AddDuplicateCommandProperty =
            DependencyProperty.Register("AddDuplicateCommand", typeof(ICommand), typeof(BuildComponentControl), new PropertyMetadata(null));


        public ICommand InfoCommand
        {
            get { return (ICommand)GetValue(InfoCommandProperty); }
            set { SetValue(InfoCommandProperty, value); }
        }

        // Using a DependencyProperty as the backing store for InfoCommand.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty InfoCommandProperty =
            DependencyProperty.Register("InfoCommand", typeof(ICommand), typeof(BuildComponentControl), new PropertyMetadata(null));

        public ICommand ValuesUpdated
        {
            get { return (ICommand)GetValue(ValuesUpdatedProperty); }
            set { SetValue(ValuesUpdatedProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ValuesUpdated.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ValuesUpdatedProperty =
            DependencyProperty.Register("ValuesUpdated", typeof(ICommand), typeof(BuildComponentControl), new PropertyMetadata(0));

        public ICommand AddPlan
        {
            get { return (ICommand)GetValue(AddPlanProperty); }
            set { SetValue(AddPlanProperty, value); }
        }

        // Using a DependencyProperty as the backing store for AddPlan.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty AddPlanProperty =
            DependencyProperty.Register("AddPlan", typeof(ICommand), typeof(BuildComponentControl), new PropertyMetadata(null));



        public BuildComponent ActiveComponent
        {
            get { return (BuildComponent)GetValue(ActiveComponentProperty); }
            set { SetValue(ActiveComponentProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ActiveComponent.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ActiveComponentProperty =
            DependencyProperty.Register("ActiveComponent", typeof(BuildComponent), typeof(BuildComponentControl), new PropertyMetadata(null, ActiveComponentChanged));

        private bool isActive;
        public bool IsActive { get => isActive; set { isActive = value; OnPropertyChanged(nameof(IsActive)); } }

        private static void ActiveComponentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is BuildComponentControl c)
            {
                c.UpdateActiveComponent();
            }
        }

        public BuildComponentControl()
        {
            this.InitializeComponent();
            (this.Content as FrameworkElement).DataContext = this;
        }

        public void UpdateActiveComponent()
        {
            IsActive = ActiveComponent == Component;
        }

        private void textBox_TextChanged(object sender, TextChangedEventArgs args)
        {
            QueryChanged?.Execute(textBox.Text);
            QueryUpdated?.Invoke(this, textBox.Text);

            //var matches = FuzzySharp.Process.ExtractTop(sender.Text, Items.Select(i => $"{i.Brand} {i.SKU} {i.Name}"), scorer: ScorerCache.Get<TokenDifferenceScorer>(), limit: 10).ToList();
            //Suggestions = new List<Item>();

            //for (int i = 0; i < matches.Count; i++)
            //{
            //    var match = matches[i];
            //    var item = Items[match.Index];
            //    Suggestions.Add(item);
            //}
        }

        private void textBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                QuerySubmitted?.Invoke(this, textBox.Text);
            }
        }

        public void SetTextBox(string text)
        {
            textBox.Text = text;
        }

        private void RemoveItemClick(object sender, RoutedEventArgs e)
        {
            RemoveCommand?.Execute(Component);
        }

        private void AddEmptyClick(object sender, RoutedEventArgs e)
        {
            AddEmptyCommand?.Execute(Component.Type);
        }

        private void AddDuplicateClick(object sender, RoutedEventArgs e)
        {
            AddDuplicateCommand?.Execute(Component);
        }

        private void InfoItemClick(object sender, RoutedEventArgs e)
        {
            InfoCommand?.Execute(Component);
        }

        private BuildComponent? GetPlan(int duration)
        {
            var price = Component.Item.Price;
            var type = price >= 500 ? PlanReference.PlanType.Carry_In : PlanReference.PlanType.Replacement;
            var plan = PlanReference.Get(type, price);
            if (plan == null)
            {
                return null;
            }

            var tier = plan.Tiers.FirstOrDefault(p => p.Duration == duration);
            var comp = new BuildComponent()
            {
                Type = BuildComponent.ComponentType.Plan,
                Item = new Item()
                {
                    Name = $"{duration} Year Plan",
                    Price = tier.Price,
                    OriginalPrice = tier.Price,
                    Brand = "Micro Center",
                    Quantity = 1,
                    Specs = new Dictionary<string, string>()
                    {
                        {"Duration", duration.ToString() }
                    }
                },
            };

            return comp;
        }

        private void AddPlan2Year(object sender, RoutedEventArgs e)
        {
            var comp = GetPlan(2);
            if (comp != null)
            {
                if(Component.Type == BuildComponent.ComponentType.BuildService)
                {
                    comp.Item.Specs.Add("Ref", "BUILD");
                }
                else
                {
                    comp.Item.Specs.Add("Ref", Component.Item.SKU);
                }

                AddPlan?.Execute(comp);
            }
        }

        private void AddPlan3Year(object sender, RoutedEventArgs e)
        {
            var comp = GetPlan(3);
            if (comp != null)
            {
                if (Component.Type == BuildComponent.ComponentType.BuildService)
                {
                    comp.Item.Specs.Add("Ref", "BUILD");
                }
                else
                {
                    comp.Item.Specs.Add("Ref", Component.Item.SKU);
                }
                AddPlan?.Execute(comp);
            }
        }

        private bool SetProperty<T>(ref T backingStore, T value, [CallerMemberName] string propertyName = "", Action? onChanged = null)
        {
            if (EqualityComparer<T>.Default.Equals(backingStore, value))
                return false;

            backingStore = value;
            onChanged?.Invoke();
            OnPropertyChanged(propertyName);
            return true;
        }

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            var changed = PropertyChanged;
            if (changed == null)
                return;

            changed.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        private void textBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if(!this.IsActive)
            {
                var buildPage = this.FindAscendant<BuildPage>();
                if(buildPage != null && buildPage.DataContext is BuildPageViewModel vm)
                {
                    vm.SelectedComponent = this.Component;
                }
            }
        }
    }
}
