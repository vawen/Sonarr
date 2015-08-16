using System.IO;
using System.Linq;
using NLog;
using NzbDrone.Common.Disk;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.MediaFiles
{
    public interface IUpgradeMovieMediaFiles
    {
        MovieFileMoveResult UpgradeMovieFile(MovieFile movieFile, LocalMovie localMovie, bool copyOnly = false);
    }

    public class UpgradeMovieMediaFileService : IUpgradeMovieMediaFiles
    {
        private readonly IRecycleBinProvider _recycleBinProvider;
        private readonly IMovieMediaFileService _movieMediaFileService;
        private readonly IMoveMovieFiles _movieFileMover;
        private readonly IDiskProvider _diskProvider;
        private readonly Logger _logger;

        public UpgradeMovieMediaFileService(IRecycleBinProvider recycleBinProvider,
                                            IMovieMediaFileService movieMediaFileService,
                                            IMoveMovieFiles movieFileMover,
                                            IDiskProvider diskProvider,
                                            Logger logger)
        {
            _recycleBinProvider = recycleBinProvider;
            _movieMediaFileService = movieMediaFileService;
            _movieFileMover = movieFileMover;
            _diskProvider = diskProvider;
            _logger = logger;
        }

        public MovieFileMoveResult UpgradeMovieFile(MovieFile movieFile, LocalMovie localMovie, bool copyOnly = false)
        {
            var moveFileResult = new MovieFileMoveResult();
            var file = localMovie.Movie.MovieFileId > 0 ? localMovie.Movie.MovieFile.Value : null;


            if (file != null)
            {
                var movieFilePath = Path.Combine(localMovie.Movie.Path, file.RelativePath);

                if (_diskProvider.FileExists(movieFilePath))
                {
                    _logger.Debug("Removing existing movie file: {0}", file);
                    _recycleBinProvider.DeleteFile(movieFilePath);
                }

                moveFileResult.OldFile = file;
                _movieMediaFileService.Delete(file, DeleteMediaFileReason.Upgrade);
            }

            if (copyOnly)
            {
                moveFileResult.MovieFile = _movieFileMover.CopyMovieFile(movieFile, localMovie);
            }
            else
            {
                moveFileResult.MovieFile = _movieFileMover.MoveMovieFile(movieFile, localMovie);
            }

            return moveFileResult;
        }
    }
}
