﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Sledge.Formats.Bsp.Lumps;
using Sledge.Formats.Bsp.Readers;
using Sledge.Formats.Bsp.Writers;

namespace Sledge.Formats.Bsp
{
    public class BspFile
    {
        public Version Version { get; set; }
        public List<Blob> Blobs { get; set; }
        public List<ILump> Lumps { get; set; }

        public BspFile(Stream stream)
        {
            using (BinaryReader br = new BinaryReader(stream, Encoding.ASCII, true))
            {
                // Read the version number
                // More recent formats have a "magic" number when different engines forked
                Magic magic = (Magic) br.ReadUInt32();
                switch (magic)
                {
                    case Magic.Ibsp:
                    case Magic.Vbsp:
                        Version = (Version) ((br.ReadUInt32() << 32) + magic);
                        break;
                    default:
                        Version = (Version) magic;
                        break;
                }

                // Initialise the reader
                IBspReader reader = _readers.First(x => x.SupportedVersion == Version);

                reader.StartHeader(this, br);

                // Read the blobs
                Blobs = new List<Blob>();
                for (int i = 0; i < reader.NumLumps; i++)
                {
                    Blob blob = reader.ReadBlob(br);
                    blob.Index = i;
                    Blobs.Add(blob);
                }

                reader.EndHeader(this, br);

                Lumps = new List<ILump>();
                foreach (Blob blob in Blobs)
                {
                    ILump lump = reader.GetLump(blob);
                    if (lump == null) continue;

                    long pos = br.BaseStream.Position;
                    br.BaseStream.Seek(blob.Offset, SeekOrigin.Begin);

                    lump.Read(br, blob, Version);
                    Lumps.Add(lump);

                    br.BaseStream.Seek(pos, SeekOrigin.Begin);
                }

                foreach (ILump lump in Lumps)
                {
                    lump.PostReadProcess(this);
                }
            }
        }

        public void WriteToStream(Stream s, Version version)
        {
            IBspWriter writer = _writers.First(x => x.SupportedVersion == version);

            foreach (ILump lump in Lumps)
            {
                lump.PreWriteProcess(this, version);
            }

            using (BinaryWriter bw = new BinaryWriter(s, Encoding.ASCII, true))
            {
                writer.SeekToFirstLump(bw);
                List<Blob> lumps = writer.GetLumps(this)
                    .Select((x, i) => new Blob
                    {
                        Offset = (int) bw.BaseStream.Position,
                        Length = x.Write(bw, version),
                        Index = i
                    })
                    .ToList();
                bw.Seek(0, SeekOrigin.Begin);
                writer.WriteHeader(this, lumps, bw);
            }
        }

        public T GetLump<T>() where T : ILump
        {
            return (T) Lumps.FirstOrDefault(x => x is T);
        }

        readonly IBspReader[] _readers =
        {
            new GoldsourceBspReader(),
            new Quake1BspReader(),
            new Quake2BspReader(), 
        };

        readonly IBspWriter[] _writers =
        {
            new GoldsourceBspWriter(),
            new Quake1BspWriter(), 
        };
    }
}