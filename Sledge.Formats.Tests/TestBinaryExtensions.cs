using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Sledge.Formats.Tests
{
    [TestClass]
    public class TestBinaryExtensions
    {
        [TestMethod]
        public void TestReadFixedLengthString()
        {
            MemoryStream ms = new MemoryStream(new byte[]
            {
                97, 97, 97, 0,
                0 , 0 ,  0, 0,
                0 , 0 ,  0, 0,
                0 , 0 ,  0, 0,
            });
            using BinaryReader br = new BinaryReader(ms);
            string fls = br.ReadFixedLengthString(Encoding.ASCII, 8);
            Assert.AreEqual("aaa", fls);
            Assert.AreEqual(8, ms.Position);
        }

        [TestMethod]
        public void TestWriteFixedLengthString()
        {
            MemoryStream ms = new MemoryStream();
            using BinaryWriter bw = new BinaryWriter(ms);
            bw.WriteFixedLengthString(Encoding.ASCII, 8, "aaa");
            Assert.AreEqual(8, ms.Position);
            CollectionAssert.AreEqual(
                new byte[] {97, 97, 97, 0, 0, 0, 0, 0},
                ms.ToArray()
            );
        }

        [TestMethod]
        public void TestReadNullTerminatedString()
        {
            MemoryStream ms = new MemoryStream(new byte[]
            {
                97, 97, 97, 0,
                0 , 0 ,  0, 0,
                0 , 0 ,  0, 0,
                0 , 0 ,  0, 0,
            });
            using BinaryReader br = new BinaryReader(ms);
            string fls = br.ReadNullTerminatedString();
            Assert.AreEqual("aaa", fls);
            Assert.AreEqual(4, ms.Position);
        }

        [TestMethod]
        public void TestWriteNullTerminatedString()
        {
            MemoryStream ms = new MemoryStream();
            using BinaryWriter bw = new BinaryWriter(ms);
            bw.WriteNullTerminatedString("aaa");
            Assert.AreEqual(4, ms.Position);
            CollectionAssert.AreEqual(
                new byte[] { 97, 97, 97, 0 },
                ms.ToArray()
            );
        }

        [TestMethod]
        public void TestReadCString()
        {
            MemoryStream ms = new MemoryStream(new byte[]
            {
                4, 97, 97, 97,
                0 , 0 ,  0, 0,
                0 , 0 ,  0, 0,
                0 , 0 ,  0, 0,
            });
            using BinaryReader br = new BinaryReader(ms);
            string fls = br.ReadCString();
            Assert.AreEqual("aaa", fls);
            Assert.AreEqual(5, ms.Position);
        }

        [TestMethod]
        public void TestWriteCString()
        {
            MemoryStream ms = new MemoryStream();
            using BinaryWriter bw = new BinaryWriter(ms);
            bw.WriteCString("aaa", 256);
            Assert.AreEqual(5, ms.Position);
            CollectionAssert.AreEqual(
                new byte[] { 4, 97, 97, 97, 0 },
                ms.ToArray()
            );
        }

        [TestMethod]
        public void TestWriteCString_MaxLength()
        {
            MemoryStream ms = new MemoryStream();
            using BinaryWriter bw = new BinaryWriter(ms);
            bw.WriteCString("aaa", 2);
            Assert.AreEqual(3, ms.Position);
            CollectionAssert.AreEqual(
                new byte[] { 2, 97, 0 },
                ms.ToArray()
            );
        }

        [TestMethod]
        public void TestReadUshortArray()
        {
            MemoryStream ms = new MemoryStream();
            using (BinaryWriter bw = new BinaryWriter(ms, Encoding.ASCII, true))
            {
                bw.Write((ushort) 123);
                bw.Write((ushort) 456);
                bw.Write((ushort) 789);
            }
            ms.Position = 0;

            using (BinaryReader br = new BinaryReader(ms))
            {
                ushort[] a = br.ReadUshortArray(2);
                CollectionAssert.AreEqual(new ushort[] { 123, 456 }, a);
                Assert.AreEqual(4, ms.Position);
            }
        }

        [TestMethod]
        public void TestReadShortArray()
        {
            MemoryStream ms = new MemoryStream();
            using (BinaryWriter bw = new BinaryWriter(ms, Encoding.ASCII, true))
            {
                bw.Write((short) 123);
                bw.Write((short) -456);
                bw.Write((short) 789);
            }
            ms.Position = 0;

            using (BinaryReader br = new BinaryReader(ms))
            {
                short[] a = br.ReadShortArray(2);
                CollectionAssert.AreEqual(new short[] { 123, -456 }, a);
                Assert.AreEqual(4, ms.Position);
            }
        }

        [TestMethod]
        public void TestReadIntArray()
        {
            MemoryStream ms = new MemoryStream();
            using (BinaryWriter bw = new BinaryWriter(ms, Encoding.ASCII, true))
            {
                bw.Write(123);
                bw.Write(-456);
                bw.Write(789);
            }
            ms.Position = 0;

            using (BinaryReader br = new BinaryReader(ms))
            {
                int[] a = br.ReadIntArray(2);
                CollectionAssert.AreEqual(new [] { 123, -456 }, a);
                Assert.AreEqual(8, ms.Position);
            }
        }

        [TestMethod]
        public void TestReadSingleArrayAsDecimal()
        {
            MemoryStream ms = new MemoryStream();
            using (BinaryWriter bw = new BinaryWriter(ms, Encoding.ASCII, true))
            {
                bw.Write(123f);
                bw.Write(456f);
                bw.Write(789f);
            }
            ms.Position = 0;

            using (BinaryReader br = new BinaryReader(ms))
            {
                decimal[] a = br.ReadSingleArrayAsDecimal(2);
                CollectionAssert.AreEqual(new[] { 123m, 456m }, a);
                Assert.AreEqual(8, ms.Position);
            }
        }

        [TestMethod]
        public void TestReadSingleArray()
        {
            MemoryStream ms = new MemoryStream();
            using (BinaryWriter bw = new BinaryWriter(ms, Encoding.ASCII, true))
            {
                bw.Write(123f);
                bw.Write(456f);
                bw.Write(789f);
            }
            ms.Position = 0;

            using (BinaryReader br = new BinaryReader(ms))
            {
                float[] a = br.ReadSingleArray(2);
                CollectionAssert.AreEqual(new[] { 123f, 456f }, a);
                Assert.AreEqual(8, ms.Position);
            }
        }

        [TestMethod]
        public void TestReadSingleAsDecimal()
        {
            MemoryStream ms = new MemoryStream();
            using (BinaryWriter bw = new BinaryWriter(ms, Encoding.ASCII, true))
            {
                bw.Write(123f);
                bw.Write(456f);
                bw.Write(789f);
            }
            ms.Position = 0;

            using (BinaryReader br = new BinaryReader(ms))
            {
                decimal a = br.ReadSingleAsDecimal();
                Assert.AreEqual(123m, a);
                Assert.AreEqual(4, ms.Position);
            }
        }

        [TestMethod]
        public void TestWriteDecimalAsSingle()
        {
            MemoryStream ms = new MemoryStream();
            using BinaryWriter bw = new BinaryWriter(ms);
            bw.WriteDecimalAsSingle(123m);
            Assert.AreEqual(4, ms.Position);
            CollectionAssert.AreEqual(BitConverter.GetBytes(123f), ms.ToArray());
        }

        [TestMethod]
        public void TestReadRGBColour()
        {
            MemoryStream ms = new MemoryStream();
            using (BinaryWriter bw = new BinaryWriter(ms, Encoding.ASCII, true))
            {
                bw.Write((byte) 255);
                bw.Write((byte) 0);
                bw.Write((byte) 0);
                bw.Write((byte) 0);
            }
            ms.Position = 0;

            using (BinaryReader br = new BinaryReader(ms))
            {
                Color a = br.ReadRGBColour();
                Assert.AreEqual(Color.Red.ToArgb(), a.ToArgb());
                Assert.AreEqual(3, ms.Position);
            }
        }

        [TestMethod]
        public void TestWriteRGBColour()
        {
            MemoryStream ms = new MemoryStream();
            using BinaryWriter bw = new BinaryWriter(ms);
            bw.WriteRGBColour(Color.Red);
            Assert.AreEqual(3, ms.Position);
            CollectionAssert.AreEqual(new byte [] { 255, 0, 0 }, ms.ToArray());
        }

        [TestMethod]
        public void TestReadRGBAColour()
        {
            MemoryStream ms = new MemoryStream();
            using (BinaryWriter bw = new BinaryWriter(ms, Encoding.ASCII, true))
            {
                bw.Write((byte)255);
                bw.Write((byte)0);
                bw.Write((byte)0);
                bw.Write((byte)255);
            }
            ms.Position = 0;

            using (BinaryReader br = new BinaryReader(ms))
            {
                Color a = br.ReadRGBAColour();
                Assert.AreEqual(Color.Red.ToArgb(), a.ToArgb());
                Assert.AreEqual(4, ms.Position);
            }
        }

        [TestMethod]
        public void TestWriteRGBAColour()
        {
            MemoryStream ms = new MemoryStream();
            using BinaryWriter bw = new BinaryWriter(ms);
            bw.WriteRGBAColour(Color.Red);
            Assert.AreEqual(4, ms.Position);
            CollectionAssert.AreEqual(new byte[] { 255, 0, 0, 255 }, ms.ToArray());
        }

        [TestMethod]
        public void TestReadVector3Array()
        {
            MemoryStream ms = new MemoryStream();
            using (BinaryWriter bw = new BinaryWriter(ms, Encoding.ASCII, true))
            {
                foreach (int n in Enumerable.Range(1, 9)) bw.Write((float) n);
            }
            ms.Position = 0;

            using (BinaryReader br = new BinaryReader(ms))
            {
                Vector3[] a = br.ReadVector3Array(2);
                CollectionAssert.AreEqual(new Vector3[] { new Vector3(1, 2, 3), new Vector3(4, 5, 6) }, a);
                Assert.AreEqual(24, ms.Position);
            }
        }

        [TestMethod]
        public void TestReadVector3()
        {
            MemoryStream ms = new MemoryStream();
            using (BinaryWriter bw = new BinaryWriter(ms, Encoding.ASCII, true))
            {
                foreach (int n in Enumerable.Range(1, 9)) bw.Write((float)n);
            }
            ms.Position = 0;

            using (BinaryReader br = new BinaryReader(ms))
            {
                Vector3 a = br.ReadVector3();
                Assert.AreEqual(new Vector3(1, 2, 3), a);
                Assert.AreEqual(12, ms.Position);
            }
        }

        [TestMethod]
        public void TestWriteVector3()
        {
            MemoryStream ms = new MemoryStream();
            using BinaryWriter bw = new BinaryWriter(ms);
            bw.WriteVector3(new Vector3(1, 2, 3));
            Assert.AreEqual(12, ms.Position);
            List<byte> exp = new List<byte>();
            exp.AddRange(BitConverter.GetBytes(1f));
            exp.AddRange(BitConverter.GetBytes(2f));
            exp.AddRange(BitConverter.GetBytes(3f));
            CollectionAssert.AreEqual(exp, ms.ToArray());
        }

        [TestMethod]
        public void TestReadPlane()
        {
            MemoryStream ms = new MemoryStream();
            using (BinaryWriter bw = new BinaryWriter(ms, Encoding.ASCII, true))
            {
                bw.Write(1f);
                bw.Write(2f);
                bw.Write(0f);
                bw.Write(3f);
                bw.Write(4f);
                bw.Write(0f);
                bw.Write(3f);
                bw.Write(-2f);
                bw.Write(0f);
            }
            ms.Position = 0;

            using (BinaryReader br = new BinaryReader(ms))
            {
                Plane a = br.ReadPlane();
                Assert.AreEqual(new Vector3(0, 0, 1), a.Normal);
                Assert.AreEqual(0f, a.D);
                Assert.AreEqual(36, ms.Position);
            }
        }

        [TestMethod]
        public void TestWritePlane()
        {
            MemoryStream ms = new MemoryStream();
            using BinaryWriter bw = new BinaryWriter(ms);
            Vector3[] vecs = new[] {new Vector3(1, 2, 0), new Vector3(3, 4, 0), new Vector3(3, -2, 0)};
            bw.WritePlane(vecs);
            Assert.AreEqual(36, ms.Position);
            List<byte> exp = new List<byte>();
            foreach (Vector3 v in vecs)
            {
                exp.AddRange(BitConverter.GetBytes(v.X));
                exp.AddRange(BitConverter.GetBytes(v.Y));
                exp.AddRange(BitConverter.GetBytes(v.Z));
            }
            CollectionAssert.AreEqual(exp, ms.ToArray());
        }
    }
}
