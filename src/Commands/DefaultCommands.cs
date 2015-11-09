using SYMM_Backend;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace Flash_dl.Commands
{
    public static class DefaultCommands
    {
        // Method names are a mess currently, need to fix that on the command caller

        public static string Help()
        {
            return Backend.Help();
        }

        public static string Clear()
        {
            Console.Clear();
            return "";
        }

        public static string Exit()
        {
            Console.WriteLine("Goodbye!");
            Thread.Sleep(1000);
            Environment.Exit(0);
            return "";
        }

        public static string DownloadVideo(string url)
        {
            SYMM_Backend.SYMMSettings settings = new SYMM_Backend.SYMMSettings(url, Properties.Settings.Default.SavePath, Properties.Settings.Default.DefaultVideoResolution, Properties.Settings.Default.DuplicateChecking);
            Backend.LoadByURL(settings);

            // No direct output. Command feedback comes from backend.
            return "";
        }

        public static string DownloadAudio(string url)
        {
            SYMM_Backend.SYMMSettings settings = new SYMM_Backend.SYMMSettings(url, Properties.Settings.Default.SavePath, SYMMSettings.Actions.ExtractAudio, Properties.Settings.Default.DuplicateChecking, (SYMMSettings.AudioFormats)Enum.Parse(typeof(SYMMSettings.AudioFormats), Properties.Settings.Default.DefaultAudioFormat, true), Properties.Settings.Default.DefaultAudioBitrate);
            Backend.LoadByURL(settings);
            
            // No direct output. Command feedback comes from backend.
            return "";
        }

        public static string StreamAudio(string url)
        {
            SYMM_Backend.SYMMSettings settings = new SYMM_Backend.SYMMSettings(url, Properties.Settings.Default.SavePath, SYMMSettings.Actions.Stream, Properties.Settings.Default.DuplicateChecking, (SYMMSettings.AudioFormats)Enum.Parse(typeof(SYMMSettings.AudioFormats), Properties.Settings.Default.DefaultAudioFormat, true), Properties.Settings.Default.DefaultAudioBitrate);
            Backend.StartStream(settings);

            // No direct output. Command feedback comes from backend.
            return "";
        }

        public static string DownloadPlaylist(string url)
        {
            return "Not implementented";
        }
    }
}
