using System;
using System.Collections.Generic;
using System.Linq;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Core.IndexerSearch.Definitions;
using TVDBSharp;
using TVDBSharp.Models;

namespace NzbDrone.Core.Indexers.Newpct
{
    public class NewpctRequestGenerator : IIndexerRequestGenerator
    {
        public NewpctSettings Settings { get; set; }
        private TVDB _tvdb;

        public NewpctRequestGenerator()
        {
            _tvdb = new TVDB("5D2D188E86E07F4F");
        }

        public virtual IList<IEnumerable<IndexerRequest>> GetRecentRequests()
        {
            var pageableRequests = new List<IEnumerable<IndexerRequest>>();

            pageableRequests.AddIfNotNull(GetPagedRequests());

            return pageableRequests;
        }

        public virtual IList<IEnumerable<IndexerRequest>> GetSearchRequests(SingleEpisodeSearchCriteria searchCriteria)
        {
            var pageableRequests = new List<IEnumerable<IndexerRequest>>();

            Show show = _tvdb.GetShow(searchCriteria.Series.TvdbId, "es");

            pageableRequests.AddIfNotNull(GetPagedRequests(PrepareQuery(show.Name),
                    String.Format("{0}{1:00}", searchCriteria.SeasonNumber, searchCriteria.EpisodeNumber)));

            return pageableRequests;
        }

        public virtual IList<IEnumerable<IndexerRequest>> GetSearchRequests(SeasonSearchCriteria searchCriteria)
        {
            var pageableRequests = new List<IEnumerable<IndexerRequest>>();

            Show show = _tvdb.GetShow(searchCriteria.Series.TvdbId, "es");

            pageableRequests.AddIfNotNull(GetPagedRequests(PrepareQuery(show.Name),
                    String.Format("{0}", searchCriteria.SeasonNumber)));


            return pageableRequests;
        }

        public virtual IList<IEnumerable<IndexerRequest>> GetSearchRequests(DailyEpisodeSearchCriteria searchCriteria)
        {
            var pageableRequests = new List<IEnumerable<IndexerRequest>>();

/*            foreach (var queryTitle in searchCriteria.QueryTitles)
            {
                pageableRequests.AddIfNotNull(GetPagedRequests(MaxPages, "usearch",
                    PrepareQuery(queryTitle),
                    String.Format("{0:yyyy-MM-dd}", searchCriteria.AirDate),
                    "category:tv"));
            }*/

            return pageableRequests;
        }

        public virtual IList<IEnumerable<IndexerRequest>> GetSearchRequests(AnimeEpisodeSearchCriteria searchCriteria)
        {
            return new List<IEnumerable<IndexerRequest>>();
        }

        public virtual IList<IEnumerable<IndexerRequest>> GetSearchRequests(SpecialEpisodeSearchCriteria searchCriteria)
        {
            var pageableRequests = new List<IEnumerable<IndexerRequest>>();

            return pageableRequests;
        }

        private IEnumerable<IndexerRequest> GetPagedRequests(params String[] searchParameters)
        {
            String searchUrl = "Helix";

            if (searchParameters.Any())
            {
                searchUrl = String.Format("{0}", String.Join("+", searchParameters).Trim());
            }
        

            //var request = new IndexerRequest(String.Format("{0}/feed"/*{1}{2}/?rss=1&field=time_add&sorder=desc"*/, Settings.BaseUrl.TrimEnd('/')/*, rssType, searchUrl*/), HttpAccept.Rss);
            var request = new IndexerRequest(String.Format("{0}{1}{2}", Settings.BaseUrl.TrimEnd('/'), Settings.SearchUrl,searchUrl), HttpAccept.Html);
            request.HttpRequest.SuppressHttpError = true;

            yield return request;
        }

        private String PrepareQuery(String query)
        {
            return SearchCriteriaBase.GetQueryTitle(query);
        }
    }
}
