using System.Text;
using Google.Protobuf;
using Grpc.Core;
using ZLinq;
using ZOVserver.Shared.Contracts.Proto;

namespace ZOVserver.DistFiles.FileServer.Services;

public class FileServerGrpcService(string rootPath = "Files") : FileServerService.FileServerServiceBase
{
    private readonly string _rootPath = Path.GetFullPath(rootPath);

    public override async Task<UploadResponse> UploadFile(UploadRequest request, ServerCallContext context)
    {
        var fullPath = Path.Combine(_rootPath, request.TargetPath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

        await File.WriteAllBytesAsync(fullPath, request.ChunkData.ToByteArray(), context.CancellationToken);

        return new UploadResponse
        {
            Success = true,
            SavedPath = request.TargetPath,
            FileSize = request.ChunkData.Length
        };
    }

    public override async Task<UploadResponse> UpdateFile(UploadRequest request, ServerCallContext context)
    {
        var fullPath = Path.Combine(_rootPath, request.TargetPath);
        if (!File.Exists(fullPath))
            throw new RpcException(new Status(StatusCode.NotFound, "File not found"));

        await File.WriteAllBytesAsync(fullPath, request.ChunkData.ToByteArray(), context.CancellationToken);

        return new UploadResponse
        {
            Success = true,
            SavedPath = request.TargetPath,
            FileSize = request.ChunkData.Length
        };
    }

    public override Task<DeleteResponse> DeleteFile(FileRequest request, ServerCallContext context)
    {
        var fullPath = Path.Combine(_rootPath, request.Path);
        if (!File.Exists(fullPath))
            return Task.FromResult(new DeleteResponse { Success = false, Message = "File not found" });

        File.Delete(fullPath);

        var directory = Path.GetDirectoryName(fullPath);

        while (directory != null && directory.StartsWith(_rootPath)
                                 && directory != _rootPath
                                 && Directory.Exists(directory))
            try
            {
                if (!Directory.EnumerateFileSystemEntries(directory).Any())
                {
                    Directory.Delete(directory);
                    directory = Path.GetDirectoryName(directory);
                }
                else
                {
                    break;
                }
            }
            catch
            {
                break;
            }

        return Task.FromResult(new DeleteResponse { Success = true, Message = "File deleted" });
    }

    private static async Task<ByteString> GetFileByteString(string filePath, bool asText)
    {
        byte[] fileBytes;
        {
            if (asText)
                fileBytes = Encoding.UTF8.GetBytes(await File.ReadAllTextAsync(filePath));
            else
                fileBytes = await File.ReadAllBytesAsync(filePath);
        }

        return ByteString.CopyFrom(fileBytes);
    }

    public override async Task<FileResponse> GetFile(FileRequest request, ServerCallContext context)
    {
        var filePath = Path.Combine(_rootPath, request.Path);
        if (!File.Exists(filePath))
            throw new RpcException(new Status(StatusCode.NotFound, "File not found"));

        return new FileResponse
        {
            Data = await GetFileByteString(filePath, !request.ReadFileAsBytes),
            Filename = Path.GetFileName(filePath)
        };
    }

    public override async Task<AllFilesResponse> GetAllFiles(FileFilter request, ServerCallContext context)
    {
        var response = new AllFilesResponse();

        var files = Directory
            .GetFiles(_rootPath, "*.*", request.IncludeSubfolders
                ? SearchOption.AllDirectories
                : SearchOption.TopDirectoryOnly)
            .Where(f => request.Extensions.Count == 0 || request.Extensions.Contains(Path.GetExtension(f).ToLower()));

        foreach (var filePath in files)
            response.Files.Add(new FileEntry
            {
                Path = Path.GetRelativePath(_rootPath, filePath),
                Data = await GetFileByteString(filePath, !request.ReadFilesAsBytes)
            });

        return response;
    }

    public override Task<FileListResponse> ListFiles(FileFilter request, ServerCallContext context)
    {
        var response = new FileListResponse();
        var searchOption = request.IncludeSubfolders ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

        var files = Directory
            .GetFiles(_rootPath, "*.*", searchOption)
            .AsValueEnumerable()
            .Where(f => request.Extensions.Count == 0 || request.Extensions.Contains(Path.GetExtension(f).ToLower()))
            .Select(f => new FileInfo(f))
            .Select(f => new FileCInfo
            {
                Path = Path.GetRelativePath(_rootPath, f.FullName),
                Extension = f.Extension,
                SizeBytes = f.Length,
                LastModified = f.LastWriteTimeUtc.ToString("o")
            }).ToArray();

        response.Files.AddRange(files);
        return Task.FromResult(response);
    }
}