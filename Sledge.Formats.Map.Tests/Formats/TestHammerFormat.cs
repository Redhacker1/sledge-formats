using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sledge.Formats.Map.Formats;
using Sledge.Formats.Map.Objects;
using Path = System.IO.Path;

namespace Sledge.Formats.Map.Tests.Formats
{
    [TestClass]
    public class TestHammerFormat
    {
        [TestMethod]
        public void TestVmfFormatLoading()
        {
            HammerVmfFormat format = new HammerVmfFormat();
            foreach (string file in Directory.GetFiles(@"D:\Documents\GModDcomps"))
            {
                using FileStream r = File.OpenRead(file);
                try
                {
                    Console.WriteLine($"Reading {file}");
                    MapFile file_data = format.Read(r);
                    Console.WriteLine(file_data.Worldspawn.Children[10]);

                }
                catch (Exception ex)
                {
                    if (file.ToLower().Contains(".map"))
                    {
                        Assert.Fail($"Unable to read file: {Path.GetFileName(file)}. {ex.Message}");   
                    }
                }
            }
        }
    }
}