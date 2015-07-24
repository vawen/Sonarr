using NzbDrone.Common.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NzbDrone.Core.Profiles
{
    public class ProfileModifiedEvent : IEvent
    {
        public int ProfileId { get; set; }

        public ProfileModifiedEvent (int profileId)
        {
            ProfileId = profileId;
        }
    }
}
