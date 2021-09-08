using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;

namespace OpenRadar.Sector
{
    public static class Utils
    {
        public static async IAsyncEnumerable<string> EnumarateAllLinesAsync(this StreamReader reader,
            [EnumeratorCancellation] CancellationToken cancellationToken = default) {
            while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested) {
                yield return await reader.ReadLineAsync() ?? string.Empty;
            }
        } 
    }
}