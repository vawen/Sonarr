﻿using System.Collections.Generic;
using System.Linq;
using Marr.Data.QGen;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Tv;
using NzbDrone.Core.Languages;

namespace NzbDrone.Core.History
{
    public interface IHistoryRepository : IBasicRepository<History>
    {
        List<BestInHistory> GetBestInHistory(int episodeId);
        History MostRecentForEpisode(int episodeId);
        History MostRecentForDownloadId(string downloadId);
        List<History> FindByDownloadId(string downloadId);
        List<History> FindDownloadHistory(int idSeriesId, QualityModel quality);
    }

    public class HistoryRepository : BasicRepository<History>, IHistoryRepository
    {

        public HistoryRepository(IMainDatabase database, IEventAggregator eventAggregator)
            : base(database, eventAggregator)
        {
        }


        public List<BestInHistory> GetBestInHistory(int episodeId)
        {
            var history = Query.Where(c => (c.EpisodeId == episodeId && c.EventType != HistoryEventType.EpisodeFileDeleted && c.EventType != HistoryEventType.DownloadFailed));
            return history.Select(h => new BestInHistory { Language = h.Language, Quality = h.Quality }).ToList();
        }

        public History MostRecentForEpisode(int episodeId)
        {
            return Query.Where(h => h.EpisodeId == episodeId)
                        .OrderByDescending(h => h.Date)
                        .FirstOrDefault();
        }

        public History MostRecentForDownloadId(string downloadId)
        {
            return Query.Where(h => h.DownloadId == downloadId)
             .OrderByDescending(h => h.Date)
             .FirstOrDefault();
        }

        public List<History> FindByDownloadId(string downloadId)
        {
            return Query.Where(h => h.DownloadId == downloadId);
        }

        public List<History> FindDownloadHistory(int idSeriesId, QualityModel quality)
        {
            return Query.Where(h =>
                 h.SeriesId == idSeriesId &&
                 h.Quality == quality &&
                 (h.EventType == HistoryEventType.Grabbed ||
                 h.EventType == HistoryEventType.DownloadFailed ||
                 h.EventType == HistoryEventType.DownloadFolderImported)
                 ).ToList();
        }

        protected override SortBuilder<History> GetPagedQuery(QueryBuilder<History> query, PagingSpec<History> pagingSpec)
        {
            var baseQuery = query.Join<History, Series>(JoinType.Inner, h => h.Series, (h, s) => h.SeriesId == s.Id)
                                 .Join<History, Episode>(JoinType.Inner, h => h.Episode, (h, e) => h.EpisodeId == e.Id);

            return base.GetPagedQuery(baseQuery, pagingSpec);
        }
    }
}