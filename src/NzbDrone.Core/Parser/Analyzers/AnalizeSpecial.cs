using System.Text.RegularExpressions;
using NLog;

namespace NzbDrone.Core.Parser.Analyzers
{
    public class AnalyzeSpecial : AnalyzeContent
    {
        public AnalyzeSpecial(Logger logger)
            : base(new Regex(@"(\b|_)(?:special|ova|ovd)(\b|_)",
                RegexOptions.Compiled | RegexOptions.IgnoreCase), logger)
        {
            Category = InfoCategory.Special;
        }
    }
}
