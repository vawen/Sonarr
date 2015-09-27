using System;
using System.Linq;
using System.Text.RegularExpressions;
using NzbDrone.Common.Extensions;

namespace NzbDrone.Core.Parser
{
    public static class ParserExtensions
    {
        private static readonly Regex NormalizeRegex = new Regex(@"((?:\b|_)(?<!^)(a(?!$)|an|the|and|or|of)(?:\b|_))|\W|_",
                                                        RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex WordDelimiterRegex = new Regex(@"(\s|\.|,|_|-|=|\|)+", RegexOptions.Compiled);
        private static readonly Regex PunctuationRegex = new Regex(@"[^\w\s]", RegexOptions.Compiled);
        private static readonly Regex CommonWordRegex = new Regex(@"\b(a|an|the|and|or|of)\b\s?", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex DuplicateSpacesRegex = new Regex(@"\s{2,}", RegexOptions.Compiled);
        private static readonly Regex SpecialEpisodeWordRegex = new Regex(@"\b(part|special|edition|christmas)\b\s?", RegexOptions.IgnoreCase | RegexOptions.Compiled);


        public static string CleanSeriesTitle(this string title)
        {
            long number = 0;

            //If Title only contains numbers return it as is.
            if (Int64.TryParse(title, out number))
                return title;

            return NormalizeRegex.Replace(title, String.Empty).ToLower().RemoveAccent();
        }



        public static string NormalizeTitle(this string title)
        {
            title = WordDelimiterRegex.Replace(title, " ");
            title = PunctuationRegex.Replace(title, String.Empty);
            title = CommonWordRegex.Replace(title, String.Empty);
            title = DuplicateSpacesRegex.Replace(title, " ");

            return title.Trim().ToLower();
        }

        public static string NormalizeEpisodeTitle(this string title)
        {
            title = SpecialEpisodeWordRegex.Replace(title, String.Empty);
            title = PunctuationRegex.Replace(title, " ");
            title = DuplicateSpacesRegex.Replace(title, " ");

            return title.Trim()
                        .ToLower();
        }

        private static readonly Regex FileExtensionRegex = new Regex(@"\.[a-z0-9]{2,4}$",
                                                        RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public static string RemoveFileExtension(this string title)
        {
            title = FileExtensionRegex.Replace(title, m =>
            {
                var extension = m.Value.ToLower();
                if (MediaFiles.MediaFileExtensions.Extensions.Contains(extension) || new[] { ".par2", ".nzb" }.Contains(extension))
                {
                    return String.Empty;
                }
                return m.Value;
            });

            return title;
        }
    }
}
