using System;
using System.Collections.Generic;
using System.Linq;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Languages;

namespace NzbDrone.Core.Profiles
{
    public class Profile : ModelBase
    {
        public String Name { get; set; }
        public Quality Cutoff { get; set; }
        public List<ProfileQualityItem> Items { get; set; }
        public List<ProfileLanguageItem> Languages { get; set; }
        public Language CutoffLanguage { get; set;  }
        public bool AllowLanguageUpgrade { get; set; }
        public bool LanguageOverQuality { get; set; }

        public Quality LastAllowedQuality()
        {
            return Items.Last(q => q.Allowed).Quality;
        }

        public Language LastAllowedLanguage()
        {
            return Languages.Last(q => q.Allowed).Language;
        }
    }
}
