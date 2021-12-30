using GeeksCoreLibrary.Modules.Payments.Enums;

namespace GeeksCoreLibrary.Modules.Payments.Interfaces
{
    public interface IPaymentServiceProviderServiceFactory
    {
        IPaymentServiceProviderService GetPaymentServiceProviderService(PaymentServiceProviders paymentServiceProvider);
    }
}