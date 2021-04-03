using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Sledge.Formats.Bsp.Objects;
using Sledge.Formats.Id;

namespace Sledge.Formats.Bsp.Lumps
{
    public class Textures : ILump, IList<MipTexture>
    {
        readonly IList<MipTexture> _textures;

        public Textures()
        {
            _textures = new List<MipTexture>();
        }

        public void Read(BinaryReader br, Blob blob, Version version)
        {
            uint numTextures = br.ReadUInt32();
            int[] offsets = new int[numTextures];
            for (int i = 0; i < numTextures; i++) offsets[i] = br.ReadInt32();
            foreach (int offset in offsets)
            {
                br.BaseStream.Seek(blob.Offset + offset, SeekOrigin.Begin);
                MipTexture tex = MipTexture.Read(br, version == Version.Goldsource);
                _textures.Add(tex);
            }
        }

        public void PostReadProcess(BspFile bsp)
        {
            
        }

        public void PreWriteProcess(BspFile bsp, Version version)
        {
            
        }

        public int Write(BinaryWriter bw, Version version)
        {
            long pos = bw.BaseStream.Position;

            bw.Write((uint) _textures.Count);
            bw.Seek(sizeof(int) * _textures.Count, SeekOrigin.Current);

            int[] offsets = new int[_textures.Count];
            for (int i = 0; i < _textures.Count; i++)
            {
                MipTexture tex = _textures[i];
                offsets[i] = (int) (bw.BaseStream.Position - pos);
                MipTexture.Write(bw, version == Version.Goldsource, tex);
            }

            long pos2 = bw.BaseStream.Position;
            bw.BaseStream.Seek(pos + sizeof(uint), SeekOrigin.Begin);
            foreach (int offset in offsets) bw.Write((int) offset);
            bw.BaseStream.Seek(pos2, SeekOrigin.Begin);

            return (int)(bw.BaseStream.Position - pos);
        }

        #region IList

        public IEnumerator<MipTexture> GetEnumerator()
        {
            return _textures.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable) _textures).GetEnumerator();
        }

        public void Add(MipTexture item)
        {
            _textures.Add(item);
        }

        public void Clear()
        {
            _textures.Clear();
        }

        public bool Contains(MipTexture item)
        {
            return _textures.Contains(item);
        }

        public void CopyTo(MipTexture[] array, int arrayIndex)
        {
            _textures.CopyTo(array, arrayIndex);
        }

        public bool Remove(MipTexture item)
        {
            return _textures.Remove(item);
        }

        public int Count => _textures.Count;

        public bool IsReadOnly => _textures.IsReadOnly;

        public int IndexOf(MipTexture item)
        {
            return _textures.IndexOf(item);
        }

        public void Insert(int index, MipTexture item)
        {
            _textures.Insert(index, item);
        }

        public void RemoveAt(int index)
        {
            _textures.RemoveAt(index);
        }

        public MipTexture this[int index]
        {
            get => _textures[index];
            set => _textures[index] = value;
        }

        #endregion
    }
}