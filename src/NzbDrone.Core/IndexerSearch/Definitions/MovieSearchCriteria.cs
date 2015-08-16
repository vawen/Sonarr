using NzbDrone.Core.Movies;
namespace NzbDrone.Core.IndexerSearch.Definitions
{
    public class MovieSearchCriteria : SearchCriteriaBase
    {
        public Movie Movie;

        public override string ToString()
        {
            return string.Format("[{0}]", Movie.Title);
        }
    }
}