using NLog;
using NzbDrone.Common.Disk;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.DecisionEngine;
using NzbDrone.Core.MediaFiles.MediaInfo;
using NzbDrone.Core.Movies;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Profiles;
using NzbDrone.Core.Qualities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;


namespace NzbDrone.Core.MediaFiles.MovieImport
{
    public interface IMakeMovieImportDecision
    {
        List<ImportMovieDecision> GetImportDecisions(List<String> videoFiles, Movie movie);
        List<ImportMovieDecision> GetImportDecisions(List<string> videoFiles, Movie movie, ParsedMovieInfo folderInfo, bool sceneSource);
    }

    public class ImportMovieDecisionMaker : IMakeMovieImportDecision
    {
        private readonly IEnumerable<IImportMovieDecisionEngineSpecification> _specifications;
        private readonly IParsingService _parsingService;
        private readonly IMovieMediaFileService _movieMediaFileService;
        private readonly IDiskProvider _diskProvider;
        private readonly IVideoFileInfoReader _videoFileInfoReader;
        private readonly IDetectSample _detectSample;
        private readonly Logger _logger;

        public ImportMovieDecisionMaker(IEnumerable<IImportMovieDecisionEngineSpecification> specifications,
                                        IParsingService parsingService,
                                        IMovieMediaFileService movieMediaFileService,
                                        IDiskProvider diskProvider,
                                        IVideoFileInfoReader videoFileInfoReader,
                                        IDetectSample detectSample,
                                        Logger logger)
        {
            _specifications = specifications;
            _parsingService = parsingService;
            _movieMediaFileService = movieMediaFileService;
            _diskProvider = diskProvider;
            _videoFileInfoReader = videoFileInfoReader;
            _detectSample = detectSample;
            _logger = logger;
        }

        public List<ImportMovieDecision> GetImportDecisions(List<string> videoFiles, Movie movie)
        {
            return GetImportDecisions(videoFiles, movie, null, false);
        }

        public List<ImportMovieDecision> GetImportDecisions(List<string> videoFiles, Movie movie, ParsedMovieInfo folderInfo, bool sceneSource)
        {
            var newFiles = _movieMediaFileService.FilterExistingFiles(videoFiles.ToList(), movie);

            _logger.Debug("Analyzing {0}/{1} files.", newFiles.Count, videoFiles.Count());

            var shouldUseFolderName = ShouldUseFolderName(videoFiles, movie, folderInfo);
            var decisions = new List<ImportMovieDecision>();

            foreach (var file in newFiles)
            {
                decisions.AddIfNotNull(GetDecision(file, movie, folderInfo, sceneSource, shouldUseFolderName));
            }

            return decisions;
        }

        private ImportMovieDecision GetDecision(string file, Movie movie, ParsedMovieInfo folderInfo, bool sceneSource, bool shouldUseFolderName)
        {
            ImportMovieDecision decision = null;

            try
            {
                var localMovie = _parsingService.GetLocalMovie(file, movie, shouldUseFolderName ? folderInfo : null, sceneSource);

                if (localMovie != null)
                {
                    localMovie.Quality = GetQuality(folderInfo, localMovie.Quality, movie.Profile);
                    localMovie.Size = _diskProvider.GetFileSize(file);

                    _logger.Debug("Size: {0}", localMovie.Size);

                    //TODO: make it so media info doesn't ruin the import process of a new series
                    if (sceneSource)
                    {
                        localMovie.MediaInfo = _videoFileInfoReader.GetMediaInfo(file);
                    }

                    if (localMovie.Movie == null)
                    {
                        decision = new ImportMovieDecision(localMovie, new Rejection("Unable to parse movie from filename"));
                    }
                    else
                    {
                        decision = GetDecision(localMovie);
                    }
                }

                else
                {
                    localMovie = new LocalMovie();
                    localMovie.Path = file;

                    decision = new ImportMovieDecision(localMovie, new Rejection("Unable to parse file"));
                }
            }
            catch (Exception e)
            {
                _logger.ErrorException("Couldn't import file. " + file, e);
            }

            return decision;
        }

        private ImportMovieDecision GetDecision(LocalMovie localMovie)
        {
            var reasons = _specifications.Select(c => EvaluateSpec(c, localMovie))
                                         .Where(c => c != null);

            return new ImportMovieDecision(localMovie, reasons.ToArray());
        }

        private Rejection EvaluateSpec(IImportMovieDecisionEngineSpecification spec, LocalMovie localMovie)
        {
            try
            {
                var result = spec.IsSatisfiedBy(localMovie);

                if (!result.Accepted)
                {
                    return new Rejection(result.Reason);
                }
            }
            catch (Exception e)
            {
                //e.Data.Add("report", remoteEpisode.Report.ToJson());
                //e.Data.Add("parsed", remoteEpisode.ParsedEpisodeInfo.ToJson());
                _logger.ErrorException("Couldn't evaluate decision on " + localMovie.Path, e);
                return new Rejection(String.Format("{0}: {1}", spec.GetType().Name, e.Message));
            }

            return null;
        }

        private bool ShouldUseFolderName(List<string> videoFiles, Movie movie, ParsedMovieInfo folderInfo)
        {
            if (folderInfo == null)
            {
                return false;
            }

            return videoFiles.Count(file =>
            {
                var size = _diskProvider.GetFileSize(file);
                var fileQuality = QualityParser.ParseQuality(file);
                var sample = _detectSample.IsSample(movie, GetQuality(folderInfo, fileQuality, movie.Profile), file, size);

                if (sample)
                {
                    return false;
                }

                if (SceneChecker.IsSceneTitle(Path.GetFileName(file)))
                {
                    return false;
                }

                return true;
            }) == 1;
        }

        private QualityModel GetQuality(ParsedMovieInfo folderInfo, QualityModel fileQuality, Profile profile)
        {
            if (folderInfo != null &&
                folderInfo.Quality.Quality != Quality.Unknown &&
                new QualityModelComparer(profile).Compare(folderInfo.Quality, fileQuality) > 0)
            {
                _logger.Debug("Using quality from folder: {0}", folderInfo.Quality);
                return folderInfo.Quality;
            }

            return fileQuality;
        }
    }
}
