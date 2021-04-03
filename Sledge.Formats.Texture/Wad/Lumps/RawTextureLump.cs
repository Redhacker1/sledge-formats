using System.IO;
using Sledge.Formats.Id;

namespace Sledge.Formats.Texture.Wad.Lumps
{
    public class RawTextureLump : Id.MipTexture, ILump
    {
        public virtual LumpType Type => LumpType.RawTexture;

        public RawTextureLump(BinaryReader br) : this(br, false)
        {
            //
        }

        protected RawTextureLump(BinaryReader br, bool readPalette)
        {
            MipTexture t = Read(br, readPalette);
            Name = t.Name;
            Width = t.Width;
            Height = t.Height;
            NumMips = t.NumMips;
            MipData = t.MipData;
            Palette = t.Palette;
        }

        public virtual int Write(BinaryWriter bw)
        {
            long pos = bw.BaseStream.Position;
            Write(bw, false, this);
            return (int) (bw.BaseStream.Position - pos);
        }
    }
}