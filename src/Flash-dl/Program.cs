using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using YoutubeExtractor;

namespace Flash_dl
{
    class Program
    {
        #region Private fields

        private const string applicationName = "Flash-dl";
        private static readonly Version applicationVersion = Assembly.GetExecutingAssembly().GetName().Version;
        private static readonly string applicationVersionName = string.Format("v{0}.{1}", applicationVersion.Major, applicationVersion.Minor, applicationVersion.Build, applicationVersion.Revision);
        private static readonly string applicationVersionVerboseName = string.Format("v{0}.{1} Patch {2} Build {3}", applicationVersion.Major, applicationVersion.Minor, applicationVersion.Build, applicationVersion.Revision);

        #endregion

        static void Main(string[] args)
        {
            UpdateTitle();
            bool menu = true;
            while (menu)
            {
                Console.Write("flash-dl > ");
                string url = Console.ReadLine();

                try
                {
                    IEnumerable<VideoInfo> videoInfos = DownloadUrlResolver.GetDownloadUrls(url, false);

                    if (url.Contains("-"))
                    {
                        //TODO: there must be a better way to do this
                        var customCommand = url.Remove(0, 45).ToLower();
                        // Handle all commands without arguments
                        switch (customCommand)
                        {
                            case "help":
                                Help();
                                return;
                            case "v":
                                DownloadVideo(videoInfos);
                                return;
                            case "a":
                                DownloadAudio(videoInfos);
                                return;
                            default:
                                Console.WriteLine("Invalid command.");
                                goto case "help";
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    Help();
                }
            }
        }

        #region Private helpers

        private static void UpdateTitle()
        {
            Console.Title = string.Format("{0} ({1})", applicationName, applicationVersionName);
        }

        private static void Help()
        {
            Console.WriteLine(string.Format("{0} {1}", applicationName, applicationVersionVerboseName));
            Console.WriteLine("Youtube: - [url] -[v]ideo|-[a]udio");
        }

        private static void DownloadVideo(IEnumerable<VideoInfo> videoInfos)
        {
            //  Our list of videos to download:
            List<VideoDownloader> videosToDownload = new List<VideoDownloader>();

            // Console.WriteLine("Single video url '{0}' was specified.  Processing...", options.VideoUrl);
            VideoInfo video = videoInfos.First(info => info.VideoType == VideoType.Mp4 && info.Resolution == 360);

            // If the video has a decrypted signature, decipher it
            if (video.RequiresDecryption)
            {
                DownloadUrlResolver.DecryptDownloadUrl(video);
            }

            var videoDownloader = new VideoDownloader(video, Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyVideos), GetSafeTitle(video.Title) + video.VideoExtension));
            videoDownloader.DownloadProgressChanged += videoDownloader_DownloadProgressChanged;

            videosToDownload.Add(videoDownloader);

            //  If we have videos to download, download them
            if (videosToDownload.Any())
            {
                Console.WriteLine("Downloading ...\n");

                //  Download each file we've queued up
                foreach (var download in videosToDownload)
                {
                    try
                    {
                        Console.WriteLine("'{0}'", download.Video.Title);
                        download.Execute();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("There was a problem downloading the file.  Continuing...", ex);
                        continue;
                    }
                }
            }
        }

        private static void DownloadAudio(IEnumerable<VideoInfo> videoInfos)
        {
            //  Our list of videos to download:
            List<AudioDownloader> audioToDownload = new List<AudioDownloader>();

            // Console.WriteLine("Single video url '{0}' was specified.  Processing...", options.VideoUrl);
            VideoInfo video = videoInfos.Where(info => info.CanExtractAudio).OrderByDescending(info => info.AudioBitrate).First();

            // If the video has a decrypted signature, decipher it
            if (video.RequiresDecryption)
            {
                DownloadUrlResolver.DecryptDownloadUrl(video);
            }

            var audioDownloader = new AudioDownloader(video, Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyMusic), GetSafeTitle(video.Title) + video.VideoExtension));

            // Register the progress events. We treat the download progress as 85% of the progress
            // and the extraction progress only as 15% of the progress, because the download will
            // take much longer than the audio extraction.
            audioDownloader.DownloadProgressChanged += audioDownloader_DownloadProgressChanged;
            audioDownloader.AudioExtractionProgressChanged += audioDownloader_AudioExtractionProgressChanged;

            audioToDownload.Add(audioDownloader);

            //  If we have videos to download, download them
            if (audioToDownload.Any())
            {
                Console.WriteLine("Downloading ...\n");

                //  Download each file we've queued up
                foreach (var download in audioToDownload)
                {
                    try
                    {
                        Console.WriteLine("'{0}'", download.Video.Title);
                        download.Execute();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("There was a problem downloading the file.  Continuing...", ex);
                        continue;
                    }
                }
            }
        }

        private static void videoDownloader_DownloadProgressChanged(object sender, ProgressEventArgs e)
        {
            DrawProgressBar(Convert.ToInt32(Math.Floor(e.ProgressPercentage)), 100, 60, '#');
        }
        private static void audioDownloader_DownloadProgressChanged(object sender, ProgressEventArgs e)
        {
            DrawProgressBar(Convert.ToInt32(Math.Floor(e.ProgressPercentage * 0.85)), 100, 60, '#');
        }

        private static void audioDownloader_AudioExtractionProgressChanged(object sender, ProgressEventArgs e)
        {
            DrawProgressBar(Convert.ToInt32(Math.Floor(85 + e.ProgressPercentage * 0.15)), 100, 60, '#');
        }


        /// <summary>
        /// Gets a filesystem safe title
        /// </summary>
        /// <param name="videoTitle"></param>
        /// <returns></returns>
        private static string GetSafeTitle(string videoTitle)
        {
            string safeTitle = string.Empty;
            safeTitle = Regex.Replace(videoTitle, @"[^a-zA-Z\d]", " ").Trim();

            Console.WriteLine("Converting title '{0}' to safe filename '{1}'", videoTitle, safeTitle);

            return safeTitle;
        }

        /// <summary>
        /// Progress bar animation
        /// </summary>
        /// <param name="complete"></param>
        /// <param name="maxVal"></param>
        /// <param name="barSize"></param>
        /// <param name="progressCharacter"></param>
        private static void DrawProgressBar(int complete, int maxVal, int barSize, char progressCharacter)
        {
            Console.CursorVisible = false;
            int left = Console.CursorLeft;
            decimal perc = (decimal)complete / (decimal)maxVal;
            int chars = (int)Math.Floor(perc / ((decimal)1 / (decimal)barSize));
            string p1 = String.Empty, p2 = String.Empty;

            for (int i = 0; i < chars; i++)
                p1 += progressCharacter;
            for (int i = 0; i < barSize - chars; i++)
                p2 += progressCharacter;

            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write(p1);
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.Write(p2);

            Console.ResetColor();
            Console.Write(" {0}%", (perc * 100).ToString("N2"));
            Console.CursorLeft = left;
        }

        #endregion
    }
}
