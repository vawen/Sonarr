using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Movies;
using System;
using System.Collections.Generic;

namespace NzbDrone.Core.Notifications
{
    public class DownloadMovieMessage
    {
        public String Message { get; set; }
        public Movie Movie { get; set; }
        public MovieFile MovieFile { get; set; }
        public MovieFile OldFile { get; set; }

        public override string ToString()
        {
            return Message;
        }
    }
}
