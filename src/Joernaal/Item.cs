using System.IO;

namespace Joernaal
{
    public class Item
    {
        internal Item(Collection parent, string path)
        {
            Parent = parent;
            TargetPath = path;
            SourcePath = path;
            Contents = File.ReadAllBytes(SourceFullPath);
            Metadata = new ItemMetadata();
        }

        public string SourceFullPath => Path.Combine(Parent.SourcePath, SourcePath);

        public string TargetFullPath => Path.Combine(Parent.TargetPath, TargetPath);

        public Collection Parent { get; }

        public string TargetPath { get; set; }

        public string SourcePath { get; }

        public byte[] Contents { get; set; }

        public override string ToString()
        {
            return $"{nameof(SourcePath)}: {SourcePath}, {nameof(TargetPath)}: {TargetPath}";
        }

        public ItemMetadata Metadata { get; }
    }
}