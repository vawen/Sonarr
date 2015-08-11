using System;
using System.Collections.Generic;
using System.Linq;
using NLog;
using NzbDrone.Common.Cache;
using NzbDrone.Core.DataAugmentation.Scene;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Tv;
using NzbDrone.Core.Tv.Events;
using NzbDrone.Common.Http;
using NzbDrone.Core.Parser;
using System.Xml.Linq;
using NzbDrone.Core.Profiles;

namespace NzbDrone.Core.DataAugmentation.TvdbLanguages
{
    public class TvdbLanguageService : ISceneMappingProvider, IHandle<SeriesUpdatedEvent>, IHandle<SeriesRefreshStartingEvent>, IHandle<ProfileModifiedEvent>
    {
        private readonly IEpisodeService _episodeService;
        private readonly ISeriesService _seriesService;
        private readonly IHttpClient _httpClient;
        private readonly Logger _logger;
        private readonly ICached<bool> _cache;
        private const string TVDB_BASE_URL = "http://www.thetvdb.com/api/1D62F2F90030C444/series/{show}/{language}.xml";
        
        private HttpRequestBuilder _tvdbRequestBuilder;


        public TvdbLanguageService(IEpisodeService episodeService,
                           ISeriesService seriesService, ICacheManager cacheManager, IHttpClient httpClient, Logger logger)
        {
            _episodeService = episodeService;
            _seriesService = seriesService;
            _logger = logger;
            _cache = cacheManager.GetCache<bool>(GetType());
            _tvdbRequestBuilder = new HttpRequestBuilder (TVDB_BASE_URL);
            _httpClient = httpClient;
        }

        public List<SceneMapping> GetSceneMappings()
        {
            var mappings = new List<SceneMapping>(); 

            var series = _seriesService.GetAllSeries().Where(s => s.Profile.Value.Language != Language.English).ToList();

            if (!series.Any())
                mappings.Add(new SceneMapping());

            foreach (var serie in series)
            {
                // Check in tvdb if language has name for that language
                try
                {
                    var httpRequest = _tvdbRequestBuilder.Build("");
                    httpRequest.AddSegment("show", serie.TvdbId.ToString());
                    httpRequest.AddSegment("language", TvdbLanguage.GetTvdbLanguage(serie.Profile.Value.Language).TvdbString);
                    var response = _httpClient.Get(httpRequest);
                    XDocument doc = XDocument.Parse(response.Content);
                    String name = doc.GetSeriesData("SeriesName");

                    if (!name.Equals(serie.Title))
                        mappings.Add(new SceneMapping { Title = name, SearchTerm = name, SeasonNumber = -1, TvdbId = serie.TvdbId });

                }
                catch (Exception e)
                {
                    _logger.Debug("Error retrieving serie information from tvdb: {0}", e.Message);
                }
            }

            return mappings.Where(m =>
            {
                int id;

                if (Int32.TryParse(m.Title, out id))
                {
                    _logger.Debug("Skipping all numeric name: {0} for {1}", m.Title, m.TvdbId);
                    return false;
                }

                return true;
            }).ToList();
        }

        private void RefreshCache()
        {
            var series = _seriesService.GetAllSeries().Where(s => s.Profile.Value.Language != Language.English).ToList();

            _cache.Clear();

            foreach (var serie in series)
            {
                // Check in tvdb if language has name for that language
                try
                {
                    var httpRequest = _tvdbRequestBuilder.Build("");
                    httpRequest.AddSegment("show", serie.TvdbId.ToString());
                    httpRequest.AddSegment("language", TvdbLanguage.GetTvdbLanguage(serie.Profile.Value.Language).TvdbString);
                    var response = _httpClient.Get(httpRequest);
                    XDocument doc = XDocument.Parse(response.Content);
                    String name = doc.GetSeriesData("SeriesName");

                    if (!name.Equals(serie.Title))
                        _cache.Set(serie.TvdbId.ToString(), true, TimeSpan.FromHours(1));

                } catch (Exception e)
                {
                    _logger.Debug("Error retrieving serie information from tvdb: {0}", e.Message);
                }
            }
            return;
        }

        public void Handle(SeriesUpdatedEvent message)
        {
            if (_cache.Count == 0)
            {
                RefreshCache();
            }

            if (_cache.Count == 0)
            {
                _logger.Debug("Scene numbering is not available");
                return;
            }

            if (!_cache.Find(message.Series.TvdbId.ToString()) && !message.Series.UseSceneNumbering)
            {
                _logger.Debug("Scene numbering is not available for {0} [{1}]", message.Series.Title, message.Series.TvdbId);
                return;
            }
        }

        public void Handle(SeriesRefreshStartingEvent message)
        {
            RefreshCache();
        }

        public void Handle(ProfileModifiedEvent message)
        {
            RefreshCache();
        }
    }
}
