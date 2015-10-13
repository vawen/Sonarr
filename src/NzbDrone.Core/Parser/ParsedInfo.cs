
using System;
using System.Collections.Generic;
using System.Linq;
using FluentValidation.Results;
using NLog;
using NzbDrone.Core.Tv;

namespace NzbDrone.Core.Parser
{
    [Flags]
    public enum InfoCategory
    {
        Unknown = 0,
        Title = 0x00001,
        Source = 0x00002,
        Resolution = 0x00004,
        Audio = 0x00008,
        Season = 0x00010,
        Codec = 0x00020,
        Hash = 0x00040,
        Language = 0x00080,
        ReleaseGroup = 0x00100,
        Daily = 0x00200,
        Special = 0x00400,
        Year = 0x00800,
        AbsoluteEpisodeNumber = 0x01000,
        FileExtension = 0x02000,
        Proper = 0x04000,
        RawHD = 0x08000,
        Real = 0x010000,
        All = 0xFFFFF
    }

    public class ParsedItem
    {
        public string Value { get; set; }
        public int Position { get; set; }
        public int Length { get; set; }
        public int GlobalLength { get; set; }
        public int End => Position + Length - 1;

        public InfoCategory Category { get; set; } 

        public override string ToString()
        {
            return String.Format("{0} [{1} - {2}]", Value, Position, Length);
        }

        public bool Contains(ParsedItem other)
        {

            return (Position <= other.Position && End >= other.End);
        }

        public ParsedItem Trim()
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
            var newValue = Value.Trim();
            var newCategory = Category;
            var newGlobalLength = GlobalLength;

            return new ParsedItem
            {
                Position = newPos,
                Value = newValue,
                Category = newCategory,
                Length = Value.Trim().Length,
                GlobalLength = newGlobalLength
            };
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
                var newCategory = item.Category;
                var newGlobalLength = GlobalLength;
                ret.Add(new ParsedItem
                {
                    Position = newPosition,
                    Length = newLength,
                    Value = newValue,
                    GlobalLength = newGlobalLength,
                    Category = newCategory
                });
            }
            else
            {
                var newPosition = Position;
                var newLength = item.Position - Position;
                var newValue = Value.Substring(0, item.Position - Position);
                var newGlobalLength = GlobalLength;
                var newCategory = item.Category;
                ret.Add(new ParsedItem
                {
                    Position = newPosition,
                    Length = newLength,
                    Value = newValue,
                    GlobalLength = newGlobalLength,
                    Category = newCategory
                });

                newPosition = item.Position + item.Length;
                newValue = Value.Substring(newPosition - Position);
                newLength = newValue.Length;
                newGlobalLength = GlobalLength;
                newCategory = item.Category;
                ret.Add(new ParsedItem
                {
                    Position = newPosition,
                    Length = newLength,
                    Value = newValue,
                    GlobalLength = newGlobalLength,
                    Category =  newCategory
                });
            }
            return ret.ToArray();
        }

        public override int GetHashCode()
        {
            var result = 0;
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
            return EqualsIgnoreCategory(other) && ((other.Category & Category) == other.Category);
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

        public bool EqualsIgnoreCategory(ParsedItem other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;

            return other.Position.Equals(Position) &&
                   other.Length.Equals(Length);
        }
    }

    public class ParsedInfo
    {
        public Series Series { get; set; }
        public string SeriesTitle { get; set; }
        public bool AbsoluteNumering { get; set; }
        private List<ParsedItem> ItemList { get; } 

        public List<ParsedItem> GetItemsInCategory(InfoCategory flags, Func<ParsedItem,bool> predicate = null)
        {
            IEnumerable<ParsedItem> ret;
            if (flags == InfoCategory.Unknown)
            {
                ret = ItemList.Where(p => p.Category == InfoCategory.Unknown);
            }
            else
            {
                ret = ItemList.Where(p => (p.Category & flags) > 0);
            }
            if (predicate == null)
            {
                return ret.ToList();
            }

            return ret.Where(predicate).ToList();
        }

        public bool IsEmpty()
        {
            if (Series != null) return false;

            if (ItemList.Any(i => (i.Category & InfoCategory.Title) > 0)) return false;
            if (ItemList.Any(i => (i.Category & InfoCategory.Source) > 0)) return false;
            if (ItemList.Any(i => (i.Category & InfoCategory.Resolution) > 0)) return false;
            if (ItemList.Any(i => (i.Category & InfoCategory.Audio) > 0)) return false;
            if (ItemList.Any(i => (i.Category & InfoCategory.Season) > 0)) return false;
            if (ItemList.Any(i => (i.Category & InfoCategory.AbsoluteEpisodeNumber) > 0)) return false;
            if (ItemList.Any(i => (i.Category & InfoCategory.Language) > 0)) return false;
            if (ItemList.Any(i => (i.Category & InfoCategory.Daily) > 0)) return false;
            if (ItemList.Any(i => (i.Category & InfoCategory.Special) > 0)) return false;
            if (ItemList.Any(i => (i.Category & InfoCategory.Year) > 0)) return false;
            return true;
        }

        public bool AddItem(ParsedItem newItem)
        {
            // Items are exacts, this includes category
            if (ItemList.Contains(newItem))
            {
                return false;
            }

            try
            {
                // Items are the same but parse for 2 categories (or unknown is in the list)
                var i = ItemList.Single(p => p.EqualsIgnoreCategory(newItem));
                if (i != null)
                {
                    ItemList.Remove(i);
                    i.Category = i.Category | newItem.Category;
                    ItemList.Add(i.Trim());
                    return true;
                }
            }
            catch (InvalidOperationException)
            {
            }

            // Intersection and same group of categories
            foreach (var item in ItemList.Where(p => p.Intersect(newItem) && p.Category  == newItem.Category))
            {
                var res = item.Value.Replace(newItem.Value, String.Empty);
                
                // The current item is the same as the new one, excluding trailing and separators
                if (res.Length == 0 || !res.Any(char.IsLetterOrDigit))
                {
                    // Keep the smaller one
                    if (item.Trim().Length <= newItem.Trim().Length)
                    {
                        return false;
                    }
                }
            }

            ItemList.Add(newItem.Trim());
            return true;
        }

        public ParsedInfo()
        {
            ItemList = new List<ParsedItem>();
        }

        public int RemoveAll(System.Predicate<ParsedItem> predicate)
        {
            return ItemList.RemoveAll(predicate);
        }

/*        public void RemoveFromAll(ParsedItem item)
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
        }*/
    }
}
