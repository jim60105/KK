using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Extension
{
    public static partial class ImageHelper
    {
        /// <summary>
        /// Load a embedded PNG or JPG resource to a Texture2D
        /// </summary>
        /// <param name="FilePath"></param>
        /// <returns>byte array</returns>
        public static byte[] LoadDllResourceToBytes(string FilePath)
        {
            Assembly myAssembly = Assembly.GetExecutingAssembly();
            using (Stream myStream = myAssembly.GetManifestResourceStream(FilePath))
            {
                return ReadToEnd(myStream);
            }
        }

        public static byte[] ReadToEnd(Stream stream)
        {
            long originalPosition = stream.Position;
            //stream.Position = 0;

            try
            {
                byte[] readBuffer = new byte[4096];

                int totalBytesRead = 0;
                int bytesRead;

                while ((bytesRead = stream.Read(readBuffer, totalBytesRead, readBuffer.Length - totalBytesRead)) > 0)
                {
                    totalBytesRead += bytesRead;

                    if (totalBytesRead == readBuffer.Length)
                    {
                        int nextByte = stream.ReadByte();
                        if (nextByte != -1)
                        {
                            byte[] temp = new byte[readBuffer.Length * 2];
                            Buffer.BlockCopy(readBuffer, 0, temp, 0, readBuffer.Length);
                            Buffer.SetByte(temp, totalBytesRead, (byte)nextByte);
                            readBuffer = temp;
                            totalBytesRead++;
                        }
                    }
                }

                byte[] buffer = readBuffer;
                if (readBuffer.Length != totalBytesRead)
                {
                    buffer = new byte[totalBytesRead];
                    Buffer.BlockCopy(readBuffer, 0, buffer, 0, totalBytesRead);
                }
                return buffer;
            }
            finally
            {
                stream.Position = originalPosition;
            }
        }


        public static long GetPngSize(BinaryReader br)
        {
            return GetPngSize(br.BaseStream);
        }

        public static long GetPngSize(Stream st)
        {
            if (st == null)
            {
                return 0L;
            }

            long position = st.Position;
            long num = 0L;
            try
            {
                byte[] array = new byte[8];
                byte[] array2 = new byte[8]
                {
                137,
                80,
                78,
                71,
                13,
                10,
                26,
                10
                };
                st.Read(array, 0, 8);
                for (int i = 0; i < 8; i++)
                {
                    if (array[i] != array2[i])
                    {
                        st.Seek(position, SeekOrigin.Begin);
                        return 0L;
                    }
                }

                int num2 = 0;
                int num3 = 0;
                bool flag = true;
                while (flag)
                {
                    byte[] array3 = new byte[4];
                    st.Read(array3, 0, 4);
                    Array.Reverse(array3);
                    num2 = BitConverter.ToInt32(array3, 0);
                    byte[] array4 = new byte[4];
                    st.Read(array4, 0, 4);
                    num3 = BitConverter.ToInt32(array4, 0);
                    if (num3 == 1145980233)
                    {
                        flag = false;
                    }

                    if (num2 + 4 > st.Length - st.Position)
                    {
                        st.Seek(position, SeekOrigin.Begin);
                        return 0L;
                    }

                    st.Seek(num2 + 4, SeekOrigin.Current);
                }

                num = st.Position - position;
                st.Seek(position, SeekOrigin.Begin);
                return num;
            }
            catch (EndOfStreamException)
            {
                st.Seek(position, SeekOrigin.Begin);
                return 0L;
            }
        }

        public static void SkipPng(Stream st)
        {
            long pngSize = GetPngSize(st);
            st.Seek(pngSize, SeekOrigin.Current);
        }

        public static void SkipPng(BinaryReader br)
        {
            long pngSize = GetPngSize(br);
            br.BaseStream.Seek(pngSize, SeekOrigin.Current);
        }

        public static byte[] LoadPngBytes(string path)
        {
            using (FileStream st = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                return LoadPngBytes(st);
            }
        }

        public static byte[] LoadPngBytes(Stream st)
        {
            using (BinaryReader br = new BinaryReader(st))
            {
                return LoadPngBytes(br);
            }
        }

        public static byte[] LoadPngBytes(BinaryReader br)
        {
            long pngSize = GetPngSize(br);
            if (pngSize == 0)
            {
                return null;
            }

            return br.ReadBytes((int)pngSize);
        }

        // https://stackoverflow.com/a/112711
        private static Dictionary<byte[], Func<BinaryReader, int[]>> imageFormatDecoders = new Dictionary<byte[], Func<BinaryReader, int[]>>()
        {
            { new byte[]{ 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }, DecodePng }
        };

        /// <summary>
        /// Gets the dimensions of an image.
        /// </summary>
        /// <param name="path">The path of the image to get the dimensions of.</param>
        /// <returns>The dimensions of the specified image.</returns>
        /// <exception cref="ArgumentException">The image was of an unrecognized format.</exception>    
        public static int[] GetDimensions(byte[] bytes)
        {
            using (MemoryStream ms = new MemoryStream(bytes))
            using (BinaryReader binaryReader = new BinaryReader(ms))
            {

                int maxMagicBytesLength = imageFormatDecoders.Keys.OrderByDescending(x => x.Length).First().Length;

                byte[] magicBytes = new byte[maxMagicBytesLength];

                for (int i = 0; i < maxMagicBytesLength; i += 1)
                {
                    magicBytes[i] = binaryReader.ReadByte();

                    foreach (var kvPair in imageFormatDecoders)
                    {
                        if (magicBytes.StartsWith(kvPair.Key))
                        {
                            return kvPair.Value(binaryReader);
                        }
                    }
                }
            }

            throw new ArgumentException("binaryReader");
        }

        private static bool StartsWith(this byte[] thisBytes, byte[] thatBytes)
        {
            for (int i = 0; i < thatBytes.Length; i += 1)
            {
                if (thisBytes[i] != thatBytes[i])
                {
                    return false;
                }
            }
            return true;
        }

        private static int ReadLittleEndianInt32(this BinaryReader binaryReader)
        {
            byte[] bytes = new byte[sizeof(int)];
            for (int i = 0; i < sizeof(int); i += 1)
            {
                bytes[sizeof(int) - 1 - i] = binaryReader.ReadByte();
            }
            return BitConverter.ToInt32(bytes, 0);
        }

        private static int[] DecodePng(BinaryReader binaryReader)
        {
            binaryReader.ReadBytes(8);
            int width = binaryReader.ReadLittleEndianInt32();
            int height = binaryReader.ReadLittleEndianInt32();
            return new int[] { width, height };
        }
    }
}

