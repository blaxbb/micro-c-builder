using MicroCBuilder.Models.OrderHistory;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MicroCBuilder
{
    public static class OrderHistoryCache
    {
        private static HttpClient client;

        public static Dictionary<string, List<TransactionLineItem>> SalesAssociates = new Dictionary<string, List<TransactionLineItem>>();

        public delegate void SalesAssociateUpdated(List<TransactionLineItem> items, string salesId);
        public static event SalesAssociateUpdated SalesAssociateUpdatedEvent;

        static OrderHistoryCache()
        {
            client = new HttpClient();
        }

        public static async Task<List<TransactionLineItem>> LoadSalesAssociate(string salesId)
        {
            try
            {
                var regex = "#Sales.*{\"Data\":(?<transactions>.*),\"Total";
                var productFile = await Windows.Storage.StorageFile.GetFileFromPathAsync(@"C:\Users\bbarr\OneDrive\Documents\MicroCBuilder\Sales Lookup - 3_8_2019.html");
                var html = await Windows.Storage.FileIO.ReadTextAsync(productFile);
                //var result = await client.GetAsync("file:///C:/dev/MCSalesHistory/Sales%20Lookup%20-%203_8_2019.html");
                //if (!result.IsSuccessStatusCode)
                //{
                //    return;
                //}

                //var html = await result.Content.ReadAsStringAsync();
                var match = Regex.Match(html, regex);
                if (match.Success)
                {
                    Console.WriteLine(match.Groups[1].Value);
                    var result = JsonConvert.DeserializeObject<List<TransactionLineItem>>(match.Groups[1].Value);
                    if (SalesAssociates.ContainsKey(salesId))
                    {
                        SalesAssociates[salesId] = result;
                    }
                    else
                    {
                        SalesAssociates.Add(salesId, result);
                    }
                    SalesAssociateUpdatedEvent?.Invoke(result, salesId);
                    return result;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            return default;
        }

        public static async Task<Transaction> LoadTransaction(string transactionId)
        {
            try
            {
                var regex = "{\"Data\":\\[(?<data>.*)]";
                var transactionFile = await Windows.Storage.StorageFile.GetFileFromPathAsync(@"C:\Users\bbarr\OneDrive\Documents\MicroCBuilder\Order History.html");
                var html = await Windows.Storage.FileIO.ReadTextAsync(transactionFile);
                //var result = await client.GetAsync("file:///C:/dev/MCSalesHistory/Sales%20Lookup%20-%203_8_2019.html");
                //if (!result.IsSuccessStatusCode)
                //{
                //    return;
                //}

                //var html = await result.Content.ReadAsStringAsync();
                var match = Regex.Match(html, regex);
                if (match.Success)
                {
                    Console.WriteLine("Success");
                    var ret = JsonConvert.DeserializeObject<Transaction>(match.Groups[1].Value);
                    ret.TransactionItems.ForEach(t => t.TransactionNumber = ret.TransactionNumber);
                    return ret;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            return default;
        }
    }
}