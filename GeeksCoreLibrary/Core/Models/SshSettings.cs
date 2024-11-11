namespace GeeksCoreLibrary.Core.Models;

/// <summary>
/// A settings model for making SSH connections.
/// </summary>
public class SshSettings
{
    /// <summary>
    /// Gets or sets the host of the SSH tunnel/server.
    /// </summary>
    public string Host { get; set; }

    /// <summary>
    /// Gets or sets the port number to use for the SSH connection, default is 22.
    /// </summary>
    public int Port { get; set; } = 22;

    /// <summary>
    /// Gets or sets the username for the SSH connection.
    /// </summary>
    public string Username { get; set; }

    /// <summary>
    /// Gets or sets the password for the SSH connection, if applicable.
    /// Check if your SSH servers uses a password. Some uses a private key with passphrase without a password,
    /// in that case you should use leave this empty and use the PrivateKeyPath and PrivateKeyPassphrase properties instead.
    /// It's also possible to use both a password and a private key + passphrase.
    /// </summary>
    public string Password { get; set; }

    /// <summary>
    /// Gets or sets the path to the private key file for the SSH connection, if applicable.
    /// If both <c>PrivateKeyPath</c> and <c>PrivateKeyBytes</c> are set, <c>PrivateKeyPath</c> will be used.
    /// </summary>
    public string PrivateKeyPath { get; set; }

    /// <summary>
    /// Gets or sets the private key bytes for the SSH connection, if applicable.
    /// If both <c>PrivateKeyPath</c> and <c>PrivateKeyBytes</c> are set, <c>PrivateKeyPath</c> will be used.
    /// </summary>
    public byte[] PrivateKeyBytes { get; set; }

    /// <summary>
    /// Gets or sets the passphrase for the private key file, if applicable.
    /// </summary>
    public string PrivateKeyPassphrase { get; set; }

    /// <summary>
    /// Gets or sets the host key fingerprint to expect from the SSH server. This can be left empty to not validate the host fingerprint,
    /// but that is not recommended as it can be a security risk. If you don't validate this, then man-in-the-middle attacks are possible.
    /// You can get the fingerprint from the administrator of the host, or by connecting to the host and reading the fingerprint from the connection.
    /// The fingerprint is a SHA256 hash and is always the same for each host.
    /// </summary>
    public string ExpectedFingerPrint { get; set; }
}