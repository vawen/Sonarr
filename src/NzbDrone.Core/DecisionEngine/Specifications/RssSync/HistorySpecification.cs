using System.Linq;
using NLog;
using NzbDrone.Core.Download;
using NzbDrone.Core.Download.Clients.Sabnzbd;
using NzbDrone.Core.History;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.DecisionEngine.Specifications.RssSync
{
    public class HistorySpecification : IDecisionEngineSpecification
    {
        private readonly IHistoryService _historyService;
        private readonly UpgradableSpecification _upgradableSpecification;
        private readonly IProvideDownloadClient _downloadClientProvider;
        private readonly Logger _logger;

        public HistorySpecification(IHistoryService historyService,
                                           UpgradableSpecification UpgradableSpecification,
                                           IProvideDownloadClient downloadClientProvider,
                                           Logger logger)
        {
            _historyService = historyService;
            _upgradableSpecification = UpgradableSpecification;
            _downloadClientProvider = downloadClientProvider;
            _logger = logger;
        }

        public RejectionType Type { get { return RejectionType.Permanent; } }

        public virtual Decision IsSatisfiedBy(RemoteEpisode subject, SearchCriteriaBase searchCriteria)
        {
            if (searchCriteria != null)
            {
                _logger.Debug("Skipping history check during search");
                return Decision.Accept();
            }

            var downloadClients = _downloadClientProvider.GetDownloadClients();

            foreach (var downloadClient in downloadClients.OfType<Sabnzbd>())
            {
                _logger.Debug("Performing history status check on report");
                foreach (var episode in subject.Episodes)
                {
                    _logger.Debug("Checking current status of episode [{0}] in history", episode.Id);
                    var mostRecent = _historyService.MostRecentForEpisode(episode.Id);

                    if (mostRecent != null && mostRecent.EventType == HistoryEventType.Grabbed)
                    {
                        _logger.Debug("Latest history item is downloading, rejecting.");
                        return Decision.Reject("Download has not been imported yet");
                    }
                }
                return Decision.Accept();
            }

            foreach (var episode in subject.Episodes)
            {
                var bestInHistory = _historyService.GetBestInHistory(subject.Series.Profile, episode.Id);
                if (bestInHistory != null)
                {
                    _logger.Debug("Comparing history quality with report. History is {0} - {1}", bestInHistory.Quality, bestInHistory.Language);

                    if (!_upgradableSpecification.IsUpgradable(subject.Series.Profile, bestInHistory.Quality, bestInHistory.Language, subject.ParsedEpisodeInfo.Quality, subject.ParsedEpisodeInfo.Language))
                    {
                        return Decision.Reject("Existing file in history is of equal or higher quality: {0} - {1}", bestInHistory.Quality, bestInHistory.Language);
                    }
                }
            }

            return Decision.Accept();
        }
    }
}
