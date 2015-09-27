using System;
using System.Text.RegularExpressions;
using NLog;

namespace NzbDrone.Core.Parser.Analizers
{
    public class AnalizeAudio : AnalizeContent
    {
        private readonly Logger _logger;

        public AnalizeAudio()
            : base(new Regex(@"(\b|_)(?:(?<dts>DTS(?:-HD)?\W?(?:MA)?\W?(?:(?:5|7)\W1)?)|(?<dd51>DD\W?(?:5|7)\W1)|(?<AAC>(?:\d{0,2}bit\W?)?AAC\d{0,1}))(\b|_)",
                RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace)) { }

        public override bool IsContent(string item, ParsedInfo parsedInfo, out string[] notParsed)
        {
            string[] parsedItems;
            bool ret = IsContent(item, out parsedItems, out notParsed);
            if (ret)
            {
                foreach (var param in parsedItems)
                {
                    Console.Out.WriteLine("Item: {0}, Detected Audio: {0}", item, param);
                    ParsedInfo.AddItem(param, parsedInfo.Audio);
                }
                foreach (var str in notParsed)
                {
                    Console.Out.WriteLine("Not parsed: {0}", str);
                }
            }
            return ret;
        }
    }
}
