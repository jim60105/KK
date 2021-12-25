Extension.Logger.logger = null;
PngCompression.PngCompression pngCompression = new();
List<Task> tasks = new();
DirectoryInfo decompressDirectory = Directory.CreateDirectory("decompress_" + DateTime.Now.Ticks);

Console.WriteLine($"Start decompression in parallel.");
Console.WriteLine($"All png files will be output to {decompressDirectory.FullName}");

var files = Directory.GetFiles(Directory.GetCurrentDirectory(), "*.png");
foreach (string file in files)
{
    tasks.Add(Task.Run(() =>
    {
        string target = Path.Combine(decompressDirectory.FullName, Path.GetFileName(file));
        try
        {
            Console.WriteLine($"Start decompression: {Path.GetFileName(file)}");
            if (0 != pngCompression.Load(file, target))
            {
                Console.WriteLine($"Finish decompression: {Path.GetFileName(file)}");
            }
            else
            {
                Console.WriteLine($"Copy not compressed file: {Path.GetFileName(file)}");
                File.Copy(file, target, true);
            }
        }
        catch (FileLoadException)
        {
            Console.WriteLine($"Skip not support file: {Path.GetFileName(file)}");
            File.Copy(file, target, true);
        };
    }));
}

Task.WaitAll(tasks.ToArray());

Console.WriteLine($"Processed {files.Length} files.");
Console.ReadKey();

