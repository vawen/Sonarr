﻿using System;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.ParserTests.NewParser
{
    [TestFixture]
    [Category("ParserTest")]
    public class ExtendedQualityParserRegex : CoreTest<NewParseProvider>
    {
        [SetUp]
        public void Setup()
        {
            UseAnalyzers();
        }

        [TestCase("Chuck.S04E05.HDTV.XviD-LOL", 0)]
        [TestCase("Gold.Rush.S04E05.Garnets.or.Gold.REAL.REAL.PROPER.HDTV.x264-W4F", 2)]
        [TestCase("Chuck.S03E17.REAL.PROPER.720p.HDTV.x264-ORENJI-RP", 1)]
        [TestCase("Covert.Affairs.S05E09.REAL.PROPER.HDTV.x264-KILLERS", 1)]
        [TestCase("Mythbusters.S14E01.REAL.PROPER.720p.HDTV.x264-KILLERS", 1)]
        [TestCase("Orange.Is.the.New.Black.s02e06.real.proper.720p.webrip.x264-2hd", 1)]
        [TestCase("Top.Gear.S21E07.Super.Duper.Real.Proper.HDTV.x264-FTP", 1)]
        [TestCase("Top.Gear.S21E07.PROPER.HDTV.x264-RiVER-RP", 0)]
        [TestCase("House.S07E11.PROPER.REAL.RERIP.1080p.BluRay.x264-TENEIGHTY", 1)]
        [TestCase("[MGS] - Kuragehime - Episode 02v2 - [D8B6C90D]", 0)]
        [TestCase("[Hatsuyuki] Tokyo Ghoul - 07 [v2][848x480][23D8F455].avi", 0)]
        [TestCase("[DeadFish] Barakamon - 01v3 [720p][AAC]", 0)]
        [TestCase("[DeadFish] Momo Kyun Sword - 01v4 [720p][AAC]", 0)]
        public void should_parse_reality_from_title(String title, Int32 reality)
        {
            Subject.ParseTitle(title).Quality.Revision.Real.Should().Be(reality);
        }

        [TestCase("Chuck.S04E05.HDTV.XviD-LOL", 1)]
        [TestCase("Gold.Rush.S04E05.Garnets.or.Gold.REAL.REAL.PROPER.HDTV.x264-W4F", 2)]
        [TestCase("Chuck.S03E17.REAL.PROPER.720p.HDTV.x264-ORENJI-RP", 2)]
        [TestCase("Covert.Affairs.S05E09.REAL.PROPER.HDTV.x264-KILLERS", 2)]
        [TestCase("Mythbusters.S14E01.REAL.PROPER.720p.HDTV.x264-KILLERS", 2)]
        [TestCase("Orange.Is.the.New.Black.s02e06.real.proper.720p.webrip.x264-2hd", 2)]
        [TestCase("Top.Gear.S21E07.Super.Duper.Real.Proper.HDTV.x264-FTP", 2)]
        [TestCase("Top.Gear.S21E07.PROPER.HDTV.x264-RiVER-RP", 2)]
        [TestCase("House.S07E11.PROPER.REAL.RERIP.1080p.BluRay.x264-TENEIGHTY", 2)]
        [TestCase("[MGS] - Kuragehime - Episode 02v2 - [D8B6C90D]", 2)]
        [TestCase("[Hatsuyuki] Tokyo Ghoul - 07 [v2][848x480][23D8F455].avi", 2)]
        [TestCase("[DeadFish] Barakamon - 01v3 [720p][AAC]", 3)]
        [TestCase("[DeadFish] Momo Kyun Sword - 01v4 [720p][AAC]", 4)]
        public void should_parse_version_from_title(String title, Int32 version)
        {
            Subject.ParseTitle(title).Quality.Revision.Version.Should().Be(version);
        }
    }
}
