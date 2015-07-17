using System.Linq;
using FizzWare.NBuilder;
using FluentAssertions;
using Marr.Data;
using NUnit.Framework;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.MediaFiles.EpisodeImport.Specifications;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Profiles;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Core.Tv;
using NzbDrone.Core.Languages;
using System.Collections.Generic;

namespace NzbDrone.Core.Test.MediaFiles.EpisodeImport.Specifications
{
    [TestFixture]
    public class UpgradeSpecificationFixture : CoreTest<UpgradeSpecification>
    {
        private Series _series;
        private LocalEpisode _localEpisode;
        private Series _seriesLanguage;
        private LocalEpisode _localEpisodeLanguage;

        [SetUp]
        public void Setup()
        {

            var _languages = new List<ProfileLanguageItem>();
            _languages.Add(new ProfileLanguageItem { Language = Language.English, Allowed = true });
            _languages.Add(new ProfileLanguageItem { Language = Language.Spanish, Allowed = true });
            _languages.Add(new ProfileLanguageItem { Language = Language.French, Allowed = true });


            _series = Builder<Series>.CreateNew()
                                     .With(s => s.SeriesType = SeriesTypes.Standard)
                                     .With(e => e.Profile = new Profile 
                                        { 
                                            Items = Qualities.QualityFixture.GetDefaultQualities(),
                                            Languages = _languages,
                                            CutoffLanguage = Language.Spanish,
                                            AllowLanguageUpgrade = false,
                                            LanguageOverQuality = false,
                                        })
                                     .Build();

            _seriesLanguage = Builder<Series>.CreateNew()
                                     .With(s => s.SeriesType = SeriesTypes.Standard)
                                     .With(e => e.Profile = new Profile
                                     {
                                         Items = Qualities.QualityFixture.GetDefaultQualities(),
                                         Languages = _languages,
                                         CutoffLanguage = Language.Spanish,
                                         AllowLanguageUpgrade = true,
                                         LanguageOverQuality = true,
                                     })
                                     .Build();


            _localEpisode = new LocalEpisode
                                {
                                    Path = @"C:\Test\30 Rock\30.rock.s01e01.avi",
                                    Quality = new QualityModel(Quality.HDTV720p, new Revision(version: 1)),
                                    Language = Language.Spanish,
                                    Series = _series
                                };

            _localEpisodeLanguage = new LocalEpisode
                                        {
                                            Path = @"C:\Test\30 Rock\30.rock.s01e01.avi",
                                            Quality = new QualityModel(Quality.HDTV720p, new Revision(version: 1)),
                                            Language = Language.Spanish,
                                            Series = _seriesLanguage
                                        };

        }

        [Test]
        public void should_return_true_if_no_existing_episodeFile()
        {
            _localEpisode.Episodes = Builder<Episode>.CreateListOfSize(1)
                                                     .All()
                                                     .With(e => e.EpisodeFileId = 0)
                                                     .With(e => e.EpisodeFile = null)
                                                     .Build()
                                                     .ToList();

            Subject.IsSatisfiedBy(_localEpisode).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_return_true_if_no_existing_episodeFile_for_multi_episodes()
        {
            _localEpisode.Episodes = Builder<Episode>.CreateListOfSize(2)
                                                     .All()
                                                     .With(e => e.EpisodeFileId = 0)
                                                     .With(e => e.EpisodeFile = null)
                                                     .Build()
                                                     .ToList();

            Subject.IsSatisfiedBy(_localEpisode).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_return_true_if_upgrade_for_existing_episodeFile()
        {
            _localEpisode.Episodes = Builder<Episode>.CreateListOfSize(1)
                                                     .All()
                                                     .With(e => e.EpisodeFileId = 1)
                                                     .With(e => e.EpisodeFile = new LazyLoaded<EpisodeFile>(
                                                                                new EpisodeFile
                                                                                {
                                                                                    Quality = new QualityModel(Quality.SDTV, new Revision(version: 1)),
                                                                                    Language = Language.Spanish
                                                                                }))
                                                     .Build()
                                                     .ToList();

            Subject.IsSatisfiedBy(_localEpisode).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_return_true_if_language_upgrade_for_existing_episodeFile()
        {
            _localEpisodeLanguage.Episodes = Builder<Episode>.CreateListOfSize(1)
                                                     .All()
                                                     .With(e => e.EpisodeFileId = 1)
                                                     .With(e => e.EpisodeFile = new LazyLoaded<EpisodeFile>(
                                                                                new EpisodeFile
                                                                                {
                                                                                    Quality = new QualityModel(Quality.Bluray1080p, new Revision(version: 1)),
                                                                                    Language = Language.English
                                                                                }))
                                                     .Build()
                                                     .ToList();

            Subject.IsSatisfiedBy(_localEpisodeLanguage).Accepted.Should().BeTrue();
        }


        [Test]
        public void should_return_true_if_upgrade_for_existing_episodeFile_for_multi_episodes()
        {
            _localEpisode.Episodes = Builder<Episode>.CreateListOfSize(2)
                                                     .All()
                                                     .With(e => e.EpisodeFileId = 1)
                                                     .With(e => e.EpisodeFile = new LazyLoaded<EpisodeFile>(
                                                                                new EpisodeFile
                                                                                {
                                                                                    Quality = new QualityModel(Quality.SDTV, new Revision(version: 1))
                                                                                }))
                                                     .Build()
                                                     .ToList();

            Subject.IsSatisfiedBy(_localEpisode).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_return_true_if_language_upgrade_for_existing_episodeFile_for_multi_episodes()
        {
            _localEpisodeLanguage.Episodes = Builder<Episode>.CreateListOfSize(2)
                                                     .All()
                                                     .With(e => e.EpisodeFileId = 1)
                                                     .With(e => e.EpisodeFile = new LazyLoaded<EpisodeFile>(
                                                                                new EpisodeFile
                                                                                {
                                                                                    Quality = new QualityModel(Quality.Bluray1080p, new Revision(version: 1)),
                                                                                    Language = Language.English
                                                                                }))
                                                     .Build()
                                                     .ToList();

            Subject.IsSatisfiedBy(_localEpisodeLanguage).Accepted.Should().BeTrue();
        }


        [Test]
        public void should_return_false_if_not_an_upgrade_for_existing_episodeFile()
        {
            _localEpisode.Episodes = Builder<Episode>.CreateListOfSize(1)
                                                     .All()
                                                     .With(e => e.EpisodeFileId = 1)
                                                     .With(e => e.EpisodeFile = new LazyLoaded<EpisodeFile>(
                                                                                new EpisodeFile
                                                                                {
                                                                                    Quality = new QualityModel(Quality.Bluray720p, new Revision(version: 1))
                                                                                }))
                                                     .Build()
                                                     .ToList();

            Subject.IsSatisfiedBy(_localEpisode).Accepted.Should().BeFalse();
        }

        [Test]
        public void should_return_false_if_not_a_language_upgrade_for_existing_episodeFile()
        {
            _localEpisodeLanguage.Episodes = Builder<Episode>.CreateListOfSize(1)
                                                     .All()
                                                     .With(e => e.EpisodeFileId = 1)
                                                     .With(e => e.EpisodeFile = new LazyLoaded<EpisodeFile>(
                                                                                new EpisodeFile
                                                                                {
                                                                                    Quality = new QualityModel(Quality.SDTV, new Revision(version: 1)),
                                                                                    Language = Language.French
                                                                                }))
                                                     .Build()
                                                     .ToList();

            Subject.IsSatisfiedBy(_localEpisodeLanguage).Accepted.Should().BeFalse();
        }

        [Test]
        public void should_return_false_if_not_an_upgrade_for_existing_episodeFile_for_multi_episodes()
        {
            _localEpisode.Episodes = Builder<Episode>.CreateListOfSize(2)
                                                     .All()
                                                     .With(e => e.EpisodeFileId = 1)
                                                     .With(e => e.EpisodeFile = new LazyLoaded<EpisodeFile>(
                                                                                new EpisodeFile
                                                                                {
                                                                                    Quality = new QualityModel(Quality.Bluray720p, new Revision(version: 1))
                                                                                }))
                                                     .Build()
                                                     .ToList();

            Subject.IsSatisfiedBy(_localEpisode).Accepted.Should().BeFalse();
        }

        [Test]
        public void should_return_false_if_not_a_language_upgrade_for_existing_episodeFile_for_multi_episodes()
        {
            _localEpisodeLanguage.Episodes = Builder<Episode>.CreateListOfSize(2)
                                                     .All()
                                                     .With(e => e.EpisodeFileId = 1)
                                                     .With(e => e.EpisodeFile = new LazyLoaded<EpisodeFile>(
                                                                                new EpisodeFile
                                                                                {
                                                                                    Quality = new QualityModel(Quality.SDTV, new Revision(version: 1)),
                                                                                    Language = Language.French
                                                                                }))
                                                     .Build()
                                                     .ToList();

            Subject.IsSatisfiedBy(_localEpisodeLanguage).Accepted.Should().BeFalse();
        }


        [Test]
        public void should_return_false_if_not_an_upgrade_for_one_existing_episodeFile_for_multi_episode()
        {
            _localEpisode.Episodes = Builder<Episode>.CreateListOfSize(2)
                                                     .TheFirst(1)
                                                     .With(e => e.EpisodeFileId = 1)
                                                     .With(e => e.EpisodeFile = new LazyLoaded<EpisodeFile>(
                                                                                new EpisodeFile
                                                                                {
                                                                                    Quality = new QualityModel(Quality.SDTV, new Revision(version: 1))
                                                                                }))
                                                     .TheNext(1)
                                                     .With(e => e.EpisodeFileId = 2)
                                                     .With(e => e.EpisodeFile = new LazyLoaded<EpisodeFile>(
                                                                                new EpisodeFile
                                                                                {
                                                                                    Quality = new QualityModel(Quality.Bluray720p, new Revision(version: 1))
                                                                                }))
                                                     .Build()
                                                     .ToList();

            Subject.IsSatisfiedBy(_localEpisode).Accepted.Should().BeFalse();
        }

        [Test]
        public void should_return_false_if_not_a_language_upgrade_for_one_existing_episodeFile_for_multi_episode()
        {
            _localEpisodeLanguage.Episodes = Builder<Episode>.CreateListOfSize(2)
                                                     .TheFirst(1)
                                                     .With(e => e.EpisodeFileId = 1)
                                                     .With(e => e.EpisodeFile = new LazyLoaded<EpisodeFile>(
                                                                                new EpisodeFile
                                                                                {
                                                                                    Quality = new QualityModel(Quality.SDTV, new Revision(version: 1)),
                                                                                    Language = Language.French
                                                                                }))
                                                     .TheNext(1)
                                                     .With(e => e.EpisodeFileId = 2)
                                                     .With(e => e.EpisodeFile = new LazyLoaded<EpisodeFile>(
                                                                                new EpisodeFile
                                                                                {
                                                                                    Quality = new QualityModel(Quality.SDTV, new Revision(version: 1)),
                                                                                    Language = Language.English
                                                                                }))
                                                     .Build()
                                                     .ToList();

            Subject.IsSatisfiedBy(_localEpisodeLanguage).Accepted.Should().BeFalse();
        }
    }
}
