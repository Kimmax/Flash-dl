using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flash_dl.Commands
{
    public static class Settings
    {
        public static string APIKey(string apikey)
        {
            Properties.Settings.Default.youtubeApiKey = apikey;
            return String.Format("Set api key to \"{0}\".", apikey);
        }
    }
}
