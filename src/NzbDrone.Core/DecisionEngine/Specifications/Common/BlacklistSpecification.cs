using NLog;
using NzbDrone.Core.Blacklisting;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.DecisionEngine.Specifications.Common
{
    public class BlacklistSpecification : IDecisionEngineSpecification
    {
        private readonly IBlacklistService _blacklistService;
        private readonly Logger _logger;

        public BlacklistSpecification(IBlacklistService blacklistService, Logger logger)
        {
            _blacklistService = blacklistService;
            _logger = logger;
        }

        public RejectionType Type { get { return RejectionType.Permanent; } }

        public Decision IsSatisfiedBy(RemoteItem subject, SearchCriteriaBase searchCriteria)
        {          
            if (_blacklistService.Blacklisted(subject.Media, subject.Release))
            {
                _logger.Debug("{0} is blacklisted, rejecting.", subject.Release.Title);
                return Decision.Reject("Release is blacklisted");
            }

            return Decision.Accept();
        }
    }
}