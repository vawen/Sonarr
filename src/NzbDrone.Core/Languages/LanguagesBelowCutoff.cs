using System;
using System.Collections.Generic;

namespace NzbDrone.Core.Languages
{
    public class LanguagesBelowCutoff
    {
        public Int32 ProfileId { get; set; }
        public IEnumerable<Int32> LanguageIds { get; set; }

        public LanguagesBelowCutoff(int profileId, IEnumerable<int> languageIds)
        {
            ProfileId = profileId;
            LanguageIds = languageIds;
        }
    }
}
