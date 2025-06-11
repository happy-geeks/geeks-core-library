using System;

namespace GeeksCoreLibrary.Core.Exceptions;

/// <summary>
/// An exception that is thrown when an encryption key is missing.
/// </summary>
public class MissingEncryptionKeyException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MissingEncryptionKeyException"/>.
    /// </summary>
    public MissingEncryptionKeyException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MissingEncryptionKeyException"/> with a specified error message.
    /// </summary>
    public MissingEncryptionKeyException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MissingEncryptionKeyException"/> with a specified error message
    /// and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    public MissingEncryptionKeyException(string message, Exception innerException) : base(message, innerException)
    {
    }
}