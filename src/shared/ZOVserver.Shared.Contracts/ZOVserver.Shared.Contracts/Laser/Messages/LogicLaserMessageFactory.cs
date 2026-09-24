using System.Collections.Frozen;
using System.Reflection;

namespace ZOVserver.Shared.Contracts.Laser.Messages;

public static class LogicLaserMessageFactory
{
    private static FrozenDictionary<int, Type> _messages;

    static LogicLaserMessageFactory()
    {
        try
        {
            _messages = Assembly.GetExecutingAssembly()
                .GetTypes()
                .Where(t => t.IsSubclassOf(typeof(PiranhaMessage)))
                .Where(t => t.GetConstructors().Any(c => c.GetParameters().Length == 0))
                .ToDictionary(t => (Activator.CreateInstance(t) as PiranhaMessage)!.GetMessageType())
                .Where(m => PiranhaMessage.IsClientToServerMessage(m.Key))
                .ToFrozenDictionary();
        }
        catch (Exception exception)
        {
            Console.WriteLine(exception.ToString());

            var duplicates = Assembly.GetExecutingAssembly()
                .GetTypes()
                .Where(t => t.IsSubclassOf(typeof(PiranhaMessage)) && !t.IsAbstract)
                .Where(t => t.GetConstructors().Any(c => c.GetParameters().Length == 0))
                .Select(t => new { Type = t, Id = (Activator.CreateInstance(t) as PiranhaMessage)!.GetMessageType() })
                .GroupBy(x => x.Id)
                .Where(g => g.Count() > 1);

            foreach (var group in duplicates)
                Console.WriteLine($"Duplicates: ID {group.Key}: " +
                                  string.Join(", ", group.Select(g => g.Type.Name)));

            throw;
        }
    }

    public static PiranhaMessage? CreateMessageByType(int type)
    {
        if (_messages.TryGetValue(type, out var message))
            return Activator.CreateInstance(message) as PiranhaMessage;
        return null;
    }

    public static void Destruct()
    {
        _messages = null!;
    }
}