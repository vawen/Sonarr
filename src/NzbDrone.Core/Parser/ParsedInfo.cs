
using System;
using System.Collections.Generic;
using System.Linq;
using NzbDrone.Core.Tv;

namespace NzbDrone.Core.Parser
{
    public class ParsedInfo
    {
        public Series Series { get; set; }
        public List<string> Title { get; set; }
        public List<string> Source { get; set; }
        public List<string> Resolution { get; set; }
        public List<string> Audio { get; set; }
        public List<string> Season { get; set; }
        public List<string> Codec { get; set; }
        public List<string> Hash { get; set; }
        public List<string> Language { get; set; }
        public List<string> ReleaseGroup { get; set; }
        public List<string> Daily { get; set; }
        public List<string> Special { get; set; }
        public List<string> Year { get; set; }
        public List<string> AbsoluteEpisodeNumber { get; set; }
        public bool AbsoluteNumering { get; set; }


        public static bool AddItem(string newItem, List<string> Container)
        {
            if (Container.Contains(newItem))
                return false;

            List<string> itemsToRemove = new List<string>();

            foreach (var item in Container)
            {
                string res = item.Replace(newItem, String.Empty);
                if (res.Length == 0 || !res.Any(char.IsLetterOrDigit))
                {
                    //New item is better
                    itemsToRemove.Add(item);
                    break;
                }
                res = newItem.Replace(item, String.Empty);
                if (res.Length == 0 || !res.Any(char.IsLetterOrDigit))
                {
                    return false;
                }

            }

            itemsToRemove.ForEach(s => Container.Remove(s));
            Container.Add(newItem);
            return true;
        }

        public ParsedInfo()
        {
            Title = new List<string>();
            Source = new List<string>();
            Resolution = new List<string>();
            Audio = new List<string>();
            Season = new List<string>();
            Codec = new List<string>();
            Hash = new List<string>();
            Language = new List<string>();
            ReleaseGroup = new List<string>();
            Daily = new List<string>();
            Special = new List<string>();
            Year = new List<string>();
            AbsoluteEpisodeNumber = new List<string>();
        }

        public void ShowInfo()
        {
            foreach (var item in Title)
            {
                Console.Out.WriteLine("Title: " + item);
            }
            foreach (var item in Source)
            {
                Console.Out.WriteLine("Source: " + item);
            }
            foreach (var item in Resolution)
            {
                Console.Out.WriteLine("Resolution: " + item);
            }
            foreach (var item in Audio)
            {
                Console.Out.WriteLine("Audio: " + item);
            }
            foreach (var item in Season)
            {
                Console.Out.WriteLine("Season: " + item);
            }
            foreach (var item in Codec)
            {
                Console.Out.WriteLine("Codec: " + item);
            }
            foreach (var item in Hash)
            {
                Console.Out.WriteLine("Hash: " + item);
            }
        }
    }
}
