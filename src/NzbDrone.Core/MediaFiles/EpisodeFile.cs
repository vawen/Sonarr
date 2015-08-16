using System;
using System.Collections.Generic;
using Marr.Data;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Tv;
using NzbDrone.Core.MediaFiles.MediaInfo;

namespace NzbDrone.Core.MediaFiles
{
    public class EpisodeFile : MediaFile
    {
        // Series
        public Int32 SeriesId { get; set; }
        public Int32 SeasonNumber { get; set; }
        public LazyLoaded<List<Episode>> Episodes { get; set; }
        public LazyLoaded<Series> Series { get; set; }
    }
}