using System.Text.RegularExpressions;
using NLog;

namespace NzbDrone.Core.Parser.Analyzers
{
    public class AnalyzeCodec : AnalyzeContent
    {
        public static readonly Regex CodecRegex = new Regex(@"(\b|_)?(?:(?<x264>x264)|(?<h264>h(\.|\s)?264)|(?<xvidhd>XvidHD)|(?<xvid>Xvid)|(?<divx>divx))(\b|_)?",
                RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);

        public AnalyzeCodec(Logger logger)
            : base(CodecRegex, logger)
        {
            Category = InfoCategory.Codec;
        }
    }
}
