﻿using System.Collections.Generic;
using FluentValidation.Results;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Tv;
using Prowlin;
using NzbDrone.Core.Movies;

namespace NzbDrone.Core.Notifications.Prowl
{
    public class Prowl : NotificationBase<ProwlSettings>
    {
        private readonly IProwlService _prowlService;

        public Prowl(IProwlService prowlService)
        {
            _prowlService = prowlService;
        }

        public override string Link
        {
            get { return "http://www.prowlapp.com/"; }
        }

        public override void OnGrab(GrabMessage grabMessage)
        {
            const string title = "Episode Grabbed";

            _prowlService.SendNotification(title, grabMessage.Message, Settings.ApiKey, (NotificationPriority)Settings.Priority);
        }

        public override void OnDownload(DownloadMessage message)
        {
            const string title = "Episode Downloaded";

            _prowlService.SendNotification(title, message.Message, Settings.ApiKey, (NotificationPriority)Settings.Priority);
        }

        public override void OnDownloadMovie(DownloadMovieMessage message)
        {
            const string title = "Movie Downloaded";

            _prowlService.SendNotification(title, message.Message, Settings.ApiKey, (NotificationPriority)Settings.Priority);
        }

        public override void OnRename(Series series)
        {
        }

        public override void OnRenameMovie(Movie movie)
        {
        }

        public override bool SupportsOnRenameMovie
        {
            get
            {
                return false;
            }
        }

        public override string Name
        {
            get
            {
                return "Prowl";
            }
        }

        public override bool SupportsOnRename
        {
            get
            {
                return false;
            }
        }

        public override ValidationResult Test()
        {
            var failures = new List<ValidationFailure>();

            failures.AddIfNotNull(_prowlService.Test(Settings));

            return new ValidationResult(failures);
        }
    }
}
