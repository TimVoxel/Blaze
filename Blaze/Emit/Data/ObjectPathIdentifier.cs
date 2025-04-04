﻿using Blaze.Symbols;

namespace Blaze.Emit.Data
{
    public class ObjectPathIdentifier : DataIdentifier
    {
        public override DataLocation Location { get; }
        public string StorageObject { get; }
        public string Path { get; }

        public override string Text
        {
            get
            {
                var location = EmittionFacts.GetSyntaxName(Location);
                return $"{location} {StorageObject} {Path}";
            }
        }

        public ObjectPathIdentifier(DataLocation location, string storageObject, string path)
        {
            Location = location;
            StorageObject = storageObject;
            Path = path;
        }
    }
}
