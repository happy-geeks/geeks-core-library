using System;
using GeeksCoreLibrary.Modules.Ftps.Enums;
using GeeksCoreLibrary.Modules.Ftps.Interfaces;
using GeeksCoreLibrary.Modules.Ftps.Handlers;
using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace GeeksCoreLibrary.Modules.Ftps.Factories;

public class FtpHandlerFactory : IFtpHandlerFactory, IScopedService
{
    private readonly IServiceProvider serviceProvider;

    public FtpHandlerFactory(IServiceProvider serviceProvider)
    {
        this.serviceProvider = serviceProvider;
    }
    
    public IFtpHandler GetFtpHandler(FtpTypes ftpType)
    {
        switch (ftpType)
        {
            case FtpTypes.Ftps:
                return serviceProvider.GetRequiredService<FtpsHandler>();
            case FtpTypes.Sftp:
                return serviceProvider.GetRequiredService<SftpHandler>();
            default:
                throw new NotImplementedException($"FTP type '{ftpType}' is not yet implemented.");
        }
    }
}