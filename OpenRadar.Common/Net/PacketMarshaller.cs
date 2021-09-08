using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Overby.Extensions.AsyncBinaryReaderWriter;

namespace FreeRadar.Common.Net
{
    public static class PacketMarshaller
    {
        public static bool IsTagSetInitialized { get; private set; }

        public static ImmutableDictionary<ushort, Type> TagReverseLookup { get; private set; } =
            ImmutableDictionary<ushort, Type>.Empty;

        /// <summary>
        /// THIS IS REALLY PERFORMANCE-EXPENSIVE ON FIRST RUN
        /// Method is a no-op if <see cref="TagReverseLookup"/> is nonnull
        /// </summary>
        public static void LoadSupportedPacketTypes() {
            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            if (!IsTagSetInitialized)
                return;

            IsTagSetInitialized = true;
            // NOTE: AppDomains are not supported in .NET 5+
            // AppDomain.CurrentDomain.GetAssemblies is a thin public wrapper
            // over internal static extern AssemblyLoadContext.GetLoadedAssemblies()
            TagReverseLookup = (from assembly in AppDomain.CurrentDomain.GetAssemblies()
                    from type in assembly.ExportedTypes
                    let attribute = type.GetCustomAttribute<PacketAttribute>()
                    where attribute is not null
                    select new KeyValuePair<ushort, Type>(attribute.PacketTag, type))
                .ToImmutableDictionary();
        }

        private static unsafe T PtrToStructure<T>(ReadOnlySpan<byte> span, Type packetType) {
            fixed (void* ptr = span)
                return (T?)Marshal.PtrToStructure(new IntPtr(ptr), packetType)
                       ?? throw new IOException();
        }

        public static async Task<T> ReadPacketAsync<T>(AsyncBinaryReader reader, CancellationToken cancellationToken = default) {
            ushort tag = await reader.ReadUInt16Async(cancellationToken);
            ushort size = await reader.ReadUInt16Async(cancellationToken);
            if (!TagReverseLookup.TryGetValue(tag, out Type? packetType))
                throw new IOException();
            byte[] span = await reader.ReadBytesAsync(size, cancellationToken);
            return PtrToStructure<T>(span, packetType);
        }

        public static async Task WritePacketAsync<T>(AsyncBinaryWriter writer, T packet, CancellationToken cancellationToken = default)
            where T : notnull {
            ushort size = (ushort)Unsafe.SizeOf<T>();
            Type type = packet.GetType();
            PacketAttribute packetAttribute = type.GetCustomAttribute<PacketAttribute>()
                                              ?? throw new InvalidOperationException();
            await writer.WriteAsync(packetAttribute.PacketTag, cancellationToken);
            await writer.WriteAsync(size, cancellationToken);
            byte[] span = StructureToBytes(packet, size);
            await writer.WriteAsync(span, cancellationToken);
        }

        private static unsafe byte[] StructureToBytes<T>(T packet, ushort size)
            where T : notnull {
            byte[] span = new byte[size];
            fixed (void* ptr = span) Unsafe.Write(ptr, packet);
            return span;
        }
    }
}