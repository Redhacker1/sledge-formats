using System;
using System.Collections.Generic;
using System.Numerics;

namespace Sledge.Formats.Bsp.Objects
{
    public class Entity
    {
        public int Model { get; set; }
        public string ClassName { get; set; }
        public Dictionary<string, string> KeyValues { get; set; }

        public Entity()
        {
            KeyValues = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
        }

        public Vector3 GetVector3(string name, Vector3 defaultValue)
        {
            if (!KeyValues.ContainsKey(name)) return defaultValue;

            string val = KeyValues[name];
            string[] spl = val.Split(' ');
            if (spl.Length != 3) return defaultValue;

            if (!float.TryParse(spl[0], out float x)) return defaultValue;
            if (!float.TryParse(spl[1], out float y)) return defaultValue;
            if (!float.TryParse(spl[2], out float z)) return defaultValue;

            return new Vector3(x, y, z);
        }

        public T Get<T>(string name, T defaultValue)
        {
            if (!KeyValues.ContainsKey(name)) return defaultValue;
            string val = KeyValues[name];
            try
            {
                return (T) Convert.ChangeType(val, typeof(T));
            }
            catch
            {
                return defaultValue;
            }
        }
    }
}