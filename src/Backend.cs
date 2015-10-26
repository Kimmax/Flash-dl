using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using YoutubeExtractor;

namespace Flash_dl
{
    public static class Backend
    {
        #region Private fields

        private const string applicationName = "Flash-dl";
        private static readonly Version applicationVersion = Assembly.GetExecutingAssembly().GetName().Version;
        private static readonly string applicationVersionName = string.Format("v{0}.{1}", applicationVersion.Major, applicationVersion.Minor, applicationVersion.Build, applicationVersion.Revision);
        private static readonly string applicationVersionVerboseName = string.Format("v{0}.{1} Patch {2} Build {3}", applicationVersion.Major, applicationVersion.Minor, applicationVersion.Build, applicationVersion.Revision);

        #endregion

        #region Private Helpers
        public static void UpdateTitle()
        {
            Console.Title = string.Format("{0} ({1})", applicationName, applicationVersionName);
        }

        public static string Help(bool fromBadCommand = false, string command = "")
        {
            string output = "";

            if(fromBadCommand)
            {
                if(!string.IsNullOrEmpty(command))
                    output += string.Format("Command \"{0}\" not found.\nPrinting help.\n", command);
            }

            output += string.Format("\n{0} {1}", applicationName, applicationVersionVerboseName) + "\n\n";
            output += "DownloadVideo http://youtube.com/watch?v=abcdefg - Downloads the video from the URL to your Video folder.\n";
            output += "DownloadAudio http://youtube.com/watch?v=abcdefg - Downloads the video from the URL, coverts it to audio and saves it.\n";
            output += "DownloadPlaylist https://www.youtube.com/watch?list=abcdefg - Downloads a whole playlist from youtube.\n";
            output += "Exit - Closes the application. Goodbye!";
            return output;
        }

        public static bool DownloadVideo(IEnumerable<VideoInfo> videoInfos)
        {
            //  Our list of videos to download:
            List<VideoDownloader> videosToDownload = new List<VideoDownloader>();

            VideoInfo video = videoInfos.First(info => info.VideoType == VideoType.Mp4 && info.Resolution == 360);
            //Console.WriteLine("Single video url '{0}' was specified.  Processing...", video.DownloadUrl);

            // If the video has a decrypted signature, decipher it
            if (video.RequiresDecryption)
            {
                DownloadUrlResolver.DecryptDownloadUrl(video);
            }

            var videoDownloader = new VideoDownloader(video, Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyVideos), RemoveIllegalPathCharacters(video.Title) + video.VideoExtension));
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

                return true;
            }
            else
            {
                return false;
            }
        }

        public static bool DownloadAudio(IEnumerable<VideoInfo> videoInfos)
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

            var audioDownloader = new AudioDownloader(video, Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyMusic), RemoveIllegalPathCharacters(video.Title) + video.AudioExtension));

            // Register the progress events. We treat the download progress as 85% of the progress
            // and the extraction progress only as 15% of the progress, because the download will
            // take much longer than the audio extraction.
            audioDownloader.DownloadProgressChanged += (sender, args) => DrawProgressBar(Convert.ToInt32(Math.Floor(args.ProgressPercentage * 0.85)), 100, 60, '#');
            audioDownloader.AudioExtractionProgressChanged += (sender, args) => DrawProgressBar(Convert.ToInt32(Math.Floor(85 + args.ProgressPercentage * 0.15)), 100, 60, '#');

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
                
                return true;
            }
            else
            {
                return false;
            }
        }

        //TODO: Download playlist not working needs panel beating
        public static bool DownloadPlaylist(IEnumerable<VideoInfo> videosInfo)
        {
            //Our list of videos to download:
            List<VideoDownloader> videosToDownload = new List<VideoDownloader>();

            List<VideoInfo> videos = VideoList.FromYouTubePlaylist("", 320);

            Console.WriteLine("A total of {0} videos have been parsed from the feed.  Adding video download object for each...", videos.Count);

            foreach (var video in videos)
            {
                var videoDownloader = new VideoDownloader(video, Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyVideos), RemoveIllegalPathCharacters(video.Title) + video.VideoExtension));
                videoDownloader.DownloadProgressChanged += videoDownloader_DownloadProgressChanged;

                videosToDownload.Add(videoDownloader);
            }

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

               return true;
            }
            else
            {
                return false;
            }
        }

        private static void videoDownloader_DownloadProgressChanged(object sender, ProgressEventArgs e)
        {
            DrawProgressBar(Convert.ToInt32(Math.Floor(e.ProgressPercentage)), 100, 60, '#');
        }

        /// <summary>
        /// Gets a filesystem valid title
        /// </summary>
        /// <param name="RemoveIllegalPathCharacters"></param>
        /// <returns></returns>
        private static string RemoveIllegalPathCharacters(string path)
        {
            string regexSearch = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());
            var r = new Regex(string.Format("[{0}]", Regex.Escape(regexSearch)));

            return r.Replace(path, "");
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
