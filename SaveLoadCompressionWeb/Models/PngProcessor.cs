using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace SaveLoadCompressionWeb.Models
{
    public class PngProcessor
    {
        //private readonly ILogger logger;
        private readonly PngCompression.PngCompression pngCompression;
        private readonly IJSRuntime JS;

        public PngProcessor(ILoggerFactory loggerFactory,
                            PngCompression.PngCompression pngCompression,
                            IJSRuntime JS)
        {
            //this.logger = loggerFactory.CreateLogger<Pages.Index>();
            this.pngCompression = pngCompression;
            this.JS = JS;
        }

        /// <summary>
        /// 讀入圖檔，將未壓縮的檔案壓縮，已壓縮的檔案解壓縮
        /// </summary>
        /// <param name="inputStream">讀入資料流</param>
        /// <param name="writeStream">輸出資料流</param>
        /// <param name="doCompare">是否在壓縮完後做比較</param>
        /// <returns>(圖檔, 是否作了壓縮)</returns>
        public async Task<(byte[], bool)> DoMainPngProcessAsync(Stream inputStream, Stream writeStream, bool doCompare)
        {
            byte[] pngData = Array.Empty<byte>();
            using (BinaryReader binaryReader = new BinaryReader(inputStream))
            {
                pngData = Extension.ImageHelper.LoadPngBytes(binaryReader);
                var pngEndPosition = binaryReader.BaseStream.Position;
                string token = pngCompression.GuessToken(binaryReader);
                Console.WriteLine($"Detact token as {token}");

                inputStream.Seek(pngEndPosition, SeekOrigin.Begin);
                if (pngCompression.GuessCompressed(binaryReader))
                {
                    // Compressed, do decompressed
                    Console.WriteLine("Get compressed file. Start making png data...");
                    pngData = await MakeWatermarkPic(pngData, token, false);
                    inputStream.Seek(0, SeekOrigin.Begin);
                    Console.WriteLine("Start decompress...");
                    pngCompression.Load(inputStream, writeStream,
                        token: token,
                        pngData: pngData,
                        decompressProgress: (progress) =>
                        {
                            Console.WriteLine($"Decompress Progress: {progress:p3}");
                        });
                    return (pngData, false);
                }
                else
                {
                    // Not compressed, do compress
                    Console.WriteLine("Get not compressed file. Start making png data...");
                    pngData = await MakeWatermarkPic(pngData, token, true);
                    inputStream.Seek(0, SeekOrigin.Begin);
                    Console.WriteLine("Start compress...");
                    pngCompression.Save(inputStream, writeStream,
                        token: token,
                        pngData: pngData,
                        compressProgress: (progress) =>
                        {
                            Console.WriteLine($"Compress Progress: {progress:p3}");
                        },
                        doComapre: doCompare,
                        compareProgress: (progress) =>
                        {
                            Console.WriteLine($"Compare Progress: {progress:p3}");
                        });
                    return (pngData, true);
                }
            }
        }

        private async Task<byte[]> MakeWatermarkPic(byte[] pngData, string token, bool zip)
        {
            string background_base64URI = $"data:image;base64,{Convert.ToBase64String(pngData)}";
            Console.WriteLine("Load background image");
            byte[] watermark = Array.Empty<byte>();
            if (zip)
            {
                watermark = Extension.ImageHelper.LoadDllResourceToBytes($"SaveLoadCompressionWeb.Resources.zip_watermark.png");
                Console.WriteLine("Load zip watermark image");
            }
            else
            {
                watermark = Extension.ImageHelper.LoadDllResourceToBytes($"SaveLoadCompressionWeb.Resources.unzip_watermark.png");
                Console.WriteLine("Load unzip watermark image");
            }
            string watermark_base64URI = $"data:image;base64,{Convert.ToBase64String(watermark)}";

            double scale = (double)(pngCompression.GetScaleTimes(token) * Extension.ImageHelper.GetDimensions(pngData)[0]) / Extension.ImageHelper.GetDimensions(watermark)[0];
            Console.WriteLine($"Png width: {Extension.ImageHelper.GetDimensions(pngData)[0]} : {Extension.ImageHelper.GetDimensions(watermark)[0]}");
            Console.WriteLine($"Watermark Scale : {scale}");

            var base64 = await JS.InvokeAsync<string>("indexJs.addWatermark", background_base64URI, watermark_base64URI, scale);
            Console.WriteLine("Add watermark finish");
            var resultData = Convert.FromBase64String(base64);
            Console.WriteLine($"New pngData size: {resultData.Length}");
            return resultData;
        }

    }
}
