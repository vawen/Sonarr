using System.Text.RegularExpressions;
using NLog;

namespace NzbDrone.Core.Parser.Analyzers
{
    public class AnalyzeAudio : AnalyzeContent
    {
        private readonly Logger _logger;

        public AnalyzeAudio(Logger logger)
            : base(new Regex(@"(\b|_)(?:(?<dts>DTS(?:-HD)?\W?(?:MA)?\W?(?:(?:5|7)\W1)?)|(?<dd51>DD\W?(?:5|7)\W1)|(?<AAC>(?:\d{0,2}bit\W?)?AAC\d{0,1}(\.\d)?))(\b|_)",
                RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace))
        {
            _logger = logger;
        }

        public override bool IsContent(ParsedItem item, ParsedInfo parsedInfo, out ParsedItem[] notParsed)
        {
            ParsedItem[] parsedItems;
            var ret = IsContent(item, out parsedItems, out notParsed);
            if (!ret)
            {
                return false;
            }

            foreach (var param in parsedItems)
            {
                _logger.Debug("Detected Audio: {0}", param);
                param.Category = InfoCategory.Audio;
                parsedInfo.AddItem(param);
            }
            return true;
        }
    }
}
