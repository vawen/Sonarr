﻿using System.Collections.Generic;
using System.IO;
using FluentValidation.Results;
using NzbDrone.Common.EnvironmentInfo;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Tv;
using NzbDrone.Core.Movies;

namespace NzbDrone.Core.Notifications.Synology
{
    public class SynologyIndexer : NotificationBase<SynologyIndexerSettings>
    {
        private readonly ISynologyIndexerProxy _indexerProxy;

        public SynologyIndexer(ISynologyIndexerProxy indexerProxy)
        {
            _indexerProxy = indexerProxy;
        }

        public override string Link
        {
            get { return "http://www.synology.com"; }
        }

        public override void OnGrab(GrabMessage grabMessage)
        {

        }

        public override void OnDownloadMovie(DownloadMovieMessage message)
        {
            if (Settings.UpdateLibrary)
            {
                var fullPath = Path.Combine(message.Movie.Path, message.OldFile.RelativePath);
                _indexerProxy.DeleteFile(fullPath);
                
                fullPath = Path.Combine(message.Movie.Path, message.MovieFile.RelativePath);
                _indexerProxy.AddFile(fullPath);
            }
        }

        public override void OnDownload(DownloadMessage message)
        {
            if (Settings.UpdateLibrary)
            {
                foreach (var oldFile in message.OldFiles)
                {
                    var fullPath = Path.Combine(message.Series.Path, oldFile.RelativePath);

                    _indexerProxy.DeleteFile(fullPath);
                }

                {
                    var fullPath = Path.Combine(message.Series.Path, message.EpisodeFile.RelativePath);

                    _indexerProxy.AddFile(fullPath);
                }
            }
        }

        public override void OnRename(Series series)
        {
            if (Settings.UpdateLibrary)
            {
                _indexerProxy.UpdateFolder(series.Path);
            }
        }

        public override void OnRenameMovie(Movie movie)
        {
            if (Settings.UpdateLibrary)
            {
                _indexerProxy.UpdateFolder(movie.Path);
            }
        }

        public override string Name
        {
            get
            {
                return "Synology Indexer";
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

            failures.AddIfNotNull(TestConnection());

            return new ValidationResult(failures);
        }

        protected virtual ValidationFailure TestConnection()
        {
            if (!OsInfo.IsLinux)
            {
                return new ValidationFailure(null, "Must be a Synology");
            }

            if (!_indexerProxy.Test())
            {
                return new ValidationFailure(null, "Not a Synology or synoindex not available");
            }

            return null;
        }
    }
}
