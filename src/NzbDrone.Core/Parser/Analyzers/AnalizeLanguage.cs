using System.Text.RegularExpressions;
using NLog;

namespace NzbDrone.Core.Parser.Analyzers
{
    public class AnalyzeLanguage : AnalyzeContent
    {
        private static readonly Regex SimpleLanguageRegex = new Regex(@"(?:\b|_)(?:english|french|spanish|danish|dutch|japanese|cantonese|mandarin|korean|russian|polish|vietnamese|swedish|norwegian|nordic|finnish|turkish|portuguese|hungarian)(?:\b|_)",
                    RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static readonly Regex LanguageRegex = new Regex(@"(?:\W|_)(?<italian>\b(?:ita|italian)\b)|(?<german>german\b|videomann)|(?<flemish>flemish)|(?<greek>greek)|(?<french>(?:\W|_)(?:FR|VOSTFR)(?:\W|_))|(?<russian>\brus\b)|(?<dutch>nl\W?subs?)|(?<hungarian>\b(?:HUNDUB|HUN)\b)",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public AnalyzeLanguage(Logger logger)
            : base(new Regex[] 
            {
                SimpleLanguageRegex,
                LanguageRegex,
            }, logger)
        {
            Category = InfoCategory.Language;
        }
    }
}
