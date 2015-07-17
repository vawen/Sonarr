using NzbDrone.Core.Datastore;
using NzbDrone.Core.Languages;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Qualities;

namespace NzbDrone.Core.Profiles
{
    public class ProfileLanguageItem : IEmbeddedDocument
    {
        public Language Language { get; set; }
        public bool Allowed { get; set; }
    }
}
