﻿using Blaze.Emit.Data;
using Blaze.Symbols;
using System.Diagnostics;

namespace Blaze.Emit.Nodes
{
    public abstract class DataCommand : CommandNode
    {
        public override string Keyword => "data";
        public override EmittionNodeKind Kind => EmittionNodeKind.DataCommand;

        public static DataModifyCommand ModifyFrom(ObjectPathIdentifier target, DataModifyCommand.ModificationType modification, ObjectPathIdentifier source)
            => new DataModifyCommand(target, modification, null, new DataModifyCommand.FromSource(source));

        public static DataModifyCommand ModifyWithValue(ObjectPathIdentifier target, DataModifyCommand.ModificationType modification, string value)
            => new DataModifyCommand(target, modification, null, new DataModifyCommand.ValueSource(value));

        public static DataModifyCommand ModifyString(ObjectPathIdentifier target, DataModifyCommand.ModificationType modification, ObjectPathIdentifier source, int? startIndex = null, int? endIndex = null)
            => new DataModifyCommand(target, modification, null, new DataModifyCommand.StringSource(source, startIndex, endIndex));
       
        public static DataModifyCommand InsertFrom(ObjectPathIdentifier target, string insertIndex, DataLocation sourceLocation, ObjectPathIdentifier source)
            => new DataModifyCommand(target, DataModifyCommand.ModificationType.Insert, insertIndex, new DataModifyCommand.FromSource(source));

        public static DataModifyCommand InsertValue(ObjectPathIdentifier target, string insertIndex, string value)
            => new DataModifyCommand(target, DataModifyCommand.ModificationType.Insert, insertIndex, new DataModifyCommand.ValueSource(value));

        public static DataModifyCommand InsertString(ObjectPathIdentifier target, string insertIndex, ObjectPathIdentifier source, int? startIndex = null, int? endIndex = null)
            => new DataModifyCommand(target, DataModifyCommand.ModificationType.Insert, insertIndex, new DataModifyCommand.StringSource(source, startIndex, endIndex));

    }

    public class DataGetCommand : DataCommand
    {
        public ObjectPathIdentifier Identifier { get; }
        public string? Multiplier { get; }

        public override string Text =>
            Multiplier == null
                ? $"{Keyword} get {Identifier.Text}"
                : $"{Keyword} get {Identifier.Text} {Multiplier}";

        public DataGetCommand(ObjectPathIdentifier location, string? multiplier)
        {
            Identifier = location;
            Multiplier = multiplier;
        }
    }

    public class DataMergeCommand : DataCommand
    {
        public DataLocation Location { get; }
        public string StorageObject { get; }
        public string Value { get; }

        public override string Text => $"{Keyword} merge {EmittionFacts.GetSyntaxName(Location)} {StorageObject} {Value}";

        public DataMergeCommand(DataLocation location, string obj, string value)
        {
            Location = location;
            StorageObject = obj;
            Value = value;
        }
    }

    public class DataRemoveCommand : DataCommand
    {
        public ObjectPathIdentifier Identifier { get; }

        public override string Text => $"{Keyword} remove {Identifier.Text}";

        public DataRemoveCommand(ObjectPathIdentifier location)
        {
            Identifier = location;
        }
    }

    public class DataModifyCommand : DataCommand
    {
        public abstract class DataModifySource
        {
            public abstract string Text { get; }
        }
    
        public class FromSource : DataModifySource
        {
            public ObjectPathIdentifier Identifier { get; }

            public override string Text => $"from {Identifier.Text}";

            public FromSource(ObjectPathIdentifier locationClause)
            {
                Identifier = locationClause;
            }
        }

        public class ValueSource : DataModifySource 
        { 
            public string Value { get; }
            public override string Text => $"value {Value}";

            public ValueSource(string value)
            {
                Value = value;
            }
        }

        public class StringSource : DataModifySource
        {
            public ObjectPathIdentifier Identifier { get; }
            public int? StartIndex { get; }
            public int? EndIndex { get; }

            public override string Text
            {
                get
                {
                    if (StartIndex != null)
                    {
                        if (EndIndex != null)
                            return $"string {Identifier.Text} {StartIndex} {EndIndex}";
                        else
                            return $"string {Identifier.Text} {StartIndex}";
                    }
                    else
                    {
                        return $"string {Identifier.Text}";
                    }
                }
            }

            public StringSource(ObjectPathIdentifier location, int? startIndex, int? endIndex)
            {
                Identifier = location;
                StartIndex = startIndex;
                EndIndex = endIndex;
            }
        }

        public enum ModificationType
        {
            Set,
            Merge,
            Append,
            Prepend,
            Insert
        }

        public ObjectPathIdentifier TargetIdentifier { get; }
        public ModificationType Modification { get; }
        public string? InsertIndex { get; }
        public DataModifySource Source { get; }

        public override string Text
        {
            get
            {
                if (Modification == ModificationType.Insert)
                {
                    Debug.Assert(InsertIndex != null);
                    return $"{Keyword} modify {TargetIdentifier.Text} {Modification.ToString().ToLower()} {InsertIndex} {Source.Text}";
                }
                else
                {
                    return $"{Keyword} modify {TargetIdentifier.Text} {Modification.ToString().ToLower()} {Source.Text}";
                }
            }
        }

        public DataModifyCommand(ObjectPathIdentifier location, ModificationType modification, string? insertIndex, DataModifySource source)
        {
            TargetIdentifier = location;
            Modification = modification;
            InsertIndex = insertIndex;
            Source = source;
        }

    }
}
