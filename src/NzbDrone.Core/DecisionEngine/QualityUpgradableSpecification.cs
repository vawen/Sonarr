using NLog;
using NzbDrone.Core.Languages;
using NzbDrone.Core.Profiles;
using NzbDrone.Core.Qualities;

namespace NzbDrone.Core.DecisionEngine
{
    public interface IQualityUpgradableSpecification
    {
        bool IsUpgradable(Profile profile, QualityModel currentQuality, Language currentLanguage, QualityModel newQuality = null, Language newLanguage = null);
        bool CutoffNotMet(Profile profile, QualityModel currentQuality, Language currentLanguage, QualityModel newQuality = null);
        bool IsRevisionUpgrade(QualityModel currentQuality, QualityModel newQuality);
    }

    public class QualityUpgradableSpecification : IQualityUpgradableSpecification
    {
        private readonly Logger _logger;

        public QualityUpgradableSpecification(Logger logger)
        {
            _logger = logger;
        }

        public bool IsUpgradable(Profile profile, QualityModel currentQuality, Language currentLanguage, QualityModel newQuality = null, Language newLanguage = null)
        {
            if (newLanguage != null && newLanguage != currentLanguage && !profile.AllowLanguageUpgrade && !profile.LanguageOverQuality)
            {
                _logger.Debug("existing item has language and no upgrade allowed. skipping");
                return false;
            }

            if (newLanguage != null && profile.AllowLanguageUpgrade && profile.LanguageOverQuality)
            {
                int compare = new LanguageComparer(profile).Compare(newLanguage, currentLanguage);
                if (compare < 0)
                {
                    _logger.Debug("existing item has better language. skipping");
                    return false;
                }
                if (compare > 0)
                    return true;
            }

            if (newQuality != null)
            {
                int compare = new QualityModelComparer(profile).Compare(newQuality, currentQuality);
                if (compare < 0)
                {
                    _logger.Debug("existing item has better quality. skipping");
                    return false;
                }

                if (IsRevisionUpgrade(currentQuality, newQuality))
                {
                    return true;
                }

                if (compare == 0 && newLanguage != null && profile.AllowLanguageUpgrade && new LanguageComparer(profile).Compare(newLanguage, currentLanguage) > 0)
                {
                    return true;
                }

                if (compare == 0)
                {
                    _logger.Debug("existing item has better quality. skipping");
                    return false;
                }
            }

            return true;
        }

        public bool CutoffNotMet(Profile profile, QualityModel currentQuality, Language currentLanguage, QualityModel newQuality = null)
        {
            int languageCompare = new LanguageComparer(profile).Compare(currentLanguage, profile.CutoffLanguage);
            int compare = new QualityModelComparer(profile).Compare(currentQuality.Quality, profile.Cutoff);

            // If we can upgrade the language and it is not the cutoff then doesn't matter the quality we can always get same quality with prefered language
            if (profile.AllowLanguageUpgrade && languageCompare < 0)
                return true;

            if (compare >= 0)
            {
                if (newQuality != null && IsRevisionUpgrade(currentQuality, newQuality))
                {
                    return true;
                }

                _logger.Debug("Existing item meets cut-off. skipping.");
                return false;
            }

            return true;
        }

        public bool IsRevisionUpgrade(QualityModel currentQuality, QualityModel newQuality)
        {
            int compare = newQuality.Revision.CompareTo(currentQuality.Revision);

            if (currentQuality.Quality == newQuality.Quality && compare > 0)
            {
                _logger.Debug("New quality is a better revision for existing quality");
                return true;
            }

            return false;
        }
    }
}
