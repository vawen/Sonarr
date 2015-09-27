using System;
using System.Text.RegularExpressions;
using NLog;

namespace NzbDrone.Core.Parser.Analizers
{
    public class AnalizeDaily : AnalizeContent
    {
        private readonly Logger _logger;

        public static readonly Regex AirDateRegex = new Regex(@"^(.*?)(?<!\d)((?<airyear>\d{4})[_.-](?<airmonth>[0-1][0-9])[_.-](?<airday>[0-3][0-9])|(?<airmonth>[0-1][0-9])[_.-](?<airday>[0-3][0-9])[_.-](?<airyear>\d{4}))(?!\d)",
                                                        RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public static readonly Regex SixDigitAirDateRegex = new Regex(@"(?<=[_.-])(?<airdate>(?<!\d)(?<airyear>[1-9]\d{1})(?<airmonth>[0-1][0-9])(?<airday>[0-3][0-9]))(?=[_.-])",
                                                                        RegexOptions.IgnoreCase | RegexOptions.Compiled);


        public AnalizeDaily()
            : base(new Regex(@"(?:\b|_)(?:\d{4}[_.-][0-1][0-9][_.-][0-3][0-9]|[0-1][0-9][_.-][0-3][0-9][_.-]\d{4}|[1-9]\d{1}[0-1][0-9][0-3][0-9])(?:\b|_)",
                RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace)) { }


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
