using SYMM.Interfaces;
using SYMM_Backend;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Flash_dl
{
    public static class Backend
    {
        #region Private fields

        private const string applicationName = "Flash-dl";
        private static readonly Version applicationVersion = Assembly.GetExecutingAssembly().GetName().Version;
        private static readonly string applicationVersionName = string.Format("v{0}.{1}", applicationVersion.Major, applicationVersion.Minor, applicationVersion.Build, applicationVersion.Revision);
        private static readonly string applicationVersionVerboseName = string.Format("v{0}.{1} Patch {2} Build {3}", applicationVersion.Major, applicationVersion.Minor, applicationVersion.Build, applicationVersion.Revision);

        public static SYMMHandler symmBackend;

        // Videolist used to store all videos that are about to get downloaded
        private static List<YouTubeVideo> rawVideoList = new List<YouTubeVideo>();

        #endregion

        #region Private Methods

        public static string[] SayHello()
        {
            string[] result = new string[4];
            result[0] = GetHeader();
            result[1] = "Welcome to Flash-DL!";
            result[2] = "What will we do today?";
            result[3] = "";
            return result;
        }

        public static void UpdateTitle()
        {
            Console.Title = string.Format("{0} ({1})", applicationName, applicationVersionName);
        }

        public static string[] Help(bool fromBadCommand = false, string command = "")
        {
            List<string> output = new List<string>();

            if(fromBadCommand)
            {
                if(!string.IsNullOrEmpty(command))
                    output.Add(string.Format("Command \"{0}\" not found.\nPrinting help.\n", command));
            }

            output.Add("This might help you!");
            output.Add("DownloadVideo <url> - Downloads the video");
            output.Add("DownloadAudio <url> - Downloads audio only");
            output.Add("StreamAudio <url> - Streams directly audio only!");
            output.Add("DownloadPlaylist <url?list=xyz> - Downloads a playlist.");
            output.Add("Set settings - See 'settings.help'");
            output.Add("Exit - Goodbye!");

            return output.ToArray();
        }

        public static void Execute(SYMMSettings settings)
        {
            symmBackend = new SYMMHandler(Properties.Settings.Default.youtubeApiKey);
            rawVideoList = new List<YouTubeVideo>();

            // Create save folder, when not existent and not streaming
            if (settings.Action != SYMMSettings.Actions.Stream && !Directory.Exists(settings.SavePath))
                Directory.CreateDirectory(settings.SavePath);

            symmBackend.OnVideoInformationLoaded += (s, e) =>
            {
                rawVideoList.Add(e.Video);
            };

            symmBackend.OnStreamPostionChanged += (dsender, deventargs) =>
            {
                // Show progress on GUI
                DrawProgressBar((int)Math.Floor(deventargs.ProgressPercentage), 100, 60, '#');
            };

            // Register finished download of one video
            symmBackend.OnStreamComplete += (dsender, deventargs) =>
            {
                Program.WriteToConsole(String.Format("\nVideo finsihed: \"{0}\"", deventargs.Video.VideoTitle));
            };

            // Register changed download progress of one video Audio process counts as 75% work
            symmBackend.OnVideoDownloadProgressChanged += (dsender, deventargs) =>
            {
                // Show progress on GUI
                if (settings.Action == SYMMSettings.Actions.ExtractAudio)
                {
                    DrawProgressBar((int)Math.Floor(deventargs.ProgressPercentage) / 4, 100, 60, '#');
                }
                else
                {
                    DrawProgressBar((int)Math.Floor(deventargs.ProgressPercentage), 100, 60, '#');
                }
            };

            // Register changed audio extraction progress of one video. Audio process counts as 25% work
            symmBackend.OnVideoAudioExtractionProgressChanged += (dsender, deventargs) =>
            {
                // Show progress on GUI
                if (settings.Action == SYMMSettings.Actions.ExtractAudio)
                {
                    DrawProgressBar((int)Math.Floor(deventargs.ProgressPercentage) / 4 + 75, 100, 60, '#');
                }
                else
                {
                    DrawProgressBar((int)Math.Floor(deventargs.ProgressPercentage), 100, 60, '#');
                }
            };

            // Register finished download of one video
            symmBackend.OnVideoDownloadComplete += (dsender, deventargs) =>
            {
                Program.WriteToConsole(String.Format("\nVideo finsihed: \"{0}\"", deventargs.Video.VideoTitle));
            };

            // Register when a video failed to download
            symmBackend.OnVideoDownloadFailed += (dsender, deventargs) =>
            {
                Program.WriteToConsole(String.Format("Video failed to download: \"{0}\"", deventargs.Video.VideoTitle));
            };

            Program.WriteToConsole("Loading data..");
            symmBackend.LoadVideosFromURL(settings);
            Program.WriteToConsole("Loaded. Starting work!");

            // We want to download every video in this list
            foreach (YouTubeVideo video in rawVideoList)
            {
                Program.WriteToConsole(String.Format("\"{0}\"", video.VideoTitle));

                if (settings.Action != SYMMSettings.Actions.Stream)
                {
                    // Prepare backend
                    settings.PathSafefileName = symmBackend.BuildPathSafeName(video.VideoTitle);

                    if (settings.CheckDuplicate && Directory.GetFiles(settings.SavePath, settings.PathSafefileName + ".*").Length > 0)
                    {
                        // Looks like we downloaded that already. Skip.
                        Program.WriteToConsole(String.Format("Looks like we already downloaded \"{0}\".\nSet 'settings.duplicatecheck' to false to ignore this.", video.VideoTitle));

                        // Skip the rest
                        continue;
                    }
                }

                // Tell backend to download the video spceifed to destination spceifed in the variable
                symmBackend.Execute(video, settings);
            }

            symmBackend = null;
        }

        public static string GetHeader()
        {
            return string.Format("{0} {1}", applicationName, applicationVersionVerboseName);
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
