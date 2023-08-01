using System;
using System.IO;
using System.Net.Http;
using System.IO.Compression;
using System.Threading.Tasks;

public class ZipDownloader
{
    private HttpClient _httpClient { get; set; }
    public ZipDownloader()
    {
        _httpClient = new();
    }
    public async Task DownloadAndExtractZip(string url, string destinationDirectory)
    {
        try
        {
            destinationDirectory = destinationDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            Directory.CreateDirectory(destinationDirectory);
            string zipFilePath = Path.Combine(destinationDirectory, "downloaded.zip");
            if (!File.Exists(zipFilePath))
            {
                // Download the zip file (.Result is ugly I know, but somehow the await keeps crashing the app, need to look into this)
                byte[] zipData = _httpClient.GetByteArrayAsync(url).Result;

                // Save the zip file to a local location
                File.WriteAllBytes(zipFilePath, zipData);
            }

            // Extract the contents of the zip file
            Console.WriteLine("> Succesfully downloaded, extracting now...");
            using (ZipArchive archive = new ZipArchive(new FileStream(zipFilePath, FileMode.Open)))
            {
                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    string entryPath = Path.Combine(Directory.GetParent(destinationDirectory)!.FullName, entry.FullName);
                    if (entry.FullName.EndsWith("/") || entry.FullName.EndsWith("\\"))
                    {
                        // Entry is a directory, create it in the destination directory
                        Directory.CreateDirectory(entryPath);
                    }
                    else
                    {
                        // Create the parent directory if it doesn't exist
                        Directory.CreateDirectory(Path.GetDirectoryName(entryPath)!);

                        // Extract the entry to the destination directory
                        entry.ExtractToFile(entryPath, true);
                    }
                }
            }

            File.Delete(zipFilePath);

            Console.WriteLine("> Zip file downloaded and extracted successfully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine("> Error downloading zip file: " + ex.Message);
        }
    }
}
