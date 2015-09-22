using NzbDrone.Api.Movies;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.DecisionEngine;
using NzbDrone.Core.MediaCover;
using NzbDrone.Core.Movies;
using NzbDrone.SignalR;

namespace NzbDrone.Api.Wanted
{
    public class MissingMovieModule : MoviesModule
    {
        public MissingMovieModule(IMovieService movieService,
                                  IQualityUpgradableSpecification qualityUpgradableSpecification,
                                  IMapCoversToLocal coverMapper,
                                  IBroadcastSignalRMessage signalRBroadcaster)
            : base(signalRBroadcaster, movieService, coverMapper, "wanted/missingMovie")
        {
            GetResourcePaged = GetMissingMovies;
        }

        private PagingResource<MoviesResource> GetMissingMovies(PagingResource<MoviesResource> pagingResource)
        {
            var pagingSpec = new PagingSpec<Movie>
            {
                Page = pagingResource.Page,
                PageSize = pagingResource.PageSize,
                SortKey = pagingResource.SortKey,
                SortDirection = pagingResource.SortDirection
            };

            if (pagingResource.FilterKey == "monitored" && pagingResource.FilterValue == "false")
            {
                pagingSpec.FilterExpression = v => v.Monitored == false;
            }
            else
            {
                pagingSpec.FilterExpression = v => v.Monitored == true;
            }

            PagingResource<MoviesResource> resource = ApplyToPage(v => _movieService.MoviesWithoutFile(v), pagingSpec);

            return resource;
        }
    }
}