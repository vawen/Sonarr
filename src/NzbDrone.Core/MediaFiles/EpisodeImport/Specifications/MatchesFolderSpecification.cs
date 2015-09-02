﻿using System;
using System.IO;
using System.Linq;
using NLog;
using NzbDrone.Core.DecisionEngine;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.MediaFiles.EpisodeImport.Specifications
{
    public class MatchesFolderSpecification : IImportDecisionEngineSpecification
    {
        private readonly Logger _logger;

        public MatchesFolderSpecification(Logger logger)
        {
            _logger = logger;
        }
        public Decision IsSatisfiedBy(LocalEpisode localEpisode)
        {
            if (localEpisode.ParsedEpisodeInfo == null)
                return Decision.Accept();

            if (localEpisode.ExistingFile)
            {
                return Decision.Accept();
            }

            var dirInfo = new FileInfo(localEpisode.Path).Directory;

            if (dirInfo == null)
            {
                return Decision.Accept();
            }

            var folderInfo = Parser.Parser.ParseTitle(dirInfo.Name);

            if (folderInfo == null)
            {
                return Decision.Accept();
            }

            if (!folderInfo.EpisodeNumbers.Any())
            {
                return Decision.Accept();
            }

            if (folderInfo.FullSeason)
            {
                return Decision.Accept();
            }

            var unexpected = localEpisode.ParsedEpisodeInfo.EpisodeNumbers.Where(f => !folderInfo.EpisodeNumbers.Contains(f)).ToList();

            if (unexpected.Any())
            {
                _logger.Debug("Unexpected episode number(s) in file: {0}", String.Join(", ", unexpected));

                if (unexpected.Count == 1)
                {
                    return Decision.Reject("Episode Number {0} was unexpected", unexpected.First());
                }

                return Decision.Reject("Episode Numbers {0} were unexpected", String.Join(", ", unexpected));
            }

            return Decision.Accept();
        }
    }
}
