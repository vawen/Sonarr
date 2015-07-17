using FizzWare.NBuilder;
using FluentAssertions;
using NUnit.Framework;
using System.Linq;
using NzbDrone.Core.Languages;
using NzbDrone.Core.Profiles;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Core.Tv;

namespace NzbDrone.Core.Test.TvTests.SeriesRepositoryTests
{
    [TestFixture]

    public class SeriesRepositoryFixture : DbTest<SeriesRepository, Series>
    {
        [Test]
        public void should_lazyload_quality_profile()
        {
            var profile = new Profile
                {
                    Items = Qualities.QualityFixture.GetDefaultQualities(Quality.Bluray1080p, Quality.DVD, Quality.HDTV720p),

                    Cutoff = Quality.Bluray1080p,
                    Name = "TestProfile",
                    Languages = Language.All.OrderByDescending(l => l.Name).Select(l => new ProfileLanguageItem { Language = l, Allowed = l == Language.English}).ToList(),
                    CutoffLanguage = Language.English
                };


            Mocker.Resolve<ProfileRepository>().Insert(profile);

            var series = Builder<Series>.CreateNew().BuildNew();
            series.ProfileId = profile.Id;

            Subject.Insert(series);


            StoredModel.Profile.Should().NotBeNull();


        }
    }
}