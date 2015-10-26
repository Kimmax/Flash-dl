using System;
using System.Collections.Generic;
using System.Threading;
using YoutubeExtractor;

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
            IEnumerable<VideoInfo> videoInfos = DownloadUrlResolver.GetDownloadUrls(url, false);
            if(Backend.DownloadVideo(videoInfos))
            {
                return "\nProcess finished\n";
            }
            else
            {
                return "\nThere are no videos to download :(\n";
            }
        }

        public static string Downloadaudio(string url) { return DownloadAudio(url); }
        public static string downloadaudio(string url) { return DownloadAudio(url); }
        public static string DownloadAudio(string url)
        {
            IEnumerable<VideoInfo> videoInfos = DownloadUrlResolver.GetDownloadUrls(url, false);
            if (Backend.DownloadAudio(videoInfos))
            {
                return "\nProcess finished\n";
            }
            else
            {
                return "\nThere are no videos to download :(\n";
            }
        }

        public static string Downloadlaylist(string url) { return DownloadPlaylist(url); }
        public static string downloadplaylist(string url) { return DownloadPlaylist(url); }
        public static string DownloadPlaylist(string url)
        {
            IEnumerable<VideoInfo> videoInfos = DownloadUrlResolver.GetDownloadUrls(url, false);
            if (Backend.DownloadPlaylist(videoInfos))
            {
                return "\nProcess finished\n";
            }
            else
            {
                return "\nThere are no videos to download :(\n";
            }
        }
    }
}
