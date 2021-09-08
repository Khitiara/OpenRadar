using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Pidgin;

namespace OpenRadar.Sector.Grammar
{
    internal sealed class RawSector
    {
        public readonly ImmutableDictionary<string, Color>   ColorDefines;
        public readonly ImmutableDictionary<string, INavaid> Navaids;
        public readonly ImmutableList<SectorParser.ISection> Sections;

        private RawSector(ImmutableDictionary<string, Color> colorDefines,
            ImmutableDictionary<string, INavaid> navaids, ImmutableList<SectorParser.ISection> sections) {
            ColorDefines = colorDefines;
            Navaids = navaids;
            Sections = sections;
        }

        internal RawSector(ImmutableDictionary<string, Color> colorDefines,
            ImmutableList<SectorParser.ISection> sections) : this(colorDefines, CollectNavaids(sections), sections) { }

        private static ImmutableDictionary<string, INavaid> CollectNavaids(
            ImmutableList<SectorParser.ISection> sections) => sections
            .OfType<SectorParser.TaggedEnumerableSection<INavaid>>().SelectMany(section => section.Contents)
            .ToImmutableDictionary(navaid => navaid.Id);

        public static RawSector Load(Stream stream, bool leaveOpen = true) {
            using StreamReader reader = new(stream, leaveOpen: leaveOpen);
            return SectorParser.Parser.ParseOrThrow(reader);
        }
    }
}