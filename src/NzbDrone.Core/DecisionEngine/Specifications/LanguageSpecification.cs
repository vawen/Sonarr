using NLog;
using System.Linq;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.DecisionEngine.Specifications
{
    public class LanguageSpecification : IDecisionEngineSpecification
    {
        private readonly Logger _logger;

        public LanguageSpecification(Logger logger)
        {
            _logger = logger;
        }

        public RejectionType Type { get { return RejectionType.Permanent; } }

        public virtual Decision IsSatisfiedBy(RemoteEpisode subject, SearchCriteriaBase searchCriteria)
        {
            var wantedLanguage = subject.Series.Profile.Value.Languages;
            var _language = subject.ParsedEpisodeInfo.Language;

            _logger.Debug("Checking if report meets language requirements. {0}", subject.ParsedEpisodeInfo.Language);
        
            if (!wantedLanguage.Exists(v => v.Allowed && v.Language == _language))
            {
                _logger.Debug("Report Language: {0} rejected because it is not wanted in profile {1}", _language, subject.Series.Profile.Value.Name);
                return Decision.Reject("{0} is not allowed in profile {1}", _language, subject.Series.Profile.Value.Name);
            }

            return Decision.Accept();
        }
    }
}
