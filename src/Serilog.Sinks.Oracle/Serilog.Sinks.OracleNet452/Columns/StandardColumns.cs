namespace Serilog.Sinks.Oracle.Columns
{
    public enum StandardColumn
    {
        /// <summary>
        /// The identifier for the row.
        /// </summary>
        Id,

        /// <summary>
        /// The message rendered with the template given the properties associated with the event.
        /// </summary>
        Message,

        /// <summary>
        /// The message template describing the event.
        /// </summary>
        MessageTemplate,

        /// <summary>
        /// The level of the event.
        /// </summary>
        Level,

        /// <summary>
        /// The time at which the event occurred.
        /// </summary>
        TimeStamp,

        /// <summary>
        /// An exception associated with the event, or null.
        /// </summary>
        Exception,

        /// <summary>
        /// Properties associated with the event, including those presented in <see cref="MessageTemplate"/>.
        /// </summary>
        Properties,

        /// <summary>
        /// A log event.
        /// </summary>
        LogEvent
    }}
