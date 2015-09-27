using System;
using System.Linq;
using NLog;
using NzbDrone.Common.Cache;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.History;
using NzbDrone.Core.Parser;

namespace NzbDrone.Core.Download.TrackedDownloads
{
    public interface ITrackedDownloadService
    {
        TrackedDownload Find(string downloadId);
        TrackedDownload TrackDownload(DownloadClientDefinition downloadClient, DownloadClientItem downloadItem);
    }

    public class TrackedDownloadService : ITrackedDownloadService
    {
        private readonly IParsingService _parsingService;
        private readonly IHistoryService _historyService;
        private readonly IParseProvider _parseProvider;
        private readonly Logger _logger;
        private readonly ICached<TrackedDownload> _cache;

        public TrackedDownloadService(IParsingService parsingService,
            ICacheManager cacheManager,
            IHistoryService historyService,
            IParseProvider parseProvider,
            Logger logger)
        {
            _parsingService = parsingService;
            _historyService = historyService;
            _parseProvider = parseProvider;
            _cache = cacheManager.GetCache<TrackedDownload>(GetType());
            _logger = logger;
        }

        public TrackedDownload Find(string downloadId)
        {
            return _cache.Find(downloadId);
        }

        public TrackedDownload TrackDownload(DownloadClientDefinition downloadClient, DownloadClientItem downloadItem)
        {
            var existingItem = Find(downloadItem.DownloadId);

            if (existingItem != null && existingItem.State != TrackedDownloadStage.Downloading)
            {
                existingItem.DownloadItem = downloadItem;
                return existingItem;
            }

            var trackedDownload = new TrackedDownload
            {
                DownloadClient = downloadClient.Id,
                DownloadItem = downloadItem,
                Protocol = downloadClient.Protocol
            };

            try
            {
                var parsedEpisodeInfo = _parseProvider.ParseTitle(trackedDownload.DownloadItem.Title);
                var historyItems = _historyService.FindByDownloadId(downloadItem.DownloadId);

                if (parsedEpisodeInfo != null)
                {
                    trackedDownload.RemoteEpisode = _parsingService.Map(parsedEpisodeInfo);
                }

                if (historyItems.Any())
                {
                    var firstHistoryItem = historyItems.OrderByDescending(h => h.Date).First();
                    trackedDownload.State = GetStateFromHistory(firstHistoryItem.EventType);

                    if (parsedEpisodeInfo == null ||
                        trackedDownload.RemoteEpisode == null ||
                        trackedDownload.RemoteEpisode.Series == null ||
                        trackedDownload.RemoteEpisode.Episodes.Empty())
                    {
                        parsedEpisodeInfo = _parseProvider.ParseTitle(firstHistoryItem.SourceTitle);
                        if (parsedEpisodeInfo != null)
                        {
                            trackedDownload.RemoteEpisode = _parsingService.Map(parsedEpisodeInfo, firstHistoryItem.SeriesId, historyItems.Where(v => v.EventType == HistoryEventType.Grabbed).Select(h => h.EpisodeId).Distinct());
                        }
                    }
                }

                if (trackedDownload.RemoteEpisode == null)
                {
                    return null;
                }
            }
            catch (Exception e)
            {
                _logger.DebugException("Failed to find episode for " + downloadItem.Title, e);
                return null;
            }

            _cache.Set(trackedDownload.DownloadItem.DownloadId, trackedDownload);
            return trackedDownload;
        }

        private static TrackedDownloadStage GetStateFromHistory(HistoryEventType eventType)
        {
            switch (eventType)
            {
                case HistoryEventType.DownloadFolderImported:
                    return TrackedDownloadStage.Imported;
                case HistoryEventType.DownloadFailed:
                    return TrackedDownloadStage.DownloadFailed;
                default:
                    return TrackedDownloadStage.Downloading;
            }
        }
    }
}
