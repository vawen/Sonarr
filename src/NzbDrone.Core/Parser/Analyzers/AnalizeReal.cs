using System.Text.RegularExpressions;
using NLog;

namespace NzbDrone.Core.Parser.Analyzers
{
    public class Analyzereal : AnalyzeContent
    {
        public Analyzereal(Logger logger)
            : base(new Regex(@"(\b|_)(?<real>real)(\b|_)",
                RegexOptions.Compiled | RegexOptions.IgnoreCase), logger)
        {
            Category = InfoCategory.Real;
        }
    }
}
