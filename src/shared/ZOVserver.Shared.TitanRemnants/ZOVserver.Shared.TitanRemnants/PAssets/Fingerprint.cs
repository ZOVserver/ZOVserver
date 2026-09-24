using System.Text;
using Newtonsoft.Json;
using ZOVserver.Shared.Contracts.Proto;

namespace ZOVserver.Shared.TitanRemnants.PAssets;

public static class Fingerprint
{
    private static dynamic? Data { get; set; }

    private static string Sha { get; set; } = null!;
    private static int Major { get; set; }
    private static int Build { get; set; }
    private static int Minor { get; set; }

    internal static void Parse(FileServerService.FileServerServiceClient client)
    {
        Data = JsonConvert.DeserializeObject(Encoding.UTF8.GetString(
            client.GetFile(new FileRequest { Path = "fingerprint.json" }).Data.ToByteArray()));

        if (Data == null) return;

        Sha = Data.sha.ToString()!;
        Major = int.Parse((Data.version.ToString().Split('.')[0] as string)!);
        Build = int.Parse(Data.version.ToString().Split('.')[1]);
        Minor = int.Parse(Data.version.ToString().Split('.')[2]);
    }

    public static string GetResourceSha()
    {
        return Sha;
    }

    public static int GetMajorVersion()
    {
        return Major;
    }

    public static int GetBuildVersion()
    {
        return Build;
    }

    public static int GetMinorVersion()
    {
        return Minor;
    }
}