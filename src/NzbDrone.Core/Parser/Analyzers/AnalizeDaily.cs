using System.Text.RegularExpressions;
using NLog;

namespace NzbDrone.Core.Parser.Analyzers
{
    public class AnalyzeDaily : AnalyzeContent
    {
        public static readonly Regex AirDateRegex = new Regex(@"^(.*?)(?<!\d)((?<airyear>\d{4})\W+(?<airmonth>[0-1][0-9])\W+(?<airday>[0-3][0-9])|(?<airmonth>[0-1][0-9])\W+(?<airday>[0-3][0-9])\W+(?<airyear>\d{4}))(?!\d)",
                                                        RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public static readonly Regex SixDigitAirDateRegex = new Regex(@"(?<airdate>(?<!\d)(?<airyear>[1-9]\d{1})(?<airmonth>[0-1][0-9])(?<airday>[0-3][0-9]))",
                                                                        RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public static readonly Regex SixDigitWAirDateRegex = new Regex(@"(?<airdate>(?<!\d)(?<airday>[0-3][0-9])\W+(?<airmonth>[0-1][0-9])\W+(?<airyear>[0-1]\d))",
                                                                        RegexOptions.IgnoreCase | RegexOptions.Compiled);


        public AnalyzeDaily(Logger logger)
            : base(new Regex[] {
                new Regex(@"(?:\b|_)(?:\d{4}\W+[0-1][0-9]\W+[0-3][0-9]|[0-1][0-9]\W+[0-3][0-9]\W+\d{4}|[1-9]\d{1}[0-1][0-9][0-3][0-9])(?:\b|_)",
                    RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace),
                new Regex(@"(?:\b|_)(?:(?<!\d)[1-9]\d{1}[0-1][0-9][0-3][0-9])(?:\b|_)", RegexOptions.IgnoreCase | RegexOptions.Compiled),
                new Regex(@"(?:\b|_)(?:[0-3][0-9]\W+[0-1][0-9]\W+[0-1]\d)(?:\b|_)",  RegexOptions.IgnoreCase | RegexOptions.Compiled)
            }, logger)
        {
            Category = InfoCategory.Daily;
        }
    }
}
