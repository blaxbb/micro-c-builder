using MicroCBuilder.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MicroCBuilder
{
    public class ChecklistFavoriteCache
    {
        private const string FILENAME = "checklistcache.json";
        public static ChecklistFavoriteCache? Current;
        public List<Checklist> Favorites = new List<Checklist>();

        public delegate void ChecklistFavoritesUpdated(ChecklistFavoriteCache sender);
        public static event ChecklistFavoritesUpdated OnChecklistFavoritesUpdated;

        public ChecklistFavoriteCache()
        {
            Current = this;
        }

        public async Task<bool> LoadCache()
        {
            try
            {
                if (File.Exists($"{Windows.Storage.ApplicationData.Current.LocalFolder.Path}/{FILENAME}"))
                {
                    var folder = Windows.Storage.ApplicationData.Current.LocalFolder;
                    var file = await folder.GetFileAsync(FILENAME);
                    if (file != null)
                    {
                        var text = await Windows.Storage.FileIO.ReadTextAsync(file);
                        Favorites = JsonConvert.DeserializeObject<List<Checklist>>(text);
                        Favorites.ForEach(f => f.IsFavorited = true);
                        OnChecklistFavoritesUpdated(this);
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            return false;
        }

        public async Task SaveCache()
        {
            OnChecklistFavoritesUpdated(this);
            var json = System.Text.Json.JsonSerializer.Serialize(Favorites, new System.Text.Json.JsonSerializerOptions() { WriteIndented = true });
            var file = await Windows.Storage.ApplicationData.Current.LocalFolder.CreateFileAsync(FILENAME, Windows.Storage.CreationCollisionOption.ReplaceExisting);
            await Windows.Storage.FileIO.WriteTextAsync(file, json);
        }

        public async Task AddItem(Checklist checklist)
        {
            var existing = Favorites.FirstOrDefault(c => c.Id == checklist.Id);
            if(existing != null)
            {
                if(existing.Created <= checklist.Created)
                {
                    return;
                }

                Favorites.Remove(existing);
            }
            Favorites.Add(checklist);
            await SaveCache();
        }

        public async Task RemoveItem(Checklist checklist)
        {
            var existing = Favorites.FirstOrDefault(c => c.Id == checklist.Id);
            if (existing != null)
            {
                Favorites.Remove(existing);
                await SaveCache();
            }
        }

        public bool IsFavorited(Checklist checklist) => Favorites.Any(c => c.Id == checklist.Id);
    }
}