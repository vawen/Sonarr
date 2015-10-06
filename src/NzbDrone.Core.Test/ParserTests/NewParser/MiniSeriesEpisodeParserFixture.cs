using System;
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
    public class MiniSeriesEpisodeParserFixture : CoreTest<NewParseProvider>
    {
        [SetUp]
        public void Setup()
        {
            UseAnalizers();
        }

        [TestCase("The.Kennedys.Part.2.DSR.XviD-SYS", "The Kennedys", 2)]
        [TestCase("the-pacific-e07-720p", "the-pacific", 7)]
        [TestCase("Hatfields and McCoys 2012 Part 1 REPACK 720p HDTV x264 2HD", "Hatfields and McCoys 2012", 1)]
        [TestCase("Band.Of.Brothers.EP02.Day.Of.Days.DVDRiP.XviD-DEiTY", "Band.Of.Brothers", 2)]
        //        [TestCase("", "", 0, 0)]
        public void should_parse_mini_series_episode(String postTitle, String title, Int32 episodeNumber)
        {
            var seasons = Builder<Season>.CreateListOfSize(20).Build().ToList();
            var episodes = Builder<Episode>.CreateListOfSize(20).Build().ToList();

            Mocker.GetMock<IEpisodeService>()
                .Setup(p => p.GetEpisodesBySeason(It.IsAny<int>(), It.IsAny<int>()))
                .Returns(episodes);

            var _title = title.NormalizeTitle();
            Mocker.GetMock<ISeriesService>()
                .Setup(o => o.FindByTitle(It.Is<string>(s => s == _title)))
                .Returns(new Series { Title = title, CleanTitle = title.CleanSeriesTitle(), Seasons = seasons });

            var result = Subject.ParseTitle(postTitle);
            result.Should().NotBeNull();
            result.EpisodeNumbers.Should().HaveCount(1);
            result.SeasonNumber.Should().Be(1);
            result.EpisodeNumbers.First().Should().Be(episodeNumber);
            result.SeriesTitle.Should().Be(title);
            result.AbsoluteEpisodeNumbers.Should().BeEmpty();
            result.FullSeason.Should().BeFalse();
        }
    }
}
