using NzbDrone.Core.Languages;
using NzbDrone.Core.Parser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NzbDrone.Core.DataAugmentation.TvdbLanguages
{
    public class TvdbLanguage
    {
        public string TvdbString { get; set; }
        public Language Language { get; set; }
        public int TvdbID { get; set; }


        // http://thetvdb.com/api/1D62F2F90030C444/languages.xml
        public static TvdbLanguage GetTvdbLanguage (Language language)
        {
            if (language == Language.English)
                return new TvdbLanguage { Language = language, TvdbID = 7, TvdbString = "en" };
            if (language == Language.Cantonese)
                return new TvdbLanguage { Language = language, TvdbID = 27, TvdbString = "zh" };
            if (language == Language.Danish)
                return new TvdbLanguage { Language = language, TvdbID = 10, TvdbString = "da" };
            if (language == Language.Dutch)
                return new TvdbLanguage { Language = language, TvdbID = 13, TvdbString = "nl" };
            if (language == Language.Finnish)
                return new TvdbLanguage { Language = language, TvdbID = 11, TvdbString = "fi" };
            if (language == Language.French)
                return new TvdbLanguage { Language = language, TvdbID = 17, TvdbString = "fr" };
            if (language == Language.German)
                return new TvdbLanguage { Language = language, TvdbID = 14, TvdbString = "de" };
            if (language == Language.Greek)
                return new TvdbLanguage { Language = language, TvdbID = 20, TvdbString = "el" };
            if (language == Language.Italian)
                return new TvdbLanguage { Language = language, TvdbID = 15, TvdbString = "it" };
            if (language == Language.Japanese)
                return new TvdbLanguage { Language = language, TvdbID = 25, TvdbString = "ja" };
            if (language == Language.Korean)
                return new TvdbLanguage { Language = language, TvdbID = 32, TvdbString = "ko" };
            if (language == Language.Mandarin)
                return new TvdbLanguage { Language = language, TvdbID = 27, TvdbString = "zh" };
            if (language == Language.Norwegian)
                return new TvdbLanguage { Language = language, TvdbID = 9, TvdbString = "no" };
            if (language == Language.Polish)
                return new TvdbLanguage { Language = language, TvdbID = 18, TvdbString = "pl" };
            if (language == Language.Portuguese)
                return new TvdbLanguage { Language = language, TvdbID = 26, TvdbString = "pt" };
            if (language == Language.Russian)
                return new TvdbLanguage { Language = language, TvdbID = 22, TvdbString = "ru" };
            if (language == Language.Spanish)
                return new TvdbLanguage { Language = language, TvdbID = 16, TvdbString = "es" };
            if (language == Language.Swedish)
                return new TvdbLanguage { Language = language, TvdbID = 8, TvdbString = "sv" };
            if (language == Language.Turkish)
                return new TvdbLanguage { Language = language, TvdbID = 21, TvdbString = "tr" };
            if (language == Language.Hungarian)
                return new TvdbLanguage { Language = language, TvdbID = 19, TvdbString = "hu" };

            return new TvdbLanguage { Language = language, TvdbID = 7, TvdbString = "en" };
        }
    }
}
