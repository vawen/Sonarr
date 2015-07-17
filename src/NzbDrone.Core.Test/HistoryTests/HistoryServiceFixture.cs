using System.IO;
using System.Linq;
using FizzWare.NBuilder;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.MediaFiles.Events;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Profiles;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Core.History;
using NzbDrone.Core.Qualities;
using System.Collections.Generic;
using NzbDrone.Core.Test.Qualities;
using FluentAssertions;
using NzbDrone.Core.Tv;
using NzbDrone.Core.Languages;

namespace NzbDrone.Core.Test.HistoryTests
{
    public class HistoryServiceFixture : CoreTest<HistoryService>
    {
        private Profile _profile;
        private Profile _profileCustom;
        private Profile _profileLanguage;

        [SetUp]
        public void Setup()
        {
            var _languages = new List<ProfileLanguageItem>();
            _languages.Add(new ProfileLanguageItem { Language = Language.English, Allowed = true });
            _languages.Add(new ProfileLanguageItem { Language = Language.Spanish, Allowed = true });
            _languages.Add(new ProfileLanguageItem { Language = Language.French, Allowed = true });


            _profile = new Profile
                {
                    Cutoff = Quality.WEBDL720p,
                    Items = QualityFixture.GetDefaultQualities(),
                    Languages = _languages,
                    CutoffLanguage = Language.Spanish,
                    AllowLanguageUpgrade = false,
                    LanguageOverQuality = false
                };
            _profileCustom = new Profile
                {
                    Cutoff = Quality.WEBDL720p, 
                    Items = QualityFixture.GetDefaultQualities(Quality.DVD),
                    Languages = _languages,
                    CutoffLanguage = Language.Spanish,
                    AllowLanguageUpgrade = false,
                    LanguageOverQuality = false

                };
            _profileLanguage = new Profile
            {
                Cutoff = Quality.WEBDL720p,
                Items = QualityFixture.GetDefaultQualities(),
                Languages = _languages,
                CutoffLanguage = Language.Spanish,
                AllowLanguageUpgrade = false,
                LanguageOverQuality = true
            };
        
        }

        [Test]
        public void should_return_null_if_no_history()
        {
            Mocker.GetMock<IHistoryRepository>()
                .Setup(v => v.GetBestInHistory(2))
                .Returns(new List<BestInHistory>());

            var bestItem = Subject.GetBestInHistory(_profile, 2);

            bestItem.Should().BeNull();
        }

        [Test]
        public void should_return_best_quality()
        {
            Mocker.GetMock<IHistoryRepository>()
                .Setup(v => v.GetBestInHistory(2))
                .Returns(new List<BestInHistory> 
                { 
                    new BestInHistory { Quality = new QualityModel(Quality.DVD), Language = Language.English }, 
                    new BestInHistory{Quality = new QualityModel(Quality.Bluray1080p), Language = Language.English} 
                });

            var quality = Subject.GetBestInHistory(_profileLanguage, 2).Quality;

            quality.Should().Be(new QualityModel(Quality.Bluray1080p));
        }

        [Test]
        public void should_return_best_quality_priorizing_language()
        {
            Mocker.GetMock<IHistoryRepository>()
                .Setup(v => v.GetBestInHistory(2))
                .Returns(new List<BestInHistory> 
                { 
                    new BestInHistory { Quality = new QualityModel(Quality.DVD), Language = Language.French }, 
                    new BestInHistory { Quality = new QualityModel(Quality.Bluray1080p), Language = Language.English } 
                });

            var bestInHistory = Subject.GetBestInHistory(_profileCustom, 2);

            bestInHistory.Quality.Should().Be(new QualityModel(Quality.DVD));
            bestInHistory.Language.Should().Be(Language.French);
        }


        [Test]
        public void should_return_best_quality_with_custom_order()
        {
            Mocker.GetMock<IHistoryRepository>()
                .Setup(v => v.GetBestInHistory(2))
                .Returns(new List<BestInHistory> { new BestInHistory {Quality = new QualityModel(Quality.DVD)}, new BestInHistory { Quality = new QualityModel(Quality.Bluray1080p)} });

            var quality = Subject.GetBestInHistory(_profileCustom, 2).Quality;

            quality.Should().Be(new QualityModel(Quality.DVD));
        }

        [Test]
        public void should_use_file_name_for_source_title_if_scene_name_is_null()
        {
            var series = Builder<Series>.CreateNew().Build();
            var episodes = Builder<Episode>.CreateListOfSize(1).Build().ToList();
            var episodeFile = Builder<EpisodeFile>.CreateNew()
                                                  .With(f => f.SceneName = null)
                                                  .Build();

            var localEpisode = new LocalEpisode
                               {
                                   Series = series,
                                   Episodes = episodes,
                                   Path = @"C:\Test\Unsorted\Series.s01e01.mkv"
                               };

            Subject.Handle(new EpisodeImportedEvent(localEpisode, episodeFile, true, "sab","abcd"));

            Mocker.GetMock<IHistoryRepository>()
                .Verify(v => v.Insert(It.Is<History.History>(h => h.SourceTitle == Path.GetFileNameWithoutExtension(localEpisode.Path))));
        }
    }
}