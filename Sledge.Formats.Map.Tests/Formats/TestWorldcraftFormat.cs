using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sledge.Formats.Map.Formats;

namespace Sledge.Formats.Map.Tests.Formats
{
    [TestClass]
    public class TestWorldcraftFormat
    {
        [TestMethod]
        public void TestRmfFormatLoading()
        {
            WorldcraftRmfFormat format = new WorldcraftRmfFormat();
            foreach (string file in Directory.GetFiles(@"D:\Downloads\formats\rmf"))
            {
                using FileStream r = File.OpenRead(file);
                try
                {
                    format.Read(r);
                }
                catch (Exception ex)
                {
                    Assert.Fail($"Unable to read file: {Path.GetFileName(file)}. {ex.Message}");
                }
            }
        }
    }
}