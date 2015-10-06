﻿using System.Text.RegularExpressions;
using NLog;

namespace NzbDrone.Core.Parser.Analizers
{
    public class AnalizeAudio : AnalizeContent
    {
        private readonly Logger _logger;

        public AnalizeAudio(Logger logger)
            : base(new Regex(@"(\b|_)(?:(?<dts>DTS(?:-HD)?\W?(?:MA)?\W?(?:(?:5|7)\W1)?)|(?<dd51>DD\W?(?:5|7)\W1)|(?<AAC>(?:\d{0,2}bit\W?)?AAC\d{0,1}(\.\d)?))(\b|_)",
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
                    _logger.Debug("Detected Audio: {0}", param);
                    ParsedInfo.AddItem(param, parsedInfo.Audio);
                }
            }
            return ret;
        }
    }
}
