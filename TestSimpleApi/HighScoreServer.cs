using SimpleApi;
using Newtonsoft.Json;
using System.Security.Cryptography;
using System.Text;

namespace HighScoreServer;

internal class HighScoreServer : Server
{
    private readonly HighScoresData highScoresData;
    public HighScoreServer() : base(defaultHost: $"http://{LocalIPAddress()}:8415/highScores/")
    {
        if (!Loadable.LoadLoadable(out highScoresData))
            highScoresData = new();
    }

    protected override IEnumerable<(string, Func<string, string>)> GetPrefixes(List<(string, Func<string, string>)> prefixes)
    {
        prefixes.Add(("upload/", UploadHighScore));
        prefixes.Add(("getscores/", DownloadHighScores));
        return prefixes;
    }

    private string UploadHighScore(string inputRequest)
    {
        HighScoreInstance? scoreInstance = JsonConvert.DeserializeObject<HighScoreInstance>(inputRequest);
        if (scoreInstance != null && scoreInstance.Hash == BitConverter.ToString(SHA256.HashData(Encoding.UTF8.GetBytes(scoreInstance.Name + scoreInstance.Score + Salt._Salt))).Replace("-", ""))
        {
            highScoresData.AddInstance(scoreInstance with { Hash = "none" });
            Loadable.WriteObject(highScoresData);
        }

        return string.Empty;
    }

    private string DownloadHighScores(string inputRequest)
    {
        return JsonConvert.SerializeObject(highScoresData);
    }
}

internal record class HighScoreInstance(DateTime Time, string Name, int Score, string Hash);

internal class HighScoresData : Loadable
{
    //dont forget to change when its leap year!!!!
    private static readonly int daysInAYear = 365;
    private static readonly int daysInAMonth = 30;

    public readonly List<HighScoreInstance> all_time = new();
    public readonly List<HighScoreInstance> past_30days = new();
    public readonly List<HighScoreInstance> past_year = new();

    protected override void Initalise(params object[] objects) 
    {
        List<HighScoreInstance> All = new();
        All.AddRange(all_time);
        All.AddRange(past_30days);
        All.AddRange(past_year);

        all_time.Clear();
        past_30days.Clear();
        past_year.Clear();

        foreach(var instance in All)
            AddInstance(instance);
    }

    public void AddInstance(HighScoreInstance instance)
    {
        if ((DateTime.Now - instance.Time).TotalDays < daysInAMonth)
            past_30days.Add(instance);

        if ((DateTime.Now - instance.Time).TotalDays < daysInAYear)
            past_year.Add(instance);

        all_time.Add(instance);
    }
}




internal abstract class Loadable
{
    protected Loadable() { }

    public static bool LoadLoadable<T>(out T @object, params object[] objects) where T : Loadable, new()
    {
        GetNames<T>(out string dataFolderPath, out string dataJsonPath);

        if(!Directory.Exists(dataFolderPath))
            Directory.CreateDirectory(dataFolderPath);

        if (!File.Exists(dataJsonPath))
            File.WriteAllText(dataJsonPath, JsonConvert.SerializeObject(new T()));

        var loadedObject = JsonConvert.DeserializeObject<T?>(File.ReadAllText(dataJsonPath));
        loadedObject?.Initalise(objects);

        if(loadedObject == null)
        {
            WriteDefault<T>();
            @object = (T?)JsonConvert.DeserializeObject(File.ReadAllText(dataJsonPath)) ?? throw new JsonSerializationException("Unable to serialise object");
            @object.Initalise(objects);
            return false;
        }

        @object = loadedObject;
        return true;
    }

    public static void WriteDefault<T>() where T : Loadable, new()
    {
        GetNames<T>(out string dataFolderPath, out string dataJsonPath);

        Directory.CreateDirectory(dataFolderPath);

        File.WriteAllText(dataJsonPath, JsonConvert.SerializeObject(new T()));
    }

    public static void WriteObject<T>(T @object) where T : Loadable
    {
        GetNames<T>(out string dataFolderPath, out string dataJsonPath);

        if (!Directory.Exists(dataFolderPath))
            Directory.CreateDirectory(dataFolderPath);

        File.WriteAllText(dataJsonPath, JsonConvert.SerializeObject(@object));
    }

    private static void GetNames<T>(out string dataFolderPath, out string dataJsonPath)
    {
        Type getType = typeof(T);

        string thisTypeName = getType.Name;

        dataFolderPath = $"{AppDomain.CurrentDomain.BaseDirectory}\\{thisTypeName}";
        dataJsonPath = $"{dataFolderPath}\\{thisTypeName}.json";
    }

    protected abstract void Initalise(params object[] objects);
}

internal static class Salt
{
    public static readonly string _Salt = "nonono";
}