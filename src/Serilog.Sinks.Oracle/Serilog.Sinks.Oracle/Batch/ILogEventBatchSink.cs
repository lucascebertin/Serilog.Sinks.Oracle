using Serilog.Events;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Serilog.Sinks.Oracle.Batch
{
    public interface ILogEventBatchSink
    {
        /// <summary>
        /// Defines outgoing log events in batches.
        /// </summary>
        void EmitBatch(IEnumerable<LogEvent> events);

        /// <summary>
        /// Defines outgoing log events in batch asynchroon.
        /// </summary>
        /// <param name="events"></param>
        /// <returns></returns>
        Task EmitBatchAsync(IEnumerable<LogEvent> events);
    }
}
