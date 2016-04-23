using ResolveList;
using SYMM.Interfaces;
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
            ISYMMSettings settings = new SYMMSettings(url, Properties.Settings.Default.SavePath, Mode.Single, Properties.Settings.Default.DefaultVideoResolution, Properties.Settings.Default.DuplicateChecking);
            Backend.Execute(settings);
            
            // No direct output. Command feedback comes from backend.
            return "";
        }

        public static string DownloadAudio(string url)
        {
            ISYMMSettings settings = new SYMMSettings(url, Properties.Settings.Default.SavePath, Actions.ExtractAudio, Mode.Single, Properties.Settings.Default.DuplicateChecking, (AudioFormats)Enum.Parse(typeof(AudioFormats), Properties.Settings.Default.DefaultAudioFormat, true), Properties.Settings.Default.DefaultAudioBitrate);
            Backend.Execute(settings);
            
            // No direct output. Command feedback comes from backend.
            return "";
        }

        public static string StreamAudio(string url)
        {
            ISYMMSettings settings = new SYMMSettings(url, Actions.Stream, Mode.Single, Properties.Settings.Default.DefaultAudioBitrate);
            Backend.Execute(settings);

            // No direct output. Command feedback comes from backend.
            return "";
        }

        public static string DownloadPlaylist(string url)
        {
            return "Not implementented";
        }
    }
}
