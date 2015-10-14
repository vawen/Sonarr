using System.Text.RegularExpressions;
using NLog;

namespace NzbDrone.Core.Parser.Analyzers
{
    public class AnalyzeSource : AnalyzeContent
    {
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

        public AnalyzeSource(Logger logger)
            : base(SourceRegex, logger)
        {
            Category = InfoCategory.Source;
        }
    }
}
