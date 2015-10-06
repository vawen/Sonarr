using System.Text.RegularExpressions;
using NLog;

namespace NzbDrone.Core.Parser.Analyzers
{
    public class Analyzereal : AnalyzeContent
    {
        private readonly Logger _logger;

        public Analyzereal(Logger logger)
            : base(new Regex(@"(\b|_)(?<real>real)(\b|_)",
                RegexOptions.Compiled | RegexOptions.IgnoreCase))
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
                    _logger.Debug("Detected Real: {0}", param);
                    ParsedInfo.AddItem(param, parsedInfo.Real);
                }
            }
            return ret;
        }
    }
}
