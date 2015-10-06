using System.Text.RegularExpressions;
using NLog;

namespace NzbDrone.Core.Parser.Analizers
{
    public class AnalizeSource : AnalizeContent
    {
        public readonly Logger _logger;

        public static readonly Regex SourceRegex = new Regex(@"(\b|_)(?:
                              (?<bluray>BluRay|Blu-Ray|HDDVD|BD)|
                              (?<webdl>WEB[-_. ]DL|WEBDL|WebRip|iTunesHD|WebHD)|
                              (?<hdtv>HDTV)|
                              (?<hdtv720p>HD[-_. ]TV)|
                              (?<bdrip>BDRiP)|
                              (?<brrip>BRRip)|
                              (?<dvd>DVD|DVDRip|NTSC|PAL|xvidvd)|
                              (?<dsr>WS[-_. ]DSR|DSR)|
                              (?<pdtv>PDTV)|
                              (?<sdtv>SD[-_. ]?TV)|
                              (?<tvrip>TVRip)
                              )(\b|_)",
                             RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);

        public AnalizeSource(Logger logger)
            : base(SourceRegex)
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
                    _logger.Debug("Detected Source: {0}", param);
                    ParsedInfo.AddItem(param, parsedInfo.Source);
                }
            }
            return ret;
        }
    }
}
