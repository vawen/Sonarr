using System.Linq;
using NLog;
using NzbDrone.Core.Download.Pending;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Profiles.Delay;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Languages;

namespace NzbDrone.Core.DecisionEngine.Specifications.RssSync
{
    public class DelaySpecification : IDecisionEngineSpecification
    {
        private readonly IPendingReleaseService _pendingReleaseService;
        private readonly IQualityUpgradableSpecification _qualityUpgradableSpecification;
        private readonly IDelayProfileService _delayProfileService;
        private readonly Logger _logger;

        public DelaySpecification(IPendingReleaseService pendingReleaseService,
                                  IQualityUpgradableSpecification qualityUpgradableSpecification,
                                  IDelayProfileService delayProfileService,
                                  Logger logger)
        {
            _pendingReleaseService = pendingReleaseService;
            _qualityUpgradableSpecification = qualityUpgradableSpecification;
            _delayProfileService = delayProfileService;
            _logger = logger;
        }

        public RejectionType Type { get { return RejectionType.Temporary; } }

        public virtual Decision IsSatisfiedBy(RemoteEpisode subject, SearchCriteriaBase searchCriteria)
        {
            //How do we want to handle drone being off and the automatic search being triggered?
            //TODO: Add a flag to the search to state it is a "scheduled" search

            if (searchCriteria != null)
            {
                _logger.Debug("Ignore delay for searches");
                return Decision.Accept();
            }

            var profile = subject.Series.Profile.Value;
            var delayProfile = _delayProfileService.BestForTags(subject.Series.Tags);
            var delay = delayProfile.GetProtocolDelay(subject.Release.DownloadProtocol);
            var isPreferredProtocol = subject.Release.DownloadProtocol == delayProfile.PreferredProtocol;

            if (delay == 0)
            {
                _logger.Debug("Profile does not require a waiting period before download for {0}.", subject.Release.DownloadProtocol);
                return Decision.Accept();
            }

            var comparer = new QualityModelComparer(profile);
            var comparerLanguage = new LanguageComparer(profile);

            if (isPreferredProtocol)
            {
                foreach (var file in subject.Episodes.Where(c => c.EpisodeFileId != 0).Select(c => c.EpisodeFile.Value))
                {
                    var upgradable = _qualityUpgradableSpecification.IsUpgradable(profile, file.Quality, file.Language, subject.ParsedEpisodeInfo.Quality, subject.ParsedEpisodeInfo.Language);

                    if (upgradable)
                    {
                        _logger.Debug("New quality is a better revision for existing quality, skipping delay");
                        return Decision.Accept();
                    }
                }
            }

            //If quality meets or exceeds the best allowed quality in the profile accept it immediately
            var bestQualityInProfile = new QualityModel(profile.LastAllowedQuality());
            var isBestInProfile = comparer.Compare(subject.ParsedEpisodeInfo.Quality, bestQualityInProfile) >= 0;
            var isBestInProfileLanguage = comparerLanguage.Compare(subject.ParsedEpisodeInfo.Language, profile.LastAllowedLanguage()) >= 0;

            if (isBestInProfile && (isBestInProfileLanguage || !profile.LanguageOverQuality) && isPreferredProtocol)
            {
                _logger.Debug("Quality is highest in profile for preferred protocol, will not delay");
                return Decision.Accept();
            }

            var episodeIds = subject.Episodes.Select(e => e.Id);

            var oldest = _pendingReleaseService.OldestPendingRelease(subject.Series.Id, episodeIds);

            if (oldest != null && oldest.Release.AgeMinutes > delay)
            {
                return Decision.Accept();
            }

            if (subject.Release.AgeMinutes < delay)
            {
                _logger.Debug("Waiting for better quality release, There is a {0} minute delay on {1}", delay, subject.Release.DownloadProtocol);
                return Decision.Reject("Waiting for better quality release");
            }

            return Decision.Accept();
        }
    }
}
