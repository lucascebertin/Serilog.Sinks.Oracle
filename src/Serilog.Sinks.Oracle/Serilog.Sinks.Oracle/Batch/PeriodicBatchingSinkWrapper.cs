using Serilog.Events;
using Serilog.Sinks.Oracle.Columns;
using Serilog.Sinks.PeriodicBatching;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Serilog.Sinks.Oracle.Batch
{
    public class PeriodicBatchingSinkWrapper : PeriodicBatchingSink
    {
        private readonly Action<IEnumerable<LogEvent>> _action;
        private readonly Func<IEnumerable<LogEvent>, Task> _asyncAction;

        public const int DefaultBatchPostingLimit = 50;
        public static readonly TimeSpan DefaultPeriod = TimeSpan.FromSeconds(5);

        public PeriodicBatchingSinkWrapper(Action<IEnumerable<LogEvent>> action, int batchSizeLimit, TimeSpan period, int queueLimit)
            : base(batchSizeLimit, period, queueLimit)
        {
            _action = action;
        }

        public PeriodicBatchingSinkWrapper(Func<IEnumerable<LogEvent>, Task> asyncAction, int batchSizeLimit, TimeSpan period, int queueLimit)
            : base(batchSizeLimit, period, queueLimit)
        {
            _asyncAction = asyncAction;
        }


        protected override void EmitBatch(IEnumerable<LogEvent> events) =>
             _action(events);

        protected override async Task EmitBatchAsync(IEnumerable<LogEvent> events) =>
            await _asyncAction(events);
    }
}
