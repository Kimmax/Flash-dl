using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.ServiceModel.Syndication;

namespace Flash_dl
{
    public class VideoList
    {
        /// <summary>
        /// The base url to use to get an RSS feed of videos for a given
        /// playlist
        /// </summary>
        private static string baseFeedUrl = "http://gdata.youtube.com/feeds/api/playlists/{0}?max-results=50";

        /// <summary>
        /// Given a YouTube playlist url, returns a 
        /// list of videos in the playlist
        /// </summary>
        /// <param name="youTubePlaylistUrl"></param>
        /// <returns></returns>
        public static List<VideoInfo> FromYouTubePlaylistUrl(string youTubePlaylistUrl)
        {
            List<VideoInfo> retval = new List<VideoInfo>();

            return retval;
        }

        /// <summary>
        /// Given a YouTube playlist identifier, returns
        /// a list of videos in the playlist
        /// </summary>
        /// <param name="youTubePlaylist"></param>
        /// <param name="videoResolution"></param>
        /// <returns></returns>
        public static List<VideoInfo> FromYouTubePlaylist(string youTubePlaylist, int videoResolution)
        {
            List<VideoInfo> retval = new List<VideoInfo>();

            //  Form our feed url:
            string url = string.Format(baseFeedUrl, youTubePlaylist);
            Console.WriteLine("Composing feed url of: '{0}'", url);

            //  Read the feed:
            Console.WriteLine("Attempting to open feed url...");
            XmlReader reader = XmlReader.Create(url);
            SyndicationFeed feed = SyndicationFeed.Load(reader);
            reader.Close();

            Console.WriteLine("Feed opened.  Got {0} items in feed.  Processing ... ", feed.Items.Count());

            //  Rip through each feed item
            foreach (SyndicationItem item in feed.Items)
            {
                //  Determine the correct video download information based on the passed resolution
                List<VideoInfo> videoInfos = DownloadUrlResolver.GetDownloadUrls(item.Links[0].Uri.AbsoluteUri).ToList();
                try
                {
                    VideoInfo video = videoInfos.First(info => info.VideoType == VideoType.Mp4 && info.Resolution == videoResolution);

                    //  Add the video
                    retval.Add(video);
                    Console.WriteLine("Adding video '{0}' (resolution: {1}) to the queue...", video.Title, video.Resolution);
                }
                catch (Exception)
                {
                    try
                    {
                        //  Fallback: Try any resolution, as long as it's an MP4
                        VideoInfo video = videoInfos.OrderByDescending(v => v.Resolution).First(info => info.VideoType == VideoType.Mp4);

                        //  Add the video
                        retval.Add(video);
                        Console.WriteLine("Fallback: adding video '{0}' (resolution: {1}) to the queue...", video.Title, video.Resolution);
                    }
                    catch (Exception) { Console.WriteLine("Couldn't find an MP4 for the video: {0}", item.Title); }
                }
            }

            return retval;
        }
    }
}
