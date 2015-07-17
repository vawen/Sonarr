using FluentAssertions;
using System.Linq;
using NUnit.Framework;
using NzbDrone.Core.Profiles;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.DecisionEngine;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Core.Languages;
using System.Collections.Generic;

namespace NzbDrone.Core.Test.DecisionEngineTests
{
    [TestFixture]
    public class CutoffSpecificationFixture : CoreTest<QualityUpgradableSpecification>
    {
        [Test]
        public void should_return_true_if_current_episode_is_less_than_cutoff()
        {
            Subject.CutoffNotMet(
                new Profile 
                { 
                    Cutoff = Quality.Bluray1080p, 
                    Items = Qualities.QualityFixture.GetDefaultQualities(), 
                    AllowLanguageUpgrade = false,
                    LanguageOverQuality = false,
                    Languages = Language.All
                        .OrderByDescending(l => l.Name)
                        .Select(v => new ProfileLanguageItem { Language = v, Allowed = v == Language.English})
                        .ToList(),

                },
                new QualityModel(Quality.DVD, new Revision(version: 2)), Language.English).Should().BeTrue();
        }

        [Test]
        public void should_return_false_if_current_episode_is_equal_to_cutoff()
        {
            Subject.CutoffNotMet(
                new Profile
                {
                    Cutoff = Quality.HDTV720p, 
                    Items = Qualities.QualityFixture.GetDefaultQualities(),
                    AllowLanguageUpgrade = false,
                    LanguageOverQuality = false,
                    Languages = Language.All
                        .OrderByDescending(l => l.Name)
                        .Select(v => new ProfileLanguageItem { Language = v, Allowed = v == Language.English })
                        .ToList()
                },
                new QualityModel(Quality.HDTV720p, new Revision(version: 2)), Language.English).Should().BeFalse();
        }

        [Test]
        public void should_return_false_if_current_episode_is_greater_than_cutoff()
        {
            Subject.CutoffNotMet(
                new Profile 
                { 
                    Cutoff = Quality.HDTV720p, 
                    Items = Qualities.QualityFixture.GetDefaultQualities(),
                    AllowLanguageUpgrade = false,
                    LanguageOverQuality = false,
                    Languages = Language.All
                        .OrderByDescending(l => l.Name)
                        .Select(v => new ProfileLanguageItem { Language = v, Allowed = v == Language.English })
                        .ToList()
                },
                 new QualityModel(Quality.Bluray1080p, new Revision(version: 2)), Language.English).Should().BeFalse();
        }

        [Test]
        public void should_return_true_when_new_episode_is_proper_but_existing_is_not()
        {
            Subject.CutoffNotMet(
                new Profile 
                { 
                    Cutoff = Quality.HDTV720p, 
                    Items = Qualities.QualityFixture.GetDefaultQualities(),
                    AllowLanguageUpgrade = false,
                    LanguageOverQuality = false,
                    Languages = Language.All
                    .OrderByDescending(l => l.Name)
                    .Select(v => new ProfileLanguageItem { Language = v, Allowed = v == Language.English })
                    .ToList()
                },
                new QualityModel(Quality.HDTV720p, new Revision(version: 1)),
                Language.English,
                new QualityModel(Quality.HDTV720p, new Revision(version: 2))).Should().BeTrue();
        }

        [Test]
        public void should_return_false_if_cutoff_is_met_and_quality_is_higher()
        {
            Subject.CutoffNotMet(new Profile 
            {
                Cutoff = Quality.HDTV720p, 
                Items = Qualities.QualityFixture.GetDefaultQualities(),
                AllowLanguageUpgrade = false,
                LanguageOverQuality = false,
                Languages = Language.All
                    .OrderByDescending(l => l.Name)
                    .Select(v => new ProfileLanguageItem { Language = v, Allowed = v == Language.English })
                    .ToList()
            },
            new QualityModel(Quality.HDTV720p, new Revision(version: 2)),
            Language.English,
            new QualityModel(Quality.Bluray1080p, new Revision(version: 2))).Should().BeFalse();
        }

        [Test]
        public void should_return_false_if_cutoff_is_met_and_quality_is_higher_but_language_is_not_met()
        {

            Profile _profile = new Profile
                {
                    Cutoff = Quality.HDTV720p,
                    Items = Qualities.QualityFixture.GetDefaultQualities(),
                    AllowLanguageUpgrade = false,
                    LanguageOverQuality = false
                };

            _profile.CutoffLanguage = Language.Spanish;
            _profile.Languages = new List<ProfileLanguageItem>();
            _profile.Languages.Add(new ProfileLanguageItem {Allowed = true, Language = Language.English });
            _profile.Languages.Add(new ProfileLanguageItem {Allowed = true, Language = Language.Spanish });
            _profile.Languages.Add(new ProfileLanguageItem {Allowed = true, Language = Language.French });

            Subject.CutoffNotMet(_profile,
            new QualityModel(Quality.HDTV720p, new Revision(version: 2)),
            Language.English,
            new QualityModel(Quality.Bluray1080p, new Revision(version: 2))).Should().BeTrue();
        }

        [Test]
        public void should_return_true_if_cutoff_is_met_and_quality_is_higher_but_language_is_not_met()
        {

            Profile _profile = new Profile
            {
                Cutoff = Quality.HDTV720p,
                Items = Qualities.QualityFixture.GetDefaultQualities(),
                AllowLanguageUpgrade = true,
                LanguageOverQuality = false
            };

            _profile.CutoffLanguage = Language.Spanish;
            _profile.Languages = new List<ProfileLanguageItem>();
            _profile.Languages.Add(new ProfileLanguageItem { Allowed = true, Language = Language.English });
            _profile.Languages.Add(new ProfileLanguageItem { Allowed = true, Language = Language.Spanish });
            _profile.Languages.Add(new ProfileLanguageItem { Allowed = true, Language = Language.French });

            Subject.CutoffNotMet(_profile,
            new QualityModel(Quality.HDTV720p, new Revision(version: 2)),
            Language.English,
            new QualityModel(Quality.Bluray1080p, new Revision(version: 2))).Should().BeTrue();
        }

        [Test]
        public void should_return_false_if_cutoff_is_met_and_quality_is_higher_and_language_is_met()
        {

            Profile _profile = new Profile
            {
                Cutoff = Quality.HDTV720p,
                Items = Qualities.QualityFixture.GetDefaultQualities(),
                AllowLanguageUpgrade = false,
                LanguageOverQuality = false
            };

            _profile.CutoffLanguage = Language.Spanish;
            _profile.Languages = new List<ProfileLanguageItem>();
            _profile.Languages.Add(new ProfileLanguageItem { Allowed = true, Language = Language.English });
            _profile.Languages.Add(new ProfileLanguageItem { Allowed = true, Language = Language.Spanish });
            _profile.Languages.Add(new ProfileLanguageItem { Allowed = true, Language = Language.French });

            Subject.CutoffNotMet(_profile,
            new QualityModel(Quality.HDTV720p, new Revision(version: 2)),
            Language.Spanish,
            new QualityModel(Quality.Bluray1080p, new Revision(version: 2))).Should().BeFalse();
        }

        [Test]
        public void should_return_false_if_cutoff_is_met_and_quality_is_higher_and_language_is_higher()
        {

            Profile _profile = new Profile
            {
                Cutoff = Quality.HDTV720p,
                Items = Qualities.QualityFixture.GetDefaultQualities(),
                AllowLanguageUpgrade = false,
                LanguageOverQuality = false
            };

            _profile.CutoffLanguage = Language.Spanish;
            _profile.Languages = new List<ProfileLanguageItem>();
            _profile.Languages.Add(new ProfileLanguageItem { Allowed = true, Language = Language.English });
            _profile.Languages.Add(new ProfileLanguageItem { Allowed = true, Language = Language.Spanish });
            _profile.Languages.Add(new ProfileLanguageItem { Allowed = true, Language = Language.French });

            Subject.CutoffNotMet(_profile,
            new QualityModel(Quality.HDTV720p, new Revision(version: 2)),
            Language.French,
            new QualityModel(Quality.Bluray1080p, new Revision(version: 2))).Should().BeFalse();
        }

        [Test]
        public void should_return_true_if_cutoff_is_not_met_and_new_quality_is_higher_and_language_is_higher()
        {

            Profile _profile = new Profile
            {
                Cutoff = Quality.HDTV720p,
                Items = Qualities.QualityFixture.GetDefaultQualities(),
                AllowLanguageUpgrade = false,
                LanguageOverQuality = false
            };

            _profile.CutoffLanguage = Language.Spanish;
            _profile.Languages = new List<ProfileLanguageItem>();
            _profile.Languages.Add(new ProfileLanguageItem { Allowed = true, Language = Language.English });
            _profile.Languages.Add(new ProfileLanguageItem { Allowed = true, Language = Language.Spanish });
            _profile.Languages.Add(new ProfileLanguageItem { Allowed = true, Language = Language.French });

            Subject.CutoffNotMet(_profile,
            new QualityModel(Quality.SDTV, new Revision(version: 2)),
            Language.French,
            new QualityModel(Quality.Bluray1080p, new Revision(version: 2))).Should().BeTrue();
        }

        [Test]
        public void should_return_true_if_cutoff_is_not_met_and_language_is_higher()
        {

            Profile _profile = new Profile
            {
                Cutoff = Quality.HDTV720p,
                Items = Qualities.QualityFixture.GetDefaultQualities(),
                AllowLanguageUpgrade = false,
                LanguageOverQuality = false
            };

            _profile.CutoffLanguage = Language.Spanish;
            _profile.Languages = new List<ProfileLanguageItem>();
            _profile.Languages.Add(new ProfileLanguageItem { Allowed = true, Language = Language.English });
            _profile.Languages.Add(new ProfileLanguageItem { Allowed = true, Language = Language.Spanish });
            _profile.Languages.Add(new ProfileLanguageItem { Allowed = true, Language = Language.French });

            Subject.CutoffNotMet(_profile,
            new QualityModel(Quality.SDTV, new Revision(version: 2)),
            Language.French).Should().BeTrue();
        }
    }
}
