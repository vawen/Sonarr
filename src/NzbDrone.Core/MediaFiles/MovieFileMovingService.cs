using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NLog;
using NzbDrone.Common.Disk;
using NzbDrone.Common.EnsureThat;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.MediaFiles.Events;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Organizer;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Movies;

namespace NzbDrone.Core.MediaFiles
{
    public interface IMoveMovieFiles
    {
        MovieFile MoveMovieFile(MovieFile movieFile, Movie movie);
        MovieFile MoveMovieFile(MovieFile movieFile, LocalMovie localMovie);
        MovieFile CopyMovieFile(MovieFile movieFile, LocalMovie localMovie);
    }

    public class MovieFileMovingService : IMoveMovieFiles
    {
        private readonly IMovieService _movieService;
        private readonly IUpdateMovieFileService _updateMovieFileService;
        private readonly IBuildFileNames _buildFileNames;
        private readonly IDiskTransferService _diskTransferService;
        private readonly IDiskProvider _diskProvider;
        private readonly IMediaFileAttributeService _mediaFileAttributeService;
        private readonly IEventAggregator _eventAggregator;
        private readonly IConfigService _configService;
        private readonly Logger _logger;

        public MovieFileMovingService(IMovieService movieService,
                                      IUpdateMovieFileService updateMovieFileService,
                                      IBuildFileNames buildFileNames,
                                      IDiskTransferService diskTransferService,
                                      IDiskProvider diskProvider,
                                      IMediaFileAttributeService mediaFileAttributeService,
                                      IEventAggregator eventAggregator,
                                      IConfigService configService,
                                      Logger logger)
        {
            _updateMovieFileService = updateMovieFileService;
            _buildFileNames = buildFileNames;
            _diskTransferService = diskTransferService;
            _diskProvider = diskProvider;
            _mediaFileAttributeService = mediaFileAttributeService;
            _eventAggregator = eventAggregator;
            _configService = configService;
            _logger = logger;
        }

        public MovieFile MoveMovieFile(MovieFile movieFile, Movie movie)
        {
            var newFileName = _buildFileNames.BuildFileName(movie, movieFile);
            var filePath = _buildFileNames.BuildFilePath(movie, newFileName, Path.GetExtension(movieFile.RelativePath));

            EnsureMovieFolder(movieFile, movie);

            _logger.Debug("Renaming movie file: {0} to {1}", movieFile, filePath);
            
            return TransferFile(movieFile, movie, filePath, TransferMode.Move);
        }

        public MovieFile MoveMovieFile(MovieFile movieFile, LocalMovie localMovie)
        {
            var movie = localMovie.Movie;
            return MoveMovieFile(movieFile, movie);
        }

        public MovieFile CopyMovieFile(MovieFile movieFile, LocalMovie localMovie)
        {
            var movie = localMovie.Movie;
            var newFileName = _buildFileNames.BuildFileName(movie, movieFile);
            var filePath = _buildFileNames.BuildFilePath(movie, newFileName, Path.GetExtension(movieFile.RelativePath));

            EnsureMovieFolder(movieFile, movie);

            if (_configService.CopyUsingHardlinks)
            {
                _logger.Debug("Hardlinking movie file: {0} to {1}", movieFile.Path, filePath);
                return TransferFile(movieFile, movie, filePath, TransferMode.HardLinkOrCopy);
            }

            _logger.Debug("Copying movie file: {0} to {1}", movieFile.Path, filePath);
            return TransferFile(movieFile, movie, filePath, TransferMode.Copy);
        }
        
        private MovieFile TransferFile(MovieFile movieFile, Movie movie, string destinationFilePath, TransferMode mode)
        {
            Ensure.That(movieFile, () => movieFile).IsNotNull();
            Ensure.That(movie,() => movie).IsNotNull();
            Ensure.That(destinationFilePath, () => destinationFilePath).IsValidPath();

            var movieFilePath = movieFile.Path ?? Path.Combine(movie.Path, movieFile.RelativePath);

            if (!_diskProvider.FileExists(movieFilePath))
            {
                throw new FileNotFoundException("Episode file path does not exist", movieFilePath);
            }

            if (movieFilePath == destinationFilePath)
            {
                throw new SameFilenameException("File not moved, source and destination are the same", movieFilePath);
            }

            _diskTransferService.TransferFile(movieFilePath, destinationFilePath, mode);

            movieFile.RelativePath = movie.Path.GetRelativePath(destinationFilePath);

            _updateMovieFileService.ChangeFileDateForFile(movieFile, movie);

            try
            {
                _mediaFileAttributeService.SetFolderLastWriteTime(movie.Path, movieFile.DateAdded);
            }

            catch (Exception ex)
            {
                _logger.WarnException("Unable to set last write time", ex);
            }

            _mediaFileAttributeService.SetFilePermissions(destinationFilePath);

            return movieFile;
        }

        private void EnsureMovieFolder(MovieFile movieFile, Movie movie)
        {
            var movieFolder = movie.Path;
            var rootFolder = Path.GetDirectoryName(movieFolder);

            if (!_diskProvider.FolderExists(rootFolder))
            {
                throw new DirectoryNotFoundException(string.Format("Root folder '{0}' was not found.", rootFolder));
            }

            if (!_diskProvider.FolderExists(movieFolder))
            {
                CreateFolder(movieFolder);
                var newEvent = new MovieFolderCreatedEvent(movie, movieFile);
                newEvent.MovieFolder = movieFolder;
                _eventAggregator.PublishEvent(newEvent);
            }
        }

        private void CreateFolder(string directoryName)
        {
            Ensure.That(directoryName, () => directoryName).IsNotNullOrWhiteSpace();

            var parentFolder = Path.GetDirectoryName(directoryName);
            if (!_diskProvider.FolderExists(parentFolder))
            {
                CreateFolder(parentFolder);
            }

            try
            {
                _diskProvider.CreateFolder(directoryName);
            }
            catch (IOException ex)
            {
                _logger.ErrorException("Unable to create directory: " + directoryName, ex);
            }

            _mediaFileAttributeService.SetFolderPermissions(directoryName);
        }
    }
}
