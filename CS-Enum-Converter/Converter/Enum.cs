using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace CSEnumConverter.Converter
{
    internal class Enum
    {
        internal struct Entry
        {
            public int Bit;
            public int Value;
            public string Name;
        }

        public List<Entry> Entries { get; } = new List<Entry>();
        public string Name { get; private set; }
        public string MetadataName { get; private set; }
        public string AccessPrefix { get; private set; }
        public ISymbol EnumSymbol { get; private set; }

        /// <summary>
        /// Parses the code and extracts the enum information.
        /// </summary>
        /// <param name="text">The code to parse.</param>
        public void ParseCode(string text)
        {
            var tree = CSharpSyntaxTree.ParseText(text);
            var root = tree.GetCompilationUnitRoot();

            var compilation = CSharpCompilation.Create("Compilation enum").AddReferences(MetadataReference.CreateFromFile(typeof(string).Assembly.Location)).AddSyntaxTrees(tree);
            SemanticModel model = compilation.GetSemanticModel(tree);

            var enumDeclaration = root.DescendantNodesAndSelf().OfType<EnumDeclarationSyntax>().FirstOrDefault();
            if (enumDeclaration == null)
            {
                Logger.WriteError("No valid enum has been provided");
                return;
            }

            var enumNodes = enumDeclaration.DescendantNodes().OfType<EnumMemberDeclarationSyntax>();

            Name = enumDeclaration.Identifier.ToString();

            var enumSymbol = model.GetDeclaredSymbol(enumDeclaration);
            MetadataName = Helper.GetFullMetadataName(enumSymbol);

            foreach (EnumMemberDeclarationSyntax enumNode in enumNodes)
            {
                var enumNodeSymbol = model.GetDeclaredSymbol(enumNode);
                if (enumNodeSymbol != null)
                {
                    int value = Convert.ToInt32(enumNodeSymbol.ConstantValue);
                    Entry entry = new Entry { Bit = -1 };

                    if (value != 0 && IsNumberPowerOfTwo(value))
                        entry.Bit = (int)Math.Log(value, 2);

                    entry.Value = value;
                    entry.Name = enumNode.Identifier.ToString();
                    Entries.Add(entry);
                }
            }
        }

        /// <summary>
        /// Gets the string representation of the entries that match the given number.
        /// </summary>
        /// <param name="number">The number to match.</param>
        /// <param name="entryCount">The number of entries that match the given number.</param>
        /// <returns>The string representation of the matching entries.</returns>
        public string GetEntriesStringFromNumber(int number, out int entryCount)
        {
            var entries = GetEntriesFromNumber(number);
            entryCount = entries.Count;

            string result = "";
            for (int i = 0; i < entries.Count; i++)
            {
                result += AccessPrefix + entries[i].Name;
                if (i < entries.Count - 1)
                    result += " | ";
            }

            return result;
        }

        /// <summary>
        /// Gets the entries that match the given number.
        /// </summary>
        /// <param name="number">The number to match.</param>
        /// <returns>The list of matching entries.</returns>
        public List<Entry> GetEntriesFromNumber(int number)
        {
            var entries = new List<Entry>();

            // Check if the number matches an entry value
            foreach (var entry in Entries)
            {
                if (entry.Value == number)
                {
                    entries.Add(entry);

                    return entries;
                }
            }

            // Get the entries from the corresponding bits of the number
            var activeBits = GetActiveBits(number);

            for (int i = 0; i < activeBits.Count; i++)
            {
                if (activeBits[i])
                {
                    var entry = GetEntryFromBit(i);
                    if (entry != null)
                        entries.Add((Entry)entry);
                    else
                        Logger.WriteError(string.Format("Could not resolve all enum entries from number: {0}", number));
                }
            }

            if (!entries.Any())
                Logger.WriteError(string.Format("Could not resolve all enum entries from number: {0}", number));

            return entries;
        }

        /// <summary>
        /// Compares the entries of this enum with the entries of another enum.
        /// </summary>
        /// <param name="other">The other enum to compare with.</param>
        /// <returns>True if the entries are equal, false otherwise.</returns>
        public bool CompareEntries(Enum other)
        {
            if (Entries.Count != other.Entries.Count)
                return false;

            for (int i = 0; i < Entries.Count; i++)
            {
                if (!Entries[i].Equals(other.Entries[i]))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Sets the symbol of the enum.
        /// </summary>
        /// <param name="symbol">The symbol to set.</param>
        public void SetSymbol(ISymbol symbol)
        {
            EnumSymbol = symbol;
            AccessPrefix = Helper.GetFullyQualifiedName(symbol) + ".";
        }

        /// <summary>
        /// Gets the entry with a value of zero.
        /// </summary>
        /// <returns>The entry with a value of zero, or null if not found.</returns>
        public Entry? GetZeroEntry()
        {
            foreach (var entry in Entries)
            {
                if (entry.Value == 0)
                    return entry;
            }

            return null;
        }

        /// <summary>
        /// Checks if a number is a power of two.
        /// </summary>
        /// <param name="number">The number to check.</param>
        /// <returns>True if the number is a power of two, false otherwise.</returns>
        private bool IsNumberPowerOfTwo(int number)
        {
            return (number & (number - 1)) == 0;
        }

        /// <summary>
        /// Gets the entry with the specified bit position.
        /// </summary>
        /// <param name="bit">The bit position.</param>
        /// <returns>The entry with the specified bit position, or null if not found.</returns>
        private Entry? GetEntryFromBit(int bit)
        {
            foreach (var entry in Entries)
            {
                if (entry.Bit == bit && entry.Value != 0)
                    return entry;
            }

            return null;
        }

        /// <summary>
        /// Gets the active bits of a number.
        /// </summary>
        /// <param name="number">The number.</param>
        /// <returns>The active bits as a BitArray.</returns>
        private BitArray GetActiveBits(int number)
        {
            return new BitArray(new int[] { number });
        }
    }
}
