using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NLog;
using NzbDrone.Common.Disk;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.MediaFiles.Events;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Download;

namespace NzbDrone.Core.MediaFiles.MovieImport
{
    public interface IImportApprovedMovies
    {
        List<ImportMovieResult> Import(List<ImportMovieDecision> decisions, bool newDownload, DownloadClientItem downloadClientItem = null);
    }

    public class ImportApprovedMovies : IImportApprovedMovies
    {
        private readonly IUpgradeMovieMediaFiles _movieFileUpgrader;
        private readonly IMovieMediaFileService _movieMediaFileService;
        private readonly IDiskProvider _diskProvider;
        private readonly IEventAggregator _eventAggregator;
        private readonly Logger _logger;

        public ImportApprovedMovies(IUpgradeMovieMediaFiles movieFileUpgrader,
                                    IMovieMediaFileService movieMediaFileService,
                                    IDiskProvider diskProvider,
                                    IEventAggregator eventAggregator,
                                      Logger logger)
        {
            _movieFileUpgrader = movieFileUpgrader;
            _movieMediaFileService = movieMediaFileService;
            _diskProvider = diskProvider;
            _eventAggregator = eventAggregator;
            _logger = logger;
        }

        public List<ImportMovieResult> Import(List<ImportMovieDecision> decisions, bool newDownload, DownloadClientItem downloadClientItem = null)
        {
            var qualifiedImports = decisions.Where(c => c.Approved)
               .GroupBy(c => c.LocalMovie.Movie.Id, (i, s) => s
                   .OrderByDescending(c => c.LocalMovie.Quality, new QualityModelComparer(s.First().LocalMovie.Movie.Profile))
                   .ThenByDescending(c => c.LocalMovie.Size))
               .SelectMany(c => c)
               .ToList();

            var importResults = new List<ImportMovieResult>();

            foreach (var importDecision in qualifiedImports.OrderByDescending(e => e.LocalMovie.Size))
            {
                var localMovie = importDecision.LocalMovie;
                var oldFile = new MovieFile();

                try
                {
                    var movieFile = new MovieFile();
                    movieFile.DateAdded = DateTime.UtcNow;
                    movieFile.MovieId = localMovie.Movie.Id;
                    movieFile.Path = localMovie.Path.CleanFilePath();
                    movieFile.Size = _diskProvider.GetFileSize(localMovie.Path);
                    movieFile.Quality = localMovie.Quality;
                    movieFile.MediaInfo = localMovie.MediaInfo;
                    movieFile.ReleaseGroup = localMovie.ParsedMovieInfo.ReleaseGroup;

                    if (newDownload)
                    {
                        bool copyOnly = downloadClientItem != null && downloadClientItem.IsReadOnly;

                        movieFile.SceneName = GetSceneName(downloadClientItem, localMovie);

                        var moveResult = _movieFileUpgrader.UpgradeMovieFile(movieFile, localMovie, copyOnly);
                        oldFile = moveResult.OldFile;
                    }
                    else
                    {
                        movieFile.RelativePath = localMovie.Movie.Path.GetRelativePath(movieFile.Path);
                    }

                    _movieMediaFileService.Add(movieFile);
                    importResults.Add(new ImportMovieResult(importDecision));

                    if (downloadClientItem != null)
                    {
                        _eventAggregator.PublishEvent(new MovieImportedEvent(localMovie, movieFile, newDownload, downloadClientItem.DownloadClient, downloadClientItem.DownloadId));
                    }
                    else
                    {
                        _eventAggregator.PublishEvent(new MovieImportedEvent(localMovie, movieFile, newDownload));
                    }

                    if (newDownload)
                    {
                        _eventAggregator.PublishEvent(new MovieDownloadedEvent(localMovie, movieFile, oldFile));
                    }
                }
                catch (Exception e)
                {
                    _logger.WarnException("Couldn't import movie " + localMovie, e);
                    importResults.Add(new ImportMovieResult(importDecision, "Failed to import movie"));
                }
            }

            //Adding all the rejected decisions
            importResults.AddRange(decisions.Where(c => !c.Approved)
                                            .Select(d => new ImportMovieResult(d, d.Rejections.Select(r => r.Reason).ToArray())));

            return importResults;
        }

        private string GetSceneName(DownloadClientItem downloadClientItem, LocalMovie localMovie)
        {
            if (downloadClientItem != null)
            {
                var title = Parser.Parser.RemoveFileExtension(downloadClientItem.Title);

                var parsedTitle = Parser.Parser.ParseMovieTitle(title);

                if (parsedTitle != null)
                {
                    return title;
                }
            }

            var fileName = Path.GetFileNameWithoutExtension(localMovie.Path.CleanFilePath());

            if (SceneChecker.IsSceneTitle(fileName))
            {
                return fileName;
            }

            return null;
        }
    }
}
