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

        public static Dictionary<string, List<CommissionLineItem>> SalesAssociates = new Dictionary<string, List<CommissionLineItem>>();

        public delegate void SalesAssociateUpdated(List<CommissionLineItem> items, string salesId);
        public static event SalesAssociateUpdated SalesAssociateUpdatedEvent;

        static OrderHistoryCache()
        {
            client = new HttpClient();
        }

        public static async Task<List<CommissionLineItem>> LoadSalesAssociate(string salesId)
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
                    var result = JsonConvert.DeserializeObject<List<CommissionLineItem>>(match.Groups[1].Value);
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
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            return default;
        }
    }
}
