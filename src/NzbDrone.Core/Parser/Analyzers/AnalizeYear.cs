using System.Text.RegularExpressions;
using NLog;

namespace NzbDrone.Core.Parser.Analyzers
{
    public class AnalyzeYear : AnalyzeContent
    {
        public AnalyzeYear(Logger logger)
            : base(new Regex(@"(\b|_)(?:[12][09]\d{2})(\b|_)",
                RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace), logger)
        {
            Category = InfoCategory.Year;
        }
    }
}
