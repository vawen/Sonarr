using System.Collections.Generic;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Analizers;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Core.Tv;

namespace NzbDrone.Core.Test.ParserTests.NewParser
{
    [TestFixture]
    public class AnimeMetadataParserFixture : CoreTest<NewParseProvider>
    {

        [SetUp]
        public void Setup()
        {
            Subject.SetAnalizers(new List<IAnalizeContent> { new AnalizeAudio(), new AnalizeCodec(), new AnalizeDaily(), new AnalizeHash(), new AnalizeLanguage(), new AnalizeResolution(), new AnalizeSeason(), new AnalizeSource(), new AnalizeSpecial(), new AnalizeYear(), new AnalizeAbsoluteEpisodeNumber() });
        }

        [TestCase("[SubDESU]_High_School_DxD_07_(1280x720_x264-AAC)_[6B7FD717]", "SubDESU", "6B7FD717")]
        [TestCase("[Chihiro]_Working!!_-_06_[848x480_H.264_AAC][859EEAFA]", "Chihiro", "859EEAFA")]
        [TestCase("[Underwater]_Rinne_no_Lagrange_-_12_(720p)_[5C7BC4F9]", "Underwater", "5C7BC4F9")]
        [TestCase("[HorribleSubs]_Hunter_X_Hunter_-_33_[720p]", "HorribleSubs", "")]
        [TestCase("[HorribleSubs] Tonari no Kaibutsu-kun - 13 [1080p].mkv", "HorribleSubs", "")]
        [TestCase("[Doremi].Yes.Pretty.Cure.5.Go.Go!.31.[1280x720].[C65D4B1F].mkv", "Doremi", "C65D4B1F")]
        [TestCase("[Doremi].Yes.Pretty.Cure.5.Go.Go!.31.[1280x720].[C65D4B1F]", "Doremi", "C65D4B1F")]
        [TestCase("[Doremi].Yes.Pretty.Cure.5.Go.Go!.31.[1280x720].mkv", "Doremi", "")]
        [TestCase("[K-F] One Piece 214", "K-F", "")]
        [TestCase("[K-F] One Piece S10E14 214", "K-F", "")]
        [TestCase("[K-F] One Piece 10x14 214", "K-F", "")]
        [TestCase("[K-F] One Piece 214 10x14", "K-F", "")]
        [TestCase("Bleach - 031 - The Resolution to Kill [Lunar].avi", "Lunar", "")]
        [TestCase("[ACX]Hack Sign 01 Role Play [Kosaka] [9C57891E].mkv", "ACX", "9C57891E")]
        [TestCase("[S-T-D] Soul Eater Not! - 06 (1280x720 10bit AAC) [59B3F2EA].mkv", "S-T-D", "59B3F2EA")]
        public void should_parse_absolute_numbers(string postTitle, string subGroup, string hash)
        {
            if (hash.Length > 0)
            {
                Mocker.GetMock<ISeriesService>()
                    .Setup(p => p.FindByTitle(It.Is<string>(s => !s.Contains(subGroup.NormalizeTitle()) && !s.Contains(hash.NormalizeTitle()))))
                    .Returns(new Series { SeriesType = SeriesTypes.Anime });
            }
            else
            {
                Mocker.GetMock<ISeriesService>()
                    .Setup(p => p.FindByTitle(It.Is<string>(s => !s.Contains(subGroup.NormalizeTitle()))))
                    .Returns(new Series { SeriesType = SeriesTypes.Anime });

            }

            var result = Subject.ParseTitle(postTitle);
            result.Should().NotBeNull();
            result.ReleaseGroup.Should().Be(subGroup);
            result.ReleaseHash.Should().Be(hash == "" ? null : hash);
        }
    }
}
