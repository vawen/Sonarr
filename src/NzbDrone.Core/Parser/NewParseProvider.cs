
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NLog;
using NzbDrone.Core.Parser.Analizers;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Tv;
namespace NzbDrone.Core.Parser
{
    public class NewParseProvider
    {
        private IEnumerable<IAnalizeContent> _analizers;
        private readonly ISeriesService _seriesService;
        private readonly IEpisodeService _episodeService;
        private readonly Logger _logger;

        private static readonly Regex ReversedTitleRegex = new Regex(@"[-._ ](p027|p0801|\d{2}E\d{2}S)[-._ ]", RegexOptions.Compiled);
        private static readonly Regex RequestInfoRegex = new Regex(@"(?:\[(?<data>.+?)\])", RegexOptions.Compiled);
        private static readonly Regex RequestInfoRegex2 = new Regex(@"(?:\((?<data>.+?)\))", RegexOptions.Compiled);
        private static readonly Regex ReleaseGroup = new Regex(@"^\W*(?<ReleaseGroup>(\w|-)+)\W*$", RegexOptions.Compiled);

        public NewParseProvider(ISeriesService seriesService, IEpisodeService episodeService, IEnumerable<IAnalizeContent> analizers, Logger logger)
        {
            _seriesService = seriesService;
            _episodeService = episodeService;
            _analizers = analizers;
            _logger = logger;
        }

        public void SetAnalizers(IEnumerable<IAnalizeContent> analizers)
        {
            _analizers = analizers;
        }

        public ParsedEpisodeInfo ParseTitle(string title)
        {
            List<string> unknownInfo;
            var parsedInfo = InternalParse(title, out unknownInfo);

            if (parsedInfo.IsEmpty())
            {
                parsedInfo = InternalParse(title.NormalizeTitle(), out unknownInfo);
            }

            if (parsedInfo.IsEmpty()) return null;

            ParsedEpisodeInfo parsedEpisodeInfo = new ParsedEpisodeInfo();

            // Hash
            if (parsedInfo.Hash.Count > 0)
            {
                parsedEpisodeInfo.ReleaseHash = parsedInfo.Hash[0];
            }

            // ReleaseGroup
            if (parsedInfo.ReleaseGroup.Count > 0)
            {
                parsedEpisodeInfo.ReleaseGroup = parsedInfo.ReleaseGroup[0];
            }

            // Language
            parsedEpisodeInfo.Language = ParseLanguage(parsedInfo);

            // Special
            parsedEpisodeInfo.Special = parsedInfo.Special.Any();

            // Series Title
            parsedEpisodeInfo.SeriesTitle = parsedInfo.Series == null ? "" : parsedInfo.Series.Title;


            // Analize episode and season
            if (parsedInfo.Series == null || parsedInfo.Series.SeriesType == SeriesTypes.Daily)
            {
                var airDate = DateTime.Today;
                var asigned = false;

                foreach (var newDate in parsedInfo.Daily)
                {
                    var newYear = 0;
                    var newMonth = 0;
                    var newDay = 0;
                    var airDateMatch = AnalizeDaily.AirDateRegex.Match(newDate);
                    var sixDigitAirDateMatch = AnalizeDaily.SixDigitAirDateRegex.Match(newDate);

                    if (airDateMatch.Success)
                    {
                        Int32.TryParse(airDateMatch.Groups["airyear"].Value, out newYear);
                        Int32.TryParse(airDateMatch.Groups["airmonth"].Value, out newMonth);
                        Int32.TryParse(airDateMatch.Groups["airday"].Value, out newDay);
                    }
                    else if (sixDigitAirDateMatch.Success)
                    {
                        Int32.TryParse("20" + sixDigitAirDateMatch.Groups["airyear"].Value, out newYear);
                        Int32.TryParse(sixDigitAirDateMatch.Groups["airmonth"].Value, out newMonth);
                        Int32.TryParse(sixDigitAirDateMatch.Groups["airday"].Value, out newDay);
                    }

                    //Swap day and month if month is bigger than 12 (scene fail)
                    if (newMonth > 12)
                    {
                        var tempDay = newDay;
                        newDay = newMonth;
                        newMonth = tempDay;
                    }

                    if (newYear <= 1900 || newMonth <= 0 || newDay <= 0)
                    {
                        continue;
                    }

                    var newAirDate = new DateTime(newYear, newMonth, newDay);

                    if (!asigned)
                    {
                        airDate = newAirDate;
                        asigned = true;
                    }
                    else if (airDate < newAirDate)
                    {
                        // 2 Dates, get the more recent one
                        airDate = newAirDate;
                    }
                }

                if (asigned)
                {
                    parsedEpisodeInfo.AirDate = airDate.ToString(Episode.AIR_DATE_FORMAT);
                }
            }
            if (parsedInfo.Series == null || parsedInfo.Series.SeriesType == SeriesTypes.Anime)
            {
                // Parse absolute episodes
                List<List<Capture>> listAbsoluteEpisodeCaptures = new List<List<Capture>>();

                foreach (var item in parsedInfo.AbsoluteEpisodeNumber)
                {
                    var matchCollection = AnalizeAbsoluteEpisodeNumber.SimpleAbsoluteNumber.Matches(item);
                    var itemCaptures = new List<Capture>();

                    foreach (Match match in matchCollection)
                    {
                        itemCaptures.AddRange(match.Groups["absoluteepisode"].Captures.Cast<Capture>());
                    }
                    listAbsoluteEpisodeCaptures.Add(itemCaptures);
                }

                List<Capture> absoluteEpisodeCaptures;

                if (listAbsoluteEpisodeCaptures.Count == 0)
                {
                    absoluteEpisodeCaptures = new List<Capture>();
                }
                else if (listAbsoluteEpisodeCaptures.Count == 1)
                {
                    absoluteEpisodeCaptures = listAbsoluteEpisodeCaptures.First();
                }
                else if (!listAbsoluteEpisodeCaptures.Any(l => l.Count > 1))
                {
                    // if all are single we may have different ids for the same episode
                    // TODO: Check which one is episode and which one is global, for now we supose that the lowest one is the episode
                    listAbsoluteEpisodeCaptures.OrderBy(l => l.First());
                    absoluteEpisodeCaptures = listAbsoluteEpisodeCaptures.First();
                }
                else
                {
                    // Use the first multiple
                    absoluteEpisodeCaptures = listAbsoluteEpisodeCaptures.Where(l => l.Count > 1).First();
                }

                absoluteEpisodeCaptures.OrderByDescending(e => Int32.Parse(e.Value));
                if (absoluteEpisodeCaptures.Any())
                {
                    var first = Convert.ToInt32(absoluteEpisodeCaptures.First().Value);
                    var last = Convert.ToInt32(absoluteEpisodeCaptures.Last().Value);

                    if (first > last)
                    {
                        return null;
                    }

                    var count = last - first + 1;
                    parsedEpisodeInfo.AbsoluteEpisodeNumbers = Enumerable.Range(first, count).ToArray();
                }
                else if (parsedInfo.Year.Count == 1)
                {
                    int year = Int32.Parse(parsedInfo.Year[0]);
                    int first = 0;
                    int last = 0;
                    var episodes = _episodeService.GetEpisodeBySeries(parsedInfo.Series.Id);
                    foreach (var episode in episodes)
                    {
                        DateTime airDate;
                        if (DateTime.TryParse(episode.AirDate, out airDate))
                        {
                            if (airDate.Year == year && episode.AbsoluteEpisodeNumber.HasValue)
                            {
                                if (episode.AbsoluteEpisodeNumber.Value < first || first == 0)
                                    first = episode.AbsoluteEpisodeNumber.Value;
                                if (episode.AbsoluteEpisodeNumber.Value > last)
                                    last = episode.AbsoluteEpisodeNumber.Value;
                            }
                        }
                    }
                    if (first != 0)
                    {
                        var count = last - first + 1;
                        parsedEpisodeInfo.AbsoluteEpisodeNumbers = Enumerable.Range(first, count).ToArray();
                    }
                }
            }

            if (parsedInfo.Series == null || parsedInfo.Series.SeriesType == SeriesTypes.Anime || parsedInfo.Series.SeriesType == SeriesTypes.Standard)
            {
                if (parsedInfo.Season.Count > 0)
                {
                    List<int> seasonNumber = new List<int>();
                    List<List<int>> episodeNumbers = new List<List<int>>();

                    foreach (var item in parsedInfo.Season)
                    {
                        var matchCollection = AnalizeSeason.SimpleSeason.Matches(item);
                        var seasons = new List<int>();

                        foreach (Match match in matchCollection)
                        {
                            foreach (Capture seasonCapture in match.Groups["season"].Captures)
                            {
                                int parsedSeason;
                                if (Int32.TryParse(seasonCapture.Value, out parsedSeason))
                                    seasons.Add(parsedSeason);
                            }
                        }

                        //If no season was found it should be treated as a mini series and season 1
                        if (seasons.Count == 0) seasons.Add(1);

                        //If more than 1 season was parsed go to the next REGEX (A multi-season release is unlikely)
                        if (seasons.Distinct().Count() > 1) return null;

                        seasonNumber.Add(seasons.First());

                        foreach (Match match in matchCollection)
                        {
                            var episodeCaptures = match.Groups["episode"].Captures.Cast<Capture>().ToList();
                            if (episodeCaptures.Any())
                            {
                                var first = Convert.ToInt32(episodeCaptures.First().Value);
                                var last = Convert.ToInt32(episodeCaptures.Last().Value);

                                if (first > last)
                                {
                                    return null;
                                }

                                var count = last - first + 1;
                                episodeNumbers.Add(Enumerable.Range(first, count).ToList());
                            }
                        }

                        // Miniseries
                        foreach (Match match in AnalizeSeason.SimpleMiniSerie.Matches(item))
                        {
                            var episodeCaptures = match.Groups["episode"].Captures.Cast<Capture>().ToList();
                            if (episodeCaptures.Any())
                            {
                                var first = Convert.ToInt32(episodeCaptures.First().Value);
                                var last = Convert.ToInt32(episodeCaptures.Last().Value);

                                if (first > last)
                                {
                                    return null;
                                }

                                var count = last - first + 1;
                                episodeNumbers.Add(Enumerable.Range(first, count).ToList());
                            }
                        }
                    }

                    //If more than 1 season was parsed go to the next REGEX (A multi-season release is unlikely)
                    if (seasonNumber.Distinct().Count() > 1)
                        return null;

                    parsedEpisodeInfo.SeasonNumber = seasonNumber.First();

                    if (episodeNumbers.Count == 1)
                    {
                        parsedEpisodeInfo.EpisodeNumbers = episodeNumbers.First().ToArray();
                    }
                    else if (episodeNumbers.Count > 1)
                    {
                        // One of the parsed item is multiepisode use that one
                        if (episodeNumbers.Count(col => col.Count > 1) == 1)
                        {
                            parsedEpisodeInfo.EpisodeNumbers = episodeNumbers.Where(col => col.Count > 1).First().ToArray();
                        }
                    }
                }
            }

            return parsedEpisodeInfo;
        }

        private Language ParseLanguage(ParsedInfo info)
        {
            var ret = Language.Unknown;

            foreach (var item in info.Language)
            {
                var newLang = AnalizeLanguage(item);
                if (ret == Language.Unknown)
                    ret = newLang;
                if (newLang != ret)
                {
                    // TODO: Maybe a multi language item, return higher in priority
                    if (ret != Language.English)
                        ret = newLang;
                }
            }

            if (ret == Language.Unknown)
                return Language.English;
            return ret;
        }

        public Language ParseLanguage(string title)
        {
            List<string> UnknownInfo;
            ParsedInfo info = InternalParse(title, out UnknownInfo);
            return ParseLanguage(info);
        }

        private ParsedInfo InternalParse(string title, out List<string> UnknownInfo)
        {
            ParsedInfo myParsedInfo = new ParsedInfo();

            if (ReversedTitleRegex.IsMatch(title))
            {
                var titleWithoutExtension = title.RemoveFileExtension().ToCharArray();
                Array.Reverse(titleWithoutExtension);

                title = new String(titleWithoutExtension) + title.Substring(titleWithoutExtension.Length);

                _logger.Debug("Reversed name detected. Converted to '{0}'", title);
            }

            // Info that we couldn't parse
            UnknownInfo = new List<string>();

            // Info that has been parsed at least once
            var ParsedInfo = new List<string>();

            var PendingInfo = new List<string>();
            foreach (var part in RequestInfoRegex.Split(title).Where(s => s.Length > 0 && s.Any(char.IsLetterOrDigit)))
            {
                RequestInfoRegex2.Split(part).Where(s => s.Length > 0 && s.Any(char.IsLetterOrDigit)).ToList().ForEach(s => PendingInfo.Add(s));
            }

            // We generate new pending info in each iteration
            var NewPendingInfo = new List<string>();

            do
            {
                NewPendingInfo.Clear();
                foreach (var info in PendingInfo)
                {
                    string[] remains = null;
                    bool gotAnyResult = false;
                    foreach (var analizer in _analizers)
                    {
                        var gotResult = analizer.IsContent(info, myParsedInfo, out remains);
                        if (gotResult)
                        {
                            if (!ParsedInfo.Contains(info))
                                ParsedInfo.Add(info);
                            foreach (var str in remains)
                            {
                                NewPendingInfo.Add(str);
                            }
                        }
                        gotAnyResult |= gotResult;
                    }
                    if (!gotAnyResult)
                    {
                        if (!UnknownInfo.Contains(info))
                            UnknownInfo.Add(info);
                    }
                }
                PendingInfo.Clear();
                NewPendingInfo.ForEach(s => { if (!PendingInfo.Any(p => p.Contains(s))) PendingInfo.Add(s); });
            } while (NewPendingInfo.Any());

            foreach (var str in UnknownInfo)
            {
                _logger.Debug("Unknown Info: {0}", str);
            }

            // Removes duplicate items from AbsoluteEpisodeNumber in Season
            foreach (var res in myParsedInfo.AbsoluteEpisodeNumber)
            {
                // If Season Contains the AbsoluteEpisodeNumber string and there is no more useful chars, remove it
                myParsedInfo.Season.Where(s => s.Contains(res) && !s.Replace(res, String.Empty).Any(char.IsLetterOrDigit))
                    .ToList()
                    .ForEach(s => myParsedInfo.Season.Remove(s));
            }

            // Removes ResolutionInfo from Season and Hash
            foreach (var res in myParsedInfo.Resolution)
            {
                // If Season Contains the Resolution string and there is no more useful chars, remove it from season
                myParsedInfo.Season.Where(s => s.Contains(res) && !s.Replace(res, String.Empty).Any(char.IsLetterOrDigit))
                    .ToList()
                    .ForEach(s => myParsedInfo.Season.Remove(s));

                // If Resolution Contains the Season string and there is no more useful chars, remove it from season
                myParsedInfo.Season.Where(s => res.Contains(s) && !res.Replace(s, String.Empty).Any(char.IsLetterOrDigit))
                    .ToList()
                    .ForEach(s => myParsedInfo.Season.Remove(s));

                // If Hash Contains the Resolution string and there is no more useful chars, remove it from hash
                myParsedInfo.Hash.Where(s => res.Length == 8 && s.Contains(res) && !res.Replace(s, String.Empty).Any(char.IsLetterOrDigit))
                    .ToList()
                    .ForEach(s => myParsedInfo.Hash.Remove(s));
            }

            // If codec match as absoluteepisodenumber, remove it
            foreach (var codec in myParsedInfo.Codec)
            {
                ParsedInfo tempInfo = new ParsedInfo();
                string[] notParsed;
                if (new AnalizeAbsoluteEpisodeNumber().IsContent(codec, tempInfo, out notParsed))
                {
                    foreach (var res in tempInfo.AbsoluteEpisodeNumber)
                        myParsedInfo.AbsoluteEpisodeNumber.Where(s => s.Contains(res) || res.Contains(s)).ToList().ForEach(s => myParsedInfo.AbsoluteEpisodeNumber.Remove(s));
                }
            }

            // Try to find the series name

            string titleToRemove = "";

            foreach (var unk in UnknownInfo)
            {
                var _title = unk.NormalizeTitle();
                var newSerie = _seriesService.FindByTitle(_title);
                if (newSerie == null)
                {
                    foreach (var _year in myParsedInfo.Year)
                    {
                        _title = String.Format("{0} ({1})", unk, _year).NormalizeTitle();
                        newSerie = _seriesService.FindByTitle(_title);
                        if (newSerie == null)
                        {
                            _title = String.Format("{0} {1}", _year, unk).NormalizeTitle();
                            newSerie = _seriesService.FindByTitle(_title);
                        }
                        if (newSerie != null)
                        {
                            myParsedInfo.Year.Remove(_year);
                            break;
                        }
                    }
                }
                if (newSerie == null)
                {
                    foreach (var _episode in myParsedInfo.AbsoluteEpisodeNumber)
                    {
                        _title = String.Format("{0} {1}", unk, _episode).NormalizeTitle();
                        newSerie = _seriesService.FindByTitle(_title);
                        if (newSerie == null)
                        {
                            _title = String.Format("{0} {1}", _episode, unk).NormalizeTitle();
                            newSerie = _seriesService.FindByTitle(_title);
                        }
                        if (newSerie != null)
                        {
                            myParsedInfo.AbsoluteEpisodeNumber.Remove(_episode);
                            break;
                        }
                    }
                }
                if (newSerie == null)
                {
                    foreach (var _hash in myParsedInfo.Hash)
                    {
                        _title = String.Format("{0} {1}", unk, _hash).NormalizeTitle();
                        newSerie = _seriesService.FindByTitle(_title);
                        if (newSerie == null)
                        {
                            _title = String.Format("{0} {1}", _hash, unk).NormalizeTitle();
                            newSerie = _seriesService.FindByTitle(_title);
                        }
                        if (newSerie != null)
                        {
                            myParsedInfo.Hash.Remove(_hash);
                            break;
                        }
                    }
                }
                if (newSerie != null)
                {
                    titleToRemove = unk;
                    myParsedInfo.Series = newSerie;
                    break;
                }
            }  

            UnknownInfo.Remove(titleToRemove);


            // Release group will be an Unknown item that is only one word
            foreach (var unk in UnknownInfo)
            {
                if (ReleaseGroup.IsMatch(unk))
                {
                    myParsedInfo.ReleaseGroup.Add(ReleaseGroup.Match(unk).Groups["ReleaseGroup"].Value);
                }
            }

            // if series is anime and there is hash, remove it from absoluteepisodenumber
            foreach (var hash in myParsedInfo.Hash)
            {
                myParsedInfo.AbsoluteEpisodeNumber.Where(s => s.Contains(hash))
                    .ToList()
                    .ForEach(s => myParsedInfo.AbsoluteEpisodeNumber.Remove(s));
            }

            return myParsedInfo;
        }

        private Language AnalizeLanguage(string item)
        {
            var lowerTitle = item.ToLower();

            if (lowerTitle.Contains("english"))
                return Language.English;

            if (lowerTitle.Contains("french"))
                return Language.French;

            if (lowerTitle.Contains("spanish"))
                return Language.Spanish;

            if (lowerTitle.Contains("danish"))
                return Language.Danish;

            if (lowerTitle.Contains("dutch"))
                return Language.Dutch;

            if (lowerTitle.Contains("japanese"))
                return Language.Japanese;

            if (lowerTitle.Contains("cantonese"))
                return Language.Cantonese;

            if (lowerTitle.Contains("mandarin"))
                return Language.Mandarin;

            if (lowerTitle.Contains("korean"))
                return Language.Korean;

            if (lowerTitle.Contains("russian"))
                return Language.Russian;

            if (lowerTitle.Contains("polish"))
                return Language.Polish;

            if (lowerTitle.Contains("vietnamese"))
                return Language.Vietnamese;

            if (lowerTitle.Contains("swedish"))
                return Language.Swedish;

            if (lowerTitle.Contains("norwegian"))
                return Language.Norwegian;

            if (lowerTitle.Contains("nordic"))
                return Language.Norwegian;

            if (lowerTitle.Contains("finnish"))
                return Language.Finnish;

            if (lowerTitle.Contains("turkish"))
                return Language.Turkish;

            if (lowerTitle.Contains("portuguese"))
                return Language.Portuguese;

            if (lowerTitle.Contains("hungarian"))
                return Language.Hungarian;

            var match = Analizers.AnalizeLanguage.LanguageRegex.Match(item);

            if (match.Groups["italian"].Captures.Cast<Capture>().Any())
                return Language.Italian;

            if (match.Groups["german"].Captures.Cast<Capture>().Any())
                return Language.German;

            if (match.Groups["flemish"].Captures.Cast<Capture>().Any())
                return Language.Flemish;

            if (match.Groups["greek"].Captures.Cast<Capture>().Any())
                return Language.Greek;

            if (match.Groups["french"].Success)
                return Language.French;

            if (match.Groups["russian"].Success)
                return Language.Russian;

            if (match.Groups["dutch"].Success)
                return Language.Dutch;

            if (match.Groups["hungarian"].Success)
                return Language.Hungarian;

            return Language.English;
        }
    }
}
