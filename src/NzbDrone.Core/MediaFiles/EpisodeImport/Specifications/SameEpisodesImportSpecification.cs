using NLog;
using NzbDrone.Core.DecisionEngine;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.MediaFiles.EpisodeImport.Specifications
{
    public class SameEpisodesImportSpecification : IImportDecisionEngineSpecification
    {
        private readonly SameEpisodesSpecification _sameEpisodesSpecification;
        private readonly Logger _logger;

        public SameEpisodesImportSpecification(SameEpisodesSpecification sameEpisodesSpecification, Logger logger)
        {
            _sameEpisodesSpecification = sameEpisodesSpecification;
            _logger = logger;
        }

        public RejectionType Type { get { return RejectionType.Permanent; } }

        public Decision IsSatisfiedBy(LocalEpisode localEpisode)
        {
            if (localEpisode.ParsedEpisodeInfo == null)
            {
                return Decision.Accept();
            }

            if (_sameEpisodesSpecification.IsSatisfiedBy(localEpisode.Episodes))
            {
                return Decision.Accept();
            }

            _logger.Debug("Episode file on disk contains more episodes than this file contains");
            return Decision.Reject("Episode file on disk contains more episodes than this file contains");
        }
    }
}
