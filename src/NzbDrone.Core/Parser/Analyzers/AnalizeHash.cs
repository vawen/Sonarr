using System.Text.RegularExpressions;
using NLog;

namespace NzbDrone.Core.Parser.Analyzers
{
    public class AnalyzeHash : AnalyzeContent
    {
        public AnalyzeHash(Logger logger)
            : base(new Regex(@"^(?<hash>\w{8})(\b|_)$",
                RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace), logger)
        {
            Category = InfoCategory.Hash;
        }
    }
}
