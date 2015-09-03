﻿using System;
using System.Globalization;
using System.IO;
using System.Text;
using NLog;
using NzbDrone.Common.Disk;
using NzbDrone.Common.EnvironmentInfo;

namespace NzbDrone.Core.MediaFiles.MediaInfo
{
    public interface IVideoFileInfoReader
    {
        MediaInfoModel GetMediaInfo(string filename);
        TimeSpan GetRunTime(string filename);
    }

    public class VideoFileInfoReader : IVideoFileInfoReader
    {
        private readonly IDiskProvider _diskProvider;
        private readonly Logger _logger;


        public VideoFileInfoReader(IDiskProvider diskProvider, Logger logger)
        {
            _diskProvider = diskProvider;
            _logger = logger;
        }


        public MediaInfoModel GetMediaInfo(string filename)
        {
            if (!_diskProvider.FileExists(filename))
                throw new FileNotFoundException("Media file does not exist: " + filename);

            MediaInfo mediaInfo = null;

            try
            {
                mediaInfo = new MediaInfo();
                _logger.Debug("Getting media info from {0}", filename);

                if (filename.ToLower().EndsWith(".ts"))
                {
                    mediaInfo.Option("ParseSpeed", "0.3");
                }
                else
                {
                    mediaInfo.Option("ParseSpeed", "0.0");
                }

                int open;

                using (var stream = _diskProvider.OpenReadStream(filename))
                {
                    open = mediaInfo.Open(stream);
                }

                if (open != 0)
                {
                    int audioRuntime;
                    int videoRuntime;
                    int generalRuntime;

                    //Runtime
                    Int32.TryParse(mediaInfo.Get(StreamKind.Video, 0, "PlayTime"), out videoRuntime);
                    Int32.TryParse(mediaInfo.Get(StreamKind.Audio, 0, "PlayTime"), out audioRuntime);
                    Int32.TryParse(mediaInfo.Get(StreamKind.General, 0, "PlayTime"), out generalRuntime);

                    if (audioRuntime == 0 && videoRuntime == 0 && generalRuntime == 0)
                    {
                        mediaInfo.Option("ParseSpeed", "1.0");

                        using (var stream = _diskProvider.OpenReadStream(filename))
                        {
                            open = mediaInfo.Open(stream);
                        }
                    }
                }

                if (open != 0)
                {
                    int width;
                    int height;
                    int videoBitRate;
                    int audioBitRate;
                    int audioRuntime;
                    int videoRuntime;
                    int generalRuntime;
                    int streamCount;
                    int audioChannels;
                    decimal videoFrameRate;

                    string subtitles = mediaInfo.Get(StreamKind.General, 0, "Text_Language_List");
                    string scanType = mediaInfo.Get(StreamKind.Video, 0, "ScanType");
                    Int32.TryParse(mediaInfo.Get(StreamKind.Video, 0, "Width"), out width);
                    Int32.TryParse(mediaInfo.Get(StreamKind.Video, 0, "Height"), out height);
                    Int32.TryParse(mediaInfo.Get(StreamKind.Video, 0, "BitRate"), out videoBitRate);
                    Decimal.TryParse(mediaInfo.Get(StreamKind.Video, 0, "FrameRate"), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out videoFrameRate);

                    //Runtime
                    Int32.TryParse(mediaInfo.Get(StreamKind.Video, 0, "PlayTime"), out videoRuntime);
                    Int32.TryParse(mediaInfo.Get(StreamKind.Audio, 0, "PlayTime"), out audioRuntime);
                    Int32.TryParse(mediaInfo.Get(StreamKind.General, 0, "PlayTime"), out generalRuntime);

                    string aBitRate = mediaInfo.Get(StreamKind.Audio, 0, "BitRate");
                    int aBindex = aBitRate.IndexOf(" /", StringComparison.InvariantCultureIgnoreCase);
                    if (aBindex > 0)
                        aBitRate = aBitRate.Remove(aBindex);

                    Int32.TryParse(aBitRate, out audioBitRate);
                    Int32.TryParse(mediaInfo.Get(StreamKind.Audio, 0, "StreamCount"), out streamCount);
                    

                    string audioChannelsStr = mediaInfo.Get(StreamKind.Audio, 0, "Channel(s)");
                    int aCindex = audioChannelsStr.IndexOf(" /", StringComparison.InvariantCultureIgnoreCase);
                    if (aCindex > 0)
                        audioChannelsStr = audioChannelsStr.Remove(aCindex);

                    string audioLanguages = mediaInfo.Get(StreamKind.General, 0, "Audio_Language_List");
                    string audioProfile = mediaInfo.Get(StreamKind.Audio, 0, "Format_Profile");

                    int aPindex = audioProfile.IndexOf(" /", StringComparison.InvariantCultureIgnoreCase);
                    if (aPindex > 0)
                        audioProfile = audioProfile.Remove(aPindex);

                    Int32.TryParse(audioChannelsStr, out audioChannels);
                    var mediaInfoModel = new MediaInfoModel
                                                {
                                                    VideoCodec = mediaInfo.Get(StreamKind.Video, 0, "Codec/String"),
                                                    VideoBitrate = videoBitRate,
                                                    Height = height,
                                                    Width = width,
                                                    AudioFormat = mediaInfo.Get(StreamKind.Audio, 0, "Format"),
                                                    AudioBitrate = audioBitRate,
                                                    RunTime = GetBestRuntime(audioRuntime, videoRuntime, generalRuntime),
                                                    AudioStreamCount = streamCount,
                                                    AudioChannels = audioChannels,
                                                    AudioProfile = audioProfile.Trim(),
                                                    VideoFps = videoFrameRate,
                                                    AudioLanguages = audioLanguages,
                                                    Subtitles = subtitles,
                                                    ScanType = scanType
                                                };

                    return mediaInfoModel;
                }
                else
                {
                    _logger.Warn("Unable to open media info from file: " + filename);
                }
            }
            catch (DllNotFoundException ex)
            {
                _logger.ErrorException("mediainfo is required but was not found", ex);
            }
            catch (Exception ex)
            {
                _logger.ErrorException("Unable to parse media info from file: " + filename, ex);
            }
            finally
            {
                if (mediaInfo != null)
                {
                    mediaInfo.Close();
                }
            }

            return null;
        }

        public TimeSpan GetRunTime(string filename)
        {
            var info = GetMediaInfo(filename);

            if (info == null)
            {
                return new TimeSpan();
            }

            return info.RunTime;
        }

        private TimeSpan GetBestRuntime(int audio, int video, int general)
        {
            if (video == 0)
            {
                if (audio == 0)
                {
                    return TimeSpan.FromMilliseconds(general);
                }

                return TimeSpan.FromMilliseconds(audio);
            }

            return TimeSpan.FromMilliseconds(video);
        }
    }
}
