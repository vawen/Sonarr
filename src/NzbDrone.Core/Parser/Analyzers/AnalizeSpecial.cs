using System.Text.RegularExpressions;
using NLog;

namespace NzbDrone.Core.Parser.Analyzers
{
    public class AnalyzeSpecial : AnalyzeContent
    {
        private readonly Logger _logger;

        public AnalyzeSpecial(Logger logger)
            : base(new Regex(@"(\b|_)(?:special|ova|ovd)(\b|_)",
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
                    _logger.Debug("Detected Special: {0}", param);
                    param.Category = InfoCategory.Special;
                    parsedInfo.AddItem(param);
                }
            }
            return ret;
        }
    }
}
