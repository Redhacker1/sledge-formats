using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sledge.Formats.Map.Formats;

namespace Sledge.Formats.Map.Tests.Formats
{
    [TestClass]
    public class TestQuakeFormat
    {
        [TestMethod]
        public void TestMapFormatLoading()
        {
            QuakeMapFormat format = new QuakeMapFormat();
            foreach (string file in Directory.GetFiles(@"D:\Downloads\formats\map"))
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