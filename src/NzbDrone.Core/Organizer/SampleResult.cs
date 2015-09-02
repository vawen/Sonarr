﻿using System;
using System.Collections.Generic;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Tv;
using NzbDrone.Core.Movies;

namespace NzbDrone.Core.Organizer
{
    public class SampleResult
    {
        public String FileName { get; set; }
        public Series Series { get; set; }
        public List<Episode> Episodes { get; set; }
        public EpisodeFile EpisodeFile { get; set; }
        public Movie Movie { get; set; }
        public MovieFile MovieFile { get; set; }
    }
}
