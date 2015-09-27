using System;
using System.Text.RegularExpressions;

namespace NzbDrone.Core.Parser.Analizers
{
    public class AnalizeCodec : AnalizeContent
    {

        public AnalizeCodec()
            : base(new Regex(@"(\b|_)(?:(?<x264>x264)|(?<h264>h\.?264)|(?<xvidhd>XvidHD)|(?<xvid>Xvid)|(?<divx>divx))(\b|_)",
                RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace)) { }

        public override bool IsContent(string item, ParsedInfo parsedInfo, out string[] notParsed)
        {
            string[] parsedItems;
            bool ret = IsContent(item, out parsedItems, out notParsed);
            if (ret)
            {
                foreach (var param in parsedItems)
                {
                    Console.Out.WriteLine("Item: {0}, Detected Codec: {0}", item, param);
                    ParsedInfo.AddItem(param, parsedInfo.Codec);
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
