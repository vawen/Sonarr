﻿using System.Text.RegularExpressions;
using NLog;

namespace NzbDrone.Core.Parser.Analyzers
{
    public class AnalyzerawHD : AnalyzeContent
    {
        private readonly Logger _logger;

        public AnalyzerawHD(Logger logger)
            : base(new Regex(@"(\b|_)(?<rawhd>TrollHD|RawHD|1080i[-_. ]HDTV|Raw[-_. ]HD|MPEG[-_. ]?2)(\b|_)",
                RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace))
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
                    _logger.Debug("Detected RawHD: {0}", param);
                    param.Category = InfoCategory.RawHD;
                    parsedInfo.AddItem(param);
                }
            }
            return ret;
        }
    }
}
