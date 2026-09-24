using System.Collections.Frozen;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using NLog;
using Spectre.Console;
using ZOVserver.Shared.Contracts.Proto;
using ZOVserver.Shared.TitanRemnants.PAssets.Data;
using ZOVserver.Shared.TitanRemnants.PAssets.Maps;
using ZOVserver.Shared.TitanRemnants.PAssets.Proto;

namespace ZOVserver.Shared.TitanRemnants.PAssets.Tables;

public static class LogicDataTables
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private static bool _isLoaded;

    private static FrozenDictionary<Type, int> _csvRealizationsT =
        FrozenDictionary<Type, int>.Empty;

    private static FrozenDictionary<int, LogicData> _csvRealizationsX =
        FrozenDictionary<int, LogicData>.Empty;

    private static FrozenDictionary<int, FrozenDictionary<string, LogicData>> _csvRealizationsY =
        FrozenDictionary<int, FrozenDictionary<string, LogicData>>.Empty;

    private static FrozenDictionary<int, LogicData[]> _csvRealizationsZ =
        FrozenDictionary<int, LogicData[]>.Empty;

    private static FrozenDictionary<int, MapsManager> _mapsManagers =
        FrozenDictionary<int, MapsManager>.Empty;

    public static void LoadFrom(FileServerService.FileServerServiceClient client)
    {
        if (_isLoaded) throw new Exception("LogicDataTables already loaded!");

        Fingerprint.Parse(client);

        var stopwatch = Stopwatch.StartNew();

        Dictionary<int, MapsManager> mapsManager = new();
        Dictionary<Type, int> csvRealizations = new();
        Dictionary<int, LogicData> csvRealizationsX = new();
        Dictionary<int, FrozenDictionary<string, LogicData>> csvRealizationsY = new();
        Dictionary<int, LogicData[]> csvRealizationsZ = new();

        AnsiConsole.Progress()
            .Columns(new TaskDescriptionColumn { Alignment = Justify.Left }, new ProgressBarColumn(),
                new PercentageColumn(), new SpinnerColumn())
            .Start(ctx =>
            {
                var task1 = ctx.AddTask("[green]LogicDataTables preloading...[/]", maxValue: 100);

                var (csvCData, csvLData) = ReadCsvFilesFromFolder(client, task1);
                task1.Increment(5);

                var csvRealizationsTypes = FindCsvRealizations();
                task1.Increment(30);

                var task2 = ctx.AddTask("[DarkCyan]LogicDataTables loading...[/]",
                    maxValue: csvRealizationsTypes.Count + 5);

                foreach (var csvData in csvCData)
                {
                    if (!csvRealizationsTypes.TryGetValue(csvData.GetTableName(), out var type))
                        continue;

                    csvRealizations.Add(type, csvData.GetTableId());
                    task2.Increment(1);

                    var baseRealizations1 = new Dictionary<int, LogicData>();
                    var baseRealizations2 = new Dictionary<string, LogicData>();

                    foreach (var csvElement in csvData.GetElements())
                    {
                        var realization = Activator.CreateInstance(type, csvElement);
                        if (realization == null) throw new Exception("LogicData instance is null.");

                        baseRealizations1.Add(csvElement.GetInstanceId(), (LogicData)realization);
                        baseRealizations2.Add(csvElement.GetStringValue("Name"), (LogicData)realization);

                        csvRealizationsX.Add(csvElement.GetGlobalId(), (LogicData)realization);
                    }

                    csvRealizationsY.Add(csvData.GetTableId(), baseRealizations2.ToFrozenDictionary());
                    csvRealizationsZ.Add(csvData.GetTableId(), baseRealizations1.Values.ToArray());
                }

                Thread.Sleep(500);

                foreach (var csvData in csvLData)
                {
                    if (csvData.IsMap())
                    {
                        mapsManager.Add(csvData.GetTableId(), new MapsManager(csvData));
                        continue;
                    }

                    if (!csvRealizationsTypes.TryGetValue(csvData.GetTableName(), out var type))
                        continue;

                    csvRealizations.Add(type, csvData.GetTableId());
                    task2.Increment(1);

                    var baseRealizations1 = new Dictionary<int, LogicData>();
                    var baseRealizations2 = new Dictionary<string, LogicData>();

                    foreach (var csvElement in csvData.GetElements())
                    {
                        var realization = Activator.CreateInstance(type, csvElement);
                        if (realization == null) throw new Exception("LogicData instance is null.");

                        baseRealizations1.Add(csvElement.GetInstanceId(), (LogicData)realization);
                        baseRealizations2.Add(csvElement.GetStringValue("Name"), (LogicData)realization);

                        csvRealizationsX.Add(csvElement.GetGlobalId(), (LogicData)realization);
                    }

                    csvRealizationsY.Add(csvData.GetTableId(), baseRealizations2.ToFrozenDictionary());
                    csvRealizationsZ.Add(csvData.GetTableId(), baseRealizations1.Values.ToArray());
                }

                task2.Increment(5);
                return Task.CompletedTask;
            });

        _mapsManagers = mapsManager.ToFrozenDictionary();

        _csvRealizationsT = csvRealizations.ToFrozenDictionary();
        _csvRealizationsX = csvRealizationsX.ToFrozenDictionary();
        _csvRealizationsY = csvRealizationsY.ToFrozenDictionary();
        _csvRealizationsZ = csvRealizationsZ.ToFrozenDictionary();

        stopwatch.Stop();
        _isLoaded = true;

        Logger.Info(
            $"LogicDataTables (for v{Fingerprint.GetMajorVersion()}.{Fingerprint.GetBuildVersion()}) loaded in {stopwatch.ElapsedMilliseconds}ms. Loaded csv realizations: {_csvRealizationsZ.Count}. Loaded maps managers: {_mapsManagers.Count}.\n");
    }

    private static (List<CsvData> c, List<CsvData> l) ReadCsvFilesFromFolder(
        FileServerService.FileServerServiceClient client, ProgressTask task1)
    {
        const string csvClientPath = "csv_client";
        const string csvLogicPath = "csv_logic";
        task1.Increment(5);

        var files = client.GetAllFiles(new FileFilter { Extensions = { ".csv" }, IncludeSubfolders = true });
        task1.Increment(10);

        List<CsvData> c = [];
        List<CsvData> l = [];

        c.AddRange(from file in files.Files
            let data =
                Encoding.UTF8.GetString(file.Data.ToByteArray())
            let lines = data.Split('\n')
            where file.Path.Contains(csvClientPath)
            select CsvData.ParseFromFile(lines, file.Path));
        task1.Increment(25);

        l.AddRange(from file in files.Files
            let data =
                Encoding.UTF8.GetString(file.Data.ToByteArray())
            let lines = data.Split('\n')
            where file.Path.Contains(csvLogicPath)
            select CsvData.ParseFromFile(lines, file.Path));
        task1.Increment(25);

        return (c, l);
    }

    private static Dictionary<string, Type> FindCsvRealizations()
    {
        var result = new Dictionary<string, Type>();

        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (assembly.FullName == null)
                continue;

            foreach (var type in assembly.GetTypes())
            {
                if (!type.IsClass || !typeof(LogicData).IsAssignableFrom(type)) continue;

                var constructors = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
                if (constructors.Length != 1) continue;

                var parameters = constructors[0].GetParameters();

                if (parameters.Length == 1 && parameters[0].ParameterType == typeof(CsvElement))
                    result[type.Name] = type;
            }
        }

        return result;
    }

    public static LogicData? GetDataById(int id1, int id2 = -1)
    {
        if (!_isLoaded) throw new Exception("LogicDataTables not loaded!");

        int id;

        if (id1 >= 1_000_000)
            id = id1;
        else if (id1 < 0 || id2 < 0)
            return null;
        else
            id = id1 * 1_000_000 + id2;

        return _csvRealizationsX.GetValueOrDefault(id);
    }

    public static T? GetDataById<T>(int id1, int id2 = -1) where T : LogicData
    {
        if (!_isLoaded) throw new Exception("LogicDataTables not loaded!");

        if (id1 >= 1_000_000 || id2 != -1)
            return GetDataById(id1, id2) as T;

        if (!_csvRealizationsT.TryGetValue(typeof(T), out var classId))
            return null;

        return GetDataById(classId, id1) as T;
    }

    public static LogicData? GetDataByName(int classId, string name)
    {
        if (!_isLoaded) throw new Exception("LogicDataTables not loaded!");

        return _csvRealizationsY.GetValueOrDefault(classId)?.GetValueOrDefault(name);
    }

    public static T? GetDataByName<T>(string name, int classId = -1) where T : LogicData
    {
        if (!_isLoaded) throw new Exception("LogicDataTables not loaded!");

        if (classId != -1)
            return GetDataByName(classId, name) as T;

        if (!_csvRealizationsT.TryGetValue(typeof(T), out classId))
            return null;

        return GetDataByName(classId, name) as T;
    }

    public static LogicData[]? GetAllDataByClassId(int classId)
    {
        if (!_isLoaded) throw new Exception("LogicDataTables not loaded!");

        return _csvRealizationsZ.GetValueOrDefault(classId);
    }

    public static T[]? GetAllDataByClassId<T>(int? classId = null) where T : LogicData
    {
        if (!_isLoaded) throw new Exception("LogicDataTables not loaded!");

        var actualClassId = classId ?? _csvRealizationsT.GetValueOrDefault(typeof(T), -1);
        if (actualClassId == -1) return null;

        var data = GetAllDataByClassId(actualClassId);
        if (data == null) return null;

        if (data.Length == 0)
            return [];

        return data[0] is not T ? null : Unsafe.As<LogicData[], T[]>(ref data);
    }

    public static MapsManager? GetMapsManager(int tableId)
    {
        if (!_isLoaded) throw new Exception("LogicDataTables not loaded!");

        return _mapsManagers.GetValueOrDefault(tableId);
    }
}