using System;
using GeeksCoreLibrary.Modules.Ftps.Enums;
using FluentFTP;

namespace GeeksCoreLibrary.Modules.Ftps.Extensions;

public static class EncryptionModesExtensions
{
    public static FtpEncryptionMode ConvertToFtpsEncryptionMode(this EncryptionModes encryptionMode)
    {
        if (!Enum.TryParse(encryptionMode.ToString("G"), out FtpEncryptionMode ftpEncryptionMode))
        {
            throw new NotImplementedException($"Encryption mode '{encryptionMode}' is not recognized or not supported.");
        }

        return ftpEncryptionMode;
    }
}