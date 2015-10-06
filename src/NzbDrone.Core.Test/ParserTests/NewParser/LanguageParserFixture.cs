using System.Linq;
using FizzWare.NBuilder;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Core.Tv;

namespace NzbDrone.Core.Test.ParserTests.Parser
{

    [TestFixture]
    [Category("ParserTest")]
    public class LanguageParserFixture : CoreTest<NewParseProvider>
    {
        [SetUp]
        public void Setup()
        {
            UseAnalyzers();
        }

        [TestCase("Castle.2009.S01E14.English.HDTV.XviD-LOL", Language.English, "Castle 2009")]
        [TestCase("Castle.2009.S01E14.French.HDTV.XviD-LOL", Language.French, "Castle 2009")]
        [TestCase("Castle.2009.S01E14.Spanish.HDTV.XviD-LOL", Language.Spanish, "Castle 2009")]
        [TestCase("Castle.2009.S01E14.German.HDTV.XviD-LOL", Language.German, "Castle 2009")]
        [TestCase("Castle.2009.S01E14.Germany.HDTV.XviD-LOL", Language.English, "Castle 2009")]
        [TestCase("Castle.2009.S01E14.Italian.HDTV.XviD-LOL", Language.Italian, "Castle 2009")]
        [TestCase("Castle.2009.S01E14.Danish.HDTV.XviD-LOL", Language.Danish, "Castle 2009")]
        [TestCase("Castle.2009.S01E14.Dutch.HDTV.XviD-LOL", Language.Dutch, "Castle 2009")]
        [TestCase("Castle.2009.S01E14.Japanese.HDTV.XviD-LOL", Language.Japanese, "Castle 2009")]
        [TestCase("Castle.2009.S01E14.Cantonese.HDTV.XviD-LOL", Language.Cantonese, "Castle 2009")]
        [TestCase("Castle.2009.S01E14.Mandarin.HDTV.XviD-LOL", Language.Mandarin, "Castle 2009")]
        [TestCase("Castle.2009.S01E14.Korean.HDTV.XviD-LOL", Language.Korean, "Castle 2009")]
        [TestCase("Castle.2009.S01E14.Russian.HDTV.XviD-LOL", Language.Russian, "Castle 2009")]
        [TestCase("Castle.2009.S01E14.Polish.HDTV.XviD-LOL", Language.Polish, "Castle 2009")]
        [TestCase("Castle.2009.S01E14.Vietnamese.HDTV.XviD-LOL", Language.Vietnamese, "Castle 2009")]
        [TestCase("Castle.2009.S01E14.Swedish.HDTV.XviD-LOL", Language.Swedish, "Castle 2009")]
        [TestCase("Castle.2009.S01E14.Norwegian.HDTV.XviD-LOL", Language.Norwegian, "Castle 2009")]
        [TestCase("Castle.2009.S01E14.Finnish.HDTV.XviD-LOL", Language.Finnish, "Castle 2009")]
        [TestCase("Castle.2009.S01E14.Turkish.HDTV.XviD-LOL", Language.Turkish, "Castle 2009")]
        [TestCase("Castle.2009.S01E14.Portuguese.HDTV.XviD-LOL", Language.Portuguese, "Castle 2009")]
        [TestCase("Castle.2009.S01E14.HDTV.XviD-LOL", Language.English, "Castle 2009")]
        [TestCase("person.of.interest.1x19.ita.720p.bdmux.x264-novarip", Language.Italian, "person of interest")]
        [TestCase("Salamander.S01E01.FLEMISH.HDTV.x264-BRiGAND", Language.Flemish, "salamander")]
        [TestCase("H.Polukatoikia.S03E13.Greek.PDTV.XviD-Ouzo", Language.Greek, "H.Polukatoikia")]
        [TestCase("Burn.Notice.S04E15.Brotherly.Love.GERMAN.DUBBED.WS.WEBRiP.XviD.REPACK-TVP", Language.German, "burn notice")]
        [TestCase("Ray Donovan - S01E01.720p.HDtv.x264-Evolve (NLsub)", Language.Dutch, "ray donovan")]
        [TestCase("Shield,.The.1x13.Tueurs.De.Flics.FR.DVDRip.XviD", Language.French, "shield")]
        [TestCase("True.Detective.S01E01.1080p.WEB-DL.Rus.Eng.TVKlondike", Language.Russian, "true detective")]
        [TestCase("The.Trip.To.Italy.S02E01.720p.HDTV.x264-TLA", Language.English, "the trip to italy")]
        [TestCase("Revolution S01E03 No Quarter 2012 WEB-DL 720p Nordic-philipo mkv", Language.Norwegian, "revolution")]
        [TestCase("Extant.S01E01.VOSTFR.HDTV.x264-RiDERS", Language.French, "extant")]
        [TestCase("Constantine.2014.S01E01.WEBRiP.H264.AAC.5.1-NL.SUBS", Language.Dutch, "Constantine 2014")]
        [TestCase("Elementary - S02E16 - Kampfhaehne - mkv - by Videomann", Language.German, "elementary")]
        [TestCase("Two.Greedy.Italians.S01E01.The.Family.720p.HDTV.x264-FTP", Language.English, "two greedy italians")]
        [TestCase("Castle.2009.S01E14.HDTV.XviD.HUNDUB-LOL", Language.Hungarian, "Castle 2009")]
        [TestCase("Castle.2009.S01E14.HDTV.XviD.ENG.HUN-LOL", Language.Hungarian, "Castle 2009")]
        [TestCase("Castle.2009.S01E14.HDTV.XviD.HUN-LOL", Language.Hungarian, "Castle 2009")]
        public void should_parse_language(string postTitle, Language language, string title)
        {
            var _title = title.NormalizeTitle();
            var seasons = Builder<Season>.CreateListOfSize(18).Build().ToList();
            var episodes = Builder<Episode>.CreateListOfSize(20).Build().ToList();

            Mocker.GetMock<IEpisodeService>()
                .Setup(p => p.GetEpisodesBySeason(It.IsAny<int>(), It.IsAny<int>()))
                .Returns(episodes);

            Mocker.GetMock<ISeriesService>()
                .Setup(p => p.FindByTitle(It.Is<string>(s => s == _title)))
                .Returns(new Series { SeriesType = SeriesTypes.Standard, Seasons = seasons });

            var result = Subject.ParseTitle(postTitle);
            result.Language.Should().Be(language);
        }
    }
}
