using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sledge.Formats.Map.Formats;

namespace Sledge.Formats.Map.Tests.Formats
{
    [TestClass]
    public class TestJackhammerFormat
    {
        [TestMethod]
        public void TestJmfFormatLoading()
        {
            JackhammerJmfFormat format = new JackhammerJmfFormat();
            foreach (string file in Directory.GetFiles(@"D:\Downloads\formats\jmf", "1group.jmf"))
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