using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Sledge.Formats.Valve
{
    /// <summary>
    /// The liblist.gam file, used by Goldsource games and mods.
    /// </summary>
    public class Liblist : Dictionary<string, string>
    {
        #region Properties

        /// <summary>
        /// The name of the game/mod.
        /// </summary>
        public string Game
        {
            get => TryGetValue("game", out string s) ? s : null;
            set => this["game"] = value;
        }

        /// <summary>
        /// A path to an uncompressed, 24bit, 16x16 resolution TGA file, relative to the mod directory, with no file extension.
        /// </summary>
        public string Icon
        {
            get => TryGetValue("icon", out string s) ? s : null;
            set => this["icon"] = value;
        }

        /// <summary>
        /// The name of the team or person who created this game/mod.
        /// </summary>
        public string Developer
        {
            get => TryGetValue("developer", out string s) ? s : null;
            set => this["developer"] = value;
        }

        /// <summary>
        /// A URL to the developer's website.
        /// </summary>
        public string DeveloperUrl
        {
            get => TryGetValue("developer_url", out string s) ? s : null;
            set => this["developer_url"] = value;
        }

        /// <summary>
        /// A URL to the game/mod's manual.
        /// </summary>
        public string Manual
        {
            get => TryGetValue("manual", out string s) ? s : null;
            set => this["manual"] = value;
        }

        /// <summary>
        /// The path to the game's DLL file on Windows, relative to the mod directory. e.g. "dlls\hl.dll"
        /// </summary>
        public string GameDll
        {
            get => TryGetValue("gamedll", out string s) ? s : null;
            set => this["gamedll"] = value;
        }

        /// <summary>
        /// The path to the game's DLL file on Linux, relative to the mod directory. e.g. "dlls/hl.so"
        /// </summary>
        public string GameDllLinux
        {
            get => TryGetValue("gamedll_linux", out string s) ? s : null;
            set => this["gamedll_linux"] = value;
        }

        /// <summary>
        /// The path to the game's DLL file on OSX, relative to the mod directory. e.g. "dlls/hl.dylib"
        /// </summary>
        public string GameDllOsx
        {
            get => TryGetValue("gamedll_osx", out string s) ? s : null;
            set => this["gamedll_osx"] = value;
        }
        
        /// <summary>
        /// Enable VAC security.
        /// </summary>
        public bool? Secure
        {
            get => TryGetValue("secure", out string s) && int.TryParse(s, out int b) ? b == 1 : (bool?)null;
            set => this["secure"] = !value.HasValue ? null : value.Value ? "1" : "0";
        }

        /// <summary>
        /// If this is a server-only mod.
        /// </summary>
        public bool? ServerOnly
        {
            get => TryGetValue("svonly", out string s) && int.TryParse(s, out int b) ? b == 1 : (bool?)null;
            set => this["svonly"] = !value.HasValue ? null : value.Value ? "1" : "0";
        }

        /// <summary>
        /// If the mod requires a new client.dll
        /// </summary>
        public bool? ClientDllRequired
        {
            get => TryGetValue("cldll", out string s) && int.TryParse(s, out int b) ? b == 1 : (bool?)null;
            set => this["cldll"] = !value.HasValue ? null : value.Value ? "1" : "0";
        }

        /// <summary>
        /// The type of game/mod. Usually "singleplayer_only" or "multiplayer_only".
        /// </summary>
        public string Type
        {
            get => TryGetValue("type", out string s) ? s : null;
            set => this["type"] = value;
        }

        /// <summary>
        /// The name of the map to load when the player starts a new game, without the extension. e.g. "c0a0"
        /// </summary>
        public string StartingMap
        {
            get => TryGetValue("startmap", out string s) ? s : null;
            set => this["startmap"] = value;
        }

        /// <summary>
        /// The name of the map to load when the player starts the training map, without the extension. e.g. "t0a0"
        /// </summary>
        public string TrainingMap
        {
            get => TryGetValue("trainmap", out string s) ? s : null;
            set => this["trainmap"] = value;
        }

        /// <summary>
        /// The name of the multiplayer entity class.
        /// </summary>
        public string MultiplayerEntity
        {
            get => TryGetValue("mpentity", out string s) ? s : null;
            set => this["mpentity"] = value;
        }

        /// <summary>
        /// Do not show maps with names containing this string in create server dialogue.
        /// </summary>
        public string MultiplayerFilter
        {
            get => TryGetValue("mpfilter", out string s) ? s : null;
            set => this["mpfilter"] = value;
        }

        /// <summary>
        /// The mod/game to base this mod/game off of. e.g. "cstrike"
        /// </summary>
        public string FallbackDirectory
        {
            get => TryGetValue("fallback_dir", out string s) ? s : null;
            set => this["fallback_dir"] = value;
        }

        /// <summary>
        /// True to load maps from the base game/mod.
        /// </summary>
        public bool? FallbackMaps
        {
            get => TryGetValue("fallback_maps", out string s) && int.TryParse(s, out int b) ? b == 1 : (bool?)null;
            set => this["fallback_maps"] = !value.HasValue ? null : value.Value ? "1" : "0";
        }

        /// <summary>
        /// Prevent the player model from being anything except player.mdl.
        /// </summary>
        public bool? NoModels
        {
            get => TryGetValue("nomodels", out string s) && int.TryParse(s, out int b) ? b == 1 : (bool?)null;
            set => this["nomodels"] = !value.HasValue ? null : value.Value ? "1" : "0";
        }

        /// <summary>
        /// Don't allow HD models.
        /// </summary>
        public bool? NoHighDefinitionModels
        {
            get => TryGetValue("nohimodels", out string s) && int.TryParse(s, out int b) ? b == 1 : (bool?)null;
            set => this["nohimodels"] = !value.HasValue ? null : value.Value ? "1" : "0";
        }

        /// <summary>
        /// Use detailed textures.
        /// </summary>
        public bool? DetailedTextures
        {
            get => TryGetValue("detailed_textures", out string s) && int.TryParse(s, out int b) ? b == 1 : (bool?)null;
            set => this["detailed_textures"] = !value.HasValue ? null : value.Value ? "1" : "0";
        }

        #endregion

        public Liblist()
        {

        }

        public Liblist(Stream stream)
        {
            using (StreamReader sr = new StreamReader(stream, Encoding.ASCII, false, 1024, true))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    int c = line.IndexOf("//", StringComparison.Ordinal);
                    if (c >= 0) line = line.Substring(0, c);
                    line = line.Trim();

                    if (string.IsNullOrWhiteSpace(line)) continue;

                    c = line.IndexOf(' ');
                    if (c < 0) continue;

                    string key = line.Substring(0, c).ToLower();
                    if (string.IsNullOrWhiteSpace(key)) continue;

                    string value = line.Substring(c + 1);
                    if (value[0] != '"' || value[value.Length - 1] != '"') continue;

                    value = value.Substring(1, value.Length - 2).Trim();
                    this[key] = value;
                }
            }
        }

        public void Write(Stream stream)
        {
            using (StreamWriter sr = new StreamWriter(stream, Encoding.ASCII, 1024, true))
            {
                foreach (KeyValuePair<string, string> kv in this)
                {
                    sr.WriteLine($"{kv.Key} \"{kv.Value}\"");
                }
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            foreach (KeyValuePair<string, string> kv in this)
            {
                sb.AppendLine($"{kv.Key} \"{kv.Value}\"");
            }
            return sb.ToString();
        }
    }
}
