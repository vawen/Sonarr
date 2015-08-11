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
using System.Xml.Linq;
using NzbDrone.Core.Profiles;
using NzbDrone.Core.Languages;

namespace NzbDrone.Core.DataAugmentation.TvdbLanguages
{
    public class TvdbLanguageService : ISceneMappingProvider
    {
        private readonly ISeriesService _seriesService;
        private readonly IHttpClient _httpClient;
        private readonly Logger _logger;
        private const string TVDB_BASE_URL = "http://www.thetvdb.com/api/1D62F2F90030C444/series/{show}/{language}.xml";
        
        private HttpRequestBuilder _tvdbRequestBuilder;


        public TvdbLanguageService(ISeriesService seriesService, IHttpClient httpClient, Logger logger)
        {
            _seriesService = seriesService;
            _logger = logger;
            _tvdbRequestBuilder = new HttpRequestBuilder (TVDB_BASE_URL);
            _httpClient = httpClient;
        }

        public List<SceneMapping> GetSceneMappings()
        {
            var mappings = new List<SceneMapping>();

            var series = _seriesService.GetAllSeries().Where(s => s.Profile.Value.Languages.Any(l => l.Allowed && l.Language != Language.English)).ToList();

            if (!series.Any())
                mappings.Add(new SceneMapping());

            foreach (var serie in series)
            {
                foreach (var language in serie.Profile.Value.Languages)
                {
                    if (!language.Allowed || TvdbLanguage.GetTvdbLanguage(language.Language).Language == Language.English)
                        continue;
                    try
                    {
                        var httpRequest = _tvdbRequestBuilder.Build("");
                        httpRequest.AddSegment("show", serie.TvdbId.ToString());
                        httpRequest.AddSegment("language", TvdbLanguage.GetTvdbLanguage(language.Language).TvdbString);
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
    }
}
