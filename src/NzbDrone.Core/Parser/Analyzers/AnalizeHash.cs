﻿using System.Text.RegularExpressions;
using NLog;

namespace NzbDrone.Core.Parser.Analyzers
{
    public class AnalyzeHash : AnalyzeContent
    {

        private readonly Logger _logger;

        public AnalyzeHash(Logger logger)
            : base(new Regex(@"(\b|_)(?<hash>\w{8})(\b|_)$",
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
                    _logger.Debug("Detected Hash: {0}", param);
                    param.Category = InfoCategory.Hash;
                    parsedInfo.AddItem(param);
                }
            }
            return ret;
        }
    }
}
