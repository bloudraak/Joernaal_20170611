using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;

namespace Joernaal
{
    public class Collection
    {
        private readonly ConcurrentBag<Item> _items;
        private IConfigurationRoot _configuration;

        public Collection(string basePath)
        {
            SourcePath = Path.Combine(basePath, "src");
            TargetPath = Path.Combine(basePath, "dist");
            ThemesPath = Path.Combine(basePath, "themes");
            _items = new ConcurrentBag<Item>();

            var builder = new ConfigurationBuilder();            
            var joernaalPath = Path.Combine(SourcePath, ".joernal");
            builder.AddJsonFile(Path.Combine(joernaalPath, "collection.json"), true, false);
            _configuration = builder.Build();

            Properties = new Dictionary<string, string>();
            var section = _configuration.GetSection("Collection");
            section.Bind(Properties);
        }

        public IDictionary<string, string> Properties { get; set; }

        public string ThemesPath { get; }

        public string SourcePath { get; }

        public string TargetPath { get; }

        public IEnumerable<Item> Items => _items;

        public Item CreateItem(string path)
        {
            dynamic properties = null;
            var configurationPath = Path.GetDirectoryName(Path.Combine(SourcePath, path));
            do
            {
                var joernaalPath = Path.Combine(configurationPath, ".joernaal");

                var s = Path.Combine(joernaalPath, "collection.json");
                if (File.Exists(s))
                {
                    dynamic o = JObject.Parse(File.ReadAllText(s));
                    if (properties != null)
                    {
                        properties.Merge(o, new JsonMergeSettings(){MergeArrayHandling = MergeArrayHandling.Merge, MergeNullValueHandling = MergeNullValueHandling.Ignore});
                    }
                    else
                    {
                        properties = o;
                    }
                }

                if (string.Equals(configurationPath, SourcePath, StringComparison.OrdinalIgnoreCase))
                {
                    break;
                }
                configurationPath = Path.GetFullPath(Path.Combine(configurationPath, ".."));
            } while (true);

            if (properties == null)
            {
                properties = new JObject();
            }

            var item = new Item(this, path, properties);
            _items.Add(item);
            return item;
        }
    }
}