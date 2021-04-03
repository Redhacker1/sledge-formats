using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using Sledge.Formats.Bsp.Objects;
using Plane = Sledge.Formats.Bsp.Objects.Plane;

namespace Sledge.Formats.Bsp.Lumps
{
    public class Lightmaps : ILump, IList<Lightmap>
    {
        readonly IList<Lightmap> _lightmaps;
        byte[] _lightmapData;

        public Lightmaps()
        {
            _lightmaps = new List<Lightmap>();
        }

        public void Read(BinaryReader br, Blob blob, Version version)
        {
            _lightmapData = br.ReadBytes(blob.Length);
        }

        public void PostReadProcess(BspFile bsp)
        {
            Texinfo textureInfos = bsp.GetLump<Texinfo>();
            Planes planes = bsp.GetLump<Planes>();
            Surfedges surfEdges = bsp.GetLump<Surfedges>();
            Edges edges = bsp.GetLump<Edges>();
            Vertices vertices = bsp.GetLump<Vertices>();
            List<Face> faces = bsp.GetLump<Faces>()
                .Where(x => x.Styles.Length > 0 && x.Styles[0] != byte.MaxValue) // Indicates a fullbright face, no offset
                .Where(x => x.LightmapOffset >= 0 && x.LightmapOffset < _lightmapData.Length) // Invalid offset
                .ToList();

            Dictionary<int, Lightmap> offsetDict = new Dictionary<int, Lightmap>();
            foreach (Face face in faces)
            {
                if (offsetDict.ContainsKey(face.LightmapOffset)) continue;

                TextureInfo ti = textureInfos[face.TextureInfo];
                Plane pl = planes[face.Plane];

                List<Vector2> uvs = new List<Vector2>();
                for (int i = 0; i < face.NumEdges; i++)
                {
                    int ei = surfEdges[face.FirstEdge + i];
                    Edge edge = edges[Math.Abs(ei)];
                    Vector3 point = vertices[ei > 0 ? edge.Start : edge.End];

                    Vector3 sn = new Vector3(ti.S.X, ti.S.Y, ti.S.Z);
                    float u = Vector3.Dot(point, sn) + ti.S.W;

                    Vector3 tn = new Vector3(ti.T.X, ti.T.Y, ti.T.Z);
                    float v = Vector3.Dot(point, tn) + ti.T.W;

                    uvs.Add(new Vector2(u, v));
                }

                float minu = uvs.Min(x => x.X);
                float maxu = uvs.Max(x => x.X);
                float minv = uvs.Min(x => x.Y);
                float maxv = uvs.Max(x => x.Y);

                int width = (int) Math.Ceiling(maxu / 16) - (int)Math.Floor(minu / 16) + 1;
                int height = (int) Math.Ceiling(maxv / 16) - (int)Math.Floor(minv / 16) + 1;
                int bpp = bsp.Version == Version.Quake1 ? 1 : 3;

                byte[] data = new byte[bpp * width * height];
                Array.Copy(_lightmapData, face.LightmapOffset, data, 0, data.Length);

                Lightmap map = new Lightmap
                {
                    Offset = face.LightmapOffset,
                    Width = width,
                    Height = height,
                    BitsPerPixel = bpp,
                    Data = data
                };
                _lightmaps.Add(map);
                offsetDict.Add(map.Offset, map);
            }
        }

        public void PreWriteProcess(BspFile bsp, Version version)
        {
            // throw new NotImplementedException("Lightmap data must be pre-processed");
        }

        public int Write(BinaryWriter bw, Version version)
        {
            bw.Write(_lightmapData);
            return _lightmapData.Length;
        }

        #region IList

        public IEnumerator<Lightmap> GetEnumerator()
        {
            return _lightmaps.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable) _lightmaps).GetEnumerator();
        }

        public void Add(Lightmap item)
        {
            _lightmaps.Add(item);
        }

        public void Clear()
        {
            _lightmaps.Clear();
        }

        public bool Contains(Lightmap item)
        {
            return _lightmaps.Contains(item);
        }

        public void CopyTo(Lightmap[] array, int arrayIndex)
        {
            _lightmaps.CopyTo(array, arrayIndex);
        }

        public bool Remove(Lightmap item)
        {
            return _lightmaps.Remove(item);
        }

        public int Count => _lightmaps.Count;

        public bool IsReadOnly => _lightmaps.IsReadOnly;

        public int IndexOf(Lightmap item)
        {
            return _lightmaps.IndexOf(item);
        }

        public void Insert(int index, Lightmap item)
        {
            _lightmaps.Insert(index, item);
        }

        public void RemoveAt(int index)
        {
            _lightmaps.RemoveAt(index);
        }

        public Lightmap this[int index]
        {
            get => _lightmaps[index];
            set => _lightmaps[index] = value;
        }

        #endregion
    }
}