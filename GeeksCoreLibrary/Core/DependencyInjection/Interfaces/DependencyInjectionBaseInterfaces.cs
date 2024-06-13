namespace GeeksCoreLibrary.Core.DependencyInjection.Interfaces;

/// <summary>
/// Use this for services that should be recreated every time they are injected.
/// </summary>
public interface ITransientService {}

/// <summary>
/// Use this for services that should be created once per request.
/// </summary>
public interface IScopedService {}

/// <summary>
/// Use this for services that should be created only once during the life time of the application.
/// </summary>
public interface ISingletonService {}