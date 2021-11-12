/***
 * https://gist.github.com/ststeiger/cb9750664952f775a341#gistcomment-2912797
 * 
 * Usage:
 * LZMA.CompressFile("input.txt", "output.txt.lzma" [, LzmaSpeed.Fastest, DictionarySize.VerySmall, Action<long, long> onProgress]);
 * LZMA.DecompressFile("input.txt", "output.txt.lzma" [, Action<long, long> onProgress])
 *
 * LZMA.Compress(inStream, outStream [, LzmaSpeed.Fastest, DictionarySize.VerySmall, Action<long, long> onProgress]);
 * LZMA.Decompress(inStream, outStream [, Action<long, long> onProgress])
***/

using SevenZip.Compression.LZMA;
using System;
using System.IO;
namespace SevenZip {
    public enum LzmaSpeed : int {
        Fastest = 5,
        VeryFast = 8,
        Fast = 16,
        Medium = 32,
        Slow = 64,
        VerySlow = 128,
    }
    public enum DictionarySize : int {
        ///<summary>64 KiB</summary>
        VerySmall = 1 << 16,
        ///<summary>1 MiB</summary>
        Small = 1 << 20,
        ///<summary>4 MiB</summary>
        Medium = 1 << 22,
        ///<summary>8 MiB</summary>
        Large = 1 << 23,
        ///<summary>16 MiB</summary>
        Larger = 1 << 24,
        ///<summary>64 MiB</summary>
        VeryLarge = 1 << 26,
    }
    public static class LZMA {
        public static void CompressFile(string filename, string outfile, LzmaSpeed speed = LzmaSpeed.Fastest, DictionarySize dictionarySize = DictionarySize.VerySmall, Action<long, long> onProgress = null) {
            using (FileStream fs = File.OpenRead(filename))
            using (FileStream target = File.Create(outfile)) {
                Compress(fs, target, speed, dictionarySize, onProgress);
            }
        }

        public static void DecompressFile(string filename, string outfile, Action<long, long> onProgress = null) {
            using (FileStream fs = File.OpenRead(filename))
            using (FileStream target = File.Create(outfile)) {
                Decompress(fs, target, onProgress);
            }
        }

        public static void Compress(Stream input, Stream output, LzmaSpeed speed = LzmaSpeed.Fastest, DictionarySize dictionarySize = DictionarySize.VerySmall, Action<long, long> onProgress = null) {
            int posStateBits = 2; // default: 2
            int litContextBits = 3; // 3 for normal files, 0; for 32-bit data
            int litPosBits = 0; // 0 for 64-bit data, 2 for 32-bit.
            int numFastBytes = (int)speed;
            string matchFinder = "BT4"; // default: BT4
            bool endMarker = true;

            CoderPropID[] propIDs =
            {
                CoderPropID.DictionarySize,
                CoderPropID.PosStateBits, // (0 <= x <= 4).
                CoderPropID.LitContextBits, // (0 <= x <= 8).
                CoderPropID.LitPosBits, // (0 <= x <= 4).
                CoderPropID.NumFastBytes,
                CoderPropID.MatchFinder, // "BT2", "BT4".
                CoderPropID.EndMarker
            };

            object[] properties =
            {
                (int)dictionarySize,
                posStateBits,
                (int)litContextBits,
                (int)litPosBits,
                numFastBytes,
                matchFinder,
                endMarker
            };

            Encoder lzmaEncoder = new Encoder();

            lzmaEncoder.SetCoderProperties(propIDs, properties);
            lzmaEncoder.WriteCoderProperties(output);
            long fileSize = input.Length;
            for (int i = 0; i < 8; i++) output.WriteByte((byte)(fileSize >> (8 * i)));

            ICodeProgress prg = null;
            if (onProgress != null) {
                prg = new DelegateCodeProgress(onProgress);
            }
            lzmaEncoder.Code(input, output, -1, -1, prg);
        }

        public static void Decompress(Stream input, Stream output, Action<long, long> onProgress = null) {
            Decoder decoder = new SevenZip.Compression.LZMA.Decoder();

            byte[] properties = new byte[5];
            if (input.Read(properties, 0, 5) != 5) {
                throw new Exception("input .lzma is too short");
            }
            decoder.SetDecoderProperties(properties);

            long fileLength = 0;
            for (int i = 0; i < 8; i++) {
                int v = input.ReadByte();
                if (v < 0) throw new Exception("Can't Read 1");
                fileLength |= ((long)(byte)v) << (8 * i);
            }

            ICodeProgress prg = null;
            if (onProgress != null) {
                prg = new DelegateCodeProgress(onProgress);
            }
            long compressedSize = input.Length - input.Position;

            decoder.Code(input, output, compressedSize, fileLength, prg);
        }

        private class DelegateCodeProgress : ICodeProgress {
            private readonly Action<long, long> handler;
            public DelegateCodeProgress(Action<long, long> handler) => this.handler = handler;
            public void SetProgress(long inSize, long outSize) => handler(inSize, outSize);
        }
    }
}