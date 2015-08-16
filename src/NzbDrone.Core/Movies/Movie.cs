using System;
using System.Collections.Generic;
using Marr.Data;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Profiles;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.MediaFiles;

namespace NzbDrone.Core.Movies
{
    public class Movie : ModelBase
    {
        public Movie()
        {
            Images = new List<MediaCover.MediaCover>();
            Tags = new HashSet<Int32>();
        }

        public string ImdbId { get; set; }
        public int TmdbId { get; set; }

        public string Title { get; set; }
        public string CleanTitle { get; set; }
        public string OriginalTitle { get; set; }

        public int Year { get; set; }
        public string Overview { get; set; }
        public int Runtime { get; set; }
        public string TagLine { get; set; }
        public List<MediaCover.MediaCover> Images { get; set; }
        public DateTime ReleaseDate { get; set; }

        public DateTime? LastInfoSync { get; set; }

        public bool Monitored { get; set; }
        public HashSet<Int32> Tags { get; set; }
        public int ProfileId { get; set; }
        public LazyLoaded<Profile> Profile { get; set; }
        public string RootFolderPath { get; set; }
        public string Path { get; set; }
        public bool AddOptions { get; set; }

        public LazyLoaded<MovieFile> MovieFile { get; set; }
        public int MovieFileId { get; set; }

        public override string ToString()
        {
            return string.Format("[{0}][{1}]", ImdbId, Title.NullSafe());
        }

    }
}
