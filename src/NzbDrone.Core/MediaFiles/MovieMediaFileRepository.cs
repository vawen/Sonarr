using NzbDrone.Core.Datastore;
using NzbDrone.Core.Messaging.Events;
using System.Linq;

namespace NzbDrone.Core.MediaFiles
{
    public interface IMovieMediaFileRepository : IBasicRepository<MovieFile>
    {
        //MovieFile GetFileByMovie(int movieId);
        //List<MovieFile> GetFilesWithoutMediaInfo();
    }


    public class MovieMediaFileRepository : BasicRepository<MovieFile>, IMovieMediaFileRepository
    {
        public MovieMediaFileRepository(IMainDatabase database, IEventAggregator eventAggregator)
            : base(database, eventAggregator)
        {
        }

        /*public MovieFile GetFileByMovie(int movieId)
        {
            return Query.Where(c => c.MovieId == movieId).SingleOrDefault();
        }*/


       /* public List<EpisodeFile> GetFilesWithoutMediaInfo()
        {
            return Query.Where(c => c.MediaInfo == null).ToList();
        }*/
    }
}