using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SYMM_Backend;
using Nuernberger.ConsoleMenu;
using System.Threading;
using System.IO;

namespace Flash_dl
{
    class Program
    {
        #region Private fields
        private const string applicationName = "Flash-dl";
        private static readonly Version applicationVersion = Assembly.GetExecutingAssembly().GetName().Version;
        private static readonly string applicationVersionName = string.Format("v{0}.{1}", applicationVersion.Major, applicationVersion.Minor, applicationVersion.Build, applicationVersion.Revision);
        private static readonly string applicationVersionVerboseName = string.Format("v{0}.{1} Patch {2} Build {3}", applicationVersion.Major, applicationVersion.Minor, applicationVersion.Build, applicationVersion.Revision);

        public static SYMMHandler symmBackend;

        // Videolist used to store all videos that are about to get downloaded
        private static List<YouTubeVideo> rawVideoList = new List<YouTubeVideo>();

        private static bool isDev = false;
        private static bool running = true;
        private static LayerManager menu = new LayerManager();
        private static Block titleBar, mainSection, inputField;
        #endregion

        static void Main(string[] args)
        {
            Console.Title = GetTitle();
            if (!isDev && Properties.Settings.Default.SettingsNeedReset)
                Commands.Settings.Reset(true);

            Console.CursorVisible = false;

            SetUpViews();

            menu.AddLayer(titleBar);
            menu.AddLayer(mainSection);

            Run();
        }

        static void Run()
        {
            while (running)
            {
                int selectedFromMain = GetSelectedItem(mainSection);
                mainSection.IsVisible = false;
                menu.Draw();
                string url = ReadFromInput("Please enter the url below:");

                /*   1) Video
                 *   2) Audio of video
                 *   3) Stream audio of video
                 *   4) Playlist
                 *   5) Audio of playlist
                 *   6) Stream audio of a playlist
                 *   7) Unkown -> parse url
                 */
                
                switch(selectedFromMain)
                {
                    case 1:
                    {
                        DownloadVideo(url);
                        break;
                    }
                    case 2:
                    {
                        DownloadAudio(url);
                        break;
                    }
                    case 3:
                    case 6:
                    {
                        StreamAudio(url);
                        break;
                    }
                    case 4:
                    case 5:
                    {
                        DownloadPlaylist(url);
                        break;
                    }
                    case 7:
                    {
                        throw new NotImplementedException();
                    }
                }

            }
        }

        static int GetSelectedItem(Block view, int preSelected = 1)
        {
            view.SetSelectedLine(preSelected);
            menu.SetSelectedLayer(view);
            menu.Draw();

            int selectedItem = -1;
            while (selectedItem == -1)
            {
                ConsoleKey pressedKey = Console.ReadKey().Key;
                if (pressedKey == ConsoleKey.DownArrow)
                {
                    int newSelectedIndex = mainSection.GetSelectedLine() + 1;

                    if (mainSection.IsSelectableBuffer.Length - 1 >= newSelectedIndex && mainSection.IsSelectableBuffer[newSelectedIndex] == true)
                        mainSection.SetSelectedLine(mainSection.GetSelectedLine() + 1);

                }
                else if (pressedKey == ConsoleKey.UpArrow)
                {
                    int newSelectedIndex = mainSection.GetSelectedLine() - 1;

                    if (mainSection.IsSelectableBuffer.Length - 1 >= newSelectedIndex && mainSection.IsSelectableBuffer[newSelectedIndex] == true)
                        mainSection.SetSelectedLine(mainSection.GetSelectedLine() - 1);

                }
                else if (pressedKey == ConsoleKey.Enter)
                {
                    selectedItem = mainSection.GetSelectedLine();
                }

                menu.Draw();
            }

            return selectedItem;
        }

        static string ReadFromInput(string prompt)
        {
            // Setup input field
            Size inputSize = new Size(2, 35);
            Position inputPos = new Position((Console.WindowWidth - inputSize.Width) / 2, (Console.WindowHeight - inputSize.Height) / 2);

            inputField = new Block(inputPos, inputSize, ConsoleColor.Black, ConsoleColor.Gray);
            inputField.WriteTextAt(new Position(0,0), "$<Gray, Black>" + prompt + new String(' ', inputSize.Width - prompt.Length) + "$</>");
            
            menu.AddLayer(inputField);
            menu.SetSelectedLayer(inputField);
            menu.Draw();

            Console.SetCursorPosition(inputPos.X, inputPos.Y + 1);
            Console.CursorVisible = true;

            ConsoleColor oldBackcolor = Console.BackgroundColor;
            ConsoleColor oldForegorundColor = Console.ForegroundColor;

            Console.ForegroundColor = inputField.BlockForegorundColor;
            Console.BackgroundColor = inputField.BlockBackgroundColor;

            string input = Console.ReadLine();

            Console.BackgroundColor = oldBackcolor;
            Console.ForegroundColor = oldForegorundColor;

            Console.CursorVisible = false;

            inputField.IsVisible = false;
            
            menu.Draw();
            menu.RemoveLayer(inputField);
            menu.Draw();

            return input;
        }

        static string GetTitle()
        {
            return string.Format("{0} ({1})", applicationName, applicationVersionName);
        }

        static void SetUpViews()
        {
            // Setup titlebar
            titleBar = new Block(new Position(0, 0), new Size(1, Console.WindowWidth), ConsoleColor.Black, ConsoleColor.Gray);
            titleBar.WriteTextAt(new Position(1, 0), GetTitle());

            // Setup main menu
            Size menuSize = new Size(8, 35);
            Position menuPos = new Position((Console.WindowWidth - menuSize.Width) / 2, (Console.WindowHeight - menuSize.Height) / 2);

            mainSection = new Block(menuPos, menuSize);

            mainSection.WriteTextAt(new Position(0, 0), "What would you like to do?");
            mainSection.WriteTextAt(new Position(0, 1), "$<Black,Gray,Gray,Black>Download a video");
            mainSection.WriteTextAt(new Position(0, 2), "$<Black,Gray,Gray,Black>Download audio of a video");
            mainSection.WriteTextAt(new Position(0, 3), "$<Black,Gray,Gray,Black>Stream audio of video");
            mainSection.WriteTextAt(new Position(0, 4), "$<Black,Gray,Gray,Black>Download a playlist");
            mainSection.WriteTextAt(new Position(0, 5), "$<Black,Gray,Gray,Black>Download audio of a video");
            mainSection.WriteTextAt(new Position(0, 6), "$<Black,Gray,DarkGray,Black>Stream audio of a playlist");
            mainSection.WriteTextAt(new Position(0, 7), "$<Black,Gray,DarkGray,Black>I don't know, but I have a URL!");

            mainSection.IsSelectableBuffer[1] = true;
            mainSection.IsSelectableBuffer[2] = true;
            mainSection.IsSelectableBuffer[3] = true;
            mainSection.IsSelectableBuffer[4] = false;
            mainSection.IsSelectableBuffer[5] = false;
            mainSection.IsSelectableBuffer[6] = true;
            mainSection.IsSelectableBuffer[7] = false;
        
            mainSection.SetSelectedLine(1);
        }

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

        #region WORK METHODS
        static void DownloadVideo(string url)
        {
            SYMM_Backend.SYMMSettings settings = new SYMM_Backend.SYMMSettings(url, Properties.Settings.Default.SavePath, Properties.Settings.Default.DefaultVideoResolution, Properties.Settings.Default.DuplicateChecking);
            Execute(settings);
        }

        public static void DownloadAudio(string url)
        {
            SYMM_Backend.SYMMSettings settings = new SYMM_Backend.SYMMSettings(url, Properties.Settings.Default.SavePath, SYMMSettings.Actions.ExtractAudio, Properties.Settings.Default.DuplicateChecking, (SYMMSettings.AudioFormats)Enum.Parse(typeof(SYMMSettings.AudioFormats), Properties.Settings.Default.DefaultAudioFormat, true), Properties.Settings.Default.DefaultAudioBitrate);
            Execute(settings);
        }

        public static void StreamAudio(string url)
        {
            SYMM_Backend.SYMMSettings settings = new SYMM_Backend.SYMMSettings(url, Properties.Settings.Default.SavePath, SYMMSettings.Actions.Stream, Properties.Settings.Default.DuplicateChecking, (SYMMSettings.AudioFormats)Enum.Parse(typeof(SYMMSettings.AudioFormats), Properties.Settings.Default.DefaultAudioFormat, true), Properties.Settings.Default.DefaultAudioBitrate);
            Execute(settings);
        }

        public static string DownloadPlaylist(string url)
        {
            throw new NotImplementedException();
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
                Console.WriteLine(String.Format("\nVideo finsihed: \"{0}\"", deventargs.Video.VideoTitle));
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
                Console.WriteLine(String.Format("\nVideo finsihed: \"{0}\"", deventargs.Video.VideoTitle));
            };

            // Register when a video failed to download
            symmBackend.OnVideoDownloadFailed += (dsender, deventargs) =>
            {
                Console.WriteLine(String.Format("Video failed to download: \"{0}\"", deventargs.Video.VideoTitle));
            };

            Console.WriteLine("Loading data..");
            symmBackend.LoadVideosFromURL(settings.DownloadURL);
            Console.WriteLine("Loaded. Starting work!");

            // We want to download every video in this list
            foreach (YouTubeVideo video in rawVideoList)
            {
                Console.WriteLine(String.Format("\"{0}\"", video.VideoTitle));

                if (settings.Action != SYMMSettings.Actions.Stream)
                {
                    // Prepare backend
                    settings.PathSafefileName = symmBackend.BuildPathSafeName(video.VideoTitle);

                    if (settings.CheckDuplicate && Directory.GetFiles(settings.SavePath, settings.PathSafefileName + ".*").Length > 0)
                    {
                        // Looks like we downloaded that already. Skip.
                        Console.WriteLine(String.Format("Looks like we already downloaded \"{0}\".\nSet 'settings.duplicatecheck' to false to ignore this.", video.VideoTitle));

                        // Skip the rest
                        continue;
                    }
                }

                // Tell backend to download the video spceifed to destination spceifed in the variable
                symmBackend.Execute(video, settings);
            }

            symmBackend = null;
        }
        #endregion
    }
}
