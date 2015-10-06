using System.Text.RegularExpressions;
using NLog;

namespace NzbDrone.Core.Parser.Analyzers
{
    public class AnalyzeYear : AnalyzeContent
    {
        private readonly Logger _logger;

        public AnalyzeYear(Logger logger)
            : base(new Regex(@"(\b|_)(?:[12][09]\d{2})(\b|_)",
                RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace))
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
                    _logger.Debug("Detected Year: {0}", param);
                    ParsedInfo.AddItem(param, parsedInfo.Year);
                }
            }
            return ret;
        }
    }
}
