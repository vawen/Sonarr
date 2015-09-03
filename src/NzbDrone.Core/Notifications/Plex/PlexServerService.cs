﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using FluentValidation.Results;
using NLog;
using NzbDrone.Common.Cache;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Notifications.Plex.Models;
using NzbDrone.Core.Tv;

namespace NzbDrone.Core.Notifications.Plex
{
    public interface IPlexServerService
    {
        void UpdateLibrary(Series series, PlexServerSettings settings);
        ValidationFailure Test(PlexServerSettings settings);
    }

    public class PlexServerService : IPlexServerService
    {
        private readonly ICached<bool> _partialUpdateCache;
        private readonly IPlexServerProxy _plexServerProxy;
        private readonly Logger _logger;

        public PlexServerService(ICacheManager cacheManager, IPlexServerProxy plexServerProxy, Logger logger)
        {
            _partialUpdateCache = cacheManager.GetCache<bool>(GetType(), "partialUpdateCache");
            _plexServerProxy = plexServerProxy;
            _logger = logger;
        }

        public void UpdateLibrary(Series series, PlexServerSettings settings)
        {
            try
            {
                _logger.Debug("Sending Update Request to Plex Server");
                
                var sections = GetSections(settings);
                var partialUpdates = _partialUpdateCache.Get(settings.Host, () => PartialUpdatesAllowed(settings), TimeSpan.FromHours(2));

                if (partialUpdates)
                {
                    UpdatePartialSection(series, sections, settings);
                }

                else
                {
                    sections.ForEach(s => UpdateSection(s.Id, settings));
                }
            }

            catch(Exception ex)
            {
                _logger.WarnException("Failed to Update Plex host: " + settings.Host, ex);
                throw;
            }
        }

        private List<PlexSection> GetSections(PlexServerSettings settings)
        {
            _logger.Debug("Getting sections from Plex host: {0}", settings.Host);

            return _plexServerProxy.GetTvSections(settings).ToList();
        }

        private bool PartialUpdatesAllowed(PlexServerSettings settings)
        {
            try
            {
                var rawVersion = GetVersion(settings);
                var version = new Version(Regex.Match(rawVersion, @"^(\d+\.){4}").Value.Trim('.'));

                if (version >= new Version(0, 9, 12, 0))
                {
                    var preferences = GetPreferences(settings);
                    var partialScanPreference = preferences.SingleOrDefault(p => p.Id.Equals("FSEventLibraryPartialScanEnabled"));

                    if (partialScanPreference == null)
                    {
                        return false;
                    }

                    return Convert.ToBoolean(partialScanPreference.Value);
                }
            }
            catch (Exception ex)
            {
                _logger.WarnException("Unable to check if partial updates are allowed", ex);
            }

            return false;
        }

        private string GetVersion(PlexServerSettings settings)
        {
            _logger.Debug("Getting version from Plex host: {0}", settings.Host);

            return _plexServerProxy.Version(settings);
        }

        private List<PlexPreference> GetPreferences(PlexServerSettings settings)
        {
            _logger.Debug("Getting preferences from Plex host: {0}", settings.Host);

            return _plexServerProxy.Preferences(settings);
        }

        private void UpdateSection(int sectionId, PlexServerSettings settings)
        {
            _logger.Debug("Updating Plex host: {0}, Section: {1}", settings.Host, sectionId);

            _plexServerProxy.Update(sectionId, settings);
        }

        private void UpdatePartialSection(Series series, List<PlexSection> sections, PlexServerSettings settings)
        {
            var partiallyUpdated = false;

            foreach (var section in sections)
            {
                var metadataId = GetMetadataId(section.Id, series, section.Language, settings);

                if (metadataId.HasValue)
                {
                    _logger.Debug("Updating Plex host: {0}, Section: {1}, Series: {2}", settings.Host, section.Id, series);
                    _plexServerProxy.UpdateSeries(metadataId.Value, settings);

                    partiallyUpdated = true;
                }
            }

            // Only update complete sections if all partial updates failed
            if (!partiallyUpdated)
            {
                _logger.Debug("Unable to update partial section, updating all TV sections");
                sections.ForEach(s => UpdateSection(s.Id, settings));
            }
        }

        private int? GetMetadataId(int sectionId, Series series, string language, PlexServerSettings settings)
        {
            _logger.Debug("Getting metadata from Plex host: {0} for series: {1}", settings.Host, series);

            return _plexServerProxy.GetMetadataId(sectionId, series.TvdbId, language, settings);
        }

        public ValidationFailure Test(PlexServerSettings settings)
        {
            try
            {
                var sections = GetSections(settings);

                if (sections.Empty())
                {
                    return new ValidationFailure("Host", "At least one TV library is required");
                }
            }
            catch(PlexAuthenticationException ex)
            {
                _logger.ErrorException("Unable to connect to Plex Server: " + ex.Message, ex);
                return new ValidationFailure("Username", "Incorrect username or password");
            }
            catch (Exception ex)
            {
                _logger.ErrorException("Unable to connect to Plex Server: " + ex.Message, ex);
                return new ValidationFailure("Host", "Unable to connect to Plex Server");
            }

            return null;
        }
    }
}
