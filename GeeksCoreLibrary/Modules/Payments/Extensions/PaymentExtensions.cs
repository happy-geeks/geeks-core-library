using GeeksCoreLibrary.Modules.Payments.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace GeeksCoreLibrary.Modules.Payments.Extensions
{
    public static class PaymentExtensions
    {
        public static void UsePaymentFunctions<T>(this IServiceCollection services) where T : IPaymentFunctionsService
        {
            services.Decorate<IPaymentFunctionsService, T>();
        }
    }
}
