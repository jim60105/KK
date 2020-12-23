using System;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace Extension {
    public static class ImageHelper{
        #region Png Stuff
        public static long GetPngSize(BinaryReader br) {
            return GetPngSize(br.BaseStream);
        }

        public static long GetPngSize(Stream st) {
            if (st == null) {
                return 0L;
            }

            long position = st.Position;
            long num = 0L;
            try {
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
                for (int i = 0; i < 8; i++) {
                    if (array[i] != array2[i]) {
                        st.Seek(position, SeekOrigin.Begin);
                        return 0L;
                    }
                }

                int num2 = 0;
                int num3 = 0;
                bool flag = true;
                while (flag) {
                    byte[] array3 = new byte[4];
                    st.Read(array3, 0, 4);
                    Array.Reverse(array3);
                    num2 = BitConverter.ToInt32(array3, 0);
                    byte[] array4 = new byte[4];
                    st.Read(array4, 0, 4);
                    num3 = BitConverter.ToInt32(array4, 0);
                    if (num3 == 1145980233) {
                        flag = false;
                    }

                    if (num2 + 4 > st.Length - st.Position) {
                        st.Seek(position, SeekOrigin.Begin);
                        return 0L;
                    }

                    st.Seek(num2 + 4, SeekOrigin.Current);
                }

                num = st.Position - position;
                st.Seek(position, SeekOrigin.Begin);
                return num;
            } catch (EndOfStreamException) {
                st.Seek(position, SeekOrigin.Begin);
                return 0L;
            }
        }

        public static void SkipPng(Stream st) {
            long pngSize = GetPngSize(st);
            st.Seek(pngSize, SeekOrigin.Current);
        }

        public static void SkipPng(BinaryReader br) {
            long pngSize = GetPngSize(br);
            br.BaseStream.Seek(pngSize, SeekOrigin.Current);
        }

        public static byte[] LoadPngBytes(string path) {
            using (FileStream st = new FileStream(path, FileMode.Open, FileAccess.Read)) {
                return LoadPngBytes(st);
            }
        }

        public static byte[] LoadPngBytes(Stream st) {
            using (BinaryReader br = new BinaryReader(st)) {
                return LoadPngBytes(br);
            }
        }

        public static byte[] LoadPngBytes(BinaryReader br) {
            long pngSize = GetPngSize(br);
            if (pngSize == 0) {
                return null;
            }

            return br.ReadBytes((int)pngSize);
        }
        #endregion

        #region Unity Stuff
        /// <summary>
        /// Load a PNG or JPG file to a Sprite 
        /// </summary>
        /// <param name="FilePath">Can be a filepath or a embedded resource path</param>
        /// <returns>Texture, or Null if load fails</returns>
        public static Sprite LoadNewSprite(string FilePath, int width = -1, int height = -1, float PixelsPerUnit = 100.0f) {
            Texture2D SpriteTexture = LoadTexture(FilePath, width, height);
            if (null == SpriteTexture || SpriteTexture.width == 0) {
                SpriteTexture = LoadDllResourceToTexture2D(FilePath, width, height);
            }

            return Sprite.Create(SpriteTexture, new Rect(0, 0, SpriteTexture.width, SpriteTexture.height), Vector2.zero, PixelsPerUnit);
        }

        /// <summary>
        /// Load a PNG or JPG file from disk to a Texture2D
        /// </summary>
        /// <param name="FilePath"></param>
        /// <returns>Texture, or Null if load fails</returns>
        public static Texture2D LoadTexture(string FilePath, int width = -1, int height = -1) {
            Texture2D texture;
            byte[] FileData;

            if (File.Exists(FilePath)) {
                FileData = File.ReadAllBytes(FilePath);
                texture = new Texture2D(2, 2, TextureFormat.ARGB32, false);
                if (texture.LoadImage(FileData)) {
                    if ((width > 0 && texture.width != width) || (height > 0 && texture.height != height)) {
                        texture = texture.Scale(width > 0 ? width : texture.width, height > 0 ? height : texture.height, mipmap: false);
                    }
                    return texture;
                }
            }
            return null;
        }

        /// <summary>
        /// Load a embedded PNG or JPG resource to a Texture2D
        /// </summary>
        /// <param name="FilePath"></param>
        /// <returns>byte array</returns>
        public static byte[] LoadDllResourceToBytes(string FilePath) {
            Assembly myAssembly = Assembly.GetExecutingAssembly();
            using (Stream myStream = myAssembly.GetManifestResourceStream(FilePath)) {
                return ReadToEnd(myStream);
            }
        }

        /// <summary>
        /// Load a embedded PNG or JPG resource to a Texture2D
        /// </summary>
        /// <param name="FilePath"></param>
        /// <returns>Texture, or Null if load fails</returns>
        public static Texture2D LoadDllResourceToTexture2D(string FilePath, int width = -1, int height = -1) {
            Assembly myAssembly = Assembly.GetExecutingAssembly();
            Texture2D texture = new Texture2D(2, 2, TextureFormat.ARGB32, false);
            using (Stream myStream = myAssembly.GetManifestResourceStream(FilePath)) {
                if (texture.LoadImage(ReadToEnd(myStream))) {
                    if ((width > 0 && texture.width != width) || (height > 0 && texture.height != height)) {
                        texture = texture.Scale(width > 0 ? width : texture.width, height > 0 ? height : texture.height, mipmap: false);
                    }
                    return texture;
                } else {
                    Logger.LogError($"Missing Dll resource: {FilePath}");
                }
            }
            return null;
        }

        private static byte[] ReadToEnd(Stream stream) {
            long originalPosition = stream.Position;
            //stream.Position = 0;

            try {
                byte[] readBuffer = new byte[4096];

                int totalBytesRead = 0;
                int bytesRead;

                while ((bytesRead = stream.Read(readBuffer, totalBytesRead, readBuffer.Length - totalBytesRead)) > 0) {
                    totalBytesRead += bytesRead;

                    if (totalBytesRead == readBuffer.Length) {
                        int nextByte = stream.ReadByte();
                        if (nextByte != -1) {
                            byte[] temp = new byte[readBuffer.Length * 2];
                            Buffer.BlockCopy(readBuffer, 0, temp, 0, readBuffer.Length);
                            Buffer.SetByte(temp, totalBytesRead, (byte)nextByte);
                            readBuffer = temp;
                            totalBytesRead++;
                        }
                    }
                }

                byte[] buffer = readBuffer;
                if (readBuffer.Length != totalBytesRead) {
                    buffer = new byte[totalBytesRead];
                    Buffer.BlockCopy(readBuffer, 0, buffer, 0, totalBytesRead);
                }
                return buffer;
            } finally {
                stream.Position = originalPosition;
            }
        }

        /// <summary>
        ///	Returns a scaled copy of given texture. 
        /// </summary>
        /// <param name="tex">Source texure to scale</param>
        /// <param name="width">Destination texture width</param>
        /// <param name="height">Destination texture height</param>
        /// <param name="mode">Filtering mode</param>
        public static Texture2D Scale(this Texture2D src, int width, int height, FilterMode mode = FilterMode.Trilinear, bool mipmap = true) {
            Rect texR = new Rect(0, 0, width, height);
            _gpu_scale(src, width, height, mode);

            //Get rendered data back to a new texture
            Texture2D result = new Texture2D(width, height, src.format, mipmap);
            result.Resize(width, height);
            result.ReadPixels(texR, 0, 0, true);
            result.Apply(true);
            return result;
        }

        ///// <summary>
        ///// Scales the texture data of the given texture.
        ///// </summary>
        ///// <param name="tex">Texure to scale</param>
        ///// <param name="width">New width</param>
        ///// <param name="height">New height</param>
        ///// <param name="mode">Filtering mode</param>
        //public static void Scale(Texture2D tex, int width, int height, FilterMode mode = FilterMode.Trilinear) {
        //    Rect texR = new Rect(0, 0, width, height);
        //    _gpu_scale(tex, width, height, mode);

        //    // Update new texture
        //    tex.Resize(width, height);
        //    tex.ReadPixels(texR, 0, 0, true);
        //    tex.Apply(true);   
        //}

        private static void _gpu_scale(Texture2D src, int width, int height, FilterMode fmode = FilterMode.Trilinear) {
            //We need the source texture in VRAM because we render with it
            src.filterMode = fmode;
            src.Apply(true);

            //Using RTT for best quality and performance. Thanks, Unity 5
            RenderTexture rtt = new RenderTexture(width, height, 32);

            //Set the RTT in order to render to it
            Graphics.SetRenderTarget(rtt);

            //Setup 2D matrix in range 0..1, so nobody needs to care about sized
            GL.LoadPixelMatrix(0, 1, 1, 0);

            //Then clear & draw the texture to fill the entire RTT.
            GL.Clear(true, true, new Color(0, 0, 0, 0));
            Graphics.DrawTexture(new Rect(0, 0, 1, 1), src);
        }

        public static Texture2D OverwriteTexture(this Texture2D background, Texture2D watermark, int startX, int startY) {
            Texture2D newTex = new Texture2D(background.width, background.height, background.format, false);
            for (int x = 0; x < background.width; x++) {
                for (int y = 0; y < background.height; y++) {
                    if (x >= startX && y >= startY && x - startX < watermark.width && y - startY < watermark.height) {
                        Color bgColor = background.GetPixel(x, y);
                        Color wmColor = watermark.GetPixel(x - startX, y - startY);

                        Color final_color = Color.Lerp(bgColor, wmColor, wmColor.a);
                        final_color.a = bgColor.a + wmColor.a;

                        newTex.SetPixel(x, y, final_color);
                    } else {
                        newTex.SetPixel(x, y, background.GetPixel(x, y));
                    }
                }
            }

            newTex.Apply();
            return newTex;
        }
        #endregion


    }
}

