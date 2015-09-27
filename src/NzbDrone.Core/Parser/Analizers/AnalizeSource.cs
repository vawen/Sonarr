using System;
using System.Text.RegularExpressions;

namespace NzbDrone.Core.Parser.Analizers
{
    public class AnalizeSource : AnalizeContent
    {
        public AnalizeSource()
            : base(new Regex(@"(\b|_)(?:
                              (?<bluray>BluRay|Blu-Ray|HDDVD|BD)|
                              (?<webdl>WEB[-_. ]DL|WEBDL|WebRip|iTunesHD|WebHD)|
                              (?<hdtv>HDTV)|
                              (?<bdrip>BDRiP)|
                              (?<brrip>BRRip)|
                              (?<dvd>DVD|DVDRip|NTSC|PAL|xvidvd)|
                              (?<dsr>WS[-_. ]DSR|DSR)|
                              (?<pdtv>PDTV)|
                              (?<sdtv>SDTV)
                              )(\b|_)",
                             RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace)) { }

        public override bool IsContent(string item, ParsedInfo parsedInfo, out string[] notParsed)
        {
            string[] parsedItems;
            bool ret = IsContent(item, out parsedItems, out notParsed);
            if (ret)
            {
                foreach (var param in parsedItems)
                {
                    Console.Out.WriteLine("Item: {0}, Detected Source: {0}", item, param);
                    ParsedInfo.AddItem(param, parsedInfo.Source);
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
