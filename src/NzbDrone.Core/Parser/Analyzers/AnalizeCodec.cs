using System.Text.RegularExpressions;
using NLog;

namespace NzbDrone.Core.Parser.Analyzers
{
    public class AnalyzeCodec : AnalyzeContent
    {

        private readonly Logger _logger;

        public static readonly Regex CodecRegex = new Regex(@"(\b|_)?(?:(?<x264>x264)|(?<h264>h(\.|\s)?264)|(?<xvidhd>XvidHD)|(?<xvid>Xvid)|(?<divx>divx))(\b|_)?",
                RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);

        public AnalyzeCodec(Logger logger)
            : base(CodecRegex)
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
                    _logger.Debug("Detected Codec: {0}", param);
                    param.Category = InfoCategory.Codec;
                    parsedInfo.AddItem(param);
                }
            }
            return ret;
        }
    }
}
