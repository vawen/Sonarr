using System.Text.RegularExpressions;
using NLog;

namespace NzbDrone.Core.Parser.Analyzers
{
    public class AnalyzeAbsoluteEpisodeNumber : AnalyzeContent
    {
        public static readonly Regex SimpleAbsoluteNumber = new Regex(@"(?:\b|[-_])(?<!\d[-_.])(?:(e|ep)?(?<absoluteepisode>\d{2,3})(?:[-._\s]?))+(?:v\d{1})?(?<!\d{4}[-._\s])(?<!\d{4})(?:\b|_|\b)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);


        public AnalyzeAbsoluteEpisodeNumber(Logger logger)
            : base(new[] 
            {
                new Regex(@"(?:\b|[-_])(?<!\d[-_.])(?:(e|ep)?\d{2,3}(?:[-._\s]?))+(?:v\d{1})?(?<!\d{4}[-._\s])(?<!\d{4})(?:\b|_|\b)",
                    RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace),
            }, logger)
        {
            Category = InfoCategory.AbsoluteEpisodeNumber;
        }
    }
}
