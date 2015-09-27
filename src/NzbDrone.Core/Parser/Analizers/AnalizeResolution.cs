using System;
using System.Text.RegularExpressions;

namespace NzbDrone.Core.Parser.Analizers
{
    public class AnalizeResolution : AnalizeContent
    {
        public AnalizeResolution()
            : base(new Regex(@"(?:\b|_)(?:(?<_480p>480p|640x480|848x480)|(?<_576p>576p)|(?<_720p>720p|1280x720)|(?<_1080p>1080p|1920x1080))(?:\b|_)",
                RegexOptions.Compiled | RegexOptions.IgnoreCase)) { }

        public override bool IsContent(string item, ParsedInfo parsedInfo, out string[] notParsed)
        {
            string[] parsedItems;
            bool ret = IsContent(item, out parsedItems, out notParsed);
            if (ret)
            {
                foreach (var param in parsedItems)
                {
                    Console.Out.WriteLine("Item: {0}, Detected Resolution: {0}", item, param);
                    ParsedInfo.AddItem(param, parsedInfo.Resolution);
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
