using ZOVserver.Shared.TitanRemnants.PAssets.Proto;

namespace ZOVserver.Shared.TitanRemnants.PAssets.Data;

public abstract class LogicData(CsvElement csvElement)
{
    public string Name { get; } = csvElement.GetStringValue("Name");

    public int GlobalId => csvElement.GetGlobalId();

    public int ClassId => csvElement.GetClassId();
    public int InstanceId => csvElement.GetInstanceId();
}