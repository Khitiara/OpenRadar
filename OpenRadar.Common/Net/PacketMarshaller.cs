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
    /// <summary>
    /// Utility for implementing a packet serialization scheme based on reinterpret-casting
    /// </summary>
    public static class PacketMarshaller
    {
        /// <summary>
        /// Whether the internal lookup of known packet types has been constructed
        /// </summary>
        public static bool IsTagSetInitialized { get; private set; }

        /// <summary>
        /// A lookup of packet types by their tag values.
        /// </summary>
        public static ImmutableDictionary<ushort, Type> TagReverseLookup { get; private set; } =
            ImmutableDictionary<ushort, Type>.Empty;

        /// <summary>
        /// Loads <seealso cref="TagReverseLookup"/> by reflecting over every loaded assembly.
        /// </summary>
        /// <remarks>
        /// THIS IS REALLY PERFORMANCE-EXPENSIVE ON FIRST RUN
        /// Method is a no-op if <see cref="IsTagSetInitialized"/> is true
        /// </remarks>
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

        /// <summary>
        /// Wrapper around <see cref="Marshal.PtrToStructure(IntPtr, Type)"/> that loads from a <see cref="ReadOnlySpan{T}"/>
        /// instead of an IntPtr.
        /// </summary>
        private static unsafe T PtrToStructure<T>(ReadOnlySpan<byte> span, Type packetType) {
            fixed (void* ptr = span)
                return (T?)Marshal.PtrToStructure(new IntPtr(ptr), packetType)
                       ?? throw new IOException();
        }

        /// <summary>
        /// Wrapper around <see cref="Unsafe.Write{T}"/> that writes to a pinned array
        /// </summary>
        private static unsafe byte[] StructureToBytes<T>(T packet, ushort size)
            where T : notnull {
            byte[] span = new byte[size];
            fixed (void* ptr = span) Unsafe.Write(ptr, packet);
            return span;
        }

        /// <summary>
        /// Asynchronously reads a single packet from the given <see cref="AsyncBinaryReader"/>
        /// Consumes four bytes plus the marshalled size of the detected packet type.
        /// </summary>
        /// <param name="reader">The reader to read a packet from</param>
        /// <param name="cancellationToken">Cancels this asynchronous operation</param>
        /// <returns>The decoded packet</returns>
        public static async Task<T> ReadPacketAsync<T>(AsyncBinaryReader reader,
            CancellationToken cancellationToken = default) {
            ushort tag = await reader.ReadUInt16Async(cancellationToken);
            ushort size = await reader.ReadUInt16Async(cancellationToken);
            if (!TagReverseLookup.TryGetValue(tag, out Type? packetType))
                throw new IOException();
            byte[] span = await reader.ReadBytesAsync(size, cancellationToken);
            return PtrToStructure<T>(span, packetType);
        }

        /// <summary>
        /// Asynchronously writes a single packet to the given <see cref="AsyncBinaryWriter"/>
        /// </summary>
        /// <param name="writer">The writer to output to</param>
        /// <param name="packet">The packet to write</param>
        /// <param name="cancellationToken">Cancels this asynchronous operation</param>
        /// <exception cref="InvalidOperationException">If the packet type given is not a valid packet type, i.e. lacks a <see cref="PacketAttribute"/></exception>
        public static async Task WritePacketAsync<T>(AsyncBinaryWriter writer, T packet,
            CancellationToken cancellationToken = default)
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
    }
}