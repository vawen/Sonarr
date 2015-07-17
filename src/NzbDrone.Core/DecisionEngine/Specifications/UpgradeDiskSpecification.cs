using System.Linq;
using NLog;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.DecisionEngine.Specifications
{
    public class UpgradeDiskSpecification : IDecisionEngineSpecification
    {
        private readonly QualityUpgradableSpecification _qualityUpgradableSpecification;
        private readonly Logger _logger;

        public UpgradeDiskSpecification(QualityUpgradableSpecification qualityUpgradableSpecification, Logger logger)
        {
            _qualityUpgradableSpecification = qualityUpgradableSpecification;
            _logger = logger;
        }

        public RejectionType Type { get { return RejectionType.Permanent; } }

        public virtual Decision IsSatisfiedBy(RemoteEpisode subject, SearchCriteriaBase searchCriteria)
        {
            foreach (var file in subject.Episodes.Where(c => c.EpisodeFileId != 0).Select(c => c.EpisodeFile.Value))
            {
                _logger.Debug("Comparing file quality with report. Existing file is {0} - {1}", file.Quality, file.Language);

                if (!_qualityUpgradableSpecification.IsUpgradable(subject.Series.Profile, file.Quality, file.Language, subject.ParsedEpisodeInfo.Quality, subject.ParsedEpisodeInfo.Language))
                {
                    return Decision.Reject("Quality for existing file on disk is of equal or higher preference: {0} - {1}", file.Quality, file.Language);
                }
            }

            return Decision.Accept();
        }
    }
}
