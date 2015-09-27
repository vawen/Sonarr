using System;
using System.Text.RegularExpressions;
using NLog;

namespace NzbDrone.Core.Parser.Analizers
{
    public class AnalizeSpecial : AnalizeContent
    {
        private readonly Logger _logger;

        public AnalizeSpecial()
            : base(new Regex(@"(\b|_)(?:special|ova|ovd)(\b|_)",
                RegexOptions.Compiled | RegexOptions.IgnoreCase)) { }

        public override bool IsContent(string item, ParsedInfo parsedInfo, out string[] notParsed)
        {
            string[] parsedItems;
            bool ret = IsContent(item, out parsedItems, out notParsed);
            if (ret)
            {
                foreach (var param in parsedItems)
                {
                    Console.Out.WriteLine("Item: {0}, Detected Special: {0}", item, param);
                    ParsedInfo.AddItem(param, parsedInfo.Special);
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
