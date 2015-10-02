using System;
using System.Text.RegularExpressions;
using NLog;

namespace NzbDrone.Core.Parser.Analizers
{
    public class AnalizeDaily : AnalizeContent
    {
        private readonly Logger _logger;

        public static readonly Regex AirDateRegex = new Regex(@"^(.*?)(?<!\d)((?<airyear>\d{4})\W+(?<airmonth>[0-1][0-9])\W+(?<airday>[0-3][0-9])|(?<airmonth>[0-1][0-9])\W+(?<airday>[0-3][0-9])\W+(?<airyear>\d{4}))(?!\d)",
                                                        RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public static readonly Regex SixDigitAirDateRegex = new Regex(@"(?<airdate>(?<!\d)(?<airyear>[1-9]\d{1})(?<airmonth>[0-1][0-9])(?<airday>[0-3][0-9]))",
                                                                        RegexOptions.IgnoreCase | RegexOptions.Compiled);


        public AnalizeDaily()
            : base(new Regex[] {
                new Regex(@"(?:\b|_)(?:\d{4}\W+[0-1][0-9]\W+[0-3][0-9]|[0-1][0-9]\W+[0-3][0-9]\W+\d{4}|[1-9]\d{1}[0-1][0-9][0-3][0-9])(?:\b|_)",
                    RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace),
                new Regex(@"(?:\b|_)(?:(?<!\d)[1-9]\d{1}[0-1][0-9][0-3][0-9])(?:\b|_)", RegexOptions.IgnoreCase | RegexOptions.Compiled)
    }) { }


        public override bool IsContent(string item, ParsedInfo parsedInfo, out string[] notParsed)
        {
            string[] parsedItems;
            bool ret = IsContent(item, out parsedItems, out notParsed);
            if (ret)
            {
                foreach (var param in parsedItems)
                {
                    Console.Out.WriteLine("Item: {0}, Detected Daily: {0}", item, param);
                    ParsedInfo.AddItem(param, parsedInfo.Daily);
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
