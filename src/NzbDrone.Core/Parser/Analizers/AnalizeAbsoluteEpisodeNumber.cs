using System;
using System.Text.RegularExpressions;

namespace NzbDrone.Core.Parser.Analizers
{
    public class AnalizeAbsoluteEpisodeNumber : AnalizeContent
    {
        public static readonly Regex SimpleAbsoluteNumber = new Regex(@"(?:\b|[-_])(?<!\d[-_.])(?:(e|ep)?(?<absoluteepisode>\d{2,3})(?:[-._\s]?))+(?:v\d{1})?(?<!\d{4}[-._\s])(?<!\d{4})(?:\b|_|\b)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);


        public AnalizeAbsoluteEpisodeNumber()
            : base(new Regex[] {
                new Regex(@"(?:\b|[-_])(?<!\d[-_.])(?:(e|ep)?\d{2,3}(?:[-._\s]?))+(?:v\d{1})?(?<!\d{4}[-._\s])(?<!\d{4})(?:\b|_|\b)",
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
                    Console.Out.WriteLine("Item: {0}, Detected Absolute: {0}", item, param);
                    ParsedInfo.AddItem(param, parsedInfo.AbsoluteEpisodeNumber);
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
