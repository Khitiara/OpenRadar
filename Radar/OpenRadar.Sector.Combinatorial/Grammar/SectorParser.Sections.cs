using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using FreeRadar.Common;
using Pidgin;
using static Pidgin.Parser;
using static Pidgin.Parser<char>;

namespace OpenRadar.Sector.Grammar
{
    internal static partial class SectorParser
    {
        private static class Sections
        {
            public static readonly Parser<char, ISection> AnySection = Combinators.ReadAnySection(
                new Dictionary<string, Parser<char, ISection>> {
                    ["INFO"] = InfoSectionBody(),
                    ["AIRPORT"] = EnumerableSectionBody(SectionId.Airport, Combinators.PAirport),
                    ["VOR"] = EnumerableSectionBody(SectionId.Vor, Combinators.PSimpleNavaid),
                    ["NDB"] = EnumerableSectionBody(SectionId.Ndb, Combinators.PSimpleNavaid),
                    ["RUNWAY"] = EnumerableSectionBody(SectionId.Runway, Combinators.PRunway),
                    ["FIXES"] = EnumerableSectionBody(SectionId.Fixes, Combinators.PFix),
                    ["ARTCC"] = EnumerableSectionBody(SectionId.Artcc, Combinators.PGeoSegment),
                    ["ARTCC HIGH"] = EnumerableSectionBody(SectionId.ArtccHigh, Combinators.PGeoSegment),
                    ["ARTCC LOW"] = EnumerableSectionBody(SectionId.ArtccLow, Combinators.PGeoSegment),
                    // ["SID"] = ???,
                    // ["STAR"] = ???,
                    ["HIGH AIRWAY"] = EnumerableSectionBody(SectionId.HighAirway, Combinators.PGeoSegment),
                    ["LOW AIRWAY"] = EnumerableSectionBody(SectionId.LowAirway, Combinators.PGeoSegment),
                    ["GEO"] = EnumerableSectionBody(SectionId.Geo, Combinators.PGeoLine),
                    // ["REGIONS"] = ???,
                    // ["LABELS"] = ???
                });

            private static Parser<char, ISection> InfoSectionBody() {
                Parser<char, string> unspecifiedLine = Any.AtLeastOnceUntil(Combinators.PSkipEmptyLines)
                    .Slice((span, _) => span.ToString());
                Parser<char, double> real = Real.Before(Combinators.PSkipEmptyLines);
                Parser<char, double> pCoord = Combinators.PCoord.Before(Combinators.PSkipEmptyLines);
                Parser<char, LatLng> pCoordLines = Map((lat, lng) => new LatLng(lat, lng), pCoord, pCoord);
                return Map(InfoBlock.Create, unspecifiedLine, unspecifiedLine, unspecifiedLine, pCoordLines, real, real,
                    real, real);
            }

            private static Parser<char, ISection> EnumerableSectionBody<T>(SectionId name, Parser<char, T> parser) =>
                Combinators.SectionBodyParser(parser, (Func<IEnumerable<T>, ISection>)(navaids =>
                    new TaggedEnumerableSection<T>(name, navaids)));
        }

        public static Parser<char, RawSector> Parser => Map((defines, sections) => new RawSector(defines, sections),
            Combinators.PSkipEmptyLines.Then(Combinators.PDefines),
            Sections.AnySection.AtLeastOnceUntil(End).Map(ImmutableList.ToImmutableList));
    }
}