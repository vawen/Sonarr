using System.Collections.Generic;

namespace NzbDrone.Core.MediaFiles
{
    public class MovieFileMoveResult
    {
        public MovieFile MovieFile { get; set; }
        public MovieFile OldFile { get; set; }
    }
}
