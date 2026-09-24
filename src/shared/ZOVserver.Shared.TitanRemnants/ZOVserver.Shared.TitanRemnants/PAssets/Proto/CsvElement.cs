using System.Collections.ObjectModel;

namespace ZOVserver.Shared.TitanRemnants.PAssets.Proto;

public class CsvElement(ReadOnlyDictionary<string, string> data, int classId, int instanceId)
{
    public int GetClassId()
    {
        return classId;
    }

    public int GetInstanceId()
    {
        return instanceId;
    }

    public int GetGlobalId()
    {
        return 1_000_000 * classId + instanceId;
    }

    private T GetValue<T>(string columnName)
    {
        if (!data.TryGetValue(columnName, out var value))
            throw new ArgumentException($"Column '{columnName}' not found.");

        var type = typeof(T);
        value = value.Trim();

        if (type == typeof(string))
            return (T)(object)value;

        if (type == typeof(bool))
            return (T)(object)value.Equals("true", StringComparison.CurrentCultureIgnoreCase);

        if (type == typeof(int))
        {
            if (string.IsNullOrEmpty(value) || string.IsNullOrWhiteSpace(value))
                return (T)(object)0;

            return (T)(object)int.Parse(value);
        }

        if (type == typeof(float))
        {
            if (string.IsNullOrEmpty(value) || string.IsNullOrWhiteSpace(value))
                return (T)(object)0f;

            return (T)(object)float.Parse(value);
        }

        throw new NotSupportedException($"Type '{type}' is not supported.");
    }

    public string GetStringValue(string columnName)
    {
        return GetValue<string>(columnName);
    }

    public bool GetBoolValue(string columnName)
    {
        return GetValue<bool>(columnName);
    }

    public int GetIntValue(string columnName)
    {
        return GetValue<int>(columnName);
    }

    public float GetFloatValue(string columnName)
    {
        return GetValue<float>(columnName);
    }
}