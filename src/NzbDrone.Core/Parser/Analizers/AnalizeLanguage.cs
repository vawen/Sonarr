using System;
using System.Text.RegularExpressions;
using NLog;

namespace NzbDrone.Core.Parser.Analizers
{
    public class AnalizeLanguage : AnalizeContent
    {
        private readonly Logger _logger;

        private static readonly Regex SimpleLanguageRegex = new Regex(@"(?:\b|_)(?:english|french|spanish|danish|dutch|japanese|cantonese|mandarin|korean|russian|polish|swedish|norwegian|nordic|finnish|turkish|portuguese|hungarian)(?:\b|_)",
                    RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static readonly Regex LanguageRegex = new Regex(@"(?:\W|_)(?<italian>\b(?:ita|italian)\b)|(?<german>german\b|videomann)|(?<flemish>flemish)|(?<greek>greek)|(?<french>(?:\W|_)(?:FR|VOSTFR)(?:\W|_))|(?<russian>\brus\b)|(?<dutch>nl\W?subs?)|(?<hungarian>\b(?:HUNDUB|HUN)\b)",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public AnalizeLanguage()
            : base(new Regex[] 
            {
                SimpleLanguageRegex,
                LanguageRegex,
            })
        {
        }

        public override bool IsContent(string item, ParsedInfo parsedInfo, out string[] notParsed)
        {
            string[] parsedItems;
            bool ret = IsContent(item, out parsedItems, out notParsed);
            if (ret)
            {
                foreach (var param in parsedItems)
                {
                    Console.Out.WriteLine("Item: {0}, Detected Language: {0}", item, param);
                    ParsedInfo.AddItem(param, parsedInfo.Language);
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
