using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;
using GeeksCoreLibrary.Modules.Payments.Enums;
using GeeksCoreLibrary.Modules.Payments.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;

namespace GeeksCoreLibrary.Modules.Payments.Services
{
    public class PaymentServiceProviderServiceFactory : IPaymentServiceProviderServiceFactory, IScopedService
    {
        private readonly IServiceProvider serviceProvider;

        public PaymentServiceProviderServiceFactory(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }

        public IPaymentServiceProviderService GetPaymentServiceProviderService(PaymentServiceProviders paymentServiceProvider)
        {
            var paymentServiceProviderName = paymentServiceProvider.ToString("G");
            return GetPaymentServiceProviderService(paymentServiceProviderName);
        }

        private IPaymentServiceProviderService GetPaymentServiceProviderService(string paymentServiceProviderName)
        {
            var serviceProviderType = FindTypeInLoadedAssemblies(paymentServiceProviderName);
            if (serviceProviderType == null)
            {
                throw new ArgumentOutOfRangeException(nameof(paymentServiceProviderName), paymentServiceProviderName, $"A payment service provider with the name '{paymentServiceProviderName}Service' was not found.");
            }

            return (IPaymentServiceProviderService)serviceProvider.GetRequiredService(serviceProviderType);
        }

        private Type FindTypeInLoadedAssemblies(string paymentServiceProviderName)
        {
            var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies().Where(assembly => assembly.FullName!.StartsWith("GeeksCoreLibrary"));
            foreach (var assembly in loadedAssemblies)
            {
                var serviceProviderTypes = assembly.GetTypes().Where(type => type.GetInterfaces().Contains(typeof(IPaymentServiceProviderService)));
                var serviceProviderType = serviceProviderTypes.FirstOrDefault(type => type.Name.Equals($"{paymentServiceProviderName}Service", StringComparison.OrdinalIgnoreCase));
                if (serviceProviderType != null)
                {
                    return serviceProviderType;
                }
            }

            return null;
        }
    }
}