using MicroCBuilder.Models;
using MicroCLib.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace MicroCBuilder
{
    public static class BuildLibrary
    {
        public const string ROOT_DIRECTORY_NAME = "MicroCBuilder";
        public const string LIBRARY_DIRECTORY_NAME = "library";
        public const string AUTOSAVE_DIRECTORY_NAME = "autosave";

        static async Task<StorageFolder> LibraryFolder()
        {
            var folder = await KnownFolders.DocumentsLibrary.GetFolderAsync(ROOT_DIRECTORY_NAME);
            return await folder.GetFolderAsync(LIBRARY_DIRECTORY_NAME);
        }

        static async Task<StorageFolder> AutosaveFolder()
        {
            var folder = await KnownFolders.DocumentsLibrary.GetFolderAsync(ROOT_DIRECTORY_NAME);
            var library = await folder.GetFolderAsync(LIBRARY_DIRECTORY_NAME);
            return await library.GetFolderAsync(AUTOSAVE_DIRECTORY_NAME);
        }

        static BuildLibrary()
        {
            Task.Run(async () =>
            {
                try
                {
                    var root = await KnownFolders.DocumentsLibrary.CreateFolderAsync(ROOT_DIRECTORY_NAME, CreationCollisionOption.OpenIfExists);
                    var library = await root.CreateFolderAsync($"{LIBRARY_DIRECTORY_NAME}", CreationCollisionOption.OpenIfExists);
                    var autosave = await library.CreateFolderAsync($"{AUTOSAVE_DIRECTORY_NAME}", CreationCollisionOption.OpenIfExists);
                }
                catch(Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }).GetAwaiter().GetResult();
        }

        public static async Task<List<ProductList>> GetLibrary()
        {
            var ret = new List<ProductList>();
            var pathsToRemove = new List<string>();
            var paths = await GetSavedPaths();

            foreach(var path in paths)
            {
                ProductList list = null;
                try
                {
                    var productFile = await StorageFile.GetFileFromPathAsync(path);
                    var json = await FileIO.ReadTextAsync(productFile);

                    ret.Add(JsonConvert.DeserializeObject<ProductList>(json));
                }
                catch (FileNotFoundException e)
                {
                    pathsToRemove.Add(path);
                }
                catch(Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }

            pathsToRemove.ForEach(p => paths.Remove(p));

            var folder = await LibraryFolder();
            var file = await folder.CreateFileAsync("saved.json", CreationCollisionOption.OpenIfExists);
            await FileIO.WriteTextAsync(file, JsonConvert.SerializeObject(paths, Formatting.Indented));

            return ret;
        }

        public static async Task RegisterLibraryEntry(string path)
        {
            try
            {
                var existing = await GetSavedPaths();
                if (existing.Contains(path))
                {
                    return;
                }

                existing.Add(path);

                var folder = await LibraryFolder();
                var file = await folder.CreateFileAsync("saved.json", CreationCollisionOption.OpenIfExists);
                await FileIO.WriteTextAsync(file, JsonConvert.SerializeObject(existing, Formatting.Indented));
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.Message);
            }
        }

        public static async Task RemoveLibraryEntry(ProductList toRemove)
        {
            await RemoveLibraryEntry(toRemove.Guid);
        }

        public static async Task RemoveLibraryEntry(Guid toRemove)
        {
            try
            {
                var paths = await GetSavedPaths();
                List<string> pathsToRemove = new List<string>();
                foreach (var path in paths)
                {
                    try
                    {
                        var productFile = await StorageFile.GetFileFromPathAsync(path);
                        var json = await FileIO.ReadTextAsync(productFile);

                        var list = JsonConvert.DeserializeObject<ProductList>(json);
                        if(list.Guid == toRemove)
                        {
                            pathsToRemove.Add(path);
                        }
                    }
                    catch(FileNotFoundException e)
                    {

                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                    }
                }

                pathsToRemove.ForEach(p => paths.Remove(p));

                var folder = await LibraryFolder();
                var file = await folder.CreateFileAsync("saved.json", CreationCollisionOption.OpenIfExists);
                await FileIO.WriteTextAsync(file, JsonConvert.SerializeObject(paths, Formatting.Indented));
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.Message);
            }
        }

        public static async Task<(ProductList? list, string? path)> Get(Guid guid)
        {
            try
            {
                var paths = await GetSavedPaths();
                foreach (var path in paths)
                {
                    try
                    {
                        var productFile = await StorageFile.GetFileFromPathAsync(path);
                        var json = await FileIO.ReadTextAsync(productFile);

                        var list = JsonConvert.DeserializeObject<ProductList>(json);
                        if(list.Guid == guid)
                        {
                            return (list, path);
                        }
                    }
                    catch (FileNotFoundException e)
                    {

                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                    }
                }
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.Message);
            }

            return default;
        }

        public static async Task Save(StorageFile file, ProductList list)
        {
            try
            {
                var json = System.Text.Json.JsonSerializer.Serialize(list, new System.Text.Json.JsonSerializerOptions() { WriteIndented = true });
                await FileIO.WriteTextAsync(file, json);
                await RegisterLibraryEntry(file.Path);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        public static async Task<ProductList> SaveNew(List<BuildComponent> Components, string name, string author)
        {
            // write to file
            var list = new ProductList(Components.Where(c => c.Item != null).ToList())
            {
                Name = name,
                Author = author
            };

            try
            {
                var folder = await AutosaveFolder();
                var filename = string.IsNullOrWhiteSpace(name) ? $"{list.Guid.ToString()}.build" : $"{name}_{list.Guid.ToString().Substring(0, 8)}.build";
                var file = await folder.CreateFileAsync(filename, CreationCollisionOption.ReplaceExisting);

                await Save(file, list);
            }
            catch(Exception e)
            {
                Console.WriteLine(e.Message);
            }
            return list;
        }

        public static async Task<ProductList> SaveExisting(Guid guid, List<BuildComponent> Components, string name = default, string author = default)
        {
            var (existing, path) = await Get(guid);
            if (existing == null)
            {
                return await SaveNew(Components, name, author);
            }

            var list = new ProductList(Components.Where(c => c.Item != null).ToList())
            {
                Name = string.IsNullOrWhiteSpace(name) ? existing.Name : name,
                Author = string.IsNullOrWhiteSpace(author) ? existing.Author: author,
                Guid = guid
            };

            try
            {
                var dir = Path.GetDirectoryName(path);
                var folder = await StorageFolder.GetFolderFromPathAsync(dir);
                var file = await folder.CreateFileAsync(Path.GetFileName(path), CreationCollisionOption.ReplaceExisting);

                await Save(file, list);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            return list;
        }

        static async Task<List<string>> GetSavedPaths()
        {
            try
            {
                var folder = await LibraryFolder();
                var existing = await folder.GetFileAsync("saved.json");
                var json = await FileIO.ReadTextAsync(existing);
                return JsonConvert.DeserializeObject<List<string>>(json);
            }
            catch (Exception e)
            {

            }

            return new List<string>();
        }
    }
}
