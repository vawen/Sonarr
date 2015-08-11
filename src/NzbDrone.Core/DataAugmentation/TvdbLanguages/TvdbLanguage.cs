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

        public static TvdbLanguage GetTvdbLanguage (Language language)
        {
            switch (language)
            {
                case Language.English:
                    return new TvdbLanguage { Language = language, TvdbID = 7, TvdbString = "en" };
                case Language.Cantonese:
                    return new TvdbLanguage { Language = language, TvdbID = 27, TvdbString = "zh" };
                case Language.Danish:
                    return new TvdbLanguage { Language = language, TvdbID = 10, TvdbString = "da" };
                case Language.Dutch:
                    return new TvdbLanguage { Language = language, TvdbID = 13, TvdbString = "nl" };
                case Language.Finnish:
                    return new TvdbLanguage { Language = language, TvdbID = 11, TvdbString = "fi" };
                case Language.French:
                    return new TvdbLanguage { Language = language, TvdbID = 17, TvdbString = "fr" };
                case Language.German:
                    return new TvdbLanguage { Language = language, TvdbID = 14, TvdbString = "de" };
                case Language.Greek:
                    return new TvdbLanguage { Language = language, TvdbID = 20, TvdbString = "el" };
                case Language.Italian:
                    return new TvdbLanguage { Language = language, TvdbID = 15, TvdbString = "it" };
                case Language.Japanese:
                    return new TvdbLanguage { Language = language, TvdbID = 25, TvdbString = "ja" };
                case Language.Korean:
                    return new TvdbLanguage { Language = language, TvdbID = 32, TvdbString = "ko" };
                case Language.Mandarin:
                    return new TvdbLanguage { Language = language, TvdbID = 27, TvdbString = "zh" };
                case Language.Norwegian:
                    return new TvdbLanguage { Language = language, TvdbID = 9, TvdbString = "no" };
                case Language.Polish:
                    return new TvdbLanguage { Language = language, TvdbID = 18, TvdbString = "pl" };
                case Language.Portuguese:
                    return new TvdbLanguage { Language = language, TvdbID = 26, TvdbString = "pt" };
                case Language.Russian:
                    return new TvdbLanguage { Language = language, TvdbID = 22, TvdbString = "ru" };
                case Language.Spanish:
                    return new TvdbLanguage { Language = language, TvdbID = 16, TvdbString = "es" };
                case Language.Swedish:
                    return new TvdbLanguage { Language = language, TvdbID = 8, TvdbString = "sv" };
                case Language.Turkish:
                    return new TvdbLanguage { Language = language, TvdbID = 21, TvdbString = "tr" };
                default:
                    return new TvdbLanguage { Language = language, TvdbID = 7, TvdbString = "en" };
            }
        }
    }
}
