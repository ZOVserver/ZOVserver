using System.Text;
using YamlDotNet.Serialization;

namespace ZOVserver.Services.Global.GameGlobalEventsService.FileProvider;

public class ThreadSafeFileProvider<T> : IDisposable where T : class, new()
{
    private readonly IDeserializer _deserializer = new DeserializerBuilder().Build();

    private readonly string _filePath;
    private readonly FileSystemWatcher _fileWatcher;
    private readonly ReaderWriterLockSlim _lock = new();

    private readonly ISerializer _serializer = new SerializerBuilder().Build();

    private T _cachedData;
    private bool _isDisposed;

    public ThreadSafeFileProvider(string filePath)
    {
        _filePath = filePath;
        _cachedData = LoadData();

        _fileWatcher = new FileSystemWatcher
        {
            Path = Path.GetDirectoryName(filePath)!,
            Filter = Path.GetFileName(filePath),
            NotifyFilter = NotifyFilters.LastWrite
        };

        _fileWatcher.Changed += OnFileChanged;
        _fileWatcher.EnableRaisingEvents = true;
    }

    public void Dispose()
    {
        if (_isDisposed) return;

        _fileWatcher.Dispose();
        _lock.Dispose();

        _isDisposed = true;

        GC.SuppressFinalize(this);
    }

    public T GetData()
    {
        _lock.EnterReadLock();

        try
        {
            return _cachedData;
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public void SaveData(T data)
    {
        _lock.EnterWriteLock();

        try
        {
            var yaml = _serializer.Serialize(data);
            File.WriteAllText(_filePath, yaml, Encoding.UTF8);
            _cachedData = data;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    private T LoadData()
    {
        var yaml = File.ReadAllText(_filePath, Encoding.UTF8);
        return _deserializer.Deserialize<T>(yaml);
    }

    private void OnFileChanged(object sender, FileSystemEventArgs e)
    {
        if (e.ChangeType != WatcherChangeTypes.Changed) return;

        _lock.EnterWriteLock();

        try
        {
            Thread.Sleep(100);

            _cachedData = LoadData();
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }
}