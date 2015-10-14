using System.Text.RegularExpressions;
using NLog;

namespace NzbDrone.Core.Parser.Analyzers
{
    public class AnalyzeFileExtension : AnalyzeContent
    {
        public AnalyzeFileExtension(Logger logger)
            : base(new Regex(@"\.[a-z0-9]{2,4}$",
                RegexOptions.Compiled | RegexOptions.IgnoreCase), logger)
        {
            Category = InfoCategory.FileExtension;
        }

        public override bool IsContent(ParsedItem item, ParsedInfo parsedInfo, out ParsedItem[] notParsed)
        {
            // We must be last items in the global string
            if (item.Position + item.Length != item.GlobalLength)
            {
                notParsed = null;
                return false;
            }
            return base.IsContent(item, parsedInfo, out notParsed);
        }
    }
}
