using System;

namespace GeeksCoreLibrary.Core.Exceptions;

/// <summary>
/// Custom exception for queries executed by the GCL.
/// </summary>
public class GclQueryException : Exception
{
    /// <summary>
    /// Gets or sets the query that was being executed when the exception was thrown.
    /// </summary>
    public string Query { get; }

    /// <summary>
    /// Create a new <see cref="GclQueryException"/> with only a message and the executed query.
    /// </summary>
    /// <param name="message">The message of the error.</param>
    /// <param name="query">The query that was executed when the exception was thrown.</param>
    public GclQueryException(string message, string query) : base(message)
    {
        Query = query;
    }

    /// <summary>
    /// Create a new <see cref="GclQueryException"/> with a message, the executed query and an inner exception to include an existing exception.
    /// </summary>
    /// <param name="message">The message of the error.</param>
    /// <param name="query">The query that was executed when the exception was thrown.</param>
    /// <param name="innerException">The inner exception to be included.</param>
    public GclQueryException(string message, string query, Exception innerException) : base(message, innerException)
    {
        Query = query;
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return $"{base.ToString()}{Environment.NewLine}{Environment.NewLine}Failed during execution of query:{Environment.NewLine}{Query}";
    }
}