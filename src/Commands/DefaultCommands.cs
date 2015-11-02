using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace Flash_dl.Commands
{
    public static class DefaultCommands
    {
        // Method names are a mess currently, need to fix that on the command caller
        public static string help() { return Help(); }
        public static string Help()
        {
            return Backend.Help();
        }

        public static string clear() { return Clear(); }
        public static string Clear()
        {
            Console.Clear();
            return "";
        }

        public static string exit() { return Exit(); }
        public static string Exit()
        {
            Console.WriteLine("Goodbye!");
            Thread.Sleep(1000);
            Environment.Exit(0);
            return "";
        }

        public static string Downloadvideo(string url) { return DownloadVideo(url); }
        public static string downloadvideo(string url) { return DownloadVideo(url); }
        public static string DownloadVideo(string url)
        {
            SYMM_Backend.SYMMSettings settings = new SYMM_Backend.SYMMSettings(url, Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "downloaded"));
            Backend.LoadByURL(settings);

            // No direct output. Command feedback comes from backend.
            return "";
        }

        public static string Downloadaudio(string url) { return DownloadAudio(url); }
        public static string downloadaudio(string url) { return DownloadAudio(url); }
        public static string DownloadAudio(string url)
        {
            SYMM_Backend.SYMMSettings settings = new SYMM_Backend.SYMMSettings(url, Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "downloaded"), true);
            Backend.LoadByURL(settings);
            
            // No direct output. Command feedback comes from backend.
            return "";
        }

        public static string Downloadlaylist(string url) { return DownloadPlaylist(url); }
        public static string downloadplaylist(string url) { return DownloadPlaylist(url); }
        public static string DownloadPlaylist(string url)
        {
            return "Not implementented";
        }
    }
}
