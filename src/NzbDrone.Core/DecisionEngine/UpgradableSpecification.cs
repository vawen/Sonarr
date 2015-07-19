using NLog;
using NzbDrone.Core.Languages;
using NzbDrone.Core.Profiles;
using NzbDrone.Core.Qualities;

namespace NzbDrone.Core.DecisionEngine
{
    public interface IUpgradableSpecification
    {
        bool IsUpgradable(Profile profile, QualityModel currentQuality, Language currentLanguage, QualityModel newQuality, Language newLanguage);
        bool CutoffNotMet(Profile profile, QualityModel currentQuality, Language currentLanguage, QualityModel newQuality = null);
        bool IsRevisionUpgrade(QualityModel currentQuality, QualityModel newQuality);
    }

    public class UpgradableSpecification : IUpgradableSpecification
    {
        private readonly Logger _logger;

        public UpgradableSpecification(Logger logger)
        {
            _logger = logger;
        }

        private bool IsLanguageBlocked(Profile profile, Language currentLanguage, Language newLanguage = null)
        {            
            if (!profile.AllowLanguageUpgrade && currentLanguage != null && newLanguage != null && newLanguage != currentLanguage)
                return true;
            return false;
        }

        private bool IsLanguageUpgradable(Profile profile, Language currentLanguage, Language newLanguage = null) 
        {
            if (newLanguage != null)
            {
                int compare = new LanguageComparer(profile).Compare(newLanguage, currentLanguage);
                if (compare <= 0)
                    return false;
            }
            return true;
        }

        private bool IsQualityUpgradable(Profile profile, QualityModel currentQuality, QualityModel newQuality = null)
        {
            if (newQuality != null)
            {
                int compare = new QualityModelComparer(profile).Compare(newQuality, currentQuality);
                if (compare <= 0)
                {
                    _logger.Debug("existing item has better quality. skipping");
                    return false;
                }
            }
            return true;
        }


        public bool IsUpgradable(Profile profile, QualityModel currentQuality, Language currentLanguage, QualityModel newQuality, Language newLanguage)
        {
            if (IsLanguageBlocked(profile, currentLanguage, newLanguage))
            {
                _logger.Debug("existing item has different language and no upgrade allowed. skipping");
                return false;
            }

            if (profile.LanguageOverQuality)
            {
                // If languages are the same then check quality
                if (newLanguage != null && currentLanguage == newLanguage)
                {
                    return IsQualityUpgradable(profile, currentQuality, newQuality);
                }

                // If language is worse then always return false
                if (!IsLanguageUpgradable(profile, currentLanguage, newLanguage))
                {
                    _logger.Debug("existing item has better language. skipping");
                    return false;
                }
            }
            else
            {
                // If qualities are the same then check language
                if (newQuality != null && currentQuality == newQuality)
                {
                    return IsLanguageUpgradable(profile, currentLanguage, newLanguage);
                }

                // If quality is worse then always return false
                if (!IsQualityUpgradable(profile, currentQuality, newQuality))
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
