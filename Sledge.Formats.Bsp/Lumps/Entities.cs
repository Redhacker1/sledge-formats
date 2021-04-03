using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Sledge.Formats.Bsp.Objects;

namespace Sledge.Formats.Bsp.Lumps
{
    public class Entities : ILump, IList<Entity>
    {
        readonly IList<Entity> _entities;

        public Entities()
        {
            _entities = new List<Entity>();
        }

        public void Read(BinaryReader br, Blob blob, Version version)
        {
            string text = Encoding.ASCII.GetString(br.ReadBytes(blob.Length));

            // Remove comments
            StringBuilder cleaned = new StringBuilder();
            foreach (string line in text.Split('\n'))
            {
                string l = line;
                int idx = l.IndexOf("//", StringComparison.Ordinal);
                if (idx >= 0) l = l.Substring(0, idx);
                l = l.Trim();
                cleaned.Append(l).Append('\n');
            }

            string data = cleaned.ToString();

            Entity cur = null;
            int i;
            string key = null;
            for (i = 0; i < data.Length; i++)
            {
                string token = GetToken();
                if (token == "{")
                {
                    // Start of new entity
                    cur = new Entity();
                    _entities.Add(cur);
                    key = null;
                }
                else if (token == "}")
                {
                    // End of entity
                    cur = null;
                    key = null;
                }
                else if (cur != null && key != null)
                {
                    // KeyValue value
                    SetKeyValue(cur, key, token);
                    key = null;
                }
                else if (cur != null)
                {
                    // KeyValue key
                    key = token;
                }
                else if (token == null)
                {
                    // End of file
                    break;
                }
            }

            string GetToken()
            {
                if (!ScanToNonWhitespace()) return null;

                if (data[i] == '{' || data[i] == '}')
                {
                    // Start/end entity
                    return data[i].ToString();
                }

                if (data[i] == '"')
                {
                    // Quoted string, find end quote
                    int idx = data.IndexOf('"', i + 1);
                    if (idx < 0) return null;
                    string tok = data.Substring(i + 1, idx - i - 1);
                    i = idx + 1;
                    return tok;
                }

                if (data[i] > 32)
                {
                    // Not whitespace
                    string s = "";
                    while (data[i] > 32)
                    {
                        s += data[i++];
                    }
                    return s;
                }

                return null;
            }

            bool ScanToNonWhitespace()
            {
                while (i < data.Length)
                {
                    if (data[i] == ' ' || data[i] == '\n') i++;
                    else return true;
                }

                return false;
            }

            void SetKeyValue(Entity e, string k, string v)
            {
                e.KeyValues[k] = v;
                switch (k)
                {
                    case "classname":
                        e.ClassName = v;
                        break;
                    case "model":
                        if (int.TryParse(v.Substring(1), out int m)) e.Model = m;
                        break;
                }
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
            StringBuilder sb = new StringBuilder();
            foreach (Entity entity in _entities)
            {
                sb.Append("{\n");

                if (entity.Model > 0)
                {
                    sb.Append($"\"model\" \"*{entity.Model}\"\n");
                }

                foreach (KeyValuePair<string, string> kv in entity.KeyValues.Where(x => x.Key?.Length > 0 && x.Value?.Length > 0))
                {
                    sb.Append($"\"{kv.Key}\" \"{kv.Value}\"\n");
                }

                if (entity.ClassName?.Length > 0)
                {
                    sb.Append($"\"classname\" \"{entity.ClassName}\"\n");
                }

                sb.Append("}\n");
            }
            bw.Write(Encoding.ASCII.GetBytes(sb.ToString()));
            return (int)(bw.BaseStream.Position - pos);
        }

        #region IList

        public IEnumerator<Entity> GetEnumerator()
        {
            return _entities.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable) _entities).GetEnumerator();
        }

        public void Add(Entity item)
        {
            _entities.Add(item);
        }

        public void Clear()
        {
            _entities.Clear();
        }

        public bool Contains(Entity item)
        {
            return _entities.Contains(item);
        }

        public void CopyTo(Entity[] array, int arrayIndex)
        {
            _entities.CopyTo(array, arrayIndex);
        }

        public bool Remove(Entity item)
        {
            return _entities.Remove(item);
        }

        public int Count => _entities.Count;

        public bool IsReadOnly => _entities.IsReadOnly;

        public int IndexOf(Entity item)
        {
            return _entities.IndexOf(item);
        }

        public void Insert(int index, Entity item)
        {
            _entities.Insert(index, item);
        }

        public void RemoveAt(int index)
        {
            _entities.RemoveAt(index);
        }

        public Entity this[int index]
        {
            get => _entities[index];
            set => _entities[index] = value;
        }

        #endregion
    }
}