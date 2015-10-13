using System.Text.RegularExpressions;
using NLog;

namespace NzbDrone.Core.Parser.Analyzers
{
    public class Analyzeresolution : AnalyzeContent
    {
        private readonly Logger _logger;

        public static readonly Regex ResolutionRegex = new Regex(@"(?:\b|_)(?:(?<_480p>480p|640x480|848x480)|(?<_576p>576p)|(?<_720p>720p|1280x720)|(?<_1080p>1080p|1920x1080))(?:\b|_)",
                RegexOptions.Compiled | RegexOptions.IgnoreCase);


        public Analyzeresolution(Logger logger)
            : base(ResolutionRegex)
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
                    _logger.Debug("Detected Resolution: {0}", param);
                    param.Category = InfoCategory.Resolution;
                    parsedInfo.AddItem(param);
                }
            }
            return ret;
        }
    }
}
