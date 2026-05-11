using System.IO;
using System.IO.Compression;
using NUnit.Framework;
using Neko.Extensions;

namespace Neko.Tests
{
    public class SnapFrameBlankDetectionTests
    {
        private string _tempDir = null!;

        [SetUp]
        public void SetUp()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "neko-snap-tests-" + Path.GetRandomFileName());
            Directory.CreateDirectory(_tempDir);
        }

        [TearDown]
        public void TearDown()
        {
            try { Directory.Delete(_tempDir, recursive: true); } catch { }
        }

        [Test]
        public void MissingFileIsBlank()
        {
            var path = Path.Combine(_tempDir, "nope.png");
            Assert.That(SnapFrameExtension.IsBlankImage(path, out var reason), Is.True);
            Assert.That(reason, Does.Contain("missing"));
        }

        [Test]
        public void EmptyFileIsBlank()
        {
            var path = Path.Combine(_tempDir, "empty.png");
            File.WriteAllBytes(path, System.Array.Empty<byte>());
            Assert.That(SnapFrameExtension.IsBlankImage(path, out _), Is.True);
        }

        [Test]
        public void NonPngFileIsNotTouched()
        {
            var path = Path.Combine(_tempDir, "not-a-png.png");
            File.WriteAllText(path, "this is plain text, definitely not a PNG");
            Assert.That(SnapFrameExtension.IsBlankImage(path, out _), Is.False,
                "Unknown formats should be left alone, not deleted.");
        }

        [Test]
        public void FullWhitePngIsBlank()
        {
            var path = Path.Combine(_tempDir, "white.png");
            WriteSolidRgbPng(path, 32, 16, 255, 255, 255);
            Assert.That(SnapFrameExtension.IsBlankImage(path, out var reason), Is.True);
            Assert.That(reason, Does.Contain("255"));
        }

        [Test]
        public void FullBlackPngIsBlank()
        {
            var path = Path.Combine(_tempDir, "black.png");
            WriteSolidRgbPng(path, 32, 16, 0, 0, 0);
            Assert.That(SnapFrameExtension.IsBlankImage(path, out _), Is.True);
        }

        [Test]
        public void PngWithContentIsNotBlank()
        {
            var path = Path.Combine(_tempDir, "content.png");
            WriteRgbPngWithSinglePixelDifference(path, 32, 16);
            Assert.That(SnapFrameExtension.IsBlankImage(path, out _), Is.False,
                "Any deviation from a uniform color should count as content.");
        }

        // --- Test helpers: minimal PNG writer producing filter-type-0 (None) scanlines. ---

        private static void WriteSolidRgbPng(string path, int width, int height, byte r, byte g, byte b)
        {
            var raw = new byte[height * (1 + width * 3)];
            for (int y = 0; y < height; y++)
            {
                int row = y * (1 + width * 3);
                raw[row] = 0; // filter type None
                for (int x = 0; x < width; x++)
                {
                    int p = row + 1 + x * 3;
                    raw[p] = r;
                    raw[p + 1] = g;
                    raw[p + 2] = b;
                }
            }
            WritePng(path, width, height, raw);
        }

        private static void WriteRgbPngWithSinglePixelDifference(string path, int width, int height)
        {
            var raw = new byte[height * (1 + width * 3)];
            for (int y = 0; y < height; y++)
            {
                int row = y * (1 + width * 3);
                raw[row] = 0;
                for (int x = 0; x < width; x++)
                {
                    int p = row + 1 + x * 3;
                    raw[p] = 200;
                    raw[p + 1] = 200;
                    raw[p + 2] = 200;
                }
            }
            // Flip one pixel red to simulate "any content".
            raw[1] = 255; raw[2] = 0; raw[3] = 0;
            WritePng(path, width, height, raw);
        }

        private static void WritePng(string path, int width, int height, byte[] rawScanlines)
        {
            using var fs = File.Create(path);
            using var bw = new BinaryWriter(fs);

            bw.Write(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A });

            // IHDR: width, height, bitDepth=8, colorType=2 (RGB), 0, 0, 0
            var ihdr = new byte[13];
            WriteUInt32BE(ihdr, 0, (uint)width);
            WriteUInt32BE(ihdr, 4, (uint)height);
            ihdr[8] = 8;
            ihdr[9] = 2;
            WriteChunk(bw, "IHDR", ihdr);

            // IDAT: zlib-compressed rawScanlines
            using var idatMem = new MemoryStream();
            using (var z = new ZLibStream(idatMem, CompressionLevel.Fastest, leaveOpen: true))
            {
                z.Write(rawScanlines, 0, rawScanlines.Length);
            }
            WriteChunk(bw, "IDAT", idatMem.ToArray());

            WriteChunk(bw, "IEND", System.Array.Empty<byte>());
        }

        private static void WriteChunk(BinaryWriter bw, string type, byte[] data)
        {
            var lenBytes = new byte[4];
            WriteUInt32BE(lenBytes, 0, (uint)data.Length);
            bw.Write(lenBytes);

            var typeBytes = System.Text.Encoding.ASCII.GetBytes(type);
            bw.Write(typeBytes);
            bw.Write(data);

            var crc = Crc32(typeBytes, data);
            var crcBytes = new byte[4];
            WriteUInt32BE(crcBytes, 0, crc);
            bw.Write(crcBytes);
        }

        private static void WriteUInt32BE(byte[] buf, int offset, uint v)
        {
            buf[offset] = (byte)((v >> 24) & 0xFF);
            buf[offset + 1] = (byte)((v >> 16) & 0xFF);
            buf[offset + 2] = (byte)((v >> 8) & 0xFF);
            buf[offset + 3] = (byte)(v & 0xFF);
        }

        private static uint Crc32(byte[] type, byte[] data)
        {
            uint c = 0xFFFFFFFFu;
            foreach (var b in type) c = StepCrc(c, b);
            foreach (var b in data) c = StepCrc(c, b);
            return c ^ 0xFFFFFFFFu;
        }

        private static uint StepCrc(uint c, byte b)
        {
            c ^= b;
            for (int k = 0; k < 8; k++)
            {
                c = (c & 1) != 0 ? (0xEDB88320u ^ (c >> 1)) : (c >> 1);
            }
            return c;
        }
    }
}
