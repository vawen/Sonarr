using System.Collections.Generic;
using System.Linq;
using FizzWare.NBuilder;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Analizers;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Core.Tv;

namespace NzbDrone.Core.Test.ParserTests.NewParser
{
    [TestFixture]
    [Category("ParserTest")]
    public class IsPossibleSpecialEpisodeFixture : CoreTest<NewParseProvider>
    {
        [SetUp]
        public void Setup()
        {
            UseAnalizers();
        }

        [Test]
        public void should_not_treat_files_without_a_series_title_as_a_special()
        {
            var parsedEpisodeInfo = new ParsedEpisodeInfo
                                    {
                                        EpisodeNumbers = new[] { 7 },
                                        SeasonNumber = 1,
                                        SeriesTitle = ""
                                    };

            parsedEpisodeInfo.IsPossibleSpecialEpisode.Should().BeFalse();
        }

        [Test]
        public void should_return_true_when_episode_numbers_is_empty()
        {
            var parsedEpisodeInfo = new ParsedEpisodeInfo
            {
                SeasonNumber = 1,
                SeriesTitle = ""
            };

            parsedEpisodeInfo.IsPossibleSpecialEpisode.Should().BeTrue();
        }

        [TestCase("Under.the.Dome.S02.Special-Inside.Chesters.Mill.HDTV.x264-BAJSKORV")]
        [TestCase("Under.the.Dome.S02.Special-Inside.Chesters.Mill.720p.HDTV.x264-BAJSKORV")]
        [TestCase("Rookie.Blue.Behind.the.Badge.S05.Special.HDTV.x264-2HD")]
        public void IsPossibleSpecialEpisode_should_be_true(string title)
        {
            var seasons = Builder<Season>.CreateListOfSize(2)
                .TheFirst(1).With(s => s.SeasonNumber = 2)
                .TheNext(1).With(s => s.SeasonNumber = 5)
                .Build().ToList();

            Mocker.GetMock<ISeriesService>().Setup(o => o.FindByTitle(It.IsAny<string>()))
                .Returns(new Series { Seasons = seasons });
            Subject.ParseTitle(title).IsPossibleSpecialEpisode.Should().BeTrue();
        }
    }
}
