using System;
using System.Text.RegularExpressions;

namespace NzbDrone.Core.Parser.Analizers
{
    public class AnalizeHash : AnalizeContent
    {

        public AnalizeHash()
            : base(new Regex(@"(\b|_)(?<hash>\w{8})(\b|_)$",
                RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace)) { }

        public override bool IsContent(string item, ParsedInfo parsedInfo, out string[] notParsed)
        {

            string[] parsedItems;
            bool ret = IsContent(item, out parsedItems, out notParsed);
            if (ret)
            {
                foreach (var param in parsedItems)
                {
                    Console.Out.WriteLine("Item: {0}, Detected Hash: {0}", item, param);
                    ParsedInfo.AddItem(param, parsedInfo.Hash);
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
