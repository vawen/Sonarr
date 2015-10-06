using Moq;
using NUnit.Framework;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Core.Tv;

namespace NzbDrone.Core.Test.ParserTests.ParsingServiceTests
{
    [TestFixture]
    public class GetSeriesFixture : CoreTest<ParsingService>
    {
        [Test]
        public void should_use_passed_in_title_when_it_cannot_be_parsed()
        {
            const string title = "30 Rock";
            var parseProvider = Mocker.Resolve<ParseProvider>();
            Mocker.GetMock<IParseProvider>().Setup(c => c.ParseTitle(It.IsAny<string>())).Returns(parseProvider.ParseTitle(title));

            Subject.GetSeries(title);

            Mocker.GetMock<ISeriesService>()
                  .Verify(s => s.FindByTitle(title), Times.Once());
        }

        [Test]
        public void should_use_parsed_series_title()
        {
            const string title = "30.Rock.S01E01.720p.hdtv";
            var parseProvider = Mocker.Resolve<ParseProvider>();
            Mocker.GetMock<IParseProvider>().Setup(c => c.ParseTitle(It.IsAny<string>())).Returns(parseProvider.ParseTitle(title));

            Subject.GetSeries(title);

            Mocker.GetMock<ISeriesService>()
                  .Verify(s => s.FindByTitle(parseProvider.ParseTitle(title).SeriesTitle), Times.Once());
        }

        [Test]
        public void should_fallback_to_title_without_year_and_year_when_title_lookup_fails()
        {
            const string title = "House.2004.S01E01.720p.hdtv";
            var parseProvider = Mocker.Resolve<ParseProvider>();
            var parsedEpisodeInfo = parseProvider.ParseTitle(title);

            Mocker.GetMock<IParseProvider>().Setup(c => c.ParseTitle(It.IsAny<string>())).Returns(parsedEpisodeInfo);

            Subject.GetSeries(title);

            Mocker.GetMock<ISeriesService>()
                  .Verify(s => s.FindByTitle(parsedEpisodeInfo.SeriesTitleInfo.TitleWithoutYear,
                                             parsedEpisodeInfo.SeriesTitleInfo.Year), Times.Once());
        }
    }
}
