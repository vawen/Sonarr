using System;
using System.Text.RegularExpressions;

namespace NzbDrone.Core.Parser.Analizers
{
    public class AnalizeSeason : AnalizeContent
    {

        public static readonly Regex SimpleSeason = new Regex(
               @"(?:\b|_)(?:S?(?<season>(?<!\d)(?:\d{1,2}|\d{4})(?!\d+))(?:(?:\-|[ex]|\W[ex]|_)(?<episode>\d{1,3})(?!\d+))+)(?:\b|_)",
               RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);


        public AnalizeSeason()
            : base(new Regex[] {
                new Regex(@"(?:\b|_)(?:S?(?<!\d)(?:\d{1,2}|\d{4})(?!\d+)(?:(?:\-|[ex]|\W[ex]|_)\d{2,3}(?!\d+))+)(?:\b|_)",
                    RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace)
            }) { }

        public override bool IsContent(string item, ParsedInfo parsedInfo, out string[] notParsed)
        {
            string[] parsedItems;
            bool ret = IsContent(item, out parsedItems, out notParsed);
            if (ret)
            {
                foreach (var param in parsedItems)
                {
                    Console.Out.WriteLine("Item: {0}, Detected Season: {0}", item, param);
                    ParsedInfo.AddItem(param, parsedInfo.Season);
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
