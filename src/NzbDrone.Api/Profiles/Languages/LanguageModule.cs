using System;
using System.Collections.Generic;
using System.Linq;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Languages;

namespace NzbDrone.Api.Profiles.Languages
{
    public class LanguageModule : NzbDroneRestModule<LanguageResource>
    {
        public LanguageModule()
        {
            GetResourceAll = GetAll;
            GetResourceById = GetById;
        }

        private LanguageResource GetById(int id)
        {
            var language = Language.FindById(id);

            return new LanguageResource
            {
                Id = language.Id,
                Name = language.Name
            };
        }

        private List<LanguageResource> GetAll()
        {

            return Language.All.
                Select(l => new LanguageResource
                    {
                        Id = l.Id,
                        Name = l.Name
                    })
                    .OrderByDescending(l => l.Name)
                    .ToList();
        }
    }
}