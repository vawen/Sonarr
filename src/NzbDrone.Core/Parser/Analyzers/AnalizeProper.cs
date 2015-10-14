using System.Text.RegularExpressions;
using NLog;

namespace NzbDrone.Core.Parser.Analyzers
{
    public class AnalyzeProper : AnalyzeContent
    {
        public static readonly Regex ProperRegex = new Regex(@"(\dv(?<version>\d)|(\b|_)v(?<version>\d)|(\b|_)(?<proper>proper|repack))(\b|_)",
                   RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);

        public AnalyzeProper(Logger logger)
            : base(ProperRegex, logger)
        {
            Category = InfoCategory.Proper;
        }
    }
}
