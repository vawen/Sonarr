using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Languages;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.ParserTests
{

    [TestFixture]
    public class LanguageParserFixture : CoreTest
    {
        [TestCase("Castle.2009.S01E14.English.HDTV.XviD-LOL", 1)]
        [TestCase("Castle.2009.S01E14.French.HDTV.XviD-LOL", 2)]
        [TestCase("Castle.2009.S01E14.Spanish.HDTV.XviD-LOL", 3)]
        [TestCase("Castle.2009.S01E14.German.HDTV.XviD-LOL", 4)]
        [TestCase("Castle.2009.S01E14.Germany.HDTV.XviD-LOL", 1)]
        [TestCase("Castle.2009.S01E14.Italian.HDTV.XviD-LOL", 5)]
        [TestCase("Castle.2009.S01E14.Danish.HDTV.XviD-LOL", 6)]
        [TestCase("Castle.2009.S01E14.Dutch.HDTV.XviD-LOL", 7)]
        [TestCase("Castle.2009.S01E14.Japanese.HDTV.XviD-LOL", 8)]
        [TestCase("Castle.2009.S01E14.Cantonese.HDTV.XviD-LOL", 9)]
        [TestCase("Castle.2009.S01E14.Mandarin.HDTV.XviD-LOL", 10)]
        [TestCase("Castle.2009.S01E14.Korean.HDTV.XviD-LOL", 21)]
        [TestCase("Castle.2009.S01E14.Russian.HDTV.XviD-LOL", 11)]
        [TestCase("Castle.2009.S01E14.Polish.HDTV.XviD-LOL", 12)]
        [TestCase("Castle.2009.S01E14.Vietnamese.HDTV.XviD-LOL", 13)]
        [TestCase("Castle.2009.S01E14.Swedish.HDTV.XviD-LOL", 14)]
        [TestCase("Castle.2009.S01E14.Norwegian.HDTV.XviD-LOL", 15)]
        [TestCase("Castle.2009.S01E14.Finnish.HDTV.XviD-LOL", 16)]
        [TestCase("Castle.2009.S01E14.Turkish.HDTV.XviD-LOL", 17)]
        [TestCase("Castle.2009.S01E14.Portuguese.HDTV.XviD-LOL", 18)]
        [TestCase("Castle.2009.S01E14.HDTV.XviD-LOL", 1)]
        [TestCase("person.of.interest.1x19.ita.720p.bdmux.x264-novarip", 5)]
        [TestCase("Salamander.S01E01.FLEMISH.HDTV.x264-BRiGAND",19)]
        [TestCase("H.Polukatoikia.S03E13.Greek.PDTV.XviD-Ouzo", 20)]
        [TestCase("Burn.Notice.S04E15.Brotherly.Love.GERMAN.DUBBED.WS.WEBRiP.XviD.REPACK-TVP",4)]
        [TestCase("Ray Donovan - S01E01.720p.HDtv.x264-Evolve (NLsub)", 7)]
        [TestCase("Shield,.The.1x13.Tueurs.De.Flics.FR.DVDRip.XviD", 2)]
        [TestCase("True.Detective.S01E01.1080p.WEB-DL.Rus.Eng.TVKlondike", 11)]
        [TestCase("The.Trip.To.Italy.S02E01.720p.HDTV.x264-TLA", 1)]
        [TestCase("Revolution S01E03 No Quarter 2012 WEB-DL 720p Nordic-philipo mkv", 15)]
        [TestCase("Extant.S01E01.VOSTFR.HDTV.x264-RiDERS", 2)]
        [TestCase("Constantine.2014.S01E01.WEBRiP.H264.AAC.5.1-NL.SUBS", 7)]
        [TestCase("Elementary - S02E16 - Kampfhaehne - mkv - by Videomann", 4)]
        [TestCase("Two.Greedy.Italians.S01E01.The.Family.720p.HDTV.x264-FTP", 1)]
        public void should_parse_language(string postTitle, int language)
        {
            var result = Parser.Parser.ParseTitle(postTitle);
            result.Language.Id.Should().Be(language);
        }
    }
}
