using System;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using FluentValidation.Results;
using NLog;
using NzbDrone.Common.Disk;
using NzbDrone.Common.Processes;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Tv;
using NzbDrone.Core.Validation;
using NzbDrone.Core.Movies;

namespace NzbDrone.Core.Notifications.CustomScript
{
    public interface ICustomScriptService
    {
        void OnDownload(Series series, EpisodeFile episodeFile, CustomScriptSettings settings);
        void OnDownloadMovie(Movie movie, MovieFile movieFile, CustomScriptSettings settings);
        void OnRename(Series series, CustomScriptSettings settings);
        void OnRenameMovie(Movie movie, CustomScriptSettings settings);
        ValidationFailure Test(CustomScriptSettings settings);
    }

    public class CustomScriptService : ICustomScriptService
    {
        private readonly IProcessProvider _processProvider;
        private readonly IDiskProvider _diskProvider;
        private readonly Logger _logger;

        public CustomScriptService(IProcessProvider processProvider, IDiskProvider diskProvider, Logger logger)
        {
            _processProvider = processProvider;
            _diskProvider = diskProvider;
            _logger = logger;
        }

        public void OnDownload(Series series, EpisodeFile episodeFile, CustomScriptSettings settings)
        {
            var environmentVariables = new StringDictionary();

            environmentVariables.Add("Sonarr.EventType", "Download");
            environmentVariables.Add("Sonarr.Series.Id", series.Id.ToString());
            environmentVariables.Add("Sonarr.Series.Title", series.Title);
            environmentVariables.Add("Sonarr.Series.Path", series.Path);
            environmentVariables.Add("Sonarr.Series.TvdbId", series.TvdbId.ToString());
            environmentVariables.Add("Sonarr.EpisodeFile.Id", episodeFile.Id.ToString());
            environmentVariables.Add("Sonarr.EpisodeFile.RelativePath", episodeFile.RelativePath);
            environmentVariables.Add("Sonarr.EpisodeFile.Path", Path.Combine(series.Path, episodeFile.RelativePath));
            environmentVariables.Add("Sonarr.EpisodeFile.SeasonNumber", episodeFile.SeasonNumber.ToString());
            environmentVariables.Add("Sonarr.EpisodeFile.EpisodeNumbers", String.Join(",", episodeFile.Episodes.Value.Select(e => e.EpisodeNumber)));
            environmentVariables.Add("Sonarr.EpisodeFile.EpisodeAirDates", String.Join(",", episodeFile.Episodes.Value.Select(e => e.AirDate)));
            environmentVariables.Add("Sonarr.EpisodeFile.EpisodeAirDatesUtc", String.Join(",", episodeFile.Episodes.Value.Select(e => e.AirDateUtc)));
            environmentVariables.Add("Sonarr.EpisodeFile.Quality", episodeFile.Quality.Quality.Name);
            environmentVariables.Add("Sonarr.EpisodeFile.QualityVersion", episodeFile.Quality.Revision.Version.ToString());
            environmentVariables.Add("Sonarr.EpisodeFile.ReleaseGroup", episodeFile.ReleaseGroup ?? String.Empty);
            environmentVariables.Add("Sonarr.EpisodeFile.SceneName", episodeFile.SceneName ?? String.Empty);
            
            ExecuteScript(environmentVariables, settings);
        }

        public void OnDownloadMovie(Movie movie, MovieFile movieFile, CustomScriptSettings settings)
        {
            var environmentVariables = new StringDictionary();

            environmentVariables.Add("Sonarr.EventType", "DownloadMovie");
            environmentVariables.Add("Sonarr.Movie.Id", movie.Id.ToString());
            environmentVariables.Add("Sonarr.Movie.Title", movie.Title);
            environmentVariables.Add("Sonarr.Movie.Path", movie.Path);
            environmentVariables.Add("Sonarr.Movie.TmdbId", movie.TmdbId.ToString());
            environmentVariables.Add("Sonarr.MovieFile.Id", movieFile.Id.ToString());
            environmentVariables.Add("Sonarr.MovieFile.RelativePath", movieFile.RelativePath);
            environmentVariables.Add("Sonarr.MovieFile.Path", Path.Combine(movie.Path, movieFile.RelativePath));
            environmentVariables.Add("Sonarr.MovieFile.Quality", movieFile.Quality.Quality.Name);
            environmentVariables.Add("Sonarr.MovieFile.QualityVersion", movieFile.Quality.Revision.Version.ToString());
            environmentVariables.Add("Sonarr.MovieFile.ReleaseGroup", movieFile.ReleaseGroup ?? String.Empty);
            environmentVariables.Add("Sonarr.MovieFile.SceneName", movieFile.SceneName ?? String.Empty);

            ExecuteScript(environmentVariables, settings);
        }

        public void OnRename(Series series, CustomScriptSettings settings)
        {
            var environmentVariables = new StringDictionary();

            environmentVariables.Add("Sonarr.EventType", "Rename");
            environmentVariables.Add("Sonarr.Series.Id", series.Id.ToString());
            environmentVariables.Add("Sonarr.Series.Title", series.Title);
            environmentVariables.Add("Sonarr.Series.Path", series.Path);
            environmentVariables.Add("Sonarr.Series.TvdbId", series.TvdbId.ToString());

            ExecuteScript(environmentVariables, settings);
        }

        public void OnRenameMovie(Movie movie, CustomScriptSettings settings)
        {
            var environmentVariables = new StringDictionary();

            environmentVariables.Add("Sonarr.EventType", "RenameMovie");
            environmentVariables.Add("Sonarr.Movie.Id", movie.Id.ToString());
            environmentVariables.Add("Sonarr.Movie.Title", movie.Title);
            environmentVariables.Add("Sonarr.Movie.Path", movie.Path);
            environmentVariables.Add("Sonarr.Movie.TmdbId", movie.TmdbId.ToString());

            ExecuteScript(environmentVariables, settings);
        }

        public ValidationFailure Test(CustomScriptSettings settings)
        {
            if (!_diskProvider.FileExists(settings.Path))
            {
                return new NzbDroneValidationFailure("Path", "File does not exist");
            }

            return null;
        }

        private void ExecuteScript(StringDictionary environmentVariables, CustomScriptSettings settings)
        {
            _logger.Debug("Executing external script: {0}", settings.Path);

            var process = _processProvider.StartAndCapture(settings.Path, settings.Arguments, environmentVariables);

            _logger.Debug("Executed external script: {0} - Status: {1}", settings.Path, process.ExitCode);
            _logger.Debug("Script Output: \r\n{0}", String.Join("\r\n", process.Lines));
        }
    }
}
