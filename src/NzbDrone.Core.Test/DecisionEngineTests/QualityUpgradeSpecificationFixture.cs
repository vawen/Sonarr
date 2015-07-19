using System;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Profiles;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.DecisionEngine;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Core.Languages;
using System.Collections.Generic;

namespace NzbDrone.Core.Test.DecisionEngineTests
{
    [TestFixture]
    
    public class QualityUpgradeSpecificationFixture : CoreTest<UpgradableSpecification>
    {
        public static object[] IsUpgradeTestCases =
        {
            new object[] { Quality.SDTV, 1, Quality.SDTV, 2, Quality.SDTV, true },
            new object[] { Quality.WEBDL720p, 1, Quality.WEBDL720p, 2, Quality.WEBDL720p, true },
            new object[] { Quality.SDTV, 1, Quality.SDTV, 1, Quality.SDTV, false },
            new object[] { Quality.WEBDL720p, 1, Quality.HDTV720p, 2, Quality.Bluray720p, false },
            new object[] { Quality.WEBDL720p, 1, Quality.HDTV720p, 2, Quality.WEBDL720p, false },
            new object[] { Quality.WEBDL720p, 1, Quality.WEBDL720p, 1, Quality.WEBDL720p, false },
            new object[] { Quality.WEBDL1080p, 1, Quality.WEBDL1080p, 1, Quality.WEBDL1080p, false }
        };

        public static object[] IsUpgradeTestCasesLanguages =
        {
            new object[] { Quality.SDTV, 1, Language.English, Quality.SDTV, 2, Language.English, Quality.SDTV, Language.Spanish, true },
            new object[] { Quality.WEBDL720p, 1, Language.French, Quality.WEBDL720p, 2, Language.English, Quality.WEBDL720p, Language.Spanish, false },
            new object[] { Quality.SDTV, 1, Language.English, Quality.SDTV, 1, Language.English, Quality.SDTV, Language.English, false },
            new object[] { Quality.WEBDL720p, 1, Language.English, Quality.HDTV720p, 2, Language.Spanish, Quality.Bluray720p, Language.Spanish, true },
            new object[] { Quality.WEBDL720p, 1, Language.Spanish, Quality.HDTV720p, 2, Language.French, Quality.WEBDL720p, Language.Spanish, true }
        };


        public static object[] IsUpgradeTestCasesLanguagesCantUpgrade =
        {
            new object[] { Quality.SDTV, 1, Language.English, Quality.SDTV, 2, Language.English, Quality.SDTV, Language.Spanish, true },
            new object[] { Quality.WEBDL720p, 1, Language.French, Quality.WEBDL720p, 2, Language.English, Quality.WEBDL720p, Language.Spanish, true },
            new object[] { Quality.WEBDL720p, 1, Language.French, Quality.WEBDL720p, 1, Language.English, Quality.WEBDL720p, Language.Spanish, false },
            new object[] { Quality.SDTV, 1, Language.English, Quality.SDTV, 1, Language.English, Quality.SDTV, Language.English, false },
            new object[] { Quality.WEBDL720p, 1, Language.English, Quality.HDTV720p, 2, Language.Spanish, Quality.Bluray720p, Language.Spanish, false },
            new object[] { Quality.WEBDL720p, 1, Language.Spanish, Quality.HDTV720p, 2, Language.French, Quality.WEBDL720p, Language.Spanish, false },
            new object[] { Quality.SDTV, 1, Language.Spanish, Quality.HDTV720p, 2, Language.English, Quality.WEBDL720p, Language.Spanish, true }
        };


        [SetUp]
        public void Setup()
        {

        }

        private void GivenAutoDownloadPropers(bool autoDownloadPropers)
        {
            Mocker.GetMock<IConfigService>()
                  .SetupGet(s => s.AutoDownloadPropers)
                  .Returns(autoDownloadPropers);
        }

        [Test, TestCaseSource("IsUpgradeTestCases")]
        public void IsUpgradeTest(Quality current, Int32 currentVersion, Quality newQuality, Int32 newVersion, Quality cutoff, Boolean expected)
        {
            GivenAutoDownloadPropers(true);


            var profile = new Profile
            {
                Items = Qualities.QualityFixture.GetDefaultQualities(),
                Languages = Language.All
                                .OrderByDescending(l => l.Name)
                                .Select(v => new ProfileLanguageItem { Language = v, Allowed = v == Language.English })
                                .ToList(),
                AllowLanguageUpgrade = false,
                LanguageOverQuality = false

            };

            Subject.IsUpgradable(profile, new QualityModel(current, new Revision(version: currentVersion)), Language.English, new QualityModel(newQuality, new Revision(version: newVersion)))
                    .Should().Be(expected);
        }

        [Test, TestCaseSource("IsUpgradeTestCasesLanguages")]
        public void IsUpgradeTestLanguage(Quality current, Int32 currentVersion, Language currentLanguage, Quality newQuality,
            Int32 newVersion, Language newLanguage, Quality cutoff, Language languageCutoff, Boolean expected)
        {
            GivenAutoDownloadPropers(true);

            var languages = new List<ProfileLanguageItem>();
            languages.Add(new ProfileLanguageItem { Allowed = true, Language = Language.English });
            languages.Add(new ProfileLanguageItem { Allowed = true, Language = Language.Spanish });
            languages.Add(new ProfileLanguageItem { Allowed = true, Language = Language.French });


            var profile = new Profile
            {
                Items = Qualities.QualityFixture.GetDefaultQualities(),
                Languages = languages,
                CutoffLanguage = languageCutoff,
                Cutoff = cutoff,
                AllowLanguageUpgrade = true,
                LanguageOverQuality = true
            };

            Subject.IsUpgradable(profile, new QualityModel(current, new Revision(version: currentVersion)), currentLanguage, new QualityModel(newQuality, new Revision(version: newVersion)), newLanguage)
                    .Should().Be(expected);
        }

        [Test, TestCaseSource("IsUpgradeTestCasesLanguagesCantUpgrade")]
        public void IsUpgradeTestLanguagesCantUpgrade(Quality current, Int32 currentVersion, Language currentLanguage, Quality newQuality,
            Int32 newVersion, Language newLanguage, Quality cutoff, Language languageCutoff, Boolean expected)
        {
            GivenAutoDownloadPropers(true);

            var languages = new List<ProfileLanguageItem>();
            languages.Add(new ProfileLanguageItem { Allowed = true, Language = Language.English });
            languages.Add(new ProfileLanguageItem { Allowed = true, Language = Language.Spanish });
            languages.Add(new ProfileLanguageItem { Allowed = true, Language = Language.French });


            var profile = new Profile
            {
                Items = Qualities.QualityFixture.GetDefaultQualities(),
                Languages = languages,
                CutoffLanguage = languageCutoff,
                Cutoff = cutoff,
                AllowLanguageUpgrade = false,
                LanguageOverQuality = true
            };

            Subject.IsUpgradable(profile, new QualityModel(current, new Revision(version: currentVersion)), currentLanguage, new QualityModel(newQuality, new Revision(version: newVersion)), newLanguage)
                    .Should().Be(expected);
        }


        [Test]
        public void should_return_false_if_proper_and_autoDownloadPropers_is_false()
        {
            GivenAutoDownloadPropers(false);

            var profile = new Profile 
            { 
                Items = Qualities.QualityFixture.GetDefaultQualities(),
                Languages = Language.All
                                .OrderByDescending(l => l.Name)
                                .Select(v => new ProfileLanguageItem { Language = v, Allowed = v == Language.English })
                                .ToList()
            };


            Subject.IsUpgradable(profile, new QualityModel(Quality.DVD, new Revision(version: 2)), Language.English, new QualityModel(Quality.DVD, new Revision(version: 1)))
                    .Should().BeFalse();
        }
    }
}