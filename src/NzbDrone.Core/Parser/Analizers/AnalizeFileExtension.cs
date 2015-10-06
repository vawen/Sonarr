using System.Text.RegularExpressions;
using NLog;

namespace NzbDrone.Core.Parser.Analizers
{
    public class AnalizeFileExtension : AnalizeContent
    {

        private readonly Logger _logger;

        public AnalizeFileExtension(Logger logger)
            : base(new Regex(@"\.[a-z0-9]{2,4}$",
                RegexOptions.Compiled | RegexOptions.IgnoreCase))
        {
            _logger = logger;
        }

        public override bool IsContent(ParsedItem item, ParsedInfo parsedInfo, out ParsedItem[] notParsed)
        {
            ParsedItem[] parsedItems;
            // We must be last items in the global string
            if (item.Position + item.Length != item.GlobalLength)
            {
                notParsed = null;
                return false;
            }
            bool ret = IsContent(item, out parsedItems, out notParsed);
            if (ret)
            {
                foreach (var param in parsedItems)
                {
                    _logger.Debug("Detected FileExtension: {0}", param);
                    ParsedInfo.AddItem(param, parsedInfo.FileExtension);
                }
            }
            return ret;
        }
    }
}
