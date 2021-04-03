using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Sledge.Formats.Packages
{
    // http://quakewiki.org/wiki/.pak
    public class PakPackage : IPackage
    {
        const string Signature = "PACK";

        readonly Stream _stream;
        readonly List<PackageEntry> _entries;

        public IEnumerable<PackageEntry> Entries => _entries;

        public PakPackage(string file)
        {
            _entries = new List<PackageEntry>();
            _stream = Stream.Synchronized(new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 4096, FileOptions.RandomAccess));

            // Read the data from the pak
            using (BinaryReader br = new BinaryReader(_stream, Encoding.ASCII, true))
            {
                string sig = br.ReadFixedLengthString(Encoding.ASCII, 4);
                if (sig != Signature) throw new Exception($"Unknown package signature: Expected '{Signature}', got '{sig}'.");

                int treeOffset = br.ReadInt32();
                int treeLength = br.ReadInt32();

                // Read all the entries from the pak
                br.BaseStream.Position = treeOffset;
                int numEntries = treeLength / 64;
                for (int i = 0; i < numEntries; i++)
                {
                    string path = br.ReadFixedLengthString(Encoding.ASCII, 56).ToLowerInvariant();
                    int offset = br.ReadInt32();
                    int size = br.ReadInt32();
                    _entries.Add(new PackageEntry
                    {
                        Path = path,
                        Offset = offset,
                        Size = size
                    });
                }
            }
        }

        public Stream Open(PackageEntry entry)
        {
            return new SubStream(_stream, entry.Offset, entry.Size);
        }

        public void Dispose()
        {
            _stream?.Dispose();
        }
    }
}
