using System.IO;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sledge.Formats.Texture.Vtf;

namespace Sledge.Formats.Texture.Tests.Vtf
{
    [TestClass]
    public class TestVtf
    {
        [TestMethod]
        public void TestLoadVtf()
        {
            using FileStream f = File.OpenRead(@"D:\Portal2Decomp\materials\cable\cable.vtf");
            VtfFile vtf = new VtfFile(f);
        }
    }
}