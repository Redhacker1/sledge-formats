using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Sledge.Formats.Map
{
    internal static class Util
    {
        public static void Assert(bool b, string message = "Malformed file.")
        {
            if (!b) throw new Exception(message);
        }

        public static bool ParseFloatArray(string input, char[] splitChars, int expected, out float[] array)
        {
            string[] spl = input.Split(splitChars, StringSplitOptions.RemoveEmptyEntries);
            if (spl.Length == expected)
            {
                List<float?> parsed = spl.Select(x => float.TryParse(x, NumberStyles.Float, CultureInfo.InvariantCulture, out float o) ? (float?)o : null).ToList();
                if (parsed.All(x => x.HasValue))
                {
                    // ReSharper disable once PossibleInvalidOperationException
                    array = parsed.Select(x => x.Value).ToArray();
                    return true;
                }
            }
            array = new float[expected];
            return false;
        }
    }
}
