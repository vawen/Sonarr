using System.Text.RegularExpressions;
using NLog;

namespace NzbDrone.Core.Parser.Analyzers
{
    public class AnalyzeAudio : AnalyzeContent
    {
        public AnalyzeAudio(Logger logger)
            : base(new Regex(@"(\b|_)(?:(?<dts>DTS(?:-HD)?\W?(?:MA)?\W?(?:(?:5|7)\W1)?)|(?<dd51>DD\W?(?:5|7)\W1)|(?<AAC>(?:\d{0,2}bit\W?)?AAC\d{0,1}(\.\d)?))(\b|_)",
                RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace), logger)
        {
            Category = InfoCategory.Audio;
        }
    }
}
