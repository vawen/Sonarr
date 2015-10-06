
using System;
using System.Collections.Generic;
using System.Linq;
using NLog;
using NzbDrone.Core.Tv;

namespace NzbDrone.Core.Parser
{
    public enum InfoCategories
    {
        Title = 1,
        Source
    }

    public class ParsedItem
    {
        public string Value { get; set; }
        public int Position { get; set; }
        public int Length { get; set; }
        public int GlobalLength { get; set; }
        public int End
        {
            get
            {
                return Position + Length - 1;
            }
        }

        public override string ToString()
        {
            return String.Format("{0} [{1} - {2}]", Value, Position, Length);
        }

        public bool Contains(ParsedItem other)
        {

            return (Position <= other.Position && End >= other.End);
        }

        public void Trim()
        {
            var valArray = Value.ToArray();
            var newPos = Position;
            foreach (var c in valArray)
            {
                if (Char.IsWhiteSpace(c))
                    newPos++;
                else
                    break;
            }

            Position = newPos;
            Value = Value.Trim();
            Length = Value.Length;
        }

        public ParsedItem[] Split(ParsedItem item)
        {
            if (!Contains(item))
                return null;

            var ret = new List<ParsedItem>();

            if (Position == item.Position)
            {
                var newPosition = Position + item.Length;
                var newLength = Length - item.Length;
                var newValue = Value.Substring(item.Length);
                var newGlobalLength = GlobalLength;
                ret.Add(new ParsedItem
                {
                    Position = newPosition,
                    Length = newLength,
                    Value = newValue,
                    GlobalLength = newGlobalLength
                });
            }
            else
            {
                var newPosition = Position;
                var newLength = item.Position - Position;
                var newValue = Value.Substring(0, item.Position - Position);
                var newGlobalLength = GlobalLength;

                ret.Add(new ParsedItem
                {
                    Position = newPosition,
                    Length = newLength,
                    Value = newValue,
                    GlobalLength = newGlobalLength
                });

                newPosition = item.Position + item.Length;
                newValue = Value.Substring(newPosition - Position);
                newLength = newValue.Length;
                newGlobalLength = GlobalLength;

                ret.Add(new ParsedItem
                {
                    Position = newPosition,
                    Length = newLength,
                    Value = newValue,
                    GlobalLength = newGlobalLength
                });
            }
            return ret.ToArray();
        }

        public override int GetHashCode()
        {
            int result = 0;
            if (Value != null)
            {
                result ^= Value.GetHashCode();
            }

            result ^= Position.GetHashCode();
            result ^= Length.GetHashCode();
            result ^= GlobalLength.GetHashCode();
            return result;
        }

        public bool Equals(ParsedItem other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;

            return other.Position.Equals(Position) && other.Length.Equals(Length);
        }

        public override bool Equals(object other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(other as ParsedItem);
        }

        public bool Intersect(ParsedItem other)
        {
            var mine = Enumerable.Range(Position, End - Position).ToList();
            var theOther = Enumerable.Range(other.Position, other.End - other.Position).ToList();

            return mine.Any(p => theOther.Contains(p));
        }

    }

    public class ParsedInfo
    {
        public Series Series { get; set; }
        public string SeriesTitle { get; set; }
        public List<ParsedItem> Title { get; set; }
        public List<ParsedItem> Source { get; set; }
        public List<ParsedItem> Resolution { get; set; }
        public List<ParsedItem> Audio { get; set; }
        public List<ParsedItem> Season { get; set; }
        public List<ParsedItem> Codec { get; set; }
        public List<ParsedItem> Hash { get; set; }
        public List<ParsedItem> Language { get; set; }
        public List<ParsedItem> ReleaseGroup { get; set; }
        public List<ParsedItem> Daily { get; set; }
        public List<ParsedItem> Special { get; set; }
        public List<ParsedItem> Year { get; set; }
        public List<ParsedItem> AbsoluteEpisodeNumber { get; set; }
        public List<ParsedItem> FileExtension { get; set; }
        public List<ParsedItem> Proper { get; set; }
        public List<ParsedItem> RawHD { get; set; }
        public List<ParsedItem> Real { get; set; }
        public List<ParsedItem> UnknownInfo { get; set; }
        public bool AbsoluteNumering { get; set; }

        public void PrintMap(Logger log)
        {

        }

        public bool IsEmpty()
        {
            if (Series != null) return false;
            if (Title.Count > 0) return false;
            if (Source.Count > 0) return false;
            if (Resolution.Count > 0) return false;
            if (Audio.Count > 0) return false;
            if (Season.Count > 0) return false;
            if (AbsoluteEpisodeNumber.Count > 0) return false;
            if (Language.Count > 0) return false;
            if (Daily.Count > 0) return false;
            if (Special.Count > 0) return false;
            if (Year.Count > 0) return false;
            return true;
        }

        public static bool AddItem(ParsedItem newItem, List<ParsedItem> Container)
        {
            if (Container.Contains(newItem))
                return false;

            var itemsToRemove = new List<ParsedItem>();

            //If item inside already contains the item to add, don't do it           
            /*if (Container.Any(p => p.Contains(newItem)))
                return false;*/
            foreach (var item in Container.Where(p => p.Intersect(newItem)))
            {
                string res = item.Value.Replace(newItem.Value, String.Empty);
                if (res.Length == 0 || !res.Any(char.IsLetterOrDigit))
                {
                    //New item is better
                    Container.Remove(item);
                    break;
                }
                res = newItem.Value.Replace(item.Value, String.Empty);
                if (res.Length == 0 || !res.Any(char.IsLetterOrDigit))
                {
                    return false;
                }

            }

            Container.Add(newItem);
            return true;
        }

        public ParsedInfo()
        {
            Title = new List<ParsedItem>();
            Source = new List<ParsedItem>();
            Resolution = new List<ParsedItem>();
            Audio = new List<ParsedItem>();
            Season = new List<ParsedItem>();
            Codec = new List<ParsedItem>();
            Hash = new List<ParsedItem>();
            Language = new List<ParsedItem>();
            ReleaseGroup = new List<ParsedItem>();
            Daily = new List<ParsedItem>();
            Special = new List<ParsedItem>();
            Year = new List<ParsedItem>();
            AbsoluteEpisodeNumber = new List<ParsedItem>();
            FileExtension = new List<ParsedItem>();
            Proper = new List<ParsedItem>();
            RawHD = new List<ParsedItem>();
            Real = new List<ParsedItem>();
            UnknownInfo = new List<ParsedItem>();
        }

        public void RemoveFromAll(ParsedItem item)
        {
            Title.Remove(item);
            Source.Remove(item);
            Resolution.Remove(item);
            Audio.Remove(item);
            Season.Remove(item);
            Codec.Remove(item);
            Hash.Remove(item);
            Language.Remove(item);
            ReleaseGroup.Remove(item);
            Daily.Remove(item);
            Special.Remove(item);
            Year.Remove(item);
            AbsoluteEpisodeNumber.Remove(item);
            FileExtension.Remove(item);
            Proper.Remove(item);
            RawHD.Remove(item);
            Real.Remove(item);
        }

        public void RemoveFromAllThatContains(ParsedItem item)
        {
            Title.RemoveAll(p => p.Contains(item));
            Source.RemoveAll(p => p.Contains(item));
            Resolution.RemoveAll(p => p.Contains(item));
            Audio.RemoveAll(p => p.Contains(item));
            Season.RemoveAll(p => p.Contains(item));
            Codec.RemoveAll(p => p.Contains(item));
            Hash.RemoveAll(p => p.Contains(item));
            Language.RemoveAll(p => p.Contains(item));
            ReleaseGroup.RemoveAll(p => p.Contains(item));
            Daily.RemoveAll(p => p.Contains(item));
            Special.RemoveAll(p => p.Contains(item));
            Year.RemoveAll(p => p.Contains(item));
            AbsoluteEpisodeNumber.RemoveAll(p => p.Contains(item));
            FileExtension.RemoveAll(p => p.Contains(item));
            Proper.RemoveAll(p => p.Contains(item));
            RawHD.RemoveAll(p => p.Contains(item));
            Real.RemoveAll(p => p.Contains(item));
        }

        public bool AnyContains(ParsedItem item, params List<ParsedItem>[] containers)
        {
            if (!containers.Contains(Title) && Title.Contains(item))
                return true;
            if (!containers.Contains(Source) && Source.Contains(item))
                return true;
            if (!containers.Contains(Resolution) && Resolution.Contains(item))
                return true;
            if (!containers.Contains(Audio) && Audio.Contains(item))
                return true;
            if (!containers.Contains(Season) && Season.Contains(item))
                return true;
            if (!containers.Contains(Codec) && Codec.Contains(item))
                return true;
            if (!containers.Contains(Hash) && Hash.Contains(item))
                return true;
            if (!containers.Contains(Language) && Language.Contains(item))
                return true;
            if (!containers.Contains(ReleaseGroup) && ReleaseGroup.Contains(item))
                return true;
            if (!containers.Contains(Daily) && Daily.Contains(item))
                return true;
            if (!containers.Contains(Special) && Special.Contains(item))
                return true;
            if (!containers.Contains(Year) && Year.Contains(item))
                return true;
            if (!containers.Contains(AbsoluteEpisodeNumber) && AbsoluteEpisodeNumber.Contains(item))
                return true;
            if (!containers.Contains(FileExtension) && FileExtension.Contains(item))
                return true;
            if (!containers.Contains(FileExtension) && Proper.Contains(item))
                return true;
            if (!containers.Contains(FileExtension) && RawHD.Contains(item))
                return true;
            if (!containers.Contains(FileExtension) && Real.Contains(item))
                return true;
            return false;
        }
    }
}
