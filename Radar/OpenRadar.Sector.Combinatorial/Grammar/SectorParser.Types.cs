using System.Collections.Generic;
using System.Collections.Immutable;
using FreeRadar.Common;

namespace OpenRadar.Sector.Grammar
{
    internal static partial class SectorParser
    {
        public interface IColor
        {
            public Color Get(ImmutableDictionary<string, Color> defines);
        }

        public readonly record struct ColorReference(string Id) : IColor
        {
            public Color Get(ImmutableDictionary<string, Color> defines) => defines[Id];
        }

        public readonly record struct ColorValue(uint Value) : IColor
        {
            public Color Get(ImmutableDictionary<string, Color> defines) => new(Value);
        }

        public readonly record struct InfoBlock(string SectorName, string CallSign, string DefaultAirport,
            LatLng Center, double NmPerDegLat, double NmPerDegLng, double MagneticVariation, double Scale) : ISection
        {
            public static ISection Create(string sectorName, string callSign, string defaultAirport, LatLng center,
                double nmPerDegLat, double nmPerDegLng, double magneticVariation, double scale) => new InfoBlock(
                sectorName, callSign, defaultAirport, center, nmPerDegLat, nmPerDegLng, magneticVariation, scale);

            public SectionId Id => SectionId.Info;
        }

        public enum SectionId
        {
            Info,
            Airport,
            Vor,
            Ndb,
            Runway,
            Fixes,
            Artcc,
            ArtccHigh,
            ArtccLow,
            Sid,
            Star,
            HighAirway,
            LowAirway,
            Geo,
            Regions,
            Labels
        }

        public interface ISection
        {
            public SectionId Id { get; }
        }

        public interface IGeoCoords
        {
            LatLng Convert(ImmutableDictionary<string, INavaid> navaids);
        }

        public readonly record struct LatLngWrapper(double Lat, double Lng) : IGeoCoords
        {
            public LatLng Convert(ImmutableDictionary<string, INavaid> navaids) {
                return new LatLng(Lat, Lng);
            }
        }

        public readonly record struct NavaidReference(string LatIdent, string LngIdent) : IGeoCoords
        {
            public LatLng Convert(ImmutableDictionary<string, INavaid> navaids) =>
                new(navaids[LatIdent].Coordinates.Latitude, navaids[LngIdent].Coordinates.Longitude);
        }


        public sealed record TaggedEnumerableSection<T>(SectionId Id, IEnumerable<T> Contents) : ISection;

        public readonly record struct GeoSegment(string BoundaryName, IGeoCoords Start, IGeoCoords End);

        public readonly record struct GeoLine(LatLng Start, LatLng End, IColor Color);
    }
}