using NLog;
using HtmlAgilityPack;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Instrumentation;
using NzbDrone.Core.Indexers.Exceptions;
using NzbDrone.Core.Parser.Model;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace NzbDrone.Core.Indexers.Newpct
{
    public class NewpctParser : IParseIndexerResponse
    {
        protected readonly Logger _logger;

        // Regex for Parse Size of the File (1.1 Gb, etc)
        private static readonly Regex ParseSizeRegex = new Regex(@"(?<value>\d+\.\d{1,2}|\d+\,\d+\.\d{1,2}|\d+)\W?(?<unit>[KMG]i?B)",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex ParseTorrentUrlRegex = new Regex(@"http[s]?://.*\.torrent", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex ParseTorrentArgsRegex = new Regex(@"\?link=(?<args>.*)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex ParseInfoUrlRegex = new Regex(@"(?<URL>http[s]?:\/\/.*(\/|\.html))""", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex ParseTitleRegex = new Regex(@"title=""Descargar (?<title>.*?)""", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        //private static readonly Regex reg = new Regex(@"(?<Serie>.*?) - (\w*)(\.| )(?<Temporada>\d*)( ?)(?<resto>.*)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        //private static readonly Regex capitulo = new Regex(@"Cap.(?<Capitulo>\d*)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public NewpctParser()
        {
            UseGuidInfoUrl = true;
            _logger = NzbDroneLogger.GetLogger(this);
        }

        public virtual IList<ReleaseInfo> ParseResponse(IndexerResponse indexerResponse)
        {
            var releases = new List<ReleaseInfo>();


            if (!PreProcess(indexerResponse))
            {
                return releases;
            }

            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(indexerResponse.Content);

            // Estructure:
            // tdLinks: Info link, Torrent link, infolink2, torrentlink2, ...
            // tdDateSize: publishdate, size, publishdate2, size2, ...

            List<HtmlNode> tdLinks = doc.DocumentNode.Descendants().Where
                (x => (x.Name == "td" && x.Attributes["style"] != null && x.Attributes["class"] == null &&
                    x.Attributes["style"].Value.Contains("border-bottom:solid 1px cyan"))).ToList();
            List<HtmlNode> tdDateSize = doc.DocumentNode.Descendants().Where
                (x => (x.Name == "td" && x.Attributes["style"] != null && x.Attributes["class"] != null && 
                    x.Attributes["class"].Value.Contains("center") &&
                    x.Attributes["style"].Value.Contains("border-bottom:solid 1px cyan"))).ToList();

            if (tdLinks.Count != tdDateSize.Count)
            {
                _logger.Error("Page structure has changed, error parsing");
                return releases;
            }

            int i = 0;
            foreach (var tdTorrentUrl in tdLinks)
            {
                try
                {
                    // TD: Has a torrent link
                    if (ParseTorrentUrlRegex.IsMatch(tdTorrentUrl.InnerHtml))
                    {
                        HtmlNode tdSize = tdDateSize[i - 1];
                        HtmlNode tdInfoUrl = tdLinks[i - 1];
                        

                        ReleaseInfo rel = CreateNewReleaseInfo();

                        rel.Guid = GetGuid(tdInfoUrl);
                        rel.Title = GetTitle(tdTorrentUrl);
                        rel.PublishDate = GetPublishDate(tdSize);
                        rel.DownloadUrl = GetDownloadUrl(tdTorrentUrl);
                        rel.InfoUrl = GetInfoUrl(tdInfoUrl);
                        rel.CommentUrl = String.Empty;
                        rel.DownloadProtocol = DownloadProtocol.Torrent;
                        rel.Size = RssParser.ParseSize(tdDateSize[i].InnerHtml, true);

                        releases.AddIfNotNull(rel);
                    }
                } catch (Exception itemEx)
                {
                    itemEx.Data.Add("Item", tdTorrentUrl.InnerHtml);
                    _logger.ErrorException("An error occurred while processing feed item from " + indexerResponse.Request.Url, itemEx);
                }
                i++;
            }
            return releases;
        }

        /*private String PrepararTitulo(string rawtitle)
        {
            Match m = reg.Match(rawtitle);
            String title = m.Groups["Serie"].Value;
            int temporada = Convert.ToInt16(m.Groups["Temporada"].Value);
            String resto = m.Groups["resto"].Value;
            int cap = -1;
            resto = resto.Replace("]", "");
            String[] partes = resto.Split('[');
            foreach (var parte in partes)
            {
                if (capitulo.IsMatch(parte))
                {
                    cap = Convert.ToInt16(capitulo.Match(parte).Groups["Capitulo"].Value) - temporada * 100;
                    resto = resto.Replace(parte, "");
                }
            }

            resto = resto.Replace("[", " ");

            String result = "";
            if (cap < 0)
            {
                result = string.Format("{0} S{1:00} {3}", title, temporada, resto);
            }
            else
            {
                result = string.Format("{0} S{1:00}E{2:00} {3}", title, temporada, cap, resto);
            }
            return result;
        }*/

        // ParseDate with current info
        protected virtual DateTime ParseDate(string dateString)
        {
            try
            {
                DateTime result;
                if (!DateTime.TryParse(dateString, DateTimeFormatInfo.CurrentInfo, DateTimeStyles.AdjustToUniversal, out result))
                {
                    dateString = XElementExtensions.RemoveTimeZoneRegex.Replace(dateString, "");
                    result = DateTime.Parse(dateString, DateTimeFormatInfo.InvariantInfo, DateTimeStyles.AdjustToUniversal);
                }
                return result.ToUniversalTime();
            }
            catch (FormatException e)
            {
                throw;
            }
        }

        protected virtual ReleaseInfo CreateNewReleaseInfo()
        {
            return new TorrentInfo();
        }

        protected virtual Boolean PreProcess(IndexerResponse indexerResponse)
        {
            if (indexerResponse.HttpResponse.StatusCode != System.Net.HttpStatusCode.OK)
            {
                throw new IndexerException(indexerResponse, "Indexer API call resulted in an unexpected StatusCode [{0}]", indexerResponse.HttpResponse.StatusCode);
            }

            return true;
        }

        protected virtual String GetGuid(HtmlNode item)
        {
            if (UseGuidInfoUrl && GetInfoUrl(item).Length>0)
            {
                return GetInfoUrl(item);
            }
            return new Guid().ToString();
        }

        protected virtual String GetTitle(HtmlNode item)
        {
            Match titleInfo = ParseTitleRegex.Match(item.InnerHtml);

            return titleInfo.Groups["title"].Value;
        }

        protected virtual DateTime GetPublishDate(HtmlNode item)
        {
            return ParseDate(item.InnerText);
        }

        protected virtual string GetCommentUrl(HtmlNode item)
        {
            return GetInfoUrl(item);
        }

        protected virtual string GetInfoUrl(HtmlNode item)
        {
            Match info = ParseInfoUrlRegex.Match(item.InnerHtml);
            return info.Groups["URL"].Value;
        }

        protected virtual string GetDownloadUrl(HtmlNode item)
        {
            // If torrentUrl has "tumejorserie" avoid redirection to real URL
            String torrentUrl = ParseTorrentUrlRegex.Match(item.InnerHtml).Value;
            if (torrentUrl.Contains("tumejorserie"))
            {
                torrentUrl = string.Format("http://www.newpct.com/{0}", ParseTorrentArgsRegex.Match(torrentUrl).Groups["args"]);
            }

            return torrentUrl;
        }

        public bool UseGuidInfoUrl { get; set; }
    }
}
