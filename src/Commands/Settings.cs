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
        public static string APIKey(string apikey = null)
        {
            if(apikey == null)
            {
                Console.WriteLine(Backend.GetHeader());
                Console.Write("\nPlease enter your api key and press enter: ");
                apikey = Console.ReadLine();
                if (String.IsNullOrEmpty(apikey))
                {
                    Console.WriteLine("Entered key is not valid. Leave? [Y]es [N]o: ");
                    if (Console.ReadKey().Key == ConsoleKey.Y)
                        DefaultCommands.Exit();
                    else
                    {
                        Console.Clear();
                        APIKey();
                        return null;
                    }
                }
            }

            Properties.Settings.Default.youtubeApiKey = apikey;
            Properties.Settings.Default.Save();
            return String.Format("Set api key to \"{0}\".", Properties.Settings.Default.youtubeApiKey);
        }

        public static string SavePath(string path = "default")
        {
            if(path == "default")
            {
                Properties.Settings.Default.SavePath = Path.Combine(GetDownloadFolderPath(), "Flash-dl");
                Properties.Settings.Default.Save();
                return String.Format("Set default download path to \"{0}\".", Properties.Settings.Default.SavePath);
            }
            else
            {
                try
                {
                    Path.GetFullPath(path);
                    Properties.Settings.Default.SavePath = path;
                    Properties.Settings.Default.Save();
                    return String.Format("Set default download path to \"{0}\".", Properties.Settings.Default.SavePath);
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

            Console.WriteLine(APIKey());
            Console.WriteLine(SavePath());
            Console.WriteLine(DuplicateChecking(true));
            Console.WriteLine(DefaultAudioFormat("mp3"));
            Console.WriteLine(DefaultVideoResolution(1080));
            Console.WriteLine(DefaultAudioBitrate(192));
            Properties.Settings.Default.SettingsNeedReset = false;
            Properties.Settings.Default.Save();
            return "Reset done.";
        }

        public static string Show()
        {
            string output = "Current Settings:\n--------------------\n";
            output += String.Format("ApiKey: {0}\n", Properties.Settings.Default.youtubeApiKey);
            output += String.Format("SavePath: {0}\n", Properties.Settings.Default.SavePath);
            output += String.Format("Duplicate checking: {0}\n", Properties.Settings.Default.DuplicateChecking ? "enabled" : "disabled");
            output += String.Format("Default aduio format: {0}\n", Properties.Settings.Default.DefaultAudioFormat);
            output += String.Format("Default audio bitrate: {0}kbit/s\n", Properties.Settings.Default.DefaultAudioBitrate);
            output += String.Format("Default video resolution: {0}p", Properties.Settings.Default.DefaultVideoResolution);
            return output;
        }

        public static string Help()
        {
            string output = "To set settings you have to enter 'settings.settingNameHere' and pass the values you wan't to set:\n";
            output += "[parameter] = optional, foo|bar = choose one, foobar = required\n";
            output += String.Format("Settings.APIKey [enter apikey here] - Sets the apikey to the value specefeid or deletes it when not passed.\n");
            output += String.Format("Settings.SavePath [savepath] - Sets the savepath to the value specefeid or to the default value if none is passed.\n");
            output += String.Format("Settings.DuplicateChecking true|false - Enables or disabled duplicate checking.\n");
            
            string formatList = "";
            foreach (string allowedFormat in Enum.GetNames(typeof(SYMMFileFormats.AudioFormats)))
                formatList += allowedFormat + "|";
            formatList.Remove(formatList.Length - 1, 1);

            output += String.Format("Settings.DefaultAudioFormat " + formatList + " - Sets the default audio format\n");
            output += String.Format("Settings.DefaultVideoResolution 144|240|360|480|720|1080|1440|2160 - Sets the default video bitrate\n");
            output += String.Format("Settings.DefaultAudioBitrate 64|128|192|256|320 - Sets the default audio bitrate");
            return output;
        }

        private static string GetDownloadFolderPath()
        {
            return Registry.GetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\Shell Folders", "{374DE290-123F-4565-9164-39C4925E467B}", String.Empty).ToString();
        }
    }
}
