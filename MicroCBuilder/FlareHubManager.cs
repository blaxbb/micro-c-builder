using DataFlareClient;
using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MicroCBuilder
{
    public static class FlareHubManager
    {
        static HubConnection Hub;
        static List<string> tagsInitialized = new List<string>();

        public delegate void FlareReceived(Flare f);
        public static event FlareReceived OnFlareReceived;

        public static void Initialize()
        {
            if (Hub == null)
            {
                Hub = new HubConnectionBuilder()
                        .WithUrl(@"https://dataflare.bbarrett.me/FlareHub")
                        .Build();

                Hub.Closed += async (args) =>
                {
                    await RepeatConnection();
                };
            }

            Task.Run(RepeatConnection);
        }

        private static async Task RepeatConnection()
        {
            while (true)
            {
                var connected = await ConnectToHub();
                if (connected)
                {
                    return;
                }

                await Task.Delay(new Random().Next(1, 5) * 1000);
            }
        }

        private static async Task<bool> ConnectToHub()
        {
            try
            {
                await Hub.StartAsync();

                if (Hub.State == HubConnectionState.Connected)
                {
                    return true;
                }
            }
            catch (Exception ex)
            {

            }
            return false;
        }

        public static void Subscribe(string tag)
        {
            if (!tagsInitialized.Contains(tag))
            {
                Hub.On<Flare>($"flare-{tag}", (f) => OnFlareReceived?.Invoke(f));
                tagsInitialized.Add(tag);
            }
        }
    }
}
