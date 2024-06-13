namespace GeeksCoreLibrary.Core.DependencyInjection.Interfaces;

internal interface IServiceLocator
{
    T Get<T>();
}