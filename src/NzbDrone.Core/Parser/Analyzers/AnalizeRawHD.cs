using System.Text.RegularExpressions;
using NLog;

namespace NzbDrone.Core.Parser.Analyzers
{
    public class AnalyzerawHD : AnalyzeContent
    {
        public AnalyzerawHD(Logger logger)
            : base(new Regex(@"(\b|_)(?<rawhd>TrollHD|RawHD|1080i[-_. ]HDTV|Raw[-_. ]HD|MPEG[-_. ]?2)(\b|_)",
                RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace), logger)
        {
            Category = InfoCategory.RawHD;
        }
    }
}
