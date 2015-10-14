using System.Text.RegularExpressions;
using NLog;

namespace NzbDrone.Core.Parser.Analyzers
{
    public class AnalyzeSeason : AnalyzeContent
    {
        public static readonly Regex WeirdSeason = new Regex(
            @"(?:\b|_)(?:S?(?<season>(?<!\d)(?:0?\d{1,2}|\d{4})(?!\d+)))(?:\-|[ex]|\W[ex]|ep|\Wep|_)(?<episode>\d{1,5})(?:(?:\s-\s)(?:\-|[ex]|ep|_)(?<episode>\d{1,5}))(?:\b|_)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);

        public static readonly Regex SimpleSeason = new Regex(
            @"(?:\b|_)(?:S?(?<season>(?<!\d)(?:0?\d{1,2}|\d{4})(?!\d+)))(?:\-|[ex]|\W[ex]|ep|\Wep|_)(?<episode>\d{1,5})((?:\W?(?<anchor>\-|[ex]|ep|_)\W?(?<episode>\d{1,5}))?(?:\W?\k<anchor>\W?(?<episode>\d{1,5}))*)(?:\b|_)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);

        public static readonly Regex SimpleMiniSerie = new Regex(
            @"(?:(?:\b|_)(?:(?:Part\W?|(?<!\d\W)ep?)(?<episode>\d{1,2}(?!\d+)))+)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);

        public static readonly Regex SeasonAndEpisodeWord = new Regex(
            @"(\b|_)(?:\W?Season\W?)(?<season>(?<!\d)\d{1,3}(?!\d+))(?:\W|_)+(?:Episode\W)(?:[-_. ]?(?<episode>(?<!\d)\d{1,2}(?!\d+)))+(\b|_)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);

        public static readonly Regex OnlyDigitsOrEp = new Regex(
            @"(\b|_)(?<!\d)(?<season>\d{1,2})(?:(?:\-|[ex]|\W[ex]|ep|_)?(?<episode>\d{2}))+(\b|_)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);

        public static readonly Regex OnlySeason = new Regex(
            @"(?:\b|_)(?:(?:\W?Season\W?)(?<season>(?<!\d)\d{1,2}(?!\d+))|(?:S\W?(?<!\d)(?<season>\d{1,2}|\d{4})(?!\d+)))(?:\b|_)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);


        public AnalyzeSeason(Logger logger)
            : base(new Regex[] {
                new Regex(@"(\b|_)(?:\W?Season\W?)(?:(?<!\d)\d{1,2}(?!\d+))(?:\W|_)+(?:Episode\W)(?:[-_. ]?(?:(?<!\d)\d{1,2}(?!\d+)))+(\b|_)",
                    RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace),
                new Regex(@"(?:\b|_)(?:S?(?:(?<!\d)(?:0?\d{1,2}|\d{4})(?!\d+)))(?:\-|[ex]|\W[ex]|ep|\Wep|_)(?:\d{1,5})(?:(?:\s-\s)(?:\-|[ex]|ep|_)(?:\d{1,5}))(?:\b|_)",
                    RegexOptions.Compiled | RegexOptions.IgnoreCase),
                new Regex(@"(?:\b|_)(?:S?(?:(?<!\d)(?:0?\d{1,2}|\d{4})(?!\d+)))(?:\-|[ex]|\W[ex]|ep|\Wep|_)(?:\d{1,5})((?:\W?(?<anchor>\-|[ex]|ep|_)\W?(?:\d{1,5}))?(?:\W?\k<anchor>\W?(?:\d{1,5}))*)(?:\b|_)",
                    RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace),
                new Regex(@"(?:(?:\b|_)(?:(?:Part\W?|(?<!\d\W)ep?)(?:\d{1,2}(?!\d+)))+(?:\b|_))",
                    RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace),
                new Regex(@"(\b|_)(?<!\d)(?:\d{1,2})(?:(?:\-|[ex]|\W[ex]|ep|_)?(?:\d{2}))+(\b|_)",
                    RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace),
                new Regex(@"(\b|_)(?:\W?Season\W?)(?:(?<!\d)\d{1,3}(?!\d+))(\b|_)",
                    RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace),
                new Regex(@"(?:\b|_)(?:S\W?(?<!\d)(?:\d{1,3}|\d{4})(?!\d+))(?:\b|_)",
                    RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace),
            }, logger)
        {
            Category = InfoCategory.Season;
        }
    }
}
