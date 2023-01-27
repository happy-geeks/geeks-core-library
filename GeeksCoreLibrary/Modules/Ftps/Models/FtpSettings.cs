using GeeksCoreLibrary.Modules.Ftps.Enums;

namespace GeeksCoreLibrary.Modules.Ftps.Models;

public class FtpSettings
{
    /// <summary>
    /// Gets or sets the encryption mode.
    /// </summary>
    public EncryptionModes EncryptionMode { get; set; } = EncryptionModes.Auto;

    /// <summary>
    /// Gets or sets the host of the FTP.
    /// </summary>
    public string Host { get; set; }

    /// <summary>
    /// Gets or sets the port of the FTP.
    /// </summary>
    public int Port { get; set; }

    /// <summary>
    /// Gets or sets the user to login with.
    /// </summary>
    public string User { get; set; }

    /// <summary>
    /// Gets or sets the password to login with.
    /// </summary>
    public string Password { get; set; }
    
    /// <summary>
    /// Gets or sets if a passive connection needs to be used.
    /// </summary>
    public bool UsePassive { get; set; } = true;

    /// <summary>
    /// Gets or sets the path to the SSH private key.
    /// </summary>
    public string SshPrivateKeyPath { get; set; }

    /// <summary>
    /// Gets or sets the passphrase for the SSH private key.
    /// </summary>
    public string SshPrivateKeyPassphrase { get; set; }
}