using System;
using System.Collections.Generic;
using NzbDrone.Api.REST;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Languages;

namespace NzbDrone.Api.Profiles
{
    public class ProfileResource : RestResource
    {
        public String Name { get; set; }
        public Quality Cutoff { get; set; }
        public List<ProfileQualityItemResource> Items { get; set; }
        public List<ProfileLanguageItemResource> Languages { get; set; }
        public Language CutoffLanguage { get; set; }
        public Boolean AllowLanguageUpgrade { get; set; }
        public Boolean LanguageOverQuality { get; set; }
    }

    public class ProfileQualityItemResource : RestResource
    {
        public Quality Quality { get; set; }
        public bool Allowed { get; set; }
    }

    public class ProfileLanguageItemResource : RestResource
    {
        public Language Language { get; set; }
        public bool Allowed { get; set; }
    }

}