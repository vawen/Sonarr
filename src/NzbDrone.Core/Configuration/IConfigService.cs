using System;
using System.Collections.Generic;
using NzbDrone.Core.MediaFiles;

namespace NzbDrone.Core.Configuration
{
    public interface IConfigService
    {
        IEnumerable<Config> All();
        Dictionary<String, Object> AllWithDefaults();
        void SaveConfigDictionary(Dictionary<string, object> configValues);

        Boolean IsDefined(String key);

        //Download Client
        String DownloadedEpisodesFolder { get; set; }
        String DownloadClientWorkingFolders { get; set; }
        Int32 DownloadedEpisodesScanInterval { get; set; }
        Int32 DownloadClientHistoryLimit { get; set; }

        //Completed/Failed Download Handling (Download client)
        Boolean EnableCompletedDownloadHandling { get; set; }
        Boolean RemoveCompletedDownloads { get; set; }

        Boolean AutoRedownloadFailed { get; set; }
        Boolean RemoveFailedDownloads { get; set; }

        //Media Management
        Boolean AutoUnmonitorPreviouslyDownloadedEpisodes { get; set; }
        String RecycleBin { get; set; }
        Boolean AutoDownloadPropers { get; set; }
        Boolean CreateEmptySeriesFolders { get; set; }
        Boolean CreateEmptyMovieFolders { get; set; }
        FileDateType FileDate { get; set; }
        Boolean SkipFreeSpaceCheckWhenImporting { get; set; }
        Boolean CopyUsingHardlinks { get; set; }
        Boolean EnableMediaInfo { get; set; }

        //Permissions (Media Management)
        Boolean SetPermissionsLinux { get; set; }
        String FileChmod { get; set; }
        String FolderChmod { get; set; }
        String ChownUser { get; set; }
        String ChownGroup { get; set; }

        //Indexers
        Int32 Retention { get; set; }
        Int32 RssSyncInterval { get; set; }
        Int32 MinimumAge { get; set; }

        //UI
        Int32 FirstDayOfWeek { get; set; }
        String CalendarWeekColumnHeader { get; set; }
        
        String ShortDateFormat { get; set; }
        String LongDateFormat { get; set; }
        String TimeFormat { get; set; }
        Boolean ShowRelativeDates { get; set; }
        Boolean EnableColorImpairedMode { get; set; }

        //Internal
        Boolean CleanupMetadataImages { get; set; }


        //Forms Auth
        string RijndaelPassphrase { get; }
        string HmacPassphrase { get; }
        string RijndaelSalt { get; }
        string HmacSalt { get; }
    }
}
