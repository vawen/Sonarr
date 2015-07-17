using FluentAssertions;
using System.Linq;
using NUnit.Framework;
using NzbDrone.Core.Languages;
using NzbDrone.Core.Profiles;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.Profiles
{
    [TestFixture]
    public class ProfileRepositoryFixture : DbTest<ProfileRepository, Profile>
    {
        [Test]
        public void should_be_able_to_read_and_write()
        {
            var profile = new Profile
                {
                    Items = Qualities.QualityFixture.GetDefaultQualities(Quality.Bluray1080p, Quality.DVD, Quality.HDTV720p),
                    Languages = Language.All.OrderByDescending(l => l.Name).Select(l => new ProfileLanguageItem {Language = l, Allowed = l == Language.English}).ToList(),
                    Cutoff = Quality.Bluray1080p,
                    Name = "TestProfile",
                    CutoffLanguage = Language.English
                };

            Subject.Insert(profile);

            StoredModel.Name.Should().Be(profile.Name);
            StoredModel.Cutoff.Should().Be(profile.Cutoff);
            StoredModel.CutoffLanguage.Should().Be(profile.CutoffLanguage);

            StoredModel.Items.Should().Equal(profile.Items, (a, b) => a.Quality == b.Quality && a.Allowed == b.Allowed);
            StoredModel.Languages.Should().Equal(profile.Languages, (a, b) => a.Language == b.Language && a.Allowed == b.Allowed);
        }
    }
}