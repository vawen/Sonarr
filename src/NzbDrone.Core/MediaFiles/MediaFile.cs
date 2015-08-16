using NzbDrone.Core.Datastore;
using NzbDrone.Core.MediaFiles.MediaInfo;
using NzbDrone.Core.Qualities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NzbDrone.Core.MediaFiles
{
    public class MediaFile: ModelBase
    {
        public String Path { get; set; }
        public String RelativePath { get; set; }
        public Int64 Size { get; set; }
        public DateTime DateAdded { get; set; }
        public String SceneName { get; set; }
        public String ReleaseGroup { get; set; }
        public QualityModel Quality { get; set; }
        public MediaInfoModel MediaInfo { get; set; }

        public override String ToString()
        {
            return String.Format("[{0}] {1}", Id, RelativePath);
        }
    }
}
