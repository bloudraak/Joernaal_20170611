// Copyright (c) Werner Strydom. All rights reserved.
// Licensed under the MIT license. See LICENSE in the project root for license information.

namespace Joernaal
{
    using System.IO;

    public class Item
    {
        internal Item(Collection parent, string path, dynamic properties)
        {
            Parent = parent;
            TargetPath = path;
            Properties = properties;
            SourcePath = path;
            Contents = File.ReadAllBytes(SourceFullPath);
            Metadata = new ItemMetadata();
        }

        public dynamic Properties { get; }


        public string SourceFullPath => Path.Combine(Parent.SourcePath, SourcePath);

        public string TargetFullPath => Path.Combine(Parent.TargetPath, TargetPath);

        public Collection Parent { get; }

        public string TargetPath { get; set; }

        public string SourcePath { get; }

        public byte[] Contents { get; set; }

        public ItemMetadata Metadata { get; }

        public override string ToString()
        {
            return $"{nameof(SourcePath)}: {SourcePath}, {nameof(TargetPath)}: {TargetPath}";
        }
    }
}