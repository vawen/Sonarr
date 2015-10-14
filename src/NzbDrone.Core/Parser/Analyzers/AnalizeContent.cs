using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace NzbDrone.Core.Parser.Analyzers
{
    public interface IAnalyzeContent
    {
        bool IsContent(ParsedItem item, ParsedInfo parsedInfo, out ParsedItem[] notParsed);
    }

    public abstract class AnalyzeContent : IAnalyzeContent
    {

        public AnalyzeContent(Regex regex)
        {
            RegexArray = new Regex[] { regex };
        }

        public AnalyzeContent(Regex[] regex)
        {
            RegexArray = regex;
        }

        protected Regex[] RegexArray { get; set; }

        public abstract bool IsContent(ParsedItem item, ParsedInfo parsedInfo, out ParsedItem[] notParsed);

        public bool IsContent(ParsedItem item, out ParsedItem[] parsedItems, out ParsedItem[] notParsed)
        {
            foreach (var Regex in RegexArray)
            {
                if (Regex.IsMatch(item.Value))
                {
                    var _parsedItems = new List<ParsedItem>();
                    var _splitInfo = new List<ParsedItem>();
                    var regexMatch = Regex.Matches(item.Value);

                    foreach (Match match in regexMatch)
                    {
                        var parsedItem = new ParsedItem
                            {
                                Value = match.Value,
                                Length = match.Length,
                                Position = item.Position + match.Index,
                                GlobalLength = item.GlobalLength,
                                Group = item.Group
                            };
                        _parsedItems.Add(parsedItem);
                        _splitInfo.AddRange(item.Split(parsedItem));
                    }

                    notParsed = _splitInfo.ToArray();
                    parsedItems = _parsedItems.ToArray();
                    return true;
                }
            }
            parsedItems = null;
            notParsed = null;
            return false;
        }
    }
}
