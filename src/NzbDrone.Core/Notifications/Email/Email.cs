﻿using System;
using System.Collections.Generic;
using FluentValidation.Results;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Tv;
using NzbDrone.Core.Movies;

namespace NzbDrone.Core.Notifications.Email
{
    public class Email : NotificationBase<EmailSettings>
    {
        private readonly IEmailService _emailService;

        public Email(IEmailService emailService)
        {
            _emailService = emailService;
        }

        public override string Link
        {
            get { return null; }
        }

        public override void OnGrab(GrabMessage grabMessage)
        {
            const string subject = "Sonarr [TV] - Grabbed";
            var body = String.Format("{0} sent to queue.", grabMessage.Message);

            _emailService.SendEmail(Settings, subject, body);
        }

        public override void OnDownload(DownloadMessage message)
        {
            const string subject = "Sonarr [TV] - Downloaded";
            var body = String.Format("{0} Downloaded and sorted.", message.Message);

            _emailService.SendEmail(Settings, subject, body);
        }

        public override void OnDownloadMovie(DownloadMovieMessage message)
        {
            const string subject = "Sonarr [Movie] - Downloaded";
            var body = String.Format("{0} Downloaded and sorted.", message.Message);

            _emailService.SendEmail(Settings, subject, body);
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
                return "Email";
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

            failures.AddIfNotNull(_emailService.Test(Settings));

            return new ValidationResult(failures);
        }
    }
}
