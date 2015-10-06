using System.Text.RegularExpressions;
using NLog;

namespace NzbDrone.Core.Parser.Analyzers
{
    public class AnalyzeProper : AnalyzeContent
    {
        private readonly Logger _logger;

        public static readonly Regex ProperRegex = new Regex(@"(\dv(?<version>\d)|(\b|_)v(?<version>\d)|(\b|_)(?<proper>proper|repack))(\b|_)",
                   RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);

        public AnalyzeProper(Logger logger)
            : base(ProperRegex)
        {
            _logger = logger;
        }

        public override bool IsContent(ParsedItem item, ParsedInfo parsedInfo, out ParsedItem[] notParsed)
        {
            ParsedItem[] parsedItems;
            bool ret = IsContent(item, out parsedItems, out notParsed);
            if (ret)
            {
                foreach (var param in parsedItems)
                {
                    _logger.Debug("Detected Proper: {0}", param);
                    ParsedInfo.AddItem(param, parsedInfo.Proper);
                }
            }
            return ret;
        }
    }
}
