using System.Collections.Generic;
using System.IO;
using System.Linq;
using NLog;
using NzbDrone.Core.MediaFiles.Events;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Tv;
using NzbDrone.Core.Tv.Events;
using NzbDrone.Common;
using NzbDrone.Core.Movies;
using NzbDrone.Core.Movies.Events;

namespace NzbDrone.Core.MediaFiles
{
    public interface IMovieMediaFileService
    {
        MovieFile Add(MovieFile movieFile);
        void Update(MovieFile movieFile);
        void Delete(MovieFile movieFile, DeleteMediaFileReason reason);
        List<MovieFile> GetFileByMovie(int movieId);
        MovieFile Get(int movieFileId);
        List<MovieFile> Get(IEnumerable<int> movieFileId);
        //MovieFile GetFilesWithoutMediaInfo();
        List<string> FilterExistingFiles(List<string> files, Movie movie);

    }

    public class MovieMediaFileService : IMovieMediaFileService, IHandleAsync<MovieDeletedEvent>
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly IMovieMediaFileRepository _movieMediaFileRepository;
        private readonly Logger _logger;

        public MovieMediaFileService(IMovieMediaFileRepository movieMediaFileRepository, IEventAggregator eventAggregator, Logger logger)
        {
            _movieMediaFileRepository = movieMediaFileRepository;
            _eventAggregator = eventAggregator;
            _logger = logger;
        }

        public MovieFile Add(MovieFile movieFile)
        {
            var addedFile = _movieMediaFileRepository.Insert(movieFile);
            _eventAggregator.PublishEvent(new MovieFileAddedEvent(addedFile));
            return addedFile;
        }

        public MovieFile Get(int movieFileId)
        {
            return _movieMediaFileRepository.Get(movieFileId);
        }

        public List<MovieFile> Get(List<int> movieFileId)
        {
            return _movieMediaFileRepository.Get(movieFileId).ToList();
        }

        public void Update(MovieFile movieFile)
        {
            _movieMediaFileRepository.Update(movieFile);
        }

        public void Delete(MovieFile movieFile, DeleteMediaFileReason reason)
        {
            //Little hack so we have the episodes and series attached for the event consumers
            movieFile.Movie.LazyLoad();
            movieFile.Path = Path.Combine(movieFile.Movie.Value.Path, movieFile.RelativePath);


            _movieMediaFileRepository.Delete(movieFile);
            _eventAggregator.PublishEvent(new MovieFileDeletedEvent(movieFile, reason));
        }

        /*public List<EpisodeFile> GetFilesWithoutMediaInfo()
        {
            return _mediaFileRepository.GetFilesWithoutMediaInfo();
        }*/

        public List<string> FilterExistingFiles(List<string> files, Movie movie)
        {
            var movieFile = _movieMediaFileRepository.All().Where(m => m.MovieId == movie.Id).SingleOrDefault();

            if (movieFile == null) return files;

            return files.Except(new List<string>{Path.Combine(movie.Path, movieFile.RelativePath)},  PathEqualityComparer.Instance).ToList();
        }

        public List<MovieFile> GetFileByMovie(int movieId)
        {
            return _movieMediaFileRepository.All().Where(m => m.MovieId == movieId).ToList();
        }

        public List<MovieFile> Get(IEnumerable<int> ids)
        {
            return _movieMediaFileRepository.Get(ids).ToList();
        }

        public void HandleAsync(MovieDeletedEvent message)
        {
            var file = GetFileByMovie(message.Movie.Id).SingleOrDefault();
            if (file != null)
                _movieMediaFileRepository.Delete(file);
        }
    }
}