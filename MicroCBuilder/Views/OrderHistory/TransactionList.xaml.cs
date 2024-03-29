﻿using MicroCBuilder.Models.OrderHistory;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace MicroCBuilder.Views.OrderHistory
{
    public sealed partial class TransactionList : UserControl
    {


        public List<TransactionLineItem> Items
        {
            get { return (List<TransactionLineItem>)GetValue(ItemsProperty); }
            set { SetValue(ItemsProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Items.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ItemsProperty =
            DependencyProperty.Register("Items", typeof(List<TransactionLineItem>), typeof(TransactionList), new PropertyMetadata(new List<TransactionLineItem>(), propchanged));

        private static void propchanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if(e.Property == ItemsProperty && d is TransactionList list)
            {
                list.SetupTransactions(list.Items);
            }
        }

        public TransactionList()
        {
            this.InitializeComponent();
        }

        public void SetupTransactions(List<TransactionLineItem> lines)
        {
            var groups = lines
                .GroupBy(l => l.TransactionNumber)
                .Select(g => new Transaction() { TransactionNumber = g.Key, TransactionItems = g.ToList() });

            transactions.ItemsSource = groups;

            //var Items = new ObservableCollection<GroupInfoCollection<TransactionLineItem>>();
            //foreach (var group in groups)
            //{
            //    var collection = new GroupInfoCollection<TransactionLineItem>();
            //    collection.Key = group.GroupName;
            //    foreach (var item in group.Items)
            //    {
            //        collection.Add(item);
            //    }
            //    collection.Add(new TransactionLineItem() { Total = group.Items.Sum(i => i.Total) });
            //    Items.Add(collection);
            //}

            //CollectionViewSource groupedItems = new CollectionViewSource();
            //groupedItems.IsSourceGrouped = true;
            //groupedItems.Source = Items;

            //transactions.ItemsSource = groupedItems.View;
        }
    }

    public class GroupInfoCollection<T> : ObservableCollection<T>
    {
        public object Key { get; set; }

        public new IEnumerator<T> GetEnumerator()
        {
            return (IEnumerator<T>)base.GetEnumerator();
        }
    }
}
