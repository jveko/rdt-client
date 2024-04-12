﻿using Microsoft.EntityFrameworkCore;
using Download = RdtClient.Data.Models.Data.Download;

namespace RdtClient.Data.Data;

public class DownloadData(DataContext dataContext)
{
    public async Task<List<Download>> GetForTorrent(Guid torrentId)
    {
        return await dataContext.Downloads
                                .AsNoTracking()
                                .Where(m => m.TorrentId == torrentId)
                                .ToListAsync();
    }

    public async Task<Download?> GetById(Guid downloadId)
    {
        return await dataContext.Downloads
                                .Include(m => m.Torrent)
                                .AsNoTracking()
                                .FirstOrDefaultAsync(m => m.DownloadId == downloadId);
    }

    public async Task<Download?> Get(Guid torrentId, String path)
    {
        return await dataContext.Downloads
                                .Include(m => m.Torrent)
                                .AsNoTracking()
                                .FirstOrDefaultAsync(m => m.TorrentId == torrentId && m.Path == path);
    }

    public async Task<Download> Add(Guid torrentId, String path)
    {
        var download = new Download
        {
            DownloadId = Guid.NewGuid(),
            TorrentId = torrentId,
            Path = path,
            Added = DateTimeOffset.UtcNow,
            DownloadQueued = DateTimeOffset.UtcNow,
            RetryCount = 0
        };

        await dataContext.Downloads.AddAsync(download);

        await dataContext.SaveChangesAsync();

        await TorrentData.VoidCache();

        return download;
    }

    public async Task UpdateUnrestrictedLink(Guid downloadId, String unrestrictedLink)
    {
        var dbDownload = await dataContext.Downloads
                                          .FirstOrDefaultAsync(m => m.DownloadId == downloadId);

        if (dbDownload == null)
        {
            return;
        }

        dbDownload.Link = unrestrictedLink;

        await dataContext.SaveChangesAsync();

        await TorrentData.VoidCache();
    }

    public async Task UpdateDownloadStarted(Guid downloadId, DateTimeOffset? dateTime)
    {
        var dbDownload = await dataContext.Downloads
                                          .FirstOrDefaultAsync(m => m.DownloadId == downloadId);

        if (dbDownload == null)
        {
            return;
        }

        dbDownload.DownloadStarted = dateTime;

        await dataContext.SaveChangesAsync();

        await TorrentData.VoidCache();
    }

    public async Task UpdateDownloadFinished(Guid downloadId, DateTimeOffset? dateTime)
    {
        var dbDownload = await dataContext.Downloads
                                          .FirstOrDefaultAsync(m => m.DownloadId == downloadId);

        if (dbDownload == null)
        {
            return;
        }

        dbDownload.DownloadFinished = dateTime;

        await dataContext.SaveChangesAsync();

        await TorrentData.VoidCache();
    }

    public async Task UpdateUnpackingQueued(Guid downloadId, DateTimeOffset? dateTime)
    {
        var dbDownload = await dataContext.Downloads
                                          .FirstOrDefaultAsync(m => m.DownloadId == downloadId);

        if (dbDownload == null)
        {
            return;
        }

        dbDownload.UnpackingQueued = dateTime;

        await dataContext.SaveChangesAsync();

        await TorrentData.VoidCache();
    }

    public async Task UpdateUnpackingStarted(Guid downloadId, DateTimeOffset? dateTime)
    {
        var dbDownload = await dataContext.Downloads
                                          .FirstOrDefaultAsync(m => m.DownloadId == downloadId);

        if (dbDownload == null)
        {
            return;
        }

        dbDownload.UnpackingStarted = dateTime;

        await dataContext.SaveChangesAsync();

        await TorrentData.VoidCache();
    }

    public async Task UpdateUnpackingFinished(Guid downloadId, DateTimeOffset? dateTime)
    {
        var dbDownload = await dataContext.Downloads
                                          .FirstOrDefaultAsync(m => m.DownloadId == downloadId);

        if (dbDownload == null)
        {
            return;
        }

        dbDownload.UnpackingFinished = dateTime;

        await dataContext.SaveChangesAsync();

        await TorrentData.VoidCache();
    }

    public async Task UpdateCompleted(Guid downloadId, DateTimeOffset? dateTime)
    {
        var dbDownload = await dataContext.Downloads
                                          .FirstOrDefaultAsync(m => m.DownloadId == downloadId);

        if (dbDownload == null)
        {
            return;
        }

        dbDownload.Completed = dateTime;

        await dataContext.SaveChangesAsync();

        await TorrentData.VoidCache();
    }

    public async Task UpdateError(Guid downloadId, String? error)
    {
        var dbDownload = await dataContext.Downloads
                                          .FirstOrDefaultAsync(m => m.DownloadId == downloadId);

        if (dbDownload == null)
        {
            return;
        }

        dbDownload.Error = error;

        await dataContext.SaveChangesAsync();

        await TorrentData.VoidCache();
    }

    public async Task UpdateRetryCount(Guid downloadId, Int32 retryCount)
    {
        var dbDownload = await dataContext.Downloads
                                          .FirstOrDefaultAsync(m => m.DownloadId == downloadId);

        if (dbDownload == null)
        {
            return;
        }

        dbDownload.RetryCount = retryCount;

        await dataContext.SaveChangesAsync();

        await TorrentData.VoidCache();
    }

    public async Task UpdateErrorInRange(Dictionary<Guid, String> errorIdRange)
    {
        foreach (var entry in errorIdRange)
        {
            var dbDownload = await dataContext.Downloads.FirstOrDefaultAsync(m => m.DownloadId == entry.Key);

            if (dbDownload == null)
            {
                continue;
            }

            dbDownload.Error = entry.Value;
        }

        await dataContext.SaveChangesAsync();
    }

    public async Task UpdateRemoteId(Guid downloadId, String remoteId)
    {
        await UpdateRemoteIdRange(new Dictionary<Guid, String>
        {
            {
                downloadId, remoteId
            }
        });
    }

    public async Task UpdateRemoteIdRange(Dictionary<Guid, String> remoteIdRange)
    {
        foreach (var entry in remoteIdRange)
        {
            var dbDownload = await dataContext.Downloads.FirstOrDefaultAsync(m => m.DownloadId == entry.Key);

            if (dbDownload == null)
            {
                continue;
            }

            dbDownload.RemoteId = entry.Value;
        }

        await dataContext.SaveChangesAsync();
    }

    public async Task DeleteForTorrent(Guid torrentId)
    {
        var downloads = await dataContext.Downloads
                                         .Where(m => m.TorrentId == torrentId)
                                         .ToListAsync();

        dataContext.Downloads.RemoveRange(downloads);

        await dataContext.SaveChangesAsync();

        await TorrentData.VoidCache();
    }

    public async Task Reset(Guid downloadId)
    {
        var dbDownload = await dataContext.Downloads
                                          .FirstOrDefaultAsync(m => m.DownloadId == downloadId)
                         ?? throw new($"Cannot find download with ID {downloadId}");

        dbDownload.RetryCount = 0;
        dbDownload.Link = null;
        dbDownload.Added = DateTimeOffset.UtcNow;
        dbDownload.DownloadQueued = DateTimeOffset.UtcNow;
        dbDownload.DownloadStarted = null;
        dbDownload.DownloadFinished = null;
        dbDownload.UnpackingQueued = null;
        dbDownload.UnpackingStarted = null;
        dbDownload.UnpackingFinished = null;
        dbDownload.Completed = null;
        dbDownload.Error = null;

        await dataContext.SaveChangesAsync();

        await TorrentData.VoidCache();
    }
}
