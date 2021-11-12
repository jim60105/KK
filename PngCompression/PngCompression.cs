/*
MMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMM
MMM               MMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMM    M7    MZ    MMO    MMMMM
MMM               MMMMMMMMMMMMM   MMM     MMMMMMMMMM    M$    MZ    MMO    MMMMM
MMM               MMMMMMMMMM       ?M     MMMMMMMMMM    M$    MZ    MMO    MMMMM
MMMMMMMMMMMM8     MMMMMMMM       ~MMM.    MMMMMMMMMM    M$    MZ    MMO    MMMMM
MMMMMMMMMMMMM     MMMMM        MMM                 M    M$    MZ    MMO    MMMMM
MMMMMMMMMMMMM     MM.         ZMMMMMM     MMMM     MMMMMMMMMMMMZ    MMO    MMMMM
MMMMMMMMMMMMM     MM      .   ZMMMMMM     MMMM     MMMMMMMMMMMM?    MMO    MMMMM
MMMMMMMMMMMMM     MMMMMMMM    $MMMMMM     MMMM     MMMMMMMMMMMM?    MM8    MMMMM
MMMMMMMMMMMMM     MMMMMMMM    7MMMMMM     MMMM     MMMMMMMMMMMMI    MM8    MMMMM
MMM               MMMMMMMM    7MMMMMM     MMMM    .MMMMMMMMMMMM.    MMMM?ZMMMMMM
MMM               MMMMMMMM.   ?MMMMMM     MMMM     MMMMMMMMMM ,:MMMMMM?    MMMMM
MMM           ..MMMMMMMMMM    =MMMMMM     MMMM     M$ MM$M7M $MOM MMMM     ?MMMM
MMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMM .+Z: M   :M M  MM   ?MMMMM
MMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMM
MMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMM
*/

using Extension;
using SevenZip;
using System;
using System.IO;
using System.Linq;

namespace PngCompression
{
    public struct Token
    {
        //https://github.com/IllusionMods/DragAndDrop/blob/v1.2/src/DragAndDrop.Koikatu/DragAndDrop.cs#L12
        public const string StudioToken = "【KStudio】";
        public const string CharaToken = "【KoiKatuChara";
        public const string SexToken = "sex";
        public const string CoordinateToken = "【KoiKatuClothes】";
        //private const string PoseToken = "【pose】";
    }

    public class PngCompression
    {
        /// <summary>
        /// 取得浮水印的縮放倍率
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        public float GetScaleTimes(string token) => (token == Token.StudioToken) ? .14375f : .30423f;

        public long Save(string inputPath, string outputPath, string token = null, byte[] pngData = null, Action<decimal> compressProgress = null, bool doComapre = true, Action<decimal> compareProgress = null)
        {
            using (FileStream fileStreamReader = new FileStream(inputPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (FileStream fileStreamWriter = new FileStream(outputPath, FileMode.Create, FileAccess.Write))
            {
                return Save(fileStreamReader,
                            fileStreamWriter,
                            token: token,
                            pngData: pngData,
                            compressProgress: compressProgress,
                            doComapre: doComapre,
                            compareProgress: compareProgress);
            }
        }

        public long Save(Stream inputStream,
                         Stream outputStream,
                         string token = null,
                         byte[] pngData = null,
                         Action<decimal> compressProgress = null,
                         bool doComapre = true,
                         Action<decimal> compareProgress = null)
        {
            long dataSize = 0;

            Action<long, long> _compressProgress = null;
            if (null != compressProgress)
            {
                _compressProgress = (long inSize, long _) => compressProgress(Convert.ToDecimal(inSize) / dataSize);
            }

            //Make png watermarked
            using (BinaryReader binaryReader = new BinaryReader(inputStream))
            using (BinaryWriter binaryWriter = new BinaryWriter(outputStream))
            {
                if (null == pngData)
                {
                    pngData = ImageHelper.LoadPngBytes(binaryReader);
                }
                else
                {
                    ImageHelper.SkipPng(binaryReader);
                    Logger.LogDebug("Skip Png:" + inputStream.Position);
                }

                dataSize = inputStream.Length - inputStream.Position;

                binaryWriter.Write(pngData);

                if (null == token)
                {
                    token = GuessToken(binaryReader);
                }

                switch (token)
                {
                    case Token.StudioToken:
                        //Studio
                        binaryWriter.Write(new Version(101, 0, 0, 0).ToString());
                        break;
                    case Token.CoordinateToken:
                        //Coordinate
                        binaryWriter.Write(101);
                        break;
                    default:
                        //Chara
                        if (token.IndexOf(Token.CharaToken) >= 0)
                        {
                            binaryWriter.Write(101);
                            break;
                        }

                        throw new Exception("Token not match.");
                }

                //為了通過 InvalidSceneFileProtection 和 DragAndDrop
                binaryWriter.Write(token);

                using (MemoryStream msCompressed = new MemoryStream())
                {
                    //PngFile.SkipPng(inputStream);
                    long fileStreamPos = inputStream.Position;

                    LZMA.Compress(
                        inputStream,
                        msCompressed,
                        LzmaSpeed.Fastest,
                        DictionarySize.VeryLarge,
                        _compressProgress
                    );

                    Logger.LogDebug("Start compression test...");
                    if (doComapre)
                    {
                        using (MemoryStream msDecompressed = new MemoryStream())
                        {
                            msCompressed.Seek(0, SeekOrigin.Begin);

                            LZMA.Decompress(msCompressed, msDecompressed);
                            inputStream.Seek(fileStreamPos, SeekOrigin.Begin);
                            msDecompressed.Seek(0, SeekOrigin.Begin);
                            int bufferSize = 1 << 10;
                            byte[] aByteA = new byte[(int)bufferSize];
                            byte[] bByteA = new byte[(int)bufferSize];

                            if ((inputStream.Length - inputStream.Position) != msDecompressed.Length)
                            {
                                return 0;
                            }

                            for (long i = 0; i < msDecompressed.Length;)
                            {
                                if (null != compressProgress)
                                {
                                    compareProgress(Convert.ToDecimal(i) / msDecompressed.Length);
                                }

                                inputStream.Read(aByteA, 0, (int)bufferSize);
                                i += msDecompressed.Read(bByteA, 0, (int)bufferSize);
                                if (!aByteA.SequenceEqual(bByteA))
                                {
                                    return 0;
                                }
                            }
                        }
                    }
                    binaryWriter.Write(msCompressed.ToArray());
                    return binaryWriter.BaseStream.Length;
                }
            }
        }

        public long Load(string inputPath, string outputPath, string token = null, Action<decimal> decompressProgress = null)
        {
            using (FileStream fileStreamReader = new FileStream(inputPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (FileStream fileStreamWriter = new FileStream(outputPath, FileMode.Create, FileAccess.Write))
            {
                return Load(
                    fileStreamReader,
                    fileStreamWriter,
                    token: token,
                    decompressProgress: decompressProgress);
            }
        }

        public long Load(Stream inputStream,
                         Stream outputStream,
                         string token = null,
                         byte[] pngData = null,
                         Action<decimal> decompressProgress = null)
        {
            long dataSize = 0;
            Action<long, long> _decompressProgress = null;
            if (null != decompressProgress)
            {
                _decompressProgress = (long inSize, long _) => decompressProgress(Convert.ToDecimal(inSize) / dataSize);
            }

            using (BinaryReader binaryReader = new BinaryReader(inputStream))
            using (BinaryWriter binaryWriter = new BinaryWriter(outputStream))
            {
                if (null == pngData)
                {
                    pngData = ImageHelper.LoadPngBytes(binaryReader);
                }
                else
                {
                    ImageHelper.SkipPng(binaryReader);
                    Logger.LogDebug("Skip Png:" + inputStream.Position);
                }

                if (!GuessCompressed(binaryReader))
                {
                    //Extension.Logger.LogDebug("Not a compressed file.");
                    return 0;
                }

                try
                {
                    if (null == token)
                    {
                        token = GuessToken(binaryReader);
                        if (null == token)
                        {
                            throw new FileLoadException();
                        }
                    }
                    bool checkfail = false;

                    switch (token)
                    {
                        case Token.StudioToken:
                            checkfail = !new Version(binaryReader.ReadString()).Equals(new Version(101, 0, 0, 0));
                            break;
                        case Token.CoordinateToken:
                        default:
                            //Token.CharaToken
                            checkfail = 101 != binaryReader.ReadInt32();
                            break;
                    }

                    if (checkfail)
                    {
                        throw new FileLoadException();
                    }
                }
                catch (FileLoadException e)
                {
                    Logger.LogError("Corrupted file");
                    throw e;
                }
                try
                {
                    //Discard token string
                    binaryReader.ReadString();

                    Logger.LogDebug("Start Decompress...");
                    binaryWriter.Write(pngData);

                    dataSize = inputStream.Length - inputStream.Position;
                    LZMA.Decompress(inputStream, outputStream, _decompressProgress);
                }
                catch (Exception)
                {
                    Logger.LogError($"Decompression FAILDED. The file was corrupted during compression or storage.");
                    Logger.LogError($"Do not disable the byte comparison setting next time to avoid this.");
                    throw;
                }
                return binaryWriter.BaseStream.Length;
            }
        }

        /// <summary>
        /// 偵測token。BinaryReader之Position必須處在pngData之後。
        /// </summary>
        /// <param name="binaryReader"></param>
        /// <returns></returns>
        public string GuessToken(BinaryReader binaryReader)
        {
            long position = binaryReader.BaseStream.Position;
            try
            {
                int r = binaryReader.ReadInt32();
                if (r != 101 && r != 100)
                {
                    return Token.StudioToken;
                }
                string token = binaryReader.ReadString();
                if (token.IndexOf(Token.CharaToken) >= 0)
                {
                    // 這裡不知道角色性別，直接給1(女性)
                    // 跨性別讀取基本上夠完善，我想可以略過判別
                    return Token.CharaToken + "】" + Token.SexToken + 1;
                }
                else if (token == Token.CoordinateToken)
                {
                    return Token.CoordinateToken;
                }
            }
            finally
            {
                binaryReader.BaseStream.Seek(position, SeekOrigin.Begin);
            }
            return null;
        }

        /// <summary>
        /// 偵測是否為已壓縮存檔。BinaryReader之Position必須處在pngData之後。
        /// </summary>
        /// <param name="binaryReader"></param>
        /// <returns></returns>
        public bool GuessCompressed(BinaryReader binaryReader)
        {
            long position = binaryReader.BaseStream.Position;
            try
            {
                int r = binaryReader.ReadInt32();
                switch (r)
                {
                    case 101:
                        return true;
                    case 100:
                        return false;
                    default:
                        // Studio
                        binaryReader.BaseStream.Seek(position, SeekOrigin.Begin);
                        string st = binaryReader.ReadString();
                        Version version = new Version(st);
                        return version.Major == 101;
                }
            }
            finally
            {
                binaryReader.BaseStream.Seek(position, SeekOrigin.Begin);
            }
        }
    }
}
