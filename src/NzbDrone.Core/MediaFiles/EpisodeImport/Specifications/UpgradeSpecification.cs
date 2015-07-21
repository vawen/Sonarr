using System.Linq;
using NLog;
using NzbDrone.Core.DecisionEngine;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Languages;
using NzbDrone.Core.Profiles;
using System.Collections.Generic;

namespace NzbDrone.Core.MediaFiles.EpisodeImport.Specifications
{
    public class UpgradeSpecification : IImportDecisionEngineSpecification
    {
        private readonly Logger _logger;

        public UpgradeSpecification(Logger logger)
        {
            _logger = logger;
        }

        private bool IsLanguageBlocked(Profile profile, Language currentLanguage, Language newLanguage = null)
        {
            if (!profile.AllowLanguageUpgrade && currentLanguage != null && newLanguage != null && newLanguage != currentLanguage)
                return true;
            return false;
        }

        public Decision IsSatisfiedBy(LocalEpisode localEpisode)
        {
            var qualityComparer = new QualityModelComparer(localEpisode.Series.Profile);
            var languageComparer = new LanguageComparer(localEpisode.Series.Profile);
            var profile = localEpisode.Series.Profile.Value;

            if (localEpisode.Episodes.Any (e => e.EpisodeFileId != 0 && IsLanguageBlocked(profile, e.EpisodeFile.Value.Language, localEpisode.Language)))
            {
                _logger.Debug("This file is different language for at least one episode and no upgrade allowed. Skipping {0}", localEpisode.Path);
                return Decision.Reject("Not an upgrade for existing episode file(s)");

            }

            if (profile.LanguageOverQuality)
            {
                if (localEpisode.Episodes.Any(e => e.EpisodeFileId != 0 && languageComparer.Compare(e.EpisodeFile.Value.Language, localEpisode.Language) > 0))
                {
                    _logger.Debug("This file isn't a language upgrade for all episodes. Skipping {0}", localEpisode.Path);
                    return Decision.Reject("Not an upgrade for existing episode file(s)");
                }
                if (localEpisode.Episodes.Any(
                        e => e.EpisodeFileId != 0 &&
                        languageComparer.Compare(e.EpisodeFile.Value.Language, localEpisode.Language) == 0 &&
                        qualityComparer.Compare(e.EpisodeFile.Value.Quality, localEpisode.Quality) > 0))
                {
                    _logger.Debug("This file isn't a quality upgrade for all episodes. Skipping {0}", localEpisode.Path);
                    return Decision.Reject("Not an upgrade for existing episode file(s)");
                }            
            }
            else 
            {
                if (localEpisode.Episodes.Any(e => e.EpisodeFileId != 0 && qualityComparer.Compare(e.EpisodeFile.Value.Quality, localEpisode.Quality) > 0))
                {
                    _logger.Debug("This file isn't a quality upgrade for all episodes. Skipping {0}", localEpisode.Path);
                    return Decision.Reject("Not an upgrade for existing episode file(s)");
                }              
                if (localEpisode.Episodes.Any(
                        e => e.EpisodeFileId != 0 &&
                        languageComparer.Compare(e.EpisodeFile.Value.Language, localEpisode.Language) > 0 &&
                        qualityComparer.Compare(e.EpisodeFile.Value.Quality, localEpisode.Quality) == 0))
                {
                    _logger.Debug("This file isn't a language upgrade for all episodes. Skipping {0}", localEpisode.Path);
                    return Decision.Reject("Not an upgrade for existing episode file(s)");
                }
            }

            return Decision.Accept();
        }
    }
}
