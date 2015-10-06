using System.Collections.Generic;
using System.Linq;
using FizzWare.NBuilder;
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
    [Category("ParserTest")]
    public class MultiEpisodeParserFixture : CoreTest<NewParseProvider>
    {
        [SetUp]
        public void Setup()
        {
            UseAnalizers();
        }

        [TestCase("WEEDS.S03E01-06.DUAL.BDRip.XviD.AC3.-HELLYWOOD", "WEEDS", 3, new[] { 1, 2, 3, 4, 5, 6 })]
        [TestCase("Two.and.a.Half.Men.103.104.720p.HDTV.X264-DIMENSION", "Two and a Half Men", 1, new[] { 3, 4 })]
        [TestCase("Weeds.S03E01.S03E02.720p.HDTV.X264-DIMENSION", "Weeds", 3, new[] { 1, 2 })]
        [TestCase("The Borgias S01e01 e02 ShoHD On Demand 1080i DD5 1 ALANiS", "The Borgias", 1, new[] { 1, 2 })]
        [TestCase("White.Collar.2x04.2x05.720p.BluRay-FUTV", "White Collar", 2, new[] { 4, 5 })]
        [TestCase("Desperate.Housewives.S07E22E23.720p.HDTV.X264-DIMENSION", "Desperate Housewives", 7, new[] { 22, 23 })]
        [TestCase("Desparate Housewives - S07E22 - S07E23 - And Lots of Security.. [HDTV-720p].mkv", "Desparate Housewives", 7, new[] { 22, 23 })]
        [TestCase("S03E01.S03E02.720p.HDTV.X264-DIMENSION", "", 3, new[] { 1, 2 })]
        [TestCase("Desparate Housewives - S07E22 - 7x23 - And Lots of Security.. [HDTV-720p].mkv", "Desparate Housewives", 7, new[] { 22, 23 })]
        [TestCase("S07E22 - 7x23 - And Lots of Security.. [HDTV-720p].mkv", "", 7, new[] { 22, 23 })]
        [TestCase("2x04x05.720p.BluRay-FUTV", "", 2, new[] { 4, 5 })]
        [TestCase("S02E04E05.720p.BluRay-FUTV", "", 2, new[] { 4, 5 })]
        [TestCase("S02E03-04-05.720p.BluRay-FUTV", "", 2, new[] { 3, 4, 5 })]
        [TestCase("Breakout.Kings.S02E09-E10.HDTV.x264-ASAP", "Breakout Kings", 2, new[] { 9, 10 })]
        [TestCase("Breakout Kings - 2x9-2x10 - Served Cold [SDTV] ", "Breakout Kings", 2, new[] { 9, 10 })]
        [TestCase("Breakout Kings - 2x09-2x10 - Served Cold [SDTV] ", "Breakout Kings", 2, new[] { 9, 10 })]
        [TestCase("Hell on Wheels S02E09 E10 HDTV x264 EVOLVE", "Hell on Wheels", 2, new[] { 9, 10 })]
        [TestCase("Hell.on.Wheels.S02E09-E10.720p.HDTV.x264-EVOLVE", "Hell on Wheels", 2, new[] { 9, 10 })]
        [TestCase("Grey's Anatomy - 8x01_02 - Free Falling", "Grey's Anatomy", 8, new[] { 1, 2 })]
        [TestCase("8x01_02 - Free Falling", "", 8, new[] { 1, 2 })]
        [TestCase("Kaamelott.S01E91-E100", "Kaamelott", 1, new[] { 91, 92, 93, 94, 95, 96, 97, 98, 99, 100 })]
        [TestCase("Neighbours.S29E161-E165.PDTV.x264-FQM", "Neighbours", 29, new[] { 161, 162, 163, 164, 165 })]
        [TestCase("Shortland.Street.S22E5363-E5366.HDTV.x264-FiHTV", "Shortland Street", 22, new[] { 5363, 5364, 5365, 5366 })]
        [TestCase("the.office.101.102.hdtv-lol", "the office", 1, new[] { 1, 2 })]
        [TestCase("extant.10708.hdtv-lol.mp4", "extant", 1, new[] { 7, 8 })]
        [TestCase("extant.10910.hdtv-lol.mp4", "extant", 1, new[] { 9, 10 })]
        [TestCase("E.010910.HDTVx264REPACKLOL.mp4", "E", 1, new[] { 9, 10 })]
        [TestCase("World Series of Poker - 2013x15 - 2013x16 - HD TV.mkv", "World Series of Poker", 2013, new[] { 15, 16 })]
        [TestCase("The Librarians US S01E01-E02 720p HDTV x264", "The Librarians US", 1, new[] { 1, 2 })]
        [TestCase("Series Title Season 01 Episode 05-06 720p", "Series Title", 1, new[] { 5, 6 })]
        [TestCase("My Name Is Earl - S03E01-E02 - My Name Is Inmate 28301-016 [SDTV]", "My Name Is Earl", 3, new[] { 1, 2 })]
        [TestCase("Adventure Time - 5x01 - x02 - Finn the Human (2) & Jake the Dog (3)", "Adventure Time", 5, new[] { 1, 2 })]
        [TestCase("The Young And The Restless - S42 Ep10718 - Ep10722", "The Young And The Restless", 42, new[] { 10718, 10719, 10720, 10721, 10722 })]
        [TestCase("The Young And The Restless - S42 Ep10688 - Ep10692", "The Young And The Restless", 42, new[] { 10688, 10689, 10690, 10691, 10692 })]
        //[TestCase("", "", ,new [] {  })]
        public void should_parse_multiple_episodes(string postTitle, string title, int season, int[] episodes)
        {
            var seasons = Builder<Season>.CreateListOfSize(1)
                .TheFirst(1).With(s => s.SeasonNumber = season)
                .Build().ToList();

            var ep = Builder<Episode>.CreateListOfSize(episodes.MaxOrDefault() + 1).Build().ToList();

            Mocker.GetMock<IEpisodeService>()
                .Setup(p => p.GetEpisodesBySeason(It.IsAny<int>(), It.IsAny<int>()))
                .Returns(ep);

            var _title = title.NormalizeTitle();

            if (_title.Length > 0)
            {
                Mocker.GetMock<ISeriesService>()
                    .Setup(o => o.FindByTitle(It.Is<string>(s => s == _title)))
                    .Returns(new Series
                    {
                        Title = title,
                        CleanTitle = title.CleanSeriesTitle(),
                        SeriesType = SeriesTypes.Standard,
                        Seasons = seasons
                    });
            }

            var result = Subject.ParseTitle(postTitle);
            result.SeasonNumber.Should().Be(season);
            result.EpisodeNumbers.Should().BeEquivalentTo(episodes);
            result.SeriesTitle.Should().Be(title);
            result.AbsoluteEpisodeNumbers.Should().BeEmpty();
            result.FullSeason.Should().BeFalse();
        }
    }
}
