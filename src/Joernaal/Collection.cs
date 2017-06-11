using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;

namespace Joernaal
{
    public class Collection
    {
        private readonly ConcurrentBag<Item> _items;

        public Collection(string basePath)
        {
            SourcePath = Path.Combine(basePath, "src");
            TargetPath = Path.Combine(basePath, "dist");
            ThemesPath = Path.Combine(basePath, "themes");
            _items = new ConcurrentBag<Item>();
        }

        public string ThemesPath { get; }

        public string SourcePath { get; }

        public string TargetPath { get; }

        public IEnumerable<Item> Items => _items;

        public Item CreateItem(string path)
        {
            var item = new Item(this, path);
            _items.Add(item);
            return item;
        }
    }
}