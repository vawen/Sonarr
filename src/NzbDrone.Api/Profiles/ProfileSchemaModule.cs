using System.Collections.Generic;
using System.Linq;
using NzbDrone.Api.Mapping;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Profiles;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Languages;

namespace NzbDrone.Api.Profiles
{
    public class ProfileSchemaModule : NzbDroneRestModule<ProfileResource>
    {
        private readonly IQualityDefinitionService _qualityDefinitionService;

        public ProfileSchemaModule(IQualityDefinitionService qualityDefinitionService)
            : base("/profile/schema")
        {
            _qualityDefinitionService = qualityDefinitionService;

            GetResourceAll = GetAll;
        }

        private List<ProfileResource> GetAll()
        {
            var items = _qualityDefinitionService.All()
                .OrderBy(v => v.Weight)
                .Select(v => new ProfileQualityItem { Quality = v.Quality, Allowed = false })
                .ToList();

            var profile = new Profile();
            profile.Cutoff = Quality.Unknown;
            profile.Items = items;
            profile.Language = Language.English;


            var result = new List<ProfileResource> { profile.InjectTo<ProfileResource>() };
            foreach (var p in result)
            {
                p.Languages = Language.All.OrderBy(l => l.Name).ToList();
            }
            return result;
        }
    }
}