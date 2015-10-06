using System.Collections.Generic;
using System.Linq;
using FizzWare.NBuilder;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Analyzers;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Core.Tv;

namespace NzbDrone.Core.Test.ParserTests.NewParser
{
    [TestFixture]
    [Category("ParserTest")]
    public class AnimeMetadataParserFixture : CoreTest<NewParseProvider>
    {

        [SetUp]
        public void Setup()
        {
            UseAnalyzers();
        }

        [TestCase("[SubDESU]_High_School_DxD_07_(1280x720_x264-AAC)_[6B7FD717]", "SubDESU", "6B7FD717", "High School DxD")]
        [TestCase("[Chihiro]_Working!!_-_06_[848x480_H.264_AAC][859EEAFA]", "Chihiro", "859EEAFA", "Working!!")]
        [TestCase("[Underwater]_Rinne_no_Lagrange_-_12_(720p)_[5C7BC4F9]", "Underwater", "5C7BC4F9", "Rinne no Lagrange")]
        [TestCase("[HorribleSubs]_Hunter_X_Hunter_-_33_[720p]", "HorribleSubs", "", "Hunter X Hunter")]
        [TestCase("[HorribleSubs] Tonari no Kaibutsu-kun - 13 [1080p].mkv", "HorribleSubs", "", "Tonari no Kaibutsu-kun")]
        [TestCase("[Doremi].Yes.Pretty.Cure.5.Go.Go!.31.[1280x720].[C65D4B1F].mkv", "Doremi", "C65D4B1F", "Yes.Pretty.Cure.5.Go.Go!")]
        [TestCase("[Doremi].Yes.Pretty.Cure.5.Go.Go!.31.[1280x720].[C65D4B1F]", "Doremi", "C65D4B1F", "Yes.Pretty.Cure.5.Go.Go!")]
        [TestCase("[Doremi].Yes.Pretty.Cure.5.Go.Go!.31.[1280x720].mkv", "Doremi", "", "Yes.Pretty.Cure.5.Go.Go!")]
        [TestCase("[K-F] One Piece 214", "K-F", "", "One Piece")]
        [TestCase("[K-F] One Piece S10E14 214", "K-F", "", "One Piece")]
        [TestCase("[K-F] One Piece 10x14 214", "K-F", "", "One Piece")]
        [TestCase("[K-F] One Piece 214 10x14", "K-F", "", "One Piece")]
        [TestCase("Bleach - 031 - The Resolution to Kill [Lunar].avi", "Lunar", "", "Bleach")]
        [TestCase("[ACX]Hack Sign 01 Role Play [Kosaka] [9C57891E].mkv", "ACX", "9C57891E", "Hack Sign")]
        [TestCase("[S-T-D] Soul Eater Not! - 06 (1280x720 10bit AAC) [59B3F2EA].mkv", "S-T-D", "59B3F2EA", "Soul Eater Not!")]
        public void should_parse_absolute_numbers(string postTitle, string subGroup, string hash, string title)
        {
            var seasons = Builder<Season>.CreateListOfSize(20).Build().ToList();
            var episodes = Builder<Episode>.CreateListOfSize(20).Build().ToList();

            Mocker.GetMock<IEpisodeService>()
                .Setup(p => p.GetEpisodesBySeason(It.IsAny<int>(), It.IsAny<int>()))
                .Returns(episodes);

            var _title = title.NormalizeTitle();
            Mocker.GetMock<ISeriesService>()
                .Setup(p => p.FindByTitle(It.Is<string>(s => s.Equals(_title))))
                .Returns(new Series { SeriesType = SeriesTypes.Anime, Seasons = seasons });

            var result = Subject.ParseTitle(postTitle);
            result.Should().NotBeNull();
            result.ReleaseGroup.Should().Be(subGroup);
            result.ReleaseHash.Should().Be(hash == "" ? null : hash);
        }
    }
}
