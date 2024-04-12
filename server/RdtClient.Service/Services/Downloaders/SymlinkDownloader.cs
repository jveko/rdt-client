﻿using RdtClient.Service.Helpers;
using Serilog;

namespace RdtClient.Service.Services.Downloaders;

public class SymlinkDownloader(String uri, String destinationPath, String path) : IDownloader
{
    public event EventHandler<DownloadCompleteEventArgs>? DownloadComplete;
    public event EventHandler<DownloadProgressEventArgs>? DownloadProgress;

    private readonly CancellationTokenSource _cancellationToken = new();

    private readonly ILogger _logger = Log.ForContext<SymlinkDownloader>();

    private const Int32 MaxRetries = 10;

    public async Task<String> Download()
    {
        _logger.Debug($"Starting symlink resolving of {uri}, writing to path: {path}");

        try
        {
            var filePath = new FileInfo(path);

            var rcloneMountPath = Settings.Get.DownloadClient.RcloneMountPath.TrimEnd(['\\', '/']);
            var fileName = filePath.Name;
            var fileExtension = filePath.Extension;
            var fileNameWithoutExtension = fileName.Replace(fileExtension, "");
            var pathWithoutFileName = path.Replace(fileName, "").TrimEnd(['\\', '/']);
            var searchPath = Path.Combine(rcloneMountPath, pathWithoutFileName);
            var destFile = new FileInfo(destinationPath);
            var destDir = destFile.Directory!;

            List<String> unWantedExtensions =
            [
                "zip",
                "rar",
                "tar"
            ];

            if (unWantedExtensions.Any(m => fileExtension == m))
            {
                throw new($"Cant handle compressed files with symlink downloader");
            }

            DownloadProgress?.Invoke(this,
                                     new()
                                     {
                                         BytesDone = 0,
                                         BytesTotal = 0,
                                         Speed = 0
                                     });

            var potentialFilePaths = new List<String>();

            var directoryInfo = new DirectoryInfo(searchPath);

            while (directoryInfo.Parent != null)
            {
                potentialFilePaths.Add(directoryInfo.FullName);
                directoryInfo = directoryInfo.Parent;

                if (directoryInfo.FullName.TrimEnd(['\\', '/']) == rcloneMountPath)
                {
                    break;
                }
            }

            potentialFilePaths.Add(Path.Combine(rcloneMountPath, fileName));
            potentialFilePaths.Add(Path.Combine(rcloneMountPath, fileNameWithoutExtension));

            FileInfo? file = null;

            for (var retryCount = 0; retryCount < MaxRetries; retryCount++)
            {
                DownloadProgress?.Invoke(this,
                                         new()
                                         {
                                             BytesDone = retryCount,
                                             BytesTotal = 10,
                                             Speed = 1
                                         });

                _logger.Debug($"Searching {rcloneMountPath} for {fileName} (attempt #{retryCount})...");

                foreach (var potentialFilePath in potentialFilePaths)
                {
                    var potentialFilePathWithFileName = Path.Combine(potentialFilePath, fileNameWithoutExtension);

                    _logger.Debug($"Searching {potentialFilePathWithFileName}...");

                    if (Directory.Exists(potentialFilePathWithFileName))
                    {
                        file = new(potentialFilePathWithFileName);

                        break;
                    }
                }

                if (file == null)
                {
                    await Task.Delay(1000 * retryCount);
                }
                else
                {
                    break;
                }
            }

            if (file == null)
            {
                _logger.Debug($"Unable to find file in rclone mount. Folders available in {rcloneMountPath}: ");

                try
                {
                    var allFolders = FileHelper.GetDirectoryContents(rcloneMountPath);

                    _logger.Debug(allFolders);
                }
                catch (Exception ex)
                {
                    _logger.Error(ex.Message);
                }

                throw new("Could not find file from rclone mount!");
            }

            _logger.Debug($"Found {file.FullName} at {file.FullName}");

            var result = TryCreateSymbolicLink(file.FullName, destDir.FullName);

            if (!result)
            {
                throw new("Could not find file from rclone mount!");
            }

            DownloadComplete?.Invoke(this, new());

            return file.FullName;
        }
        catch (Exception ex)
        {
            DownloadComplete?.Invoke(this,
                                     new()
                                     {
                                         Error = ex.Message
                                     });

            throw;
        }
    }

    public Task Cancel()
    {
        _cancellationToken.Cancel(false);

        return Task.CompletedTask;
    }

    public Task Pause()
    {
        return Task.CompletedTask;
    }

    public Task Resume()
    {
        return Task.CompletedTask;
    }

    private Boolean TryCreateSymbolicLink(String sourcePath, String symlinkPath)
    {
        try
        {
            File.CreateSymbolicLink(symlinkPath, sourcePath);

            if (Directory.Exists(symlinkPath)) // Double-check that the link was created
            {
                _logger.Information($"Created symbolic link from {sourcePath} to {symlinkPath}");

                return true;
            }

            _logger.Error($"Failed to create symbolic link from {sourcePath} to {symlinkPath}");

            return false;
        }
        catch (Exception ex)
        {
            _logger.Error($"Error creating symbolic link from {sourcePath} to {symlinkPath}: {ex.Message}");

            return false;
        }
    }
}
