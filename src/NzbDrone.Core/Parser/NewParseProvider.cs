﻿
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Parser.Analizers;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Tv;

namespace NzbDrone.Core.Parser
{
    public class NewParseProvider// : IParseProvider
    {
        private class SeasonAndEpisode
        {
            public Nullable<int> Season { get; set; }
            public bool isMiniOrSpecial { get; set; }
            public List<int> Episodes { get; set; }
            public bool FullSeason { get; set; }
            public ParsedItem Item { get; set; }

            public SeasonAndEpisode()
            {
                Episodes = new List<int>();
                isMiniOrSpecial = false;
                FullSeason = false;
            }

            public bool Equals(SeasonAndEpisode other)
            {
                if (ReferenceEquals(null, other)) return false;
                if (ReferenceEquals(this, other)) return true;

                return other.Season.Equals(Season) &&
                    other.Episodes.Count == Episodes.Count &&
                    Episodes.All(p => other.Episodes.Contains(p)) &&
                    FullSeason == other.FullSeason;
            }

            public override bool Equals(object other)
            {
                if (ReferenceEquals(null, other)) return false;
                if (ReferenceEquals(this, other)) return true;
                return Equals(other as SeasonAndEpisode);
            }

            public override int GetHashCode()
            {
                int result = 0;
                if (Season.HasValue) result ^= Season.GetHashCode();
                result ^= isMiniOrSpecial.GetHashCode();
                for (int i = 0; i < Episodes.Count; i++)
                    result ^= Episodes[i].GetHashCode();
                result ^= FullSeason.GetHashCode();
                result ^= Item.GetHashCode();

                return result;
            }
        }

        private IEnumerable<IAnalizeContent> _analizers;
        private readonly ISeriesService _seriesService;
        private readonly IEpisodeService _episodeService;
        private readonly IParsingService _parsingService;
        private readonly Logger _logger;

        private static readonly Regex ReversedTitleRegex = new Regex(@"[-._ ](p027|p0801|\d{2}E\d{2}S)[-._ ]", RegexOptions.Compiled);
        private static readonly Regex SplitClaspRegex = new Regex(@"(?:\[(?<data>.+?)\])", RegexOptions.Compiled);
        private static readonly Regex SplitSeparatorsRegex = new Regex(@"(?:[._\s])", RegexOptions.Compiled);
        private static readonly Regex SplitParenthesisRegex = new Regex(@"(?:\((?<data>.+?)\))", RegexOptions.Compiled);
        private static readonly Regex SplitHumanReadableRegex = new Regex(@"(?:(\s[-._]\s)|(_[-.]_))", RegexOptions.Compiled);
        private static readonly Regex ReleaseGroup = new Regex(@"^\W*(\w{2,}-)?(?<ReleaseGroup>(\w|-){3,})\W*$", RegexOptions.Compiled);

        private static readonly Regex HighDefPdtvRegex = new Regex(@"hr[-_. ]ws", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex AnimeBlurayRegex = new Regex(@"bd(?:720|1080)|(?<=[-_. (\[])bd(?=[-_. )\]])", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex[] RejectHashedReleasesRegex = new Regex[]
        {
            // Generic match for md5 and mixed-case hashes.
            new Regex(@"^[0-9a-zA-Z]{32}", RegexOptions.Compiled),
                
            // Generic match for shorter lower-case hashes.
            new Regex(@"^[a-z0-9]{24}$", RegexOptions.Compiled),

            // Format seen on some NZBGeek releases
            // Be very strict with these coz they are very close to the valid 101 ep numbering.
            new Regex(@"^[A-Z]{11}\d{3}$", RegexOptions.Compiled),
            new Regex(@"^[a-z]{12}\d{3}$", RegexOptions.Compiled),

            //Backup filename (Unknown origins)
            new Regex(@"^Backup_\d{5,}S\d{2}-\d{2}$", RegexOptions.Compiled),

            //123 - Started appearing December 2014
            new Regex(@"^123$", RegexOptions.Compiled),

            //abc - Started appearing January 2015
            new Regex(@"^abc$", RegexOptions.Compiled | RegexOptions.IgnoreCase),

            //b00bs - Started appearing January 2015
            new Regex(@"^b00bs$", RegexOptions.Compiled | RegexOptions.IgnoreCase)
        };

        public NewParseProvider(ISeriesService seriesService,
                                IEpisodeService episodeService,
                                IEnumerable<IAnalizeContent> analizers,
                                IParsingService parsingService,
                                Logger logger)
        {
            _seriesService = seriesService;
            _episodeService = episodeService;
            _parsingService = parsingService;
            _analizers = analizers;
            _logger = logger;
        }

        #region IParseProvider
        public string ParseReleaseGroup(string title)
        {
            var info = ParseTitle(title);
            return (info != null) ? info.ReleaseGroup : "";
        }

        public Language ParseLanguage(string title)
        {
            List<ParsedItem> UnknownInfo;
            ParsedInfo info = InternalParse(title, out UnknownInfo, null, null);
            return ParseLanguage(info);
        }

        public ParsedEpisodeInfo ParsePath(string path)
        {
            var fileInfo = new FileInfo(path);

            var result = ParseTitle(fileInfo.Name);

            if (result == null)
            {
                _logger.Debug("Attempting to parse episode info using directory and file names. {0}", fileInfo.Directory.Name);
                // Why add extension? it is already on FileInfo.Name
                result = ParseTitle(fileInfo.Directory.Name + " " + fileInfo.Name);// + fileInfo.Extension); 
            }

            if (result == null)
            {
                _logger.Debug("Attempting to parse episode info using directory name. {0}", fileInfo.Directory.Name);
                result = ParseTitle(fileInfo.Directory.Name + fileInfo.Extension);
            }

            if (result == null)
            {
                _logger.Warn("Unable to parse episode info from path {0}", path);
                return null;
            }

            return result;
        }

        public ParsedEpisodeInfo ParseTitle(string title)
        {
            _logger.Debug("Parsing: {0}", title);

            if (!ValidateBeforeParsing(title)) return null;

            List<ParsedItem> unknownInfo;
            var parsedInfo = InternalParse(title, out unknownInfo, null, null);

            if (parsedInfo.IsEmpty())
            {
                parsedInfo = InternalParse(title.NormalizeTitle(), out unknownInfo, null, null);
            }

            foreach (var unk in unknownInfo)
            {
                _logger.Debug("Unknown Item: {0}", unk);
            }

            if (parsedInfo.IsEmpty()) return null;

            ParsedEpisodeInfo parsedEpisodeInfo = new ParsedEpisodeInfo();

            // Hash
            if (parsedInfo.Hash.Count > 0)
            {
                parsedEpisodeInfo.ReleaseHash = parsedInfo.Hash[0].Value;
            }

            // ReleaseGroup
            if (parsedInfo.ReleaseGroup.Count > 0)
            {
                // If group start with - probably it is this one
                var item = parsedInfo.ReleaseGroup.Where(p => p.Value.Trim().StartsWith("-"));
                if (item.Any())
                {
                    parsedEpisodeInfo.ReleaseGroup = ReleaseGroup.Match(item.First().Value.Trim()).Groups["ReleaseGroup"].Value;
                }
                else
                {
                    parsedEpisodeInfo.ReleaseGroup = ReleaseGroup.Match(parsedInfo.ReleaseGroup.First().Value.Trim()).Groups["ReleaseGroup"].Value;
                }
            }

            // Language
            parsedEpisodeInfo.Language = ParseLanguage(parsedInfo);

            // Special
            parsedEpisodeInfo.Special = parsedInfo.Special.Any();

            // Series Title
            parsedEpisodeInfo.SeriesTitle = parsedInfo.Series == null ? "" : parsedInfo.Series.Title;

            // Daily
            if (parsedInfo.Series == null || parsedInfo.Series.SeriesType == SeriesTypes.Daily)
            {
                if (!ExtractDaily(parsedInfo, parsedEpisodeInfo) && parsedInfo.Series != null)
                {
                    return null;
                }
            }

            // Absolute episode
            if (parsedInfo.Series == null || parsedInfo.Series.SeriesType == SeriesTypes.Anime)
            {
                if (parsedInfo.AbsoluteEpisodeNumber.Count > 0)
                {
                    // Remove subsets
                    var subset = parsedInfo.AbsoluteEpisodeNumber.Where(s => parsedInfo.AbsoluteEpisodeNumber.Any(k => !k.Equals(s) && k.Contains(s))).ToList();
                    parsedInfo.AbsoluteEpisodeNumber.RemoveAll(s => parsedInfo.AbsoluteEpisodeNumber.Any(k => !k.Equals(s) && k.Contains(s)));

                    if (!ExtractAbsoluteEpisodeNumber(parsedInfo, parsedEpisodeInfo))
                    {
                        parsedInfo.AbsoluteEpisodeNumber = subset;
                        ExtractAbsoluteEpisodeNumber(parsedInfo, parsedEpisodeInfo);
                    }
                }
            }

            // Season and Episode
            if (parsedInfo.Series == null || parsedInfo.Series.SeriesType == SeriesTypes.Anime || parsedInfo.Series.SeriesType == SeriesTypes.Standard)
            {
                if (parsedInfo.Season.Count > 0)
                {
                    // Remove subsets
                    var subset = parsedInfo.Season.Where(s => parsedInfo.Season.Any(k => !k.Equals(s) && k.Contains(s))).ToList();
                    parsedInfo.Season.RemoveAll(s => parsedInfo.Season.Any(k => !k.Equals(s) && k.Contains(s)));

                    if (!ExtractSeasonAndEpisode(parsedInfo, parsedEpisodeInfo))
                    {
                        parsedInfo.Season = subset;
                        ExtractSeasonAndEpisode(parsedInfo, parsedEpisodeInfo);
                    }

                    if (parsedEpisodeInfo.FullSeason)
                    {
                        foreach (var unk in unknownInfo)
                        {
                            if (unk.Value.NormalizeTitle().IndexOf("subpack") >= 0)
                                return null;
                            if (unk.Value.NormalizeTitle().IndexOf("extras") >= 0)
                                return null;
                        }
                    }
                }
            }

            // Quality
            parsedEpisodeInfo.Quality = ExtractQuality(title, parsedInfo);


            return parsedEpisodeInfo;
        }
        #endregion

        #region Internal parsing
        private bool ValidateBeforeParsing(string title)
        {
            if (title.ToLower().Contains("password") && title.ToLower().Contains("yenc"))
            {
                _logger.Debug("");
                return false;
            }

            if (!title.Any(Char.IsLetterOrDigit))
            {
                return false;
            }

            var titleWithoutExtension = title.RemoveFileExtension();

            if (RejectHashedReleasesRegex.Any(v => v.IsMatch(titleWithoutExtension)))
            {
                _logger.Debug("Rejected Hashed Release Title: " + title);
                return false;
            }

            return true;
        }

        private string[] PrepareTitle(string title)
        {
            var preparedTitle = PrepareItems(title);
            var splits = new List<string>();
            foreach (var item in preparedTitle)
            {
                var subSplit = SplitSeparatorsRegex.Split(item.Value).Where(s => s.Length > 0);
                splits.AddRange(subSplit);
            }
            return splits.ToArray();
        }

        private List<ParsedItem> PrepareItems(string title)
        {
            var ret = new List<ParsedItem>();
            var splitClaps = SplitClaspRegex.Split(title).Where(s => s.Length > 0);
            int pos = 0;
            foreach (var item in splitClaps)
            {
                var splitP = SplitParenthesisRegex.Split(item).Where(s => s.Length > 0);
                foreach (var inner in splitP)
                {
                    if (inner.Any(char.IsLetterOrDigit))
                    {
                        var parsedItem = new ParsedItem { Value = inner.Trim(), Length = inner.Length, Position = pos };
                        ret.Add(parsedItem);
                    }
                    pos += inner.Length;
                }
            }
            // Analize separators?
            /*var itemsToAdd = new List<ParsedItem>();
            var itemsToRemove = new List<ParsedItem>();
            foreach (var item in ret)
            {
                pos = item.Position;
                var splitH = SplitHumanReadableRegex.Split(item.Value).Where(s => s.Length > 0);
                foreach (var inner in splitH)
                {
                    if (inner.Any(char.IsLetterOrDigit))
                    {
                        var parsedItem = new ParsedItem { Value = inner.Trim(), Length = inner.Length, Position = pos };
                        itemsToAdd.Add(parsedItem);
                    }
                    pos += inner.Length;
                }
            }

            ret.Clear();
            ret.AddRange(itemsToAdd);*/
            ret.ForEach(p => p.GlobalLength = pos);

            return ret;
        }

        private ParsedInfo InternalParse(string title, out List<ParsedItem> unknownItems, Series serie, string seriesTitle)
        {
            var myParsedInfo = new ParsedInfo();
            myParsedInfo.Series = serie;
            myParsedInfo.SeriesTitle = seriesTitle;

            // Info that we couldn't parse
            unknownItems = new List<ParsedItem>();
            // Info that has been parsed at least once
            var parsedItems = new List<ParsedItem>();

            if (ReversedTitleRegex.IsMatch(title))
            {
                var titleWithoutExtension = title.RemoveFileExtension().ToCharArray();
                Array.Reverse(titleWithoutExtension);

                title = new String(titleWithoutExtension) + title.Substring(titleWithoutExtension.Length);

                _logger.Debug("Reversed name detected. Converted to '{0}'", title);
            }

            var pendingItems = PrepareItems(title);

            // We generate new pending info in each iteration
            var newPendingItems = new List<ParsedItem>();

            do
            {
                newPendingItems.Clear();
                foreach (var item in pendingItems)
                {
                    ParsedItem[] remains = null;
                    bool gotAnyResult = false;
                    foreach (var analizer in _analizers)
                    {
                        var gotResult = analizer.IsContent(item, myParsedInfo, out remains);
                        if (gotResult)
                        {
                            if (!parsedItems.Contains(item))
                                parsedItems.Add(item);
                            foreach (var remainItem in remains)
                            {
                                if (remainItem.Length > 0)
                                    newPendingItems.Add(remainItem);
                            }
                        }
                        gotAnyResult |= gotResult;
                    }
                    if (!gotAnyResult)
                    {
                        if (!unknownItems.Contains(item))
                            unknownItems.Add(item);
                    }
                }
                pendingItems.Clear();
                //TODO: This doesn't cut it
                newPendingItems.ForEach(s =>
                {
                    s.Trim();
                    if (!pendingItems.Any(p => p.Contains(s)))
                    {
                        pendingItems.Add(s);
                    }
                });
            } while (newPendingItems.Any());

            // Removes ResolutionInfo from Season and Hash
            foreach (var resolution in myParsedInfo.Resolution)
            {
                // If Season Contains the Resolution item, remove it from season
                myParsedInfo.Season.RemoveAll(season => season.Contains(resolution));

                // If Resolution Contains the Season string and there is no more useful chars, remove it from season
                myParsedInfo.Season.RemoveAll(season => resolution.Contains(season));

                // If Hash Contains the Resolution string and there is no more useful chars, remove it from hash
                myParsedInfo.Hash.RemoveAll(hash => resolution.Length == 8 && hash.Contains(resolution));
            }

            // If codec match as absoluteepisodenumber or season, remove it

            foreach (var codec in myParsedInfo.Codec)
            {
                myParsedInfo.Season.RemoveAll(season => season.Contains(codec));
                myParsedInfo.Season.RemoveAll(season => codec.Contains(season));

                myParsedInfo.AbsoluteEpisodeNumber.RemoveAll(ep => ep.Contains(codec));
                myParsedInfo.AbsoluteEpisodeNumber.RemoveAll(ep => codec.Contains(ep));
            }

            // Try to find the series name
            if (!FindSeries(unknownItems, myParsedInfo))
            {
                var titleSplit = PrepareTitle(title);
                var titlePosition = FindSeries(titleSplit, myParsedInfo);
                if (myParsedInfo.Series != null)
                {
                    unknownItems.Clear();
                    var _newTitle = "";
                    for (var i = 0; i < titleSplit.Length; i++)
                    {
                        if (!titlePosition.Contains(i))
                        {
                            _newTitle = String.Format("{0} {1}", _newTitle, titleSplit[i]);
                        }
                    }
                    return InternalParse(_newTitle.Trim(), out unknownItems, myParsedInfo.Series, myParsedInfo.SeriesTitle);
                }
            }

            // There should be only one file extension
            if (myParsedInfo.FileExtension.Count > 1)
            {
                return new ParsedInfo();
            }

            var lengthWithoutExtension = 0;
            if (myParsedInfo.FileExtension.Count == 1)
            {
                lengthWithoutExtension = myParsedInfo.FileExtension[0].Position;
            }

            // If serie is anime then:
            // - Should be only one hash
            // - Hash should be at the end of the string (there may exist extension)

            ParsedItem realHash = null;
            var containers = new List<ParsedItem>[] { myParsedInfo.Hash };

            foreach (var item in myParsedInfo.Hash)
            {
                if (myParsedInfo.Series != null && myParsedInfo.Series.SeriesType == SeriesTypes.Anime &&
                    ((lengthWithoutExtension != 0 && item.Position + item.Length == lengthWithoutExtension) ||
                    (lengthWithoutExtension == 0 && item.Position + item.Length == item.GlobalLength)))
                {
                    realHash = item;
                }
                else
                {
                    // If myParsedInfo doesn't have this item, add to unknown items
                    if (!myParsedInfo.AnyContains(item, containers))
                    {
                        unknownItems.Add(item);
                    }
                }
            }

            myParsedInfo.Hash.Clear();
            if (realHash != null)
            {
                myParsedInfo.RemoveFromAll(realHash);
                myParsedInfo.Hash.Add(realHash);
            }


            // Clear non char unknownItems

            unknownItems.RemoveAll(u => !u.Value.Any(char.IsLetterOrDigit));

            // Release group will be an Unknown item that is only one word
            foreach (var unk in unknownItems)
            {
                var splitedUnk = SplitSeparatorsRegex.Split(unk.Value);
                var pos = unk.Position;
                foreach (var splited in splitedUnk)
                {
                    if (ReleaseGroup.IsMatch(splited))
                    {
                        //myParsedInfo.ReleaseGroup.Add(ReleaseGroup.Match(unk).Groups["ReleaseGroup"].Value);
                        _logger.Debug("Posible Release Group: {0}", splited);
                        myParsedInfo.ReleaseGroup.Add(new ParsedItem
                        {
                            Value = splited,
                            Position = pos,
                            GlobalLength = unk.GlobalLength,
                            Length = splited.Length
                        });
                    }
                    pos += splited.Length;
                }
            }

            return myParsedInfo;
        }
        #endregion

        #region Quality extraction
        private static Resolution ParseResolution(String name)
        {
            var match = AnalizeResolution.ResolutionRegex.Match(name);

            if (!match.Success) return Resolution.Unknown;
            if (match.Groups["_480p"].Success) return Resolution._480p;
            if (match.Groups["_576p"].Success) return Resolution._576p;
            if (match.Groups["_720p"].Success) return Resolution._720p;
            if (match.Groups["_1080p"].Success) return Resolution._1080p;

            return Resolution.Unknown;
        }

        private QualityModel ExtractQuality(string name, ParsedInfo parsedInfo)
        {
            var result = new QualityModel { Quality = Quality.Unknown };
            var normalizedName = name.Replace('_', ' ').Trim().ToLower();

            foreach (var proper in parsedInfo.Proper)
            {
                var matches = AnalizeProper.ProperRegex.Matches(proper.Value);
                foreach (Match match in matches)
                {
                    if (match.Groups["proper"].Success && result.Revision.Version < 2)
                    {
                        result.Revision.Version = 2;
                    }

                    if (match.Groups["version"].Success)
                    {
                        int newVersion = Convert.ToInt32(match.Groups["version"].Value);
                        if (newVersion > result.Revision.Version)
                        {
                            result.Revision.Version = newVersion;
                        }
                    }
                }
            }

            foreach (var real in parsedInfo.Real)
            {
                var isReal = !parsedInfo.Season.Any(p => p.End >= real.Position) &&
                             !parsedInfo.AbsoluteEpisodeNumber.Any(p => p.End >= real.Position) &&
                             !parsedInfo.Daily.Any(p => p.End >= real.Position);

                if (isReal)
                {
                    result.Revision.Real++;
                }
            }

            if (parsedInfo.RawHD.Any())
            {
                result.Quality = Quality.RAWHD;
                return result;
            }

            var source = parsedInfo.Source.Any() ? parsedInfo.Source[0].Value : "";
            var codec = parsedInfo.Codec.Any() ? parsedInfo.Codec[0].Value : "";

            var sourceMatch = AnalizeSource.SourceRegex.Match(source);
            var codecMatch = AnalizeCodec.CodecRegex.Match(codec);
            var resolution = ParseResolution(parsedInfo.Resolution.Any() ? parsedInfo.Resolution[0].Value : "");

            if (sourceMatch.Groups["bluray"].Success)
            {
                if (codecMatch.Groups["xvid"].Success || codecMatch.Groups["divx"].Success)
                {
                    result.Quality = Quality.DVD;
                    return result;
                }

                if (resolution == Resolution._1080p)
                {
                    result.Quality = Quality.Bluray1080p;
                    return result;
                }

                if (resolution == Resolution._480p || resolution == Resolution._576p)
                {
                    result.Quality = Quality.DVD;
                    return result;
                }

                result.Quality = Quality.Bluray720p;
                return result;
            }

            if (sourceMatch.Groups["webdl"].Success)
            {
                if (resolution == Resolution._1080p)
                {
                    result.Quality = Quality.WEBDL1080p;
                    return result;
                }

                if (resolution == Resolution._720p)
                {
                    result.Quality = Quality.WEBDL720p;
                    return result;
                }

                if (name.Contains("[WEBDL]"))
                {
                    result.Quality = Quality.WEBDL720p;
                    return result;
                }

                result.Quality = Quality.WEBDL480p;
                return result;
            }

            if (sourceMatch.Groups["hdtv"].Success)
            {
                if (resolution == Resolution._1080p)
                {
                    result.Quality = Quality.HDTV1080p;
                    return result;
                }

                if (resolution == Resolution._720p)
                {
                    result.Quality = Quality.HDTV720p;
                    return result;
                }

                if (name.Contains("[HDTV]"))
                {
                    result.Quality = Quality.HDTV720p;
                    return result;
                }

                result.Quality = Quality.SDTV;
                return result;
            }

            if (sourceMatch.Groups["bdrip"].Success ||
                sourceMatch.Groups["brrip"].Success)
            {
                switch (resolution)
                {
                    case Resolution._720p:
                        result.Quality = Quality.Bluray720p;
                        return result;
                    case Resolution._1080p:
                        result.Quality = Quality.Bluray1080p;
                        return result;
                    default:
                        result.Quality = Quality.DVD;
                        return result;
                }
            }

            if (sourceMatch.Groups["dvd"].Success)
            {
                result.Quality = Quality.DVD;
                return result;
            }

            if (sourceMatch.Groups["pdtv"].Success ||
                sourceMatch.Groups["sdtv"].Success ||
                sourceMatch.Groups["dsr"].Success ||
                sourceMatch.Groups["tvrip"].Success)
            {
                if (HighDefPdtvRegex.IsMatch(name))
                {
                    result.Quality = Quality.HDTV720p;
                    return result;
                }

                result.Quality = Quality.SDTV;
                return result;
            }


            //Anime Bluray matching
            if (AnimeBlurayRegex.Match(normalizedName).Success)
            {
                if (resolution == Resolution._480p || resolution == Resolution._576p || normalizedName.Contains("480p"))
                {
                    result.Quality = Quality.DVD;
                    return result;
                }

                if (resolution == Resolution._1080p || normalizedName.Contains("1080p"))
                {
                    result.Quality = Quality.Bluray1080p;
                    return result;
                }

                result.Quality = Quality.Bluray720p;
                return result;
            }

            if (resolution == Resolution._1080p)
            {
                result.Quality = Quality.HDTV1080p;
                return result;
            }

            if (resolution == Resolution._720p)
            {
                result.Quality = Quality.HDTV720p;
                return result;
            }

            if (resolution == Resolution._480p)
            {
                result.Quality = Quality.SDTV;
                return result;
            }

            if (codecMatch.Groups["x264"].Success)
            {
                result.Quality = Quality.SDTV;
                return result;
            }

            if (normalizedName.Contains("848x480"))
            {
                if (normalizedName.Contains("dvd"))
                {
                    result.Quality = Quality.DVD;
                }

                result.Quality = Quality.SDTV;
            }

            if (normalizedName.Contains("1280x720"))
            {
                if (normalizedName.Contains("bluray"))
                {
                    result.Quality = Quality.Bluray720p;
                }

                result.Quality = Quality.HDTV720p;
            }

            if (normalizedName.Contains("1920x1080"))
            {
                if (normalizedName.Contains("bluray"))
                {
                    result.Quality = Quality.Bluray1080p;
                }

                result.Quality = Quality.HDTV1080p;
            }

            if (normalizedName.Contains("bluray720p"))
            {
                result.Quality = Quality.Bluray720p;
            }

            if (normalizedName.Contains("bluray1080p"))
            {
                result.Quality = Quality.Bluray1080p;
            }

            if (sourceMatch.Groups["hdtv720p"].Success)
            {
                result.Quality = Quality.HDTV720p;
            }

            //Based on extension
            if (result.Quality == Quality.Unknown && !name.ContainsInvalidPathChars())
            {
                try
                {
                    result.Quality = MediaFileExtensions.GetQualityForExtension(Path.GetExtension(name.Trim()));
                }
                catch (ArgumentException)
                {
                    //Swallow exception for cases where string contains illegal 
                    //path characters.
                }
            }


            return result;
        }
        #endregion

        #region Language extraction
        private Language ParseLanguage(ParsedInfo info)
        {
            var ret = Language.Unknown;

            foreach (var item in info.Language)
            {
                var newLang = AnalizeLanguage(item.Value);
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
        #endregion

        #region Find series and episodes
        private List<Episode> FindEpisodes(int seriesId, string releaseTitle)
        {
            var normalizedReleaseTitle = releaseTitle.NormalizeEpisodeTitle().Replace(".", " ").Trim();
            var episodes = _episodeService.GetEpisodeBySeries(seriesId);

            var matches = episodes.Select(
                episode => new
                {
                    Position = normalizedReleaseTitle.IndexOf(episode.Title, StringComparison.CurrentCultureIgnoreCase),
                    Length = episode.Title.Length,
                    Episode = episode
                })
                                .Where(e => e.Episode.Title.Length > 0 && e.Position == 0)
                                .OrderBy(e => e.Position)
                                .ThenByDescending(e => e.Length)
                                .ToList();
            return matches.Select(p => p.Episode).ToList();
        }

        private List<int> FindEpisode(string[] splits, int seriesId, out List<Episode> episodeInfo)
        {
            var used = new List<int>();
            var episodes = _episodeService.GetEpisodeBySeries(seriesId);
            episodes.Select(p =>
                {
                    p.Title = p.Title.NormalizeEpisodeTitle().Replace(".", " ").Trim();
                    return p;
                });
            episodeInfo = new List<Episode>();

            for (int i = 0; i < splits.Length && episodeInfo == null; i++)
            {
                used.Clear();
                used.Add(i);
                var item = splits[i];
                var _title = item;
                episodeInfo.AddRange(FindEpisodes(seriesId, _title));

                {
                    var rest = "";
                    for (int j = i + 1; j < splits.Length && episodeInfo == null; j++)
                    {
                        used.Add(j);
                        rest = String.Format("{0} {1}", rest, splits[j]);
                        _title = String.Format("{0} {1}", item, rest).NormalizeTitle();
                        episodeInfo.AddRange(FindEpisodes(seriesId, _title));
                    }
                }
            }

            return used;

        }

        private List<int> FindSeries(string[] splits, ParsedInfo parsedInfo)
        {
            var used = new List<int>();

            for (int i = 0; i < splits.Length && parsedInfo.Series == null; i++)
            {
                used.Clear();
                used.Add(i);
                var item = splits[i];
                var _title = item;
                parsedInfo.Series = _seriesService.FindByTitle(_title.NormalizeTitle());

                if (parsedInfo.Series == null)
                {
                    var rest = "";
                    for (int j = i + 1; j < splits.Length && parsedInfo.Series == null; j++)
                    {
                        used.Add(j);
                        rest = String.Format("{0} {1}", rest, splits[j]);
                        _title = String.Format("{0} {1}", item, rest);
                        parsedInfo.Series = _seriesService.FindByTitle(_title.NormalizeTitle());
                    }
                }
                if (parsedInfo.Series != null)
                {
                    parsedInfo.SeriesTitle = _title;
                }
            }
            return used;
        }

        private bool FindSeries(List<ParsedItem> unknownItems, ParsedInfo myParsedInfo)
        {
            if (myParsedInfo.Series != null)
            {
                _logger.Debug("Found Series: {0}", myParsedInfo.Series.Title);
                return true;
            }
            foreach (var unknownItem in unknownItems)
            {
                var _title = unknownItem.Value;
                var newSerie = _seriesService.FindByTitle(_title.NormalizeTitle());

                // TODO: Modify this and select only the next and previous possible item(s)
                if (newSerie == null)
                {
                    foreach (var _year in myParsedInfo.Year)
                    {
                        /*_title = String.Format("{0} ({1})", unknownItem.Value, _year.Value);
                        newSerie = _seriesService.FindByTitle(_title.NormalizeTitle());
                        if (newSerie == null)
                        {
                            _title = String.Format("{0} {1}", _year.Value, unknownItem.Value);
                            newSerie = _seriesService.FindByTitle(_title.NormalizeTitle());
                        }*/
                        newSerie = _seriesService.FindByTitle(_title.NormalizeTitle(), Convert.ToInt32(_year.Value));
                        if (newSerie != null)
                        {
                            myParsedInfo.RemoveFromAllThatContains(_year);
                            break;
                        }
                    }
                }
                if (newSerie == null)
                {
                    foreach (var _episode in myParsedInfo.AbsoluteEpisodeNumber)
                    {
                        _title = String.Format("{0} {1}", unknownItem.Value, _episode.Value);
                        newSerie = _seriesService.FindByTitle(_title.NormalizeTitle());
                        if (newSerie == null)
                        {
                            _title = String.Format("{0} {1}", _episode.Value, unknownItem.Value);
                            newSerie = _seriesService.FindByTitle(_title.NormalizeTitle());
                        }
                        if (newSerie != null)
                        {
                            myParsedInfo.RemoveFromAllThatContains(_episode);
                            break;
                        }
                    }
                }
                if (newSerie == null)
                {
                    foreach (var _hash in myParsedInfo.Hash)
                    {
                        _title = String.Format("{0} {1}", unknownItem.Value, _hash.Value);
                        newSerie = _seriesService.FindByTitle(_title.NormalizeTitle());
                        if (newSerie == null)
                        {
                            _title = String.Format("{0} {1}", _hash.Value, unknownItem.Value);
                            newSerie = _seriesService.FindByTitle(_title.NormalizeTitle());
                        }
                        if (newSerie != null)
                        {
                            myParsedInfo.RemoveFromAllThatContains(_hash);
                            break;
                        }
                    }
                }
                if (newSerie == null)
                {
                    foreach (var _season in myParsedInfo.Season)
                    {
                        _title = String.Format("{0} ({1})", unknownItem.Value, _season.Value);
                        newSerie = _seriesService.FindByTitle(_title.NormalizeTitle());
                        if (newSerie == null)
                        {
                            _title = String.Format("{0} {1}", _season.Value, unknownItem.Value);
                            newSerie = _seriesService.FindByTitle(_title.NormalizeTitle());
                        }
                        if (newSerie != null)
                        {
                            myParsedInfo.RemoveFromAllThatContains(_season);
                            break;
                        }
                    }
                }
                if (newSerie != null)
                {
                    unknownItems.Remove(unknownItem);
                    myParsedInfo.Series = newSerie;
                    myParsedInfo.SeriesTitle = _title;
                    _logger.Debug("Found Series: {0}", myParsedInfo.Series.Title);
                    return true;
                }
            }
            return false;
        }
        #endregion

        #region Episode, season number and daily extraction

        private bool ExtractAbsoluteEpisodeNumber(ParsedInfo parsedInfo, ParsedEpisodeInfo parsedEpisodeInfo)
        {
            // Removes duplicate items from AbsoluteEpisodeNumber in Season
            foreach (var res in parsedInfo.AbsoluteEpisodeNumber)
            {
                parsedInfo.Season.RemoveAll(season => season.Contains(res));
                parsedInfo.Season.RemoveAll(season => res.Contains(season));
            }

            // Parse absolute episodes
            List<List<int>> listAbsoluteEpisodes = new List<List<int>>();

            foreach (var item in parsedInfo.AbsoluteEpisodeNumber)
            {
                var matchCollection = AnalizeAbsoluteEpisodeNumber.SimpleAbsoluteNumber.Matches(item.Value);
                var absoluteEpisodes = new List<Capture>();

                var listToAdd = new List<int>();

                foreach (Match match in matchCollection)
                {
                    absoluteEpisodes.AddRange(match.Groups["absoluteepisode"].Captures.Cast<Capture>());
                }

                if (absoluteEpisodes.Count == 0)
                {
                    continue;
                }
                else if (absoluteEpisodes.Count == 1)
                {
                    listToAdd = absoluteEpisodes.Select(p => Convert.ToInt32(p.Value)).ToList();
                }
                else if (absoluteEpisodes.Count > 1)
                {
                    if (absoluteEpisodes.Count > 2)
                    {
                        if (!AreConsecutive(absoluteEpisodes))
                            continue;
                    }

                    var first = Convert.ToInt32(absoluteEpisodes.First().Value);
                    var last = Convert.ToInt32(absoluteEpisodes.Last().Value);

                    if (first > last)
                    {
                        continue;
                    }

                    var count = last - first + 1;

                    // More than 20 episodes in one file?
                    if (count > 20)
                        continue;

                    listToAdd = Enumerable.Range(first, count).ToList();
                }

                // Check if that episodes exists for the serie in the season
                if (parsedInfo.Series != null)
                {
                    bool found = true;
                    foreach (var absId in listToAdd)
                    {
                        var ep = _episodeService.FindEpisode(parsedInfo.Series.Id, absId);
                        found = ep != null;
                        if (!found)
                            break;
                    }
                    if (!found)
                        continue;
                }

                listAbsoluteEpisodes.Add(listToAdd);
            }

            if (listAbsoluteEpisodes.Count == 0)
            {
                if (parsedInfo.Year.Count == 1 && parsedInfo.Series != null)
                {
                    int year = Int32.Parse(parsedInfo.Year[0].Value);
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
                        return true;
                    }
                }
            }
            else if (listAbsoluteEpisodes.Count == 1)
            {
                parsedEpisodeInfo.AbsoluteEpisodeNumbers = listAbsoluteEpisodes.First().ToArray();
                return true;
            }
            else if (!listAbsoluteEpisodes.Any(l => l.Count > 1))
            {
                // if all are single we may have different ids for the same episode

                // TODO: Check which one is episode and which one is global, for now we supose that the lowest one is the episode
                listAbsoluteEpisodes.OrderBy(l => l.First());
                parsedEpisodeInfo.AbsoluteEpisodeNumbers = listAbsoluteEpisodes.First().ToArray();
                return true;
            }
            return false;
        }

        private bool ExtractDaily(ParsedInfo parsedInfo, ParsedEpisodeInfo parsedEpisodeInfo)
        {
            var airDate = DateTime.Today;
            var asigned = false;

            foreach (var newDate in parsedInfo.Daily)
            {
                var newYear = 0;
                var newMonth = 0;
                var newDay = 0;
                var airDateMatch = AnalizeDaily.AirDateRegex.Match(newDate.Value);
                var sixDigitAirDateMatch = AnalizeDaily.SixDigitAirDateRegex.Match(newDate.Value);
                var sixDigitAirWDateMatch = AnalizeDaily.SixDigitWAirDateRegex.Match(newDate.Value);

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
                else if (sixDigitAirWDateMatch.Success)
                {
                    Int32.TryParse("20" + sixDigitAirWDateMatch.Groups["airyear"].Value, out newYear);
                    Int32.TryParse(sixDigitAirWDateMatch.Groups["airmonth"].Value, out newMonth);
                    Int32.TryParse(sixDigitAirWDateMatch.Groups["airday"].Value, out newDay);
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

                //Check if episode is in the future (most likely a parse error)
                if (newAirDate > DateTime.Now.AddDays(1).Date || newAirDate < new DateTime(1970, 1, 1))
                {
                    continue;
                }

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
                return true;
            }

            return false;
        }

        private bool AreConsecutive(List<Capture> captures)
        {
            // If more than 2 then all episodes should be consecutives
            var consecutive = true;
            var first = Convert.ToInt32(captures.First().Value);
            for (int i = 1; i < captures.Count && consecutive; i++)
            {
                var current = Convert.ToInt32(captures[i].Value);
                consecutive = first + 1 == current;
                first = current;
            }
            return consecutive;
        }

        private bool ExtractSeasonAndEpisode(ParsedInfo parsedInfo, ParsedEpisodeInfo parsedEpisodeInfo)
        {
            _logger.Debug("Extracting Season and Episode info");
            if (parsedInfo.Season.Count > 0)
            {
                List<SeasonAndEpisode> seasonsAndEpisodesParsed = new List<SeasonAndEpisode>();

                foreach (var item in parsedInfo.Season)
                {
                    var seasonAndEpisode = new SeasonAndEpisode();
                    seasonAndEpisode.Item = item;

                    var seasons = new List<int>();

                    foreach (Match match in AnalizeSeason.SeasonAndEpisodeWord.Matches(item.Value))
                    {
                        foreach (Capture seasonCapture in match.Groups["season"].Captures)
                        {
                            int parsedSeason;
                            if (Int32.TryParse(seasonCapture.Value, out parsedSeason))
                                seasons.Add(parsedSeason);
                        }
                    }

                    if (!seasons.Any())
                    {
                        foreach (Match match in AnalizeSeason.WeirdSeason.Matches(item.Value))
                        {
                            foreach (Capture seasonCapture in match.Groups["season"].Captures)
                            {
                                int parsedSeason;
                                if (Int32.TryParse(seasonCapture.Value, out parsedSeason))
                                    seasons.Add(parsedSeason);
                            }
                        }
                    }

                    if (!seasons.Any())
                    {
                        foreach (Match match in AnalizeSeason.SimpleSeason.Matches(item.Value))
                        {
                            foreach (Capture seasonCapture in match.Groups["season"].Captures)
                            {
                                int parsedSeason;
                                if (Int32.TryParse(seasonCapture.Value, out parsedSeason))
                                    seasons.Add(parsedSeason);
                            }
                        }
                    }

                    if (!seasons.Any())
                    {
                        foreach (Match match in AnalizeSeason.OnlyDigitsOrEp.Matches(item.Value))
                        {
                            foreach (Capture seasonCapture in match.Groups["season"].Captures)
                            {
                                int parsedSeason;
                                if (Int32.TryParse(seasonCapture.Value, out parsedSeason))
                                    seasons.Add(parsedSeason);
                            }
                        }
                    }

                    if (!seasons.Any())
                    {
                        foreach (Match match in AnalizeSeason.OnlySeason.Matches(item.Value))
                        {
                            foreach (Capture seasonCapture in match.Groups["season"].Captures)
                            {
                                int parsedSeason;
                                if (Int32.TryParse(seasonCapture.Value, out parsedSeason))
                                {
                                    seasonAndEpisode.FullSeason = true;
                                    seasons.Add(parsedSeason);
                                }
                            }
                        }
                    }

                    //If no season was found it should be treated as a mini series or special
                    if (seasons.Count == 0)
                    {
                        seasonAndEpisode.isMiniOrSpecial = true;
                        seasonAndEpisode.Season = 1;
                    }
                    else
                    {
                        //If more than 1 season was parsed go to the next REGEX (A multi-season release is unlikely)
                        if (seasons.Distinct().Count() > 1)
                        {
                            _logger.Debug("Discarted {0} - more than one season detected.", item);
                            continue;
                        }

                        // Check if that season exists for the serie

                        seasonAndEpisode.isMiniOrSpecial = seasons.First() == 0;

                        if (parsedInfo.Series == null || seasonAndEpisode.isMiniOrSpecial)
                        {
                            seasonAndEpisode.Season = seasons.First();
                        }
                        else if (parsedInfo.Series.Seasons.Any(p => p.SeasonNumber == seasons[0]))
                        {
                            seasonAndEpisode.Season = seasons.First();
                        }
                        else
                        {
                            _logger.Debug("Discarted {0} - season detected is not included in the serie.", item);
                            continue;
                        }
                    }

                    // If full season detected stop searching for episodes
                    if (seasonAndEpisode.FullSeason)
                    {
                        seasonsAndEpisodesParsed.Add(seasonAndEpisode);
                        continue;
                    }

                    var episodes = new List<int>();

                    foreach (Match match in AnalizeSeason.SeasonAndEpisodeWord.Matches(item.Value))
                    {
                        var episodeCaptures = match.Groups["episode"].Captures.Cast<Capture>().ToList();
                        // If more than 2 then all episodes should be consecutives
                        if (episodeCaptures.Count > 2)
                        {
                            if (!AreConsecutive(episodeCaptures))
                            {
                                _logger.Debug("Discarted {0} - episodes are not consecutives.", item);
                                continue;
                            }
                        }
                        if (episodeCaptures.Any())
                        {
                            var first = Convert.ToInt32(episodeCaptures.First().Value);
                            var last = Convert.ToInt32(episodeCaptures.Last().Value);

                            if (first > last)
                            {
                                _logger.Debug("Discarted {0} - episodes are not correctly ordered", item);
                                continue;
                            }

                            var count = last - first + 1;
                            episodes = Enumerable.Range(first, count).ToList();
                        }
                    }

                    if (!episodes.Any())
                    {
                        foreach (Match match in AnalizeSeason.WeirdSeason.Matches(item.Value))
                        {
                            var episodeCaptures = match.Groups["episode"].Captures.Cast<Capture>().ToList();
                            // If more than 2 then all episodes should be consecutives
                            if (episodeCaptures.Count > 2)
                            {
                                if (!AreConsecutive(episodeCaptures))
                                {
                                    _logger.Debug("Discarted {0} - episodes are not consecutives.", item);
                                    continue;
                                }
                            }
                            if (episodeCaptures.Any())
                            {
                                var first = Convert.ToInt32(episodeCaptures.First().Value);
                                var last = Convert.ToInt32(episodeCaptures.Last().Value);

                                if (first > last)
                                {
                                    // There are only 2 items? if so then ignore last number
                                    // TODO: Do something better
                                    if (episodeCaptures.Count == 2)
                                    {
                                        _logger.Warn("{0} - discarting episode info from {1}", item, last);
                                        last = first;
                                    }
                                    else
                                    {
                                        _logger.Debug("Discarted {0} - episodes are not correctly ordered", item);
                                        continue;
                                    }
                                }

                                var count = last - first + 1;
                                episodes = Enumerable.Range(first, count).ToList();
                            }
                        }
                    }

                    if (!episodes.Any())
                    {
                        foreach (Match match in AnalizeSeason.SimpleSeason.Matches(item.Value))
                        {
                            var episodeCaptures = match.Groups["episode"].Captures.Cast<Capture>().ToList();
                            // If more than 2 then all episodes should be consecutives
                            if (episodeCaptures.Count > 2)
                            {
                                if (!AreConsecutive(episodeCaptures))
                                {
                                    _logger.Debug("Discarted {0} - episodes are not consecutives.", item);
                                    continue;
                                }
                            }
                            if (episodeCaptures.Any())
                            {
                                var first = Convert.ToInt32(episodeCaptures.First().Value);
                                var last = Convert.ToInt32(episodeCaptures.Last().Value);

                                if (first > last)
                                {
                                    // There are only 2 items? if so then ignore last number
                                    // TODO: Do something better
                                    if (episodeCaptures.Count == 2)
                                    {
                                        _logger.Warn("{0} - discarting episode info from {1}", item, last);
                                        last = first;
                                    }
                                    else
                                    {
                                        continue;
                                    }
                                }

                                var count = last - first + 1;
                                episodes = Enumerable.Range(first, count).ToList();
                            }
                        }
                    }

                    if (!episodes.Any())
                    {
                        foreach (Match match in AnalizeSeason.OnlyDigitsOrEp.Matches(item.Value))
                        {
                            var episodeCaptures = match.Groups["episode"].Captures.Cast<Capture>().ToList();
                            // If more than 2 then all episodes should be consecutives
                            if (episodeCaptures.Count > 2)
                            {
                                if (!AreConsecutive(episodeCaptures))
                                {
                                    _logger.Debug("Discarted {0} - episodes are not consecutives.", item);
                                    continue;
                                }
                            }
                            if (episodeCaptures.Any())
                            {
                                var first = Convert.ToInt32(episodeCaptures.First().Value);
                                var last = Convert.ToInt32(episodeCaptures.Last().Value);

                                if (first > last)
                                {
                                    _logger.Debug("Discarted {0} - episodes are not correctly ordered", item);
                                    continue;
                                }

                                var count = last - first + 1;
                                episodes = Enumerable.Range(first, count).ToList();
                            }
                        }
                    }

                    // Miniseries
                    if (!episodes.Any() && seasonAndEpisode.isMiniOrSpecial)
                    {
                        foreach (Match match in AnalizeSeason.SimpleMiniSerie.Matches(item.Value))
                        {
                            var episodeCaptures = match.Groups["episode"].Captures.Cast<Capture>().ToList();
                            // If more than 2 then all episodes should be consecutives
                            if (episodeCaptures.Count > 2)
                            {
                                if (!AreConsecutive(episodeCaptures))
                                {
                                    _logger.Debug("Discarted {0} - episodes are not consecutives.", item);
                                    continue;
                                }
                            }
                            if (episodeCaptures.Any())
                            {
                                var first = Convert.ToInt32(episodeCaptures.First().Value);
                                var last = Convert.ToInt32(episodeCaptures.Last().Value);

                                if (first > last)
                                {
                                    _logger.Debug("Discarted {0} - episodes are not correctly ordered", item);
                                    continue;
                                }

                                var count = last - first + 1;
                                episodes = Enumerable.Range(first, count).ToList();
                                seasonAndEpisode.Season = 1;
                            }
                        }
                    }

                    if (episodes.Any())
                    {
                        // More than 20 episodes in one file?
                        if (episodes.Count > 20)
                        {
                            _logger.Debug("Discarted {0} - more than 20 episodes in one file", item);
                            continue;
                        }
                        // Check if that episodes exists for the serie in the season
                        if (parsedInfo.Series != null && !seasonAndEpisode.isMiniOrSpecial)
                        {
                            var ep = _episodeService.GetEpisodesBySeason(
                                parsedInfo.Series.Id,
                                seasonAndEpisode.Season.Value);

                            if (episodes.All(i => ep.Any(p => p.EpisodeNumber == i)))
                            {
                                seasonAndEpisode.Episodes.AddRange(episodes);
                            }
                            else
                            {
                                _logger.Debug("Discarted {0} - episodes doesn't exist for the serie", item);
                                continue;
                            }
                        }
                        else
                        {
                            seasonAndEpisode.Episodes.AddRange(episodes);
                        }
                        seasonsAndEpisodesParsed.Add(seasonAndEpisode);
                    }
                }

                return SelectSeasonAndEpisode(seasonsAndEpisodesParsed, parsedInfo, parsedEpisodeInfo);
            }
            return false;
        }

        private bool SelectSeasonAndEpisode(List<SeasonAndEpisode> seasonsAndEpisodesParsed, ParsedInfo parsedInfo, ParsedEpisodeInfo parsedEpisodeInfo)
        {
            if (seasonsAndEpisodesParsed.Any())
            {
                if (seasonsAndEpisodesParsed.Count == 1)
                {
                    parsedEpisodeInfo.SeasonNumber = seasonsAndEpisodesParsed.First().Season.Value;
                    parsedEpisodeInfo.FullSeason = seasonsAndEpisodesParsed.First().FullSeason;
                    parsedEpisodeInfo.EpisodeNumbers = seasonsAndEpisodesParsed.First().Episodes.ToArray();
                    return true;
                }
                else if (seasonsAndEpisodesParsed.All(p => p.Equals(seasonsAndEpisodesParsed.First())))
                {
                    parsedEpisodeInfo.SeasonNumber = seasonsAndEpisodesParsed.First().Season.Value;
                    parsedEpisodeInfo.FullSeason = seasonsAndEpisodesParsed.First().FullSeason;
                    parsedEpisodeInfo.EpisodeNumbers = seasonsAndEpisodesParsed.First().Episodes.ToArray();
                    return true;
                }
                else
                {
                    seasonsAndEpisodesParsed = FilterSeasonsAndEpisodes(seasonsAndEpisodesParsed, parsedInfo);

                    if (seasonsAndEpisodesParsed.Count == 1)
                    {
                        return SelectSeasonAndEpisode(seasonsAndEpisodesParsed, parsedInfo, parsedEpisodeInfo);
                    }

                    if (!seasonsAndEpisodesParsed.All(p => p.Season == seasonsAndEpisodesParsed.First().Season))
                    {
                        _logger.Debug("SeasonAndEpisode: More than one season in the file");
                        return false;
                    }

                    parsedEpisodeInfo.SeasonNumber = seasonsAndEpisodesParsed.First().Season.Value;
                    parsedEpisodeInfo.FullSeason = seasonsAndEpisodesParsed.First().FullSeason;

                    if (parsedEpisodeInfo.FullSeason)
                        return true;

                    //If all episodes are singles and all have the same season (S01E01 - S01E03)
                    if (seasonsAndEpisodesParsed.All(p => p.Episodes.Count == 1))
                    {
                        var first = Convert.ToInt32(seasonsAndEpisodesParsed.First().Episodes.First());
                        var last = Convert.ToInt32(seasonsAndEpisodesParsed.Last().Episodes.Last());

                        if (first > last)
                        {
                            _logger.Debug("SeasonAndEpisode: Discarted episodes, bad order.");
                            return false;
                        }

                        var count = last - first + 1;
                        parsedEpisodeInfo.EpisodeNumbers = Enumerable.Range(first, count).ToArray();
                        return true;
                    }

                    _logger.Debug("SeasonAndEpisode: Unable to detect correct info.");
                }
            }
            return false;
        }

        private List<SeasonAndEpisode> FilterSeasonsAndEpisodes(List<SeasonAndEpisode> seasonsAndEpisodesParsed, ParsedInfo parsedInfo)
        {
            // If more than one season was parsed and one can be a Year, discard it.
            var ret = new List<SeasonAndEpisode>();
            ret.AddRange(seasonsAndEpisodesParsed);

            ret.RemoveAll(p => parsedInfo.Year.Contains(p.Item));

            if (ret.Count == 1)
                return ret;

            if (ret.Count == 0)
                return seasonsAndEpisodesParsed;

            // Discard Specials and Miniseries
            ret.RemoveAll(p => p.isMiniOrSpecial);

            if (ret.Count == 1)
                return ret;

            if (ret.Count == 0)
                return seasonsAndEpisodesParsed;

            // Discard full seasons
            ret.RemoveAll(p => p.FullSeason);

            if (ret.Count == 1)
                return ret;

            if (ret.Count == 0)
                return seasonsAndEpisodesParsed;

            return ret;
        }
        #endregion
    }
}
