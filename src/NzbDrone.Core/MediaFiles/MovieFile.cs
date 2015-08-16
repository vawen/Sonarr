using System;
using System.Collections.Generic;
using Marr.Data;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Tv;
using NzbDrone.Core.MediaFiles.MediaInfo;

namespace NzbDrone.Core.MediaFiles
{
    public class MovieFile : MediaFile
    {
        // Movies
        public Int32 MovieId { get; set; }
        public LazyLoaded<Movies.Movie> Movie { get; set; }
    }
}