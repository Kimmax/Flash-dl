using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SYMM_Backend;

namespace Flash_dl.Commands
{
    public static class Settings
    {
        public static string APIKey(string apikey)
        {
            Properties.Settings.Default.youtubeApiKey = apikey;
            Properties.Settings.Default.Save();
            return String.Format("Set api key to \"{0}\".", apikey);
        }

        public static string SavePath(string path = "default")
        {
            if(path == "default")
            {
                Properties.Settings.Default.SavePath = Path.Combine(GetDownloadFolderPath(), "Flash-dl");
                Properties.Settings.Default.Save();
                return String.Format("Set api key to \"{0}\".", Properties.Settings.Default.SavePath);
            }
            else
            {
                try
                {
                    Path.GetFullPath(path);
                    Properties.Settings.Default.SavePath = path;
                    Properties.Settings.Default.Save();
                    return String.Format("Set api key to \"{0}\".", Properties.Settings.Default.SavePath);
                }catch
                {
                    return "This is not a valid folder path.";
                }
            }
        }

        public static string DuplicateChecking(bool value)
        {
            Properties.Settings.Default.DuplicateChecking = value;
            Properties.Settings.Default.Save();
            return String.Format("Duplicate checking is now {0}", value ? "enabled" : "disabled");
        }

        public static string DefaultAudioFormat(string format)
        {
            if(Enum.GetNames(typeof(SYMMFileFormats.AudioFormats)).Contains(format))
            {
                Properties.Settings.Default.DefaultAudioFormat = format;
                Properties.Settings.Default.Save();
                return String.Format("Default audio format is set to \"{0}\"", format);
            }
            else
            {
                string output = "This is not a allowed audio format.\nAllowed formarts are:\n";
                foreach (string allowedFormat in Enum.GetNames(typeof(SYMMFileFormats.AudioFormats)))
                    output += allowedFormat + "\n";
                return output;
            }
        }

        public static string DefaultVideoResolution(short res)
        {
            Properties.Settings.Default.DefaultVideoResolution = res;
            Properties.Settings.Default.Save();
            return String.Format("Set default video resolution to {0}p", res);
        }

        public static string DefaultAudioBitrate(short bitrate)
        {
            Properties.Settings.Default.DefaultAudioBitrate = bitrate;
            Properties.Settings.Default.Save();
            return String.Format("Set default audio bitrate to {0}kbit/s", bitrate);
        }

        public static string Reset(bool skipQuestion = false)
        {
            if(!skipQuestion)
            {
                Console.WriteLine("Are you sure you want to reset the settings?");
                Console.Write("Reset: [Y]es or [N]o? ");
                if (Console.ReadKey().Key != ConsoleKey.Y)
                    return "Settings were NOT resettet.";
            }

            Console.WriteLine(APIKey("NONE"));
            Console.WriteLine(SavePath());
            Console.WriteLine(DuplicateChecking(true));
            Console.WriteLine(DefaultAudioFormat("mp3"));
            Console.WriteLine(DefaultVideoResolution(1080));
            Console.WriteLine(DefaultAudioBitrate(192));
            Properties.Settings.Default.SettingsNeedReset = false;
            Properties.Settings.Default.Save();
            return "Reset done.";
        }

        private static string GetDownloadFolderPath()
        {
            return Registry.GetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\Shell Folders", "{374DE290-123F-4565-9164-39C4925E467B}", String.Empty).ToString();
        }
    }
}
