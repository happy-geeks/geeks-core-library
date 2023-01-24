using System;
using GeeksCoreLibrary.Modules.Ftps.Enums;
using FluentFTP;

namespace GeeksCoreLibrary.Modules.Ftps.Extensions;

public static class EncryptionModesExtensions
{
    public static FtpEncryptionMode ConvertToFtpsEncryptionMode(this EncryptionModes encryptionMode)
    {
        switch (encryptionMode)
        {
            case EncryptionModes.Auto:
                return FtpEncryptionMode.Auto;
            case EncryptionModes.None:
                return FtpEncryptionMode.None;
            case EncryptionModes.Implicit:
                return FtpEncryptionMode.Implicit;
            case EncryptionModes.Explicit:
                return FtpEncryptionMode.Explicit;
            default:
                throw new NotImplementedException($"Encryption mode '{encryptionMode}' is not yet implemented to convert to FTPS.");
        }
    }
}