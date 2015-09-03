﻿using System;
using System.Collections.Generic;
using System.Linq;
using FluentValidation;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Datastore.Events;
using NzbDrone.Core.MediaCover;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.MediaFiles.Events;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.SeriesStats;
using NzbDrone.Core.Tv;
using NzbDrone.Api.Validation;
using NzbDrone.Api.Mapping;
using NzbDrone.Core.Tv.Events;
using NzbDrone.Core.Validation.Paths;
using NzbDrone.Core.DataAugmentation.Scene;
using NzbDrone.SignalR;

namespace NzbDrone.Api.Series
{
    public class SeriesModule : NzbDroneRestModuleWithSignalR<SeriesResource, Core.Tv.Series>, 
                                IHandle<EpisodeImportedEvent>, 
                                IHandle<EpisodeFileDeletedEvent>,
                                IHandle<SeriesUpdatedEvent>,       
                                IHandle<SeriesEditedEvent>,  
                                IHandle<SeriesDeletedEvent>,
                                IHandle<SeriesRenamedEvent>,
                                IHandle<MediaCoversUpdatedEvent>

    {
        private readonly ISeriesService _seriesService;
        private readonly ISeriesStatisticsService _seriesStatisticsService;
        private readonly ISceneMappingService _sceneMappingService;
        private readonly IMapCoversToLocal _coverMapper;

        public SeriesModule(IBroadcastSignalRMessage signalRBroadcaster,
                            ISeriesService seriesService,
                            ISeriesStatisticsService seriesStatisticsService,
                            ISceneMappingService sceneMappingService,
                            IMapCoversToLocal coverMapper,
                            RootFolderValidator rootFolderValidator,
                            SeriesPathValidator seriesPathValidator,
                            SeriesExistsValidator seriesExistsValidator,
                            DroneFactoryValidator droneFactoryValidator,
                            SeriesAncestorValidator seriesAncestorValidator
            )
            : base(signalRBroadcaster)
        {
            _seriesService = seriesService;
            _seriesStatisticsService = seriesStatisticsService;
            _sceneMappingService = sceneMappingService;

            _coverMapper = coverMapper;

            GetResourceAll = AllSeries;
            GetResourceById = GetSeries;
            CreateResource = AddSeries;
            UpdateResource = UpdateSeries;
            DeleteResource = DeleteSeries;

            SharedValidator.RuleFor(s => s.ProfileId).ValidId();

            SharedValidator.RuleFor(s => s.Path)
                           .Cascade(CascadeMode.StopOnFirstFailure)
                           .IsValidPath()
                           .SetValidator(rootFolderValidator)
                           .SetValidator(seriesPathValidator)
                           .SetValidator(droneFactoryValidator)
                           .SetValidator(seriesAncestorValidator)
                           .When(s => !s.Path.IsNullOrWhiteSpace());

            PostValidator.RuleFor(s => s.Path).IsValidPath().When(s => s.RootFolderPath.IsNullOrWhiteSpace());
            PostValidator.RuleFor(s => s.RootFolderPath).IsValidPath().When(s => s.Path.IsNullOrWhiteSpace());
            PostValidator.RuleFor(s => s.Title).NotEmpty();
            PostValidator.RuleFor(s => s.TvdbId).GreaterThan(0).SetValidator(seriesExistsValidator);

            PutValidator.RuleFor(s => s.Path).IsValidPath();
        }

        private SeriesResource GetSeries(int id)
        {
            var series = _seriesService.GetSeries(id);
            return GetSeriesResource(series);
        }

        private SeriesResource GetSeriesResource(Core.Tv.Series series)
        {
            if (series == null) return null;

            var resource = series.InjectTo<SeriesResource>();
            MapCoversToLocal(resource);
            FetchAndLinkSeriesStatistics(resource);
            PopulateAlternateTitles(resource);

            return resource;
        }

        private List<SeriesResource> AllSeries()
        {
            var seriesStats = _seriesStatisticsService.SeriesStatistics();
            var seriesResources = ToListResource(_seriesService.GetAllSeries);

            MapCoversToLocal(seriesResources.ToArray());
            LinkSeriesStatistics(seriesResources, seriesStats);
            PopulateAlternateTitles(seriesResources);

            return seriesResources;
        }

        private int AddSeries(SeriesResource seriesResource)
        {
            var series = _seriesService.AddSeries(seriesResource.InjectTo<Core.Tv.Series>());

            return series.Id;
        }

        private void UpdateSeries(SeriesResource seriesResource)
        {
            GetNewId<Core.Tv.Series>(_seriesService.UpdateSeries, seriesResource);

            BroadcastResourceChange(ModelAction.Updated, seriesResource);
        }

        private void DeleteSeries(int id)
        {
            var deleteFiles = false;
            var deleteFilesQuery = Request.Query.deleteFiles;

            if (deleteFilesQuery.HasValue)
            {
                deleteFiles = Convert.ToBoolean(deleteFilesQuery.Value);
            }

            _seriesService.DeleteSeries(id, deleteFiles);
        }

        private void MapCoversToLocal(params SeriesResource[] series)
        {
            foreach (var seriesResource in series)
            {
                _coverMapper.ConvertToLocalUrls(seriesResource.Id, seriesResource.Images);
            }
        }

        private void FetchAndLinkSeriesStatistics(SeriesResource resource)
        {
            LinkSeriesStatistics(resource, _seriesStatisticsService.SeriesStatistics(resource.Id));
        }

        private void LinkSeriesStatistics(List<SeriesResource> resources, List<SeriesStatistics> seriesStatistics)
        {
            foreach (var series in resources)
            {
                var stats = seriesStatistics.SingleOrDefault(ss => ss.SeriesId == series.Id);
                if (stats == null) continue;

                LinkSeriesStatistics(series, stats);
            }
        }

        private void LinkSeriesStatistics(SeriesResource resource, SeriesStatistics seriesStatistics)
        {
            resource.TotalEpisodeCount = seriesStatistics.TotalEpisodeCount;
            resource.EpisodeCount = seriesStatistics.EpisodeCount;
            resource.EpisodeFileCount = seriesStatistics.EpisodeFileCount;
            resource.NextAiring = seriesStatistics.NextAiring;
            resource.PreviousAiring = seriesStatistics.PreviousAiring;
            resource.SizeOnDisk = seriesStatistics.SizeOnDisk;

            if (seriesStatistics.SeasonStatistics != null)
            {
               foreach (var season in resource.Seasons)
                {
                    season.Statistics = seriesStatistics.SeasonStatistics.SingleOrDefault(s => s.SeasonNumber == season.SeasonNumber).InjectTo<SeasonStatisticsResource>();
                }
            }
        }

        private void PopulateAlternateTitles(List<SeriesResource> resources)
        {
            foreach (var resource in resources)
            {
                PopulateAlternateTitles(resource);
            }
        }

        private void PopulateAlternateTitles(SeriesResource resource)
        {
            var mappings = _sceneMappingService.FindByTvdbId(resource.TvdbId);

            if (mappings == null) return;

            resource.AlternateTitles = mappings.InjectTo<List<AlternateTitleResource>>();
        }

        public void Handle(EpisodeImportedEvent message)
        {
            BroadcastResourceChange(ModelAction.Updated, message.ImportedEpisode.SeriesId);
        }

        public void Handle(EpisodeFileDeletedEvent message)
        {
            if (message.EpisodeFile.SeriesId == 0)
                return;
            if (message.Reason == DeleteMediaFileReason.Upgrade) return;

            BroadcastResourceChange(ModelAction.Updated, message.EpisodeFile.SeriesId);
        }

        public void Handle(SeriesUpdatedEvent message)
        {
            BroadcastResourceChange(ModelAction.Updated, message.Series.Id);
        }

        public void Handle(SeriesEditedEvent message)
        {
            BroadcastResourceChange(ModelAction.Updated, message.Series.Id);
        }

        public void Handle(SeriesDeletedEvent message)
        {
            BroadcastResourceChange(ModelAction.Deleted, message.Series.InjectTo<SeriesResource>());
        }

        public void Handle(SeriesRenamedEvent message)
        {
            BroadcastResourceChange(ModelAction.Updated, message.Series.Id);
        }

        public void Handle(MediaCoversUpdatedEvent message)
        {
            if (message.CoverOrigin == MediaCoverOrigin.Series)
                BroadcastResourceChange(ModelAction.Updated, message.Series.Id);
        }
    }
}
