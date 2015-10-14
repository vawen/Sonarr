using System.Text.RegularExpressions;
using NLog;

namespace NzbDrone.Core.Parser.Analyzers
{
    public class AnalyzeResolution : AnalyzeContent
    {
        public static readonly Regex ResolutionRegex = new Regex(@"(?:(?<_480p>480p|640x480|848x480)|(?<_576p>576p)|(?<_720p>720p|1280x720)|(?<_1080p>1080p|1920x1080))(?:\b|_)",
                RegexOptions.Compiled | RegexOptions.IgnoreCase);


        public AnalyzeResolution(Logger logger)
            : base(ResolutionRegex, logger)
        {
            Category = InfoCategory.Resolution;
        }
    }
}
