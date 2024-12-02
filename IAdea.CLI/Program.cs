using IAdea.Json;

namespace IAdea.CLI;

public sealed class Program
{
    public static async Task Main(string[] args)
    {
#if !DEBUG
        string deviceIPAddress = Console.ReadLine();
        var device = new DeviceSession(deviceIPAddress!);
#else
        var device = new DeviceSession("99.99.99.99");
#endif

        if (!await device.AuthenticateAsync())
        {
            Console.WriteLine("Failed to authenticate with the device.");
            return;
        }

        IAFileResources? fileResources = await device.FindFilesAsync().ConfigureAwait(false);
        if (fileResources == null)
        {
            Console.WriteLine("Failed to retrieve files from the device.");
            return;
        }
        Console.WriteLine($"Found {fileResources.Resources.Length} files stored on the device.");

        Console.WriteLine("Downloading files...");
        await foreach (IAFileResource downloadedResource in device.DownloadFilesAsync(fileResources.Resources, "C:\\Temp\\Extracted_Files").ConfigureAwait(false))
        {
            Console.WriteLine($"   File Downloaded( {SizeSuffix(downloadedResource.FileSize, 2)} ): {device.GetPathWithToken(downloadedResource.DownloadPath)}");
        }
        Console.WriteLine("All files have been downloaded from the device.");
        Console.ReadLine();
    }

    // https://stackoverflow.com/questions/14488796/does-net-provide-an-easy-way-convert-bytes-to-kb-mb-gb-etc
    static readonly string[] SizeSuffixes = ["bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB"];
    static string SizeSuffix(long value, int decimalPlaces = 1)
    {
        if (decimalPlaces < 0) { throw new ArgumentOutOfRangeException("decimalPlaces"); }
        if (value < 0) { return "-" + SizeSuffix(-value, decimalPlaces); }
        if (value == 0) { return string.Format("{0:n" + decimalPlaces + "} bytes", 0); }

        // mag is 0 for bytes, 1 for KB, 2, for MB, etc.
        int mag = (int)Math.Log(value, 1024);

        // 1L << (mag * 10) == 2 ^ (10 * mag) 
        // [i.e. the number of bytes in the unit corresponding to mag]
        decimal adjustedSize = (decimal)value / (1L << (mag * 10));

        // make adjustment when the value is large enough that
        // it would round up to 1000 or more
        if (Math.Round(adjustedSize, decimalPlaces) >= 1000)
        {
            mag += 1;
            adjustedSize /= 1024;
        }

        return string.Format("{0:n" + decimalPlaces + "} {1}",
            adjustedSize,
            SizeSuffixes[mag]);
    }
}