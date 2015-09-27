using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace NzbDrone.Core.Parser.Analizers
{
    public interface IAnalizeContent
    {
        bool IsContent(string item, ParsedInfo parsedInfo, out string[] notParsed);
    }

    public abstract class AnalizeContent : IAnalizeContent
    {

        public AnalizeContent(Regex regex)
        {
            RegexArray = new Regex[] { regex };
        }

        public AnalizeContent(Regex[] regex)
        {
            RegexArray = regex;
        }

        protected Regex[] RegexArray { get; set; }

        public abstract bool IsContent(string item, ParsedInfo parsedInfo, out string[] notParsed);

        public bool IsContent(string item, out string[] parsedInfo, out string[] notParsed)
        {
            foreach (var Regex in RegexArray)
            {
                if (Regex.IsMatch(item))
                {
                    var _parsedInfo = new List<string>();
                    var regexMatch = Regex.Matches(item);
                    var split =
                        Regex.Split(item)
                            .Where(s => s.Length > 0 && s.Any(char.IsLetterOrDigit))
                            .Select(s => s.Trim())
                            .ToList();

                    foreach (var group in regexMatch)
                    {
                        split.Remove(group.ToString());
                        _parsedInfo.Add(group.ToString());
                    }

                    notParsed = split.ToArray();
                    parsedInfo = _parsedInfo.ToArray();
                    return true;
                }
            }
            parsedInfo = null;
            notParsed = null;
            return false;
        }
    }
}
