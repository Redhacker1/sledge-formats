using System.IO;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sledge.Formats.Texture.Wad;
using Sledge.Formats.Valve;

namespace Sledge.Formats.Texture.Tests.Wad
{
    [TestClass]
    public class TestWad
    {
        [TestMethod]
        public void TestLoadWad2()
        {
            using FileStream f = File.OpenRead(@"F:\SteamLibrary\steamapps\common\quake\Id1\gfx_modified.wad");
            WadFile wad = new WadFile(f);
        }

        [TestMethod]
        public void TestLoadWad3()
        {
            using FileStream f = File.OpenRead(@"F:\SteamLibrary\steamapps\common\Half-Life\valve\halflife.wad");
            WadFile wad = new WadFile(f);
        }
    }
}