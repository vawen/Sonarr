using System.Collections.Generic;
using System.Linq;
using NLog;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Profiles;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Languages;

namespace NzbDrone.Core.Tv
{
    public interface IEpisodeCutoffService
    {
        PagingSpec<Episode> EpisodesWhereCutoffUnmet(PagingSpec<Episode> pagingSpec);
    }

    public class EpisodeCutoffService : IEpisodeCutoffService
    {
        private readonly IEpisodeRepository _episodeRepository;
        private readonly IProfileService _profileService;
        private readonly Logger _logger;

        public EpisodeCutoffService(IEpisodeRepository episodeRepository, IProfileService profileService, Logger logger)
        {
            _episodeRepository = episodeRepository;
            _profileService = profileService;
            _logger = logger;
        }

        public PagingSpec<Episode> EpisodesWhereCutoffUnmet(PagingSpec<Episode> pagingSpec)
        {
            var qualitiesBelowCutoff = new List<QualitiesBelowCutoff>();
            var languagesBelowCutoff = new List<LanguagesBelowCutoff>();
            var profiles = _profileService.All();
            
            //Get all items less than the cutoff
            foreach (var profile in profiles)
            {
                var cutoffIndex = profile.Items.FindIndex(v => v.Quality == profile.Cutoff);
                var belowCutoff = profile.Items.Take(cutoffIndex).ToList();
                var languageCutoffIndex = profile.Languages.FindIndex(v => v.Language == profile.CutoffLanguage);
                var belowLanguageCutoff = profile.Languages.Take(languageCutoffIndex).ToList();

                if (belowCutoff.Any())
                {
                    qualitiesBelowCutoff.Add(new QualitiesBelowCutoff(profile.Id, belowCutoff.Select(i => i.Quality.Id)));
                }

                if (belowLanguageCutoff.Any() && profile.AllowLanguageUpgrade)
                {
                    languagesBelowCutoff.Add(new LanguagesBelowCutoff(profile.Id, belowLanguageCutoff.Select(l => l.Language.Id)));
                }
            }

            return _episodeRepository.EpisodesWhereCutoffUnmet(pagingSpec, qualitiesBelowCutoff, languagesBelowCutoff, false);
        }
    }
}
