using Phext;

namespace PhextNotepad
{
    // A display-ready description of one node in the phext tree: its
    // coordinate, its tree level, the Name and Text a TreeNode would
    // carry, and whether it has children. Entries are computed from
    // the document on demand, so opening a document only ever
    // describes the nodes the user actually looks at — the rest of
    // the tree costs nothing until it is expanded into.
    //
    // This type and PhextTree below are the tree-model logic of the
    // editor with the WinForms layer removed: child enumeration,
    // find-by-coordinate (as a path of entries), and subtree walks
    // all run against the document and are unit-testable without a
    // TreeView. The form turns entries into TreeNodes lazily.
    //
    // The formatting reproduces the eager tree the loader used to
    // build, exactly:
    // - a group node's Name is its root coordinate (lower dimensions 0),
    //   its Text is "Library 1" / "Section 12" / ...;
    // - a scroll node's Name is its full coordinate, its Text is the
    //   scroll summary, prefixed "{coordinate}! " when coordinates are
    //   shown (the same prefix the old insertScroll used at load).
    //
    // The document is one coordinate-sorted list (PhextDocument),
    // so the tree is read out of it with index ranges: the scrolls
    // under any group prefix form one contiguous range, bounded by
    // the prefix with 1s below it and the prefix with int.MaxValue
    // below it, and a binary search finds where the range starts.
    // Group children are enumerated by seeking — take the first key
    // at or after a cursor, read its child component, then move the
    // cursor past that child's whole range — so a level's children
    // cost one seek each, never a scan of the scrolls beneath them.
    // A group exists exactly when a scroll exists beneath it, so a
    // group entry's HasChildren is always true here; the vendored
    // node tree could also report empty groups that navigation alone
    // had materialized.
    public class PhextTreeEntry
    {
        public Coordinates Coord { get; }
        public int Level { get; }
        public string Name { get; }
        public string Text { get; }
        public bool HasChildren { get; }
        public bool IsScroll
        {
            get { return Level == PhextTree.ScrollLevel; }
        }

        public PhextTreeEntry(Coordinates coord, int level, string text, bool hasChildren)
        {
            Coord = coord;
            Level = level;
            Name = coord.ToString();
            Text = text;
            HasChildren = hasChildren;
        }
    }

    public static class PhextTree
    {
        public const int LibraryLevel = 0;
        public const int ShelfLevel = 1;
        public const int SeriesLevel = 2;
        public const int CollectionLevel = 3;
        public const int VolumeLevel = 4;
        public const int BookLevel = 5;
        public const int ChapterLevel = 6;
        public const int SectionLevel = 7;
        public const int ScrollLevel = 8;

        private static readonly string[] KindNames =
        {
            "Library", "Shelf", "Series", "Collection",
            "Volume", "Book", "Chapter", "Section"
        };

        // Component order matches the coordinate string:
        // Library.Shelf.Series/Collection.Volume.Book/Chapter.Section.Scroll
        private static int[] Components(Coordinates c)
        {
            return new[]
            {
                c.Library, c.Shelf, c.Series,
                c.Collection, c.Volume, c.Book,
                c.Chapter, c.Section, c.Scroll
            };
        }

        private static int ComponentOf(Coordinate c, int level)
        {
            return level switch
            {
                LibraryLevel => c.Library,
                ShelfLevel => c.Shelf,
                SeriesLevel => c.Series,
                CollectionLevel => c.Collection,
                VolumeLevel => c.Volume,
                BookLevel => c.Book,
                ChapterLevel => c.Chapter,
                SectionLevel => c.Section,
                _ => c.Scroll
            };
        }

        private static void SetComponent(Coordinates c, int level, int value)
        {
            switch (level)
            {
                case LibraryLevel: c.Library = value; break;
                case ShelfLevel: c.Shelf = value; break;
                case SeriesLevel: c.Series = value; break;
                case CollectionLevel: c.Collection = value; break;
                case VolumeLevel: c.Volume = value; break;
                case BookLevel: c.Book = value; break;
                case ChapterLevel: c.Chapter = value; break;
                case SectionLevel: c.Section = value; break;
                case ScrollLevel: c.Scroll = value; break;
            }
        }

        private static void SetComponent(Coordinate c, int level, int value)
        {
            switch (level)
            {
                case LibraryLevel: c.Library = value; break;
                case ShelfLevel: c.Shelf = value; break;
                case SeriesLevel: c.Series = value; break;
                case CollectionLevel: c.Collection = value; break;
                case VolumeLevel: c.Volume = value; break;
                case BookLevel: c.Book = value; break;
                case ChapterLevel: c.Chapter = value; break;
                case SectionLevel: c.Section = value; break;
                case ScrollLevel: c.Scroll = value; break;
            }
        }

        // The coordinate of the child at `level` under a parent whose
        // components are `parentComponents`: the child's own component
        // is set, everything below it is 0 (the root-coordinate shape
        // the old Get*Root methods produced) — except at scroll level,
        // where the full coordinate is the node's identity.
        private static Coordinates ChildCoordinate(int[] parentComponents, int level, int key)
        {
            var result = new Coordinates();
            for (int i = 0; i <= level; ++i)
            {
                SetComponent(result, i, i == level ? key : parentComponents[i]);
            }
            if (level < ScrollLevel)
            {
                for (int i = level + 1; i <= ScrollLevel; ++i)
                {
                    SetComponent(result, i, 0);
                }
            }
            return result;
        }

        // The bounds of the contiguous range of scrolls under a
        // prefix: components 0..level fixed, everything below set to
        // 1 (low) or int.MaxValue (high). Level -1 is the virtual
        // root: the whole map.
        private static Coordinate RangeLow(int[] prefix, int level)
        {
            var result = new Coordinate();
            for (int i = 0; i <= ScrollLevel; ++i)
            {
                SetComponent(result, i, i <= level ? prefix[i] : 1);
            }
            return result;
        }

        private static Coordinate RangeHigh(int[] prefix, int level)
        {
            var result = new Coordinate();
            for (int i = 0; i <= ScrollLevel; ++i)
            {
                SetComponent(result, i, i <= level ? prefix[i] : int.MaxValue);
            }
            return result;
        }

        private static bool HasPrefix(Coordinate c, int[] prefix, int level)
        {
            for (int i = 0; i <= level; ++i)
            {
                if (ComponentOf(c, i) != prefix[i])
                {
                    return false;
                }
            }
            return true;
        }

        // The index of the first key at or after `low` (the lower
        // bound). SortedList.IndexOfKey cannot provide this — it
        // returns -1 for a missing key rather than the insertion
        // point — so the search is spelled out here.
        private static int LowerBound(SortedList<Coordinate, string> list, Coordinate low)
        {
            int lo = 0;
            int hi = list.Count;
            while (lo < hi)
            {
                int mid = lo + ((hi - lo) >> 1);
                if (list.Keys[mid].CompareTo(low) < 0)
                {
                    lo = mid + 1;
                }
                else
                {
                    hi = mid;
                }
            }
            return lo;
        }

        // The scrolls whose coordinates lie in [low, high], in
        // coordinate order: a binary search for the first key at or
        // after `low`, then a walk until the keys pass `high`.
        private static IEnumerable<KeyValuePair<Coordinate, string>> RangeFrom(PhextDocument text, Coordinate low, Coordinate high)
        {
            var list = text.Scrolls;
            for (int i = LowerBound(list, low); i < list.Count; ++i)
            {
                var key = list.Keys[i];
                if (key.CompareTo(high) > 0)
                {
                    yield break;
                }
                yield return new KeyValuePair<Coordinate, string>(key, list.Values[i]);
            }
        }

        private static IEnumerable<KeyValuePair<Coordinate, string>> Range(PhextDocument text, int[] prefix, int level)
        {
            return RangeFrom(text, RangeLow(prefix, level), RangeHigh(prefix, level));
        }

        // True when at least one scroll sits under the prefix — which
        // is what "this group node exists" means in the document.
        private static bool ExistsUnder(PhextDocument text, int[] prefix, int level)
        {
            foreach (var kv in Range(text, prefix, level))
            {
                return HasPrefix(kv.Key, prefix, level);
            }
            return false;
        }

        public static List<PhextTreeEntry> Roots(PhextDocument text, bool showCoordinates)
        {
            return Children(text, null, -1, showCoordinates);
        }

        // The entries for the children of the node at
        // (parentCoord, parentLevel); parentLevel -1 describes the
        // virtual root above the libraries. Children come from the
        // document as it is right now — edits made since an entry was
        // produced are reflected the next time children are asked for.
        public static List<PhextTreeEntry> Children(PhextDocument text, Coordinates? parentCoord, int parentLevel, bool showCoordinates)
        {
            var result = new List<PhextTreeEntry>();
            int childLevel = parentLevel + 1;
            if (childLevel > ScrollLevel)
            {
                return result;
            }

            int[] parentComponents;
            if (parentLevel < 0 || parentCoord == null)
            {
                parentComponents = new int[9];
            }
            else
            {
                parentComponents = Components(parentCoord);
                if (!ExistsUnder(text, parentComponents, parentLevel))
                {
                    return result;
                }
            }

            if (childLevel == ScrollLevel)
            {
                foreach (var kv in Range(text, parentComponents, parentLevel))
                {
                    var coord = ChildCoordinate(parentComponents, childLevel, ComponentOf(kv.Key, ScrollLevel));
                    var summary = PhextModel.GetScrollSummary(coord, kv.Value);
                    var label = showCoordinates ? $"{coord}! {summary}" : summary;
                    result.Add(new PhextTreeEntry(coord, childLevel, label, false));
                }
                return result;
            }

            // Group children: seek the first scroll at or after the
            // cursor, take its child-level component as the next
            // child, then move the cursor past that child's range.
            var high = RangeHigh(parentComponents, parentLevel);
            var cursor = RangeLow(parentComponents, parentLevel);
            while (true)
            {
                Coordinate? first = null;
                foreach (var kv in RangeFrom(text, cursor, high))
                {
                    first = kv.Key;
                    break;
                }
                if (first == null)
                {
                    break;
                }
                int key = ComponentOf(first, childLevel);
                var coord = ChildCoordinate(parentComponents, childLevel, key);
                result.Add(new PhextTreeEntry(coord, childLevel, $"{KindNames[childLevel]} {key}", true));
                if (key == int.MaxValue)
                {
                    break;
                }
                cursor = RangeLow(ChildComponents(parentComponents, childLevel, key + 1), childLevel);
            }
            return result;
        }

        // The prefix for the sibling after `key` at `level`: the
        // parent's components, with `level` set to the given value.
        private static int[] ChildComponents(int[] parentComponents, int level, int value)
        {
            var result = (int[])parentComponents.Clone();
            result[level] = value;
            return result;
        }

        public static bool ScrollExists(PhextDocument text, Coordinates coord)
        {
            return text.ExistsAt(coord);
        }

        // The entry for one scroll that exists in the document,
        // formatted exactly like a child entry of its section.
        public static PhextTreeEntry ScrollEntry(PhextDocument text, Coordinates coord, bool showCoordinates)
        {
            var summary = PhextModel.GetScrollSummary(coord, text.GetAt(coord));
            var label = showCoordinates ? $"{coord}! {summary}" : summary;
            return new PhextTreeEntry(new Coordinates(coord), ScrollLevel, label, false);
        }

        // The path of entries from the outermost library down to
        // `target`: one group entry per ancestor that exists in the
        // document, plus the scroll entry itself when the target
        // scroll exists. The form walks this path to materialize (and
        // then select) a node that lazy loading has not created yet.
        // The walk stops at the first missing ancestor — nothing
        // below a missing group can exist.
        public static List<PhextTreeEntry> PathTo(PhextDocument text, Coordinates target, bool showCoordinates)
        {
            var path = new List<PhextTreeEntry>();
            var components = Components(target);
            for (int level = 0; level <= SectionLevel; ++level)
            {
                if (!ExistsUnder(text, components, level))
                {
                    return path;
                }
                var coord = ChildCoordinate(components, level, components[level]);
                path.Add(new PhextTreeEntry(coord, level, $"{KindNames[level]} {components[level]}", true));
            }
            if (ScrollExists(text, target))
            {
                path.Add(ScrollEntry(text, target, showCoordinates));
            }
            return path;
        }

        // The tree level a coordinate addresses: the deepest dimension
        // that is non-zero. Scroll coordinates have Scroll >= 1; group
        // (root) coordinates are zero below their own level.
        public static int LevelOf(Coordinates coord)
        {
            var components = Components(coord);
            for (int level = ScrollLevel; level >= 0; --level)
            {
                if (components[level] != 0)
                {
                    return level;
                }
            }
            return -1;
        }

        // Total number of tree nodes in the subtree rooted at
        // (rootCoord, level), including the root itself — the same
        // count the eager tree reported for delete confirmations.
        // Counted from the range under the root's prefix: every
        // scroll is a node, and every distinct group prefix between
        // the root and the scrolls is a node.
        public static int CountSubtree(PhextDocument text, Coordinates rootCoord, int level)
        {
            var components = Components(rootCoord);
            if (level == ScrollLevel)
            {
                return text.ExistsAt(rootCoord) ? 1 : 0;
            }
            if (level < 0)
            {
                return 0;
            }
            int scrolls = 0;
            var groups = new HashSet<(int, int, int, int, int, int, int, int)>();
            foreach (var kv in Range(text, components, level))
            {
                ++scrolls;
                for (int l = level + 1; l <= SectionLevel; ++l)
                {
                    groups.Add(PrefixTuple(kv.Key, l));
                }
            }
            if (scrolls == 0)
            {
                return 0;
            }
            return 1 + scrolls + groups.Count;
        }

        // The first level+1 components of a key as a value tuple
        // (deeper components zeroed), for counting distinct group
        // prefixes. Group levels run 0..7, so eight components cover
        // every prefix; real components are >= 1, so prefixes of
        // different lengths can never collide.
        private static (int, int, int, int, int, int, int, int) PrefixTuple(Coordinate key, int level)
        {
            int At(int i) => i <= level ? ComponentOf(key, i) : 0;
            return (At(0), At(1), At(2), At(3), At(4), At(5), At(6), At(7));
        }

        // Every scroll coordinate in the subtree rooted at
        // (rootCoord, level), in coordinate order — including the root
        // itself when it addresses a scroll.
        public static List<Coordinates> SubtreeScrolls(PhextDocument text, Coordinates rootCoord, int level)
        {
            var result = new List<Coordinates>();
            if (level == ScrollLevel)
            {
                if (text.ExistsAt(rootCoord))
                {
                    result.Add(new Coordinates(rootCoord));
                }
                return result;
            }
            if (level < 0)
            {
                return result;
            }
            var components = Components(rootCoord);
            foreach (var kv in Range(text, components, level))
            {
                result.Add(new Coordinates(kv.Key));
            }
            return result;
        }
    }
}
