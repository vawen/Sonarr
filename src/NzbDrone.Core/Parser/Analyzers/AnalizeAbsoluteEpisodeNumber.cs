using System.Text.RegularExpressions;
using NLog;

namespace NzbDrone.Core.Parser.Analyzers
{
    public class AnalyzeAbsoluteEpisodeNumber : AnalyzeContent
    {
        private readonly Logger _logger;

        public static readonly Regex SimpleAbsoluteNumber = new Regex(@"(?:\b|[-_])(?<!\d[-_.])(?:(e|ep)?(?<absoluteepisode>\d{2,3})(?:[-._\s]?))+(?:v\d{1})?(?<!\d{4}[-._\s])(?<!\d{4})(?:\b|_|\b)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);


        public AnalyzeAbsoluteEpisodeNumber(Logger logger)
            : base(new Regex[] {
                new Regex(@"(?:\b|[-_])(?<!\d[-_.])(?:(e|ep)?\d{2,3}(?:[-._\s]?))+(?:v\d{1})?(?<!\d{4}[-._\s])(?<!\d{4})(?:\b|_|\b)",
                    RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace)
            })
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
                    _logger.Debug("Detected Absolute: {0}", param);
                    ParsedInfo.AddItem(param, parsedInfo.AbsoluteEpisodeNumber);
                }
            }
            return ret;
        }
    }
}
