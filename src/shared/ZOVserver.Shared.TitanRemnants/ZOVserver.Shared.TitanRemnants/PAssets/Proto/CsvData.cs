using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Text.RegularExpressions;
using ZOVserver.Shared.TitanRemnants.PAssets.Maps;
using ZOVserver.Shared.TitanRemnants.PAssets.Tables;

namespace ZOVserver.Shared.TitanRemnants.PAssets.Proto;

public class CsvData(int tableId, string tableName, bool map = false)
{
    private readonly ConcurrentDictionary<int, CsvElement> _elements = new();
    private FrozenDictionary<string, HighLevelMapData> _dictionaryH = FrozenDictionary<string, HighLevelMapData>.Empty;
    private HighLevelMapData[] _maps = [];

    private void LoadMapsNew(string[] lines)
    {
        if (!map) return;

        Dictionary<string, LowLevelMapData> dictionaryL = new();

        LowLevelMapData currentLowLevelMapData = null!;

        foreach (var line in lines)
        {
            var parts = line.Split([','], 3);

            if (!string.IsNullOrWhiteSpace(parts[0]))
            {
                var currentGroup = parts[0].Replace("\"", "");

                if (!dictionaryL.TryGetValue(currentGroup, out var value))
                {
#pragma warning disable SYSLIB1045
                    currentLowLevelMapData = new LowLevelMapData
                    {
                        GroupName = currentGroup,
#pragma warning disable S6444
                        Metadata = Regex.Replace(parts[2], "(?<!\")\"(?!\"|$)", "").Replace("\"\"", "\"")
#pragma warning restore S6444
                    };

                    if (currentLowLevelMapData.Metadata.Length > 0)
                        currentLowLevelMapData.Metadata = currentLowLevelMapData.Metadata[..^1];
#pragma warning restore SYSLIB1045

                    dictionaryL[currentGroup] = currentLowLevelMapData;
                }
                else
                {
                    currentLowLevelMapData = value;
                }
            }

            if (currentLowLevelMapData != null && parts.Length > 2 && !string.IsNullOrWhiteSpace(parts[1]))
                currentLowLevelMapData.DataLines.Add(parts[1]);
        }

        dictionaryL.Remove(dictionaryL.First().Key);
        dictionaryL.Remove(dictionaryL.First().Key);
        dictionaryL.Remove(dictionaryL.Last().Key);

        var origHdict = new Dictionary<string, HighLevelMapData>();
        {
            foreach (var (key, val) in dictionaryL)
            {
                {
                    var dl = val.DataLines;
                    var c = dl.Count;

                    var width = 0;

                    for (var l = 0; l < c; l++)
                    {
                        val.FullData += dl[l].Replace("\"", "");
                        if (l == 0) width = val.FullData.Length;
                    }

                    val.WidthAndHeight = (width, c);
                }

                origHdict.Add(key,
                    new HighLevelMapData(val.GroupName, val.FullData, val.Metadata, val.WidthAndHeight.Item1,
                        val.WidthAndHeight.Item2));
            }
        }

        _dictionaryH = origHdict.ToFrozenDictionary();
        _maps = _dictionaryH.Values.ToArray();
    }

    private void LoadMapsOld(string[] lines)
    {
        var maps = new List<HighLevelMapData>();
        using var reader = new StringReader(string.Join(Environment.NewLine, lines));

        reader.ReadLine();
        reader.ReadLine();

        string currentGroup = null!;
        var currentGroupData = new List<string>();

        while (true)
        {
            var line = reader.ReadLine();

            if (line == null)
            {
                if (currentGroupData.Count != 0)
                {
                    var width = currentGroupData.Max(line1 => line1.Length);
                    var height = currentGroupData.Count;
                    var data1 = string.Join("", currentGroupData);

                    maps.Add(new HighLevelMapData(currentGroup, data1, "", width, height));
                }

                break;
            }

            var parts = line.Split(',');
            if (parts.Length < 4) continue;

            var group = parts[1].Trim('"');
            var data = parts[2].Trim('"');

            if (!string.IsNullOrEmpty(group) && group != currentGroup)
            {
                if (currentGroupData.Count != 0)
                {
                    var width = currentGroupData.Max(line1 => line1.Length);
                    var height = currentGroupData.Count;
                    var data1 = string.Join("", currentGroupData);

                    maps.Add(new HighLevelMapData(currentGroup, data1, "", width, height));
                }

                currentGroup = group;
                currentGroupData = [];
            }

            currentGroupData.Add(data);
        }

        _dictionaryH = maps.ToDictionary(x => x.MapName, y => y).ToFrozenDictionary();
        _maps = _dictionaryH.Values.ToArray();
    }

    public bool IsMap()
    {
        return map;
    }

    public HighLevelMapData? GetMapData(string mapName)
    {
        if (!map)
            throw new Exception("An attempt was made to incorrectly access a CsvData instance that is not a Map.");

        return _dictionaryH.GetValueOrDefault(mapName);
    }

    public HighLevelMapData[] GetMapsData()
    {
        if (!map)
            throw new Exception("An attempt was made to incorrectly access a CsvData instance that is not a Map.");

        return _maps;
    }

    public static CsvData ParseFromFile(string[] lines, string filePath)
    {
        if (lines.Length < 2)
            throw new InvalidDataException("CSV file must have at least two lines.");

        var tableFname = DataTablesInfo.GetTableFilenameByPath(filePath);
        var tableId = DataTablesInfo.GetTableIdByCsvName(tableFname);
        var tableName = DataTablesInfo.GetTableNameByCsvName(tableFname);

        if (tableId == -1)
            throw new Exception($"Information about CSV ({tableFname}.csv) has not been found!");

        if (tableFname.StartsWith("map"))
        {
            var map = new CsvData(tableId, tableName, true);

            if (lines[0].Contains("MetaData"))
                map.LoadMapsNew(lines);
            else
                map.LoadMapsOld(lines);

            return map;
        }

        var csvData = new CsvData(tableId, tableName);
        var columnNames = lines[0].Split(',')
            .Select(name => name.Trim('"').Replace("\"", "").Replace("\r", "")).ToArray();

        var insid = 0;
        for (var i = 2; i < lines.Length; i++)
        {
            var line = lines[i];

            if (string.IsNullOrWhiteSpace(line))
                // Console.WriteLine($"Warning ({tableFname}): Skipping empty line at row {i + 1}.");
                continue;

            var values1 = new List<string>();
#pragma warning disable SYSLIB1045
#pragma warning disable S6444
            var pattern = new Regex("(?<=^|,)(\"(?:[^\"]|\"\")*\"|[^,]*)");
#pragma warning restore S6444
#pragma warning restore SYSLIB1045
            var matches = pattern.Matches(line);

            foreach (Match match in matches)
            {
                var value1 = match.Value;

                if (value1.StartsWith('"') && value1.EndsWith('"'))
                    value1 = value1.Substring(1, value1.Length - 2).Replace("\"\"", "\"");

                values1.Add(value1);
            }

            var values = values1.ToArray();

            var value2 = values[0];
            if (string.IsNullOrEmpty(value2) || string.IsNullOrWhiteSpace(value2))
                continue;

            var nname = false;
            var data = new Dictionary<string, string>(columnNames.Length);
            {
                for (var j = 0; j < columnNames.Length; j++)
                {
                    var value = j < values.Length ? values[j].Trim('"') : string.Empty;
                    data[columnNames[j]] = value;

                    if (columnNames[j] != "Name") continue;
                    if (!string.IsNullOrWhiteSpace(value)) continue;

                    nname = true;
                    break;
                }
            }

            if (nname)
                continue;

            var instanceId = insid++;
            csvData._elements.TryAdd(instanceId, new CsvElement(data.AsReadOnly(), tableId, instanceId));
        }

        return csvData;
    }

    public CsvElement GetElement(int id, bool byLineInFile = false)
    {
        if (map) throw new Exception("An attempt was made to incorrectly access a CsvData instance that is Map.");

        if (byLineInFile)
            id -= 3;

        if (_elements.TryGetValue(id, out var element))
            return element;

        throw new KeyNotFoundException($"Element with ID '{id}' not found.");
    }

    public CsvElement[] GetElements()
    {
        if (map) throw new Exception("An attempt was made to incorrectly access a CsvData instance that is Map.");

        return _elements.Values.ToArray();
    }

    public int GetTableId()
    {
        return tableId;
    }

    public string GetTableName()
    {
        return tableName;
    }
}