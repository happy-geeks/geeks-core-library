using System.Threading.Tasks;
using GeeksCoreLibrary.Modules.Databases.Interfaces;
using GeeksCoreLibrary.Modules.Payments.Enums;
using GeeksCoreLibrary.Modules.Payments.Models;

namespace GeeksCoreLibrary.Modules.Payments.Helpers
{
    public static class LoggingHelpers
    {
        public static async Task CreateLogTableIfMissingAsync(IDatabaseConnection databaseConnection)
        {
            await databaseConnection.ExecuteAsync($@"
                CREATE TABLE IF NOT EXISTS `{Constants.PaymentServiceProviderLogTableName}` (
                    `id` bigint UNSIGNED NOT NULL AUTO_INCREMENT,
                    `added_on` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    `payment_service_provider` varchar(50) NOT NULL DEFAULT '',
                    `unique_payment_number` varchar(100) NOT NULL DEFAULT '',
                    `status` int NOT NULL DEFAULT 0,
                    `request_headers` text NULL,
                    `request_query_string` text NULL,
                    `request_form_values` text NULL,
                    `request_body` text NULL,
                    `response_body` text NULL,
                    `error` text NULL,
                    PRIMARY KEY (`id`)
                );");
        }

        public static async Task<bool> AddLogEntryAsync(IDatabaseConnection databaseConnection, PaymentServiceProviders paymentServiceProvider, string uniquePaymentNumber = "", int status = 0, string requestHeaders = "", string requestQueryString = "", string requestFormValues = "", string requestBody = "", string responseBody = "", string error = "")
        {
            await CreateLogTableIfMissingAsync(databaseConnection);

            databaseConnection.ClearParameters();
            databaseConnection.AddParameter("payment_service_provider", paymentServiceProvider.ToString("G"));
            databaseConnection.AddParameter("unique_payment_number", uniquePaymentNumber);
            databaseConnection.AddParameter("status", status);
            databaseConnection.AddParameter("request_headers", requestHeaders);
            databaseConnection.AddParameter("request_query_string", requestQueryString);
            databaseConnection.AddParameter("request_form_values", requestFormValues);
            databaseConnection.AddParameter("request_body", requestBody);
            databaseConnection.AddParameter("response_body", responseBody);
            databaseConnection.AddParameter("error", error);

            return await databaseConnection.InsertOrUpdateRecordBasedOnParametersAsync(Constants.PaymentServiceProviderLogTableName, 0UL) > 0;
        }
    }
}
