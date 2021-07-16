using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;

namespace SSIPTV_Server
{
    class Program
    {
        private static string rootDirectory = Assembly.GetExecutingAssembly().Location
                .Replace(Path.GetFileName(Assembly.GetExecutingAssembly().Location), "");

        private static string playlistDirectory = rootDirectory + "Playlist\\";
        private static string myfile = playlistDirectory + "playlist.m3u";

        private static string myServer = "http://" + Dns.GetHostAddresses(Dns.GetHostName())
                    .Where(x => x.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork && x.ToString()
                    .Contains("192")).First().ToString();

        private static List<string> fileOnPath = new List<string>();

        static void Main(string[] args)
        {
            PrepareStart();

            List<string> directories = Directory.GetDirectories(rootDirectory).ToList()
                .Where(x => !x.Contains("Playlist")).ToList();
            if (directories.Count > 0) CreateM3uFileByDirectory(directories);

            CreateM3u(Directory.GetFiles(rootDirectory).ToList()
                .Where(x => ConfigurationManager.AppSettings[Enums.AppConfig_Ext.playlistExt.ToString()].ToString().Split(',').ToList()
                .Contains(Path.GetExtension(x))).ToList());
        }

        private static void PrepareStart()
        {
            if (!Directory.Exists(playlistDirectory)) Directory.CreateDirectory(playlistDirectory);

            foreach (string s in Directory.GetFiles(rootDirectory).Where(x => Path.GetExtension(x) == ".m3u")) File.Delete(s);
        }

        private static void CreateM3uFileByDirectory(List<string> directories)
        {
            foreach (string direc in directories)
            {
                List<Tuple<string, string>> urls = new List<Tuple<string, string>>();
                List<string> seasons = Directory.GetDirectories(direc).ToList();

                foreach (string sea in seasons)
                {
                    List<Tuple<string, string>> filesAndPath = Directory.GetFiles(sea)
                        .Where(x => ConfigurationManager.AppSettings[Enums.AppConfig_Ext.videoExt.ToString()].Split(',').ToList().Contains(Path.GetExtension(x)))
                        .Select(x => new Tuple<string, string>(sea.Split("\\".ToCharArray()).Last(), x)).ToList();

                    urls.AddRange(filesAndPath);
                }

                if (seasons.Count > 0 || Directory.GetFiles(direc)
                    .Where(x => ConfigurationManager.AppSettings[Enums.AppConfig_Ext.videoExt.ToString()].Split(',').ToList()
                    .Contains(Path.GetExtension(x))).Count() > 1)
                    urls.AddRange(Directory.GetFiles(direc).Select(x => new Tuple<string, string>("", x)).ToList());
                else
                {
                    fileOnPath.AddRange(Directory.GetFiles(direc)
                    .Where(x => ConfigurationManager.AppSettings[Enums.AppConfig_Ext.videoExt.ToString()].Split(',').ToList()
                    .Contains(Path.GetExtension(x))).ToList());

                    continue;
                }

                CreateM3u(urls, direc.Split("\\".ToCharArray()).Last());
            }
        }

        private static void CreateM3u(object urls, string name = "")
        {
            myfile = urls is List<Tuple<string, string>> ? rootDirectory + name + ".m3u" : playlistDirectory + "playlist.m3u";
            string m3u = "#EXTM3U";

            if (urls is List<string>)
            {
                (urls as List<string>).AddRange(fileOnPath);
                foreach (string s in urls as List<string>) m3u += M3u(s);
            }
            else foreach (Tuple<string, string> file in urls as List<Tuple<string, string>>) m3u += M3u(file.Item2, file.Item1);

            using (StreamWriter str = new StreamWriter(myfile, false)) str.Write(m3u.Replace("\\", "/"));
        }

        private static string M3u(string url, string group = "")
        {
            string extensao = (Path.GetExtension(url) == ".m3u") ? "playlist" : "video";

            string logo = Directory.GetFiles(rootDirectory).ToList()
                .Where(x => ConfigurationManager.AppSettings[Enums.AppConfig_Ext.logoExt.ToString()].Split(',').ToList().Contains(Path.GetExtension(x)) &&
                x.Contains(Path.GetFileNameWithoutExtension(url))).FirstOrDefault();
            string logoHttp = String.IsNullOrEmpty(logo) ? String.Empty : logo.Replace(rootDirectory, myServer + "/");

            string sub = Directory.GetFiles(url.Replace(Path.GetFileName(url), ""))
                .Where(x => x.Contains(Path.GetFileNameWithoutExtension(url)) &&
                ConfigurationManager.AppSettings[Enums.AppConfig_Ext.subExt.ToString()].Split(',').ToList().Contains(Path.GetExtension(x))).FirstOrDefault();
            string subHttp = String.IsNullOrEmpty(sub) ? String.Empty : sub.Replace(rootDirectory, myServer + "/");

            return "\r\n#EXTINF:0 tvg-logo='" + logoHttp + "' group-title='" + group + "' type='" + extensao + "' description='' subtitles='" + subHttp + "', " +
                    Path.GetFileNameWithoutExtension(url) + " " + url.Replace(rootDirectory, myServer + "/");
        }
    }

    public class Enums
    {
        public enum AppConfig_Ext
        {
            videoExt = 0,
            subExt = 1,
            logoExt = 2,
            playlistExt = 3,
            fileExtPermited = 4,
        }
    }
}
