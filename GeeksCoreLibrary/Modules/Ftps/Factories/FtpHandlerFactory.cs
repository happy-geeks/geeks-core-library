using System;
using GeeksCoreLibrary.Modules.Ftps.Enums;
using GeeksCoreLibrary.Modules.Ftps.Interfaces;
using GeeksCoreLibrary.Modules.Ftps.Handlers;
using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace GeeksCoreLibrary.Modules.Ftps.Factories;

public class FtpHandlerFactory(IServiceProvider serviceProvider) : IFtpHandlerFactory, IScopedService
{
    public IFtpHandler GetFtpHandler(FtpTypes ftpType)
    {
        return ftpType switch
        {
            FtpTypes.Ftps => serviceProvider.GetRequiredService<FtpsHandler>(),
            FtpTypes.Sftp => serviceProvider.GetRequiredService<SftpHandler>(),
            _ => throw new ArgumentOutOfRangeException(nameof(ftpType), ftpType.ToString(), null)
        };
    }
}