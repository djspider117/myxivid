using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MyxiVid
{
    public class TrackInfo
    {
        public TimeSpan Timestamp { get; set; }
        public string Artist { get; set; }
        public string TrackName { get; set; }
        public string Remix { get; set; }
    }

    public class TrackInfoCollection : List<TrackInfo> { }

    public class Parser
    {
        public TrackInfoCollection Parse(string[] lines)
        {
            var rv = new TrackInfoCollection();

            //Parallel.ForEach(lines, line =>
            //{
            foreach (var line in lines)
            {
                try
                {

                    var trackInfo = new TrackInfo();
                    var tsEnding = line.IndexOf(']');
                    var timestamp = line.Substring(1, tsEnding - 1);
                    var remaining = line.Remove(0, tsEnding + 1);
                    var split = remaining.Split('-');

                    if (split[1].Contains('('))
                    {
                        var q = split[1].IndexOf('(') + 1;
                        int q2 = split[1].IndexOf(')');
                        var remix = split[1].Substring(q, q2 - q);
                        trackInfo.Remix = remix.Trim();

                        remaining = split[1].Substring(0, q - 1).Trim();
                    }
                    else
                    {
                        remaining = split[1].Trim();
                    }

                    trackInfo.TrackName = remaining;
                    trackInfo.Artist = split[0].Trim();
                    try
                    {
                        trackInfo.Timestamp = TimeSpan.Parse(timestamp);
                        if (trackInfo.Timestamp.Seconds == 0 && timestamp.Count(x => x == ':') == 1)
                            trackInfo.Timestamp = new TimeSpan(0, trackInfo.Timestamp.Hours, trackInfo.Timestamp.Minutes);
                    }
                    catch (OverflowException)
                    {
                        var tss = timestamp.Split(':');
                        var hours = 0;
                        var minutes = int.Parse(tss[0]);
                        var sec = int.Parse(tss[1]);
                        if (minutes > 60)
                        {
                            hours = minutes / 60;
                            minutes %= hours;
                        }

                        trackInfo.Timestamp = new TimeSpan(hours, minutes, sec);
                    }

                    rv.Add(trackInfo);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex);
                }
                //});
            }


            return rv;
        }

        public TrackInfoCollection Parse(Uri filePath)
        {
            var lines = File.ReadAllLines(filePath.AbsoluteUri);
            return Parse(lines);
        }
    }

    //public class Program
    //{
    //    static void Main(string[] args)
    //    {
    //        var parser = new Parser();
    //        var trackInfos = parser.Parse("playlist.txt").OrderBy(x => x.Timestamp);
    //        int i = 1;
    //        foreach (var x in trackInfos)
    //        {
    //            Console.WriteLine($"------ Track {i++} ------");
    //            Console.WriteLine($"Timestamp: {x.Timestamp}");
    //            Console.WriteLine($"Artist: {x.Artist}");
    //            Console.WriteLine($"Track Name: {x.TrackName}");
    //            if (x.Remix != null)
    //                Console.WriteLine($"Remix: {x.Remix}");
    //        }
    //        Console.Read();
    //    }
    //}
}