using System;
using System.Collections.Generic;
using System.Linq;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Instrumentation.Extensions;
using NzbDrone.Common.Serializer;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.DecisionEngine
{
    public interface IMakeDownloadDecision
    {
        List<DownloadDecision> GetRssDecision(List<ReleaseInfo> reports);
        List<DownloadDecision> GetSearchDecision(List<ReleaseInfo> reports, SearchCriteriaBase searchCriteriaBase);
    }

    public class DownloadDecisionMaker : IMakeDownloadDecision
    {
        private readonly IEnumerable<IDecisionEngineSpecification> _specifications;
        private readonly IParseProvider _parseProvider;
        private readonly IParsingService _parsingService;
        private readonly Logger _logger;

        public DownloadDecisionMaker(IEnumerable<IDecisionEngineSpecification> specifications, IParsingService parsingService, IParseProvider parseProvider, Logger logger)
        {
            _specifications = specifications;
            _parsingService = parsingService;
            _parseProvider = parseProvider;
            _logger = logger;
        }

        public List<DownloadDecision> GetRssDecision(List<ReleaseInfo> reports)
        {
            return GetDecisions(reports).ToList();
        }

        public List<DownloadDecision> GetSearchDecision(List<ReleaseInfo> reports, SearchCriteriaBase searchCriteriaBase)
        {
            return GetDecisions(reports, searchCriteriaBase).ToList();
        }

        private IEnumerable<DownloadDecision> GetDecisions(List<ReleaseInfo> reports, SearchCriteriaBase searchCriteria = null)
        {
            if (reports.Any())
            {
                _logger.ProgressInfo("Processing {0} releases", reports.Count);
            }

            else
            {
                _logger.ProgressInfo("No results found");
            }

            var reportNumber = 1;

            foreach (var report in reports)
            {
                DownloadDecision decision = null;
                _logger.ProgressTrace("Processing release {0}/{1}", reportNumber, reports.Count);

                try
                {
                    var parsedEpisodeInfo = _parseProvider.ParseTitle(report.Title);

                    if (parsedEpisodeInfo == null || parsedEpisodeInfo.IsPossibleSpecialEpisode)
                    {
                        var specialEpisodeInfo = _parsingService.ParseSpecialEpisodeTitle(report.Title, report.TvRageId, searchCriteria);

                        if (specialEpisodeInfo != null)
                        {
                            parsedEpisodeInfo = specialEpisodeInfo;
                        }
                    }

                    if (parsedEpisodeInfo != null && !parsedEpisodeInfo.SeriesTitle.IsNullOrWhiteSpace())
                    {
                        var remoteEpisode = _parsingService.Map(parsedEpisodeInfo, report.TvRageId, searchCriteria);
                        remoteEpisode.Release = report;

                        if (remoteEpisode.Series != null)
                        {
                            remoteEpisode.DownloadAllowed = remoteEpisode.Episodes.Any();
                            decision = GetDecisionForReport(remoteEpisode, searchCriteria);
                        }
                        else
                        {
                            decision = new DownloadDecision(remoteEpisode, new Rejection("Unknown Series"));
                        }
                    }
                }
                catch (Exception e)
                {
                    _logger.ErrorException("Couldn't process release.", e);
                }

                reportNumber++;

                if (decision != null)
                {
                    if (decision.Rejections.Any())
                    {
                        _logger.Debug("Release rejected for the following reasons: {0}", String.Join(", ", decision.Rejections));
                    }

                    yield return decision;
                }
            }
        }

        private DownloadDecision GetDecisionForReport(RemoteEpisode remoteEpisode, SearchCriteriaBase searchCriteria = null)
        {
            var reasons = _specifications.Select(c => EvaluateSpec(c, remoteEpisode, searchCriteria))
                                         .Where(c => c != null);

            return new DownloadDecision(remoteEpisode, reasons.ToArray());
        }

        private Rejection EvaluateSpec(IDecisionEngineSpecification spec, RemoteEpisode remoteEpisode, SearchCriteriaBase searchCriteriaBase = null)
        {
            try
            {
                var result = spec.IsSatisfiedBy(remoteEpisode, searchCriteriaBase);

                if (!result.Accepted)
                {
                    return new Rejection(result.Reason, spec.Type);
                }
            }
            catch (Exception e)
            {
                e.Data.Add("report", remoteEpisode.Release.ToJson());
                e.Data.Add("parsed", remoteEpisode.ParsedEpisodeInfo.ToJson());
                _logger.ErrorException("Couldn't evaluate decision on " + remoteEpisode.Release.Title, e);
                return new Rejection(String.Format("{0}: {1}", spec.GetType().Name, e.Message));
            }

            return null;
        }
    }
}
