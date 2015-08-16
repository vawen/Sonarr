using System;
using System.Linq;
using System.Collections.Generic;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Tv;
using NzbDrone.Core.MediaFiles.MediaInfo;
using NzbDrone.Core.Movies;

namespace NzbDrone.Core.Parser.Model
{
    public class LocalMovie
    {
        public String Path { get; set; }
        public Int64 Size { get; set; }
        public ParsedMovieInfo ParsedMovieInfo { get; set; }
        public Movie Movie { get; set; }
        public QualityModel Quality { get; set; }
        public MediaInfoModel MediaInfo { get; set; }
        public Boolean ExistingFile { get; set; }
               
        public override string ToString()
        {
            return Path;
        }
    }
}