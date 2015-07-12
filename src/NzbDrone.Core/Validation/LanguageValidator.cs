using FluentValidation.Validators;
using NzbDrone.Core.Languages;

namespace NzbDrone.Core.Validation
{
    public class LanguageValidator : PropertyValidator
    {
        public LanguageValidator()
            : base("Unknown Language")
        {
        }

        protected override bool IsValid(PropertyValidatorContext context)
        {
            if (context.PropertyValue == null) return false;
            if (!context.PropertyValue.GetType().Equals(typeof(Language))) return false;

            if (((Language) context.PropertyValue).Id == 0) return false;

            return true;
        }
    }
}
