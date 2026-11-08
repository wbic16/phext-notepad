namespace PhextNotepad
{
    // A display-ready description of one node in the phext tree: its
    // coordinate, its tree level, the Name and Text a TreeNode would
    // carry, and whether it has children. Entries are computed from the
    // sparse PhextText model on demand, so opening a document only ever
    // describes the nodes the user actually looks at — the rest of the
    // tree costs nothing until it is expanded into.
    //
    // This type and PhextTree below are the tree-model logic of the
    // editor with the WinForms layer removed: child enumeration,
    // find-by-coordinate (as a path of entries), and subtree walks all
    // run against the model dictionaries and are unit-testable without
    // a TreeView. The form turns entries into TreeNodes lazily.
    //
    // The formatting here reproduces the eager tree the loader used to
    // build, exactly:
    // - a group node's Name is its root coordinate (lower dimensions 0),
    //   its Text is "Library 1" / "Section 12" / ...;
    // - a scroll node's Name is its full coordinate, its Text is the
    //   scroll summary, prefixed "{coordinate}! " when coordinates are
    //   shown (the same prefix the old insertScroll used at load).
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
                (int)c.Library, (int)c.Shelf, (int)c.Series,
                (int)c.Collection, (int)c.Volume, (int)c.Book,
                (int)c.Chapter, (int)c.Section, (int)c.Scroll
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

        // The child dictionary of a model node, as (key, node) pairs in
        // sorted order. Group nodes expose theirs through IPhextNode;
        // the root and the section's scroll dictionary are handled
        // directly. Returns an empty list for scroll nodes.
        private static List<KeyValuePair<int, object>> ChildEntries(object node)
        {
            var result = new List<KeyValuePair<int, object>>();
            switch (node)
            {
                case RootNode root:
                    foreach (var kv in root.Library)
                    {
                        result.Add(new((int)kv.Key, kv.Value));
                    }
                    break;
                case LibraryNode library:
                    foreach (var kv in library.Shelf)
                    {
                        result.Add(new((int)kv.Key, kv.Value));
                    }
                    break;
                case ShelfNode shelf:
                    foreach (var kv in shelf.Series)
                    {
                        result.Add(new((int)kv.Key, kv.Value));
                    }
                    break;
                case SeriesNode series:
                    foreach (var kv in series.Collection)
                    {
                        result.Add(new((int)kv.Key, kv.Value));
                    }
                    break;
                case CollectionNode collection:
                    foreach (var kv in collection.Volume)
                    {
                        result.Add(new((int)kv.Key, kv.Value));
                    }
                    break;
                case VolumeNode volume:
                    foreach (var kv in volume.Book)
                    {
                        result.Add(new((int)kv.Key, kv.Value));
                    }
                    break;
                case BookNode book:
                    foreach (var kv in book.Chapter)
                    {
                        result.Add(new((int)kv.Key, kv.Value));
                    }
                    break;
                case ChapterNode chapter:
                    foreach (var kv in chapter.Section)
                    {
                        result.Add(new((int)kv.Key, kv.Value));
                    }
                    break;
                case SectionNode section:
                    foreach (var kv in section.Scroll)
                    {
                        result.Add(new((int)kv.Key, kv.Value));
                    }
                    break;
            }
            return result;
        }

        private static int ChildCount(object node)
        {
            switch (node)
            {
                case RootNode root: return root.Library.Count;
                case LibraryNode library: return library.Shelf.Count;
                case ShelfNode shelf: return shelf.Series.Count;
                case SeriesNode series: return series.Collection.Count;
                case CollectionNode collection: return collection.Volume.Count;
                case VolumeNode volume: return volume.Book.Count;
                case BookNode book: return book.Chapter.Count;
                case ChapterNode chapter: return chapter.Section.Count;
                case SectionNode section: return section.Scroll.Count;
                default: return 0;
            }
        }

        // Walks the model dictionaries along `components` for `depth`
        // levels (depth 0 = the root, 1 = a LibraryNode, ..., 8 = a
        // SectionNode, 9 = a ScrollNode). Returns null as soon as any
        // step is missing. The final scroll step uses the typed
        // dictionary directly — a section can hold hundreds of
        // thousands of scrolls, and a point lookup must not enumerate
        // them.
        private static object? Navigate(PhextText text, int[] components, int depth)
        {
            object current = text.Root;
            for (int level = 0; level < depth; ++level)
            {
                if (level == ScrollLevel)
                {
                    if (current is SectionNode section &&
                        section.Scroll.TryGetValue(new Coordinates.ScrollIndex(components[level]), out var scroll))
                    {
                        return scroll;
                    }
                    return null;
                }
                object? next = null;
                foreach (var kv in ChildEntries(current))
                {
                    if (kv.Key == components[level])
                    {
                        next = kv.Value;
                        break;
                    }
                }
                if (next == null)
                {
                    return null;
                }
                current = next;
            }
            return current;
        }

        public static List<PhextTreeEntry> Roots(PhextText text, bool showCoordinates)
        {
            return Children(text, null, -1, showCoordinates);
        }

        // The entries for the children of the node at
        // (parentCoord, parentLevel); parentLevel -1 describes the
        // virtual root above the libraries. Children come from the
        // model as it is right now — edits made since an entry was
        // produced are reflected the next time children are asked for.
        public static List<PhextTreeEntry> Children(PhextText text, Coordinates? parentCoord, int parentLevel, bool showCoordinates)
        {
            var result = new List<PhextTreeEntry>();
            object? parentNode;
            int[] parentComponents;
            if (parentLevel < 0 || parentCoord == null)
            {
                parentNode = text.Root;
                parentComponents = new int[9];
            }
            else
            {
                parentComponents = Components(parentCoord);
                parentNode = Navigate(text, parentComponents, parentLevel + 1);
                if (parentNode == null)
                {
                    return result;
                }
            }

            int childLevel = parentLevel + 1;
            if (childLevel > ScrollLevel)
            {
                return result;
            }
            foreach (var kv in ChildEntries(parentNode))
            {
                var coord = ChildCoordinate(parentComponents, childLevel, kv.Key);
                if (childLevel == ScrollLevel)
                {
                    var scroll = (ScrollNode)kv.Value;
                    var summary = PhextModel.GetScrollSummary(coord, scroll.Text);
                    var label = showCoordinates ? $"{coord}! {summary}" : summary;
                    result.Add(new PhextTreeEntry(coord, childLevel, label, false));
                }
                else
                {
                    var label = $"{KindNames[childLevel]} {kv.Key}";
                    result.Add(new PhextTreeEntry(coord, childLevel, label, ChildCount(kv.Value) > 0));
                }
            }
            return result;
        }

        public static bool ScrollExists(PhextText text, Coordinates coord)
        {
            var components = Components(coord);
            return Navigate(text, components, ScrollLevel + 1) != null;
        }

        // The entry for one scroll that exists in the model, formatted
        // exactly like a child entry of its section.
        public static PhextTreeEntry ScrollEntry(PhextText text, Coordinates coord, bool showCoordinates)
        {
            var components = Components(coord);
            var node = Navigate(text, components, ScrollLevel + 1) as ScrollNode;
            var summary = PhextModel.GetScrollSummary(coord, node?.Text ?? string.Empty);
            var label = showCoordinates ? $"{coord}! {summary}" : summary;
            return new PhextTreeEntry(new Coordinates(coord), ScrollLevel, label, false);
        }

        // The path of entries from the outermost library down to
        // `target`: one group entry per ancestor that exists in the
        // model, plus the scroll entry itself when the target scroll
        // exists. The form walks this path to materialize (and then
        // select) a node that lazy loading has not created yet. The
        // walk stops at the first missing ancestor — nothing below a
        // missing group can exist.
        public static List<PhextTreeEntry> PathTo(PhextText text, Coordinates target, bool showCoordinates)
        {
            var path = new List<PhextTreeEntry>();
            var components = Components(target);
            Coordinates? parentCoord = null;
            for (int level = 0; level <= SectionLevel; ++level)
            {
                var siblings = Children(text, parentCoord, level - 1, showCoordinates);
                PhextTreeEntry? match = null;
                foreach (var entry in siblings)
                {
                    if (Components(entry.Coord)[level] == components[level])
                    {
                        match = entry;
                        break;
                    }
                }
                if (match == null)
                {
                    return path;
                }
                path.Add(match);
                parentCoord = match.Coord;
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
        public static int CountSubtree(PhextText text, Coordinates rootCoord, int level)
        {
            var components = Components(rootCoord);
            var node = Navigate(text, components, level + 1);
            if (node == null)
            {
                return 0;
            }
            return CountSubtree(node);
        }

        private static int CountSubtree(object node)
        {
            int count = 1;
            foreach (var kv in ChildEntries(node))
            {
                count += CountSubtree(kv.Value);
            }
            return count;
        }

        // Every scroll coordinate in the subtree rooted at
        // (rootCoord, level), in coordinate order — including the root
        // itself when it addresses a scroll.
        public static List<Coordinates> SubtreeScrolls(PhextText text, Coordinates rootCoord, int level)
        {
            var result = new List<Coordinates>();
            var components = Components(rootCoord);
            var node = Navigate(text, components, level + 1);
            if (node == null)
            {
                return result;
            }
            CollectScrolls(node, components, level, result);
            return result;
        }

        private static void CollectScrolls(object node, int[] components, int level, List<Coordinates> result)
        {
            if (level == ScrollLevel)
            {
                result.Add(ChildCoordinate(components, ScrollLevel, components[ScrollLevel]));
                return;
            }
            foreach (var kv in ChildEntries(node))
            {
                var childComponents = (int[])components.Clone();
                childComponents[level + 1] = kv.Key;
                CollectScrolls(kv.Value, childComponents, level + 1, result);
            }
        }
    }
}
