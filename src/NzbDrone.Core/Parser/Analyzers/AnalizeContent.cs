using System.Collections.Generic;
using System.Text.RegularExpressions;
using NLog;

namespace NzbDrone.Core.Parser.Analyzers
{
    public interface IAnalyzeContent
    {
        bool IsContent(ParsedItem item, ParsedInfo parsedInfo, out ParsedItem[] notParsed);
    }

    public abstract class AnalyzeContent : IAnalyzeContent
    {
        protected InfoCategory Category;
        private readonly Logger _logger;
        protected Regex[] RegexArray { get; set; }

        public AnalyzeContent(Regex regex, Logger logger)
        {
            RegexArray = new Regex[] { regex };
            _logger = logger;
            Category = InfoCategory.Unknown;
        }

        public AnalyzeContent(Regex[] regex, Logger logger)
        {
            RegexArray = regex;
            _logger = logger;
            Category = InfoCategory.Unknown;
        }

        public virtual bool IsContent(ParsedItem item, ParsedInfo parsedInfo, out ParsedItem[] notParsed)
        {
            ParsedItem[] parsedItems;
            var ret = IsContent(item, out parsedItems, out notParsed);
            if (!ret)
            {
                return false;
            }
            foreach (var param in parsedItems)
            {
                _logger.Debug("Detected {0}", param);
                parsedInfo.AddItem(param.Trim());
            }
            return true;
        }

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
                                Group = item.Group,
                                Category = Category
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
