using System.Threading.Tasks;
using DocumentFormat.OpenXml.Wordprocessing;
using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;
using GeeksCoreLibrary.Modules.Databases.Interfaces;

namespace GeeksCoreLibrary.Modules.OAuth;

public class OAuthService : IOAuthService, IScopedService
{
    private readonly IDatabaseConnection databaseConnection;

    public OAuthService(IDatabaseConnection databaseConnection)
    {
        this.databaseConnection = databaseConnection;
    }

    public async Task<int> HandleCallbackAsync(string code)
    {
        databaseConnection.AddParameter("authorization_code", code);
        var result = await databaseConnection.ExecuteAsync($"""
              UPDATE easy_objects 
              SET value = ?authorization_code 
              WHERE `key` = 'authorizationCode';

              INSERT INTO easy_objects (`key`, `value`) 
                SELECT 'authorizationCode', ?authorization_code 
                WHERE NOT EXISTS (SELECT 1 FROM easy_objects WHERE `key` = 'authorizationCode');
            """);
        return result;
    }
}