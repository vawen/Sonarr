using System;
using FluentValidation;
using FluentValidation.Results;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.ThingiProvider;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Indexers.Newpct
{
    public class NewpctSettingsValidator : AbstractValidator<NewpctSettings>
    {
        public NewpctSettingsValidator()
        {
            RuleFor(c => c.BaseUrl).ValidRootUrl();
        }
    }

    public class NewpctSettings : IProviderConfig
    {
        private static readonly NewpctSettingsValidator validator = new NewpctSettingsValidator();

        public NewpctSettings()
        {
            BaseUrl = "http://www.newpct.com";
            VerifiedOnly = true;
            SearchUrl = "/buscar-descargas/cID=0&tLang=0&oBy=0&oMode=0&category_=All&subcategory_=All&idioma_=1&calidad_=All&oByAux=0&oModeAux=0&size_=0&q=";
        }

        [FieldDefinition(0, Label = "Website URL")]
        public String BaseUrl { get; set; }

        [FieldDefinition(1, Label = "Verified Only", Type = FieldType.Checkbox, Advanced = true, HelpText = "By setting this to No you will likely get more junk and unconfirmed releases, so use it with caution.")]
        public Boolean VerifiedOnly { get; set; }

        public ValidationResult Validate()
        {
            return validator.Validate(this);
        }

        public string SearchUrl { get; set; }
    }
}
