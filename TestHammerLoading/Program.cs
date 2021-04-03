using System;
using Sledge.Formats.Map.Tests.Formats;

namespace TestHammerLoading
{
    internal class Program
    {
        static void Main(string[] args)
        {
            TestHammerFormat hammerTest = new TestHammerFormat();
            hammerTest.TestVmfFormatLoading();
        }
    }
}