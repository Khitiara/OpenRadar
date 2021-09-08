using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using FreeRadar.Common;
using Pidgin;
using static Pidgin.Parser;
using static Pidgin.Comment.CommentParser;

namespace OpenRadar.Sector.Grammar
{
    internal static partial class SectorParser
    {
        private static class Combinators
        {
            private static readonly string[] KnownCommentHeaders = { "INFO", "AIRPORT" };

            private static readonly Parser<char, string>[] PKnownCommentHeaders =
                KnownCommentHeaders.Select(String).ToArray();

            public static readonly Parser<char, Unit> Spaces         = OneOf(" \t").SkipMany();
            public static readonly Parser<char, Unit> RequiredSpaces = OneOf(" \t").SkipAtLeastOnce();

            public static readonly Parser<char, Unit> PComment =
                Spaces.Then(SkipLineComment(Char(';')).Or(EndOfLine.IgnoreResult()));

            public static readonly Parser<char, Unit> PSkipEmptyLines = PComment.SkipMany();

            public static readonly Parser<char, string> PSectionHeader =
                OneOf(PKnownCommentHeaders).Between(Char('['), Char(']').Before(PSkipEmptyLines));

            public static readonly Parser<char, Unit>   PDefineTag = String("#define").IgnoreResult();
            public static readonly Parser<char, string> PIdent     = LetterOrDigit.Or(OneOf("_-")).AtLeastOnceString();
            public static readonly Parser<char, Color>  PColor     = UnsignedInt(10).Map(i => new Color((uint)i));
            public static readonly Parser<char, char>   PDot       = Char('.');
            public static readonly Parser<char, int>    P3Digits   = Digit.RepeatString(3).Map(int.Parse);

            public static readonly Parser<char, double> PDegrees =
                Digit.RepeatString(3).Before(PDot).Map(double.Parse);

            public static readonly Parser<char, double> PMinutes =
                Digit.RepeatString(2).Before(PDot).Map(s => double.Parse(s) / 60);

            public static readonly Parser<char, double> PSeconds = Digit.RepeatString(2).Before(PDot)
                .Bind(secs => Digit.RepeatString(3).Map(part => double.Parse(part) / 1000 + double.Parse(secs)))
                .Map(d => d / 60 / 60);

            public static readonly Parser<char, double> PCoord = Map((dir, deg, min, sec) => dir * (deg + min + sec),
                OneOf("NESW").Map(c => c switch {
                    'N' or 'E' => 1,
                    'S' or 'W' => -1,
                    _ => throw new InvalidDataException()
                }), PDegrees, PMinutes, PSeconds);

            public static readonly Parser<char, int> PFreq = Map((upper, lower) => upper * 1000 + lower,
                P3Digits.Before(PDot), P3Digits);


            public static readonly Parser<char, LatLng> PCoords =
                Map((lat, _, lng) => new LatLng(lat, lng), PCoord, RequiredSpaces, PCoord);

            public static readonly Parser<char, IGeoCoords> PGeoCoord =
                Map<char, string, string, IGeoCoords>((latIdent, lngIdent) => new NavaidReference(latIdent, lngIdent),
                        PIdent.Before(RequiredSpaces), PIdent)
                    .Or(PCoords.Map<IGeoCoords>(coord => new LatLngWrapper(coord.Latitude, coord.Longitude)));

            public static readonly Parser<char, IColor> PColorOrRef = PIdent.Map<IColor>(id => new ColorReference(id))
                .Or(PColor.Map<IColor>(color => new ColorValue(color.Value)));

            public static readonly Parser<char, (string, Color)> PDefine = Map(
                (tag, ws1, id, ws2, color) => (id, color),
                PDefineTag, SkipWhitespaces, PIdent, SkipWhitespaces, PColor);

            public static readonly Parser<char, ImmutableDictionary<string, Color>> PDefines = PDefine
                .Before(PSkipEmptyLines).Many().Map(it => it.ToImmutableDictionary(kvp => kvp.Item1, kvp => kvp.Item2));

            public static readonly Parser<char, Airport> PAirport = Map(Airport.Create,
                PIdent.Before(RequiredSpaces), PFreq.Before(RequiredSpaces), PCoords.Before(RequiredSpaces),
                OneOf("ABCDE"));

            public static readonly Parser<char, SimpleNavaid> PSimpleNavaid = Map(SimpleNavaid.Create,
                PIdent.Before(RequiredSpaces), PFreq.Before(RequiredSpaces), PCoords);

            public static readonly Parser<char, string> PRunwayNumber =
                Map((num, side) => side.HasValue ? num + side.Value : num, Digit.RepeatString(2),
                    OneOf("LCR").Optional());

            public static readonly Parser<char, Runway> PRunway = Map(Runway.Create,
                PRunwayNumber.Before(RequiredSpaces), PRunwayNumber.Before(RequiredSpaces),
                P3Digits.Before(RequiredSpaces), P3Digits.Before(RequiredSpaces), PCoords.Before(RequiredSpaces),
                PCoords);

            public static readonly Parser<char, Fix> PFix = Map(Fix.Create, PIdent.Before(RequiredSpaces), PCoords);

            public static readonly Parser<char, GeoSegment> PGeoSegment =
                Map((name, start, end) => new GeoSegment(name, start, end), PIdent.Before(RequiredSpaces),
                    PGeoCoord.Before(RequiredSpaces), PGeoCoord.Before(RequiredSpaces));

            public static readonly Parser<char, GeoLine> PGeoLine =
                Map((start, end, color) => new GeoLine(start, end, color), PCoords.Before(RequiredSpaces),
                    PCoords.Before(RequiredSpaces), PColorOrRef);

            private static Parser<char, T> SectionParser<T>(KeyValuePair<string, Parser<char, T>> kvp) =>
                String(kvp.Key).Between(Char('['), Char(']')).Then(PSkipEmptyLines)
                    .Then(kvp.Value);

            internal static Parser<char, TOut> SectionBodyParser<TEntry, TOut>(Parser<char, TEntry> bodyLines,
                Func<IEnumerable<TEntry>, TOut> outputProcessor) {
                return bodyLines.Before(PSkipEmptyLines).AtLeastOnceUntil(Lookahead(PSectionHeader))
                    .Map(outputProcessor);
            }

            internal static Parser<char, T> ReadAnySection<T>(Dictionary<string, Parser<char, T>> sections) =>
                OneOf(sections.Select(SectionParser));
        }
    }
}