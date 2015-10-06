using System.Text.RegularExpressions;
using NLog;

namespace NzbDrone.Core.Parser.Analizers
{
    public class AnalizeHash : AnalizeContent
    {

        private readonly Logger _logger;

        public AnalizeHash(Logger logger)
            : base(new Regex(@"(\b|_)(?<hash>\w{8})(\b|_)$",
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
                    _logger.Debug("Detected Hash: {0}", param);
                    ParsedInfo.AddItem(param, parsedInfo.Hash);
                }
            }
            return ret;
        }
    }
}
