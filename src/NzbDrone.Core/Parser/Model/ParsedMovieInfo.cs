using System;
using System.Linq;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Qualities;

namespace NzbDrone.Core.Parser.Model
{
    public class ParsedMovieInfo
    {
        public String Title { get; set; }
        public MovieTitleInfo MovieTitleInfo { get; set; }
        public QualityModel Quality { get; set; }
        public Language Language { get; set; }
        public String ReleaseGroup { get; set; }
        public String ReleaseHash { get; set; }

        public override string ToString()
        {
            return String.Format("{0} - {1}", Title, Quality);
        }
    }
}