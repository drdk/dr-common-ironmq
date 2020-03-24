using System;

namespace DR.Common.IronMq.Kermit.Model
{
    /// <summary>
    ///     Interface for an translated queue message.
    /// </summary>
    /// <typeparam name="TIdentifier">Identifier type</typeparam>
    /// <typeparam name="TVersion">Version/timestamp type</typeparam>
    /// <typeparam name="TCorrelationId">Correlation key</typeparam>
    public interface IQueueMessage<TIdentifier, TCorrelationId, TVersion>
        where TIdentifier : IEquatable<TIdentifier>
        where TCorrelationId : IEquatable<TCorrelationId>
        where TVersion : IComparable
    {
        /// <summary>
        ///     Resource key
        /// </summary>
        TIdentifier Identifier { get; }

        /// <summary>
        ///     Queue time stamp/version
        /// </summary>
        TVersion Version { get; }

        /// <summary>
        ///     Correlation key
        /// </summary>
        TCorrelationId CorrelationId { get; }

        /// <summary>
        /// Used for target type
        /// </summary>
        string CorrelationType { get; }
    }
}