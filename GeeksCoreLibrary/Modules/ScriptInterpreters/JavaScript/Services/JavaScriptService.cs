using System;
using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;
using GeeksCoreLibrary.Core.Extensions;
using GeeksCoreLibrary.Modules.ScriptInterpreters.JavaScript.Interfaces;
using Jint;
using Microsoft.Extensions.Logging;

namespace GeeksCoreLibrary.Modules.ScriptInterpreters.JavaScript.Services;

/// <summary>
/// A service for interpreting JavaScript.
/// </summary>
public class JavaScriptService : IJavaScriptService, ITransientService
{
    private readonly ILogger<JavaScriptService> logger;

    private Engine engine;

    public JavaScriptService(ILogger<JavaScriptService> logger)
    {
        this.logger = logger;

        engine = new Engine(new Options
        {
            Strict = true,
            Host =
            {
                StringCompilationAllowed = false
            }
        });

        // Add GCL methods to the state.
        var gcl = new
        {
            ConvertToSeo = new Func<string, string>(StringExtensions.ConvertToSeo),
            EncryptWithAes = new Func<string, string, bool, bool, string>(StringExtensions.EncryptWithAes),
            DecryptWithAes = new Func<string, string, bool, int, bool, string>(StringExtensions.DecryptWithAes),
            ToSha512 = new Func<string, string>(StringExtensions.ToSha512Simple),
            ToSha512ForPasswords = new Func<string, byte[], string>(StringExtensions.ToSha512ForPasswords)
        };

        // Expose the GCL methods to the engine.
        engine.SetValue("GCL", gcl);
    }

    /// <inheritdoc />
    public void SetValue(string name, object value)
    {
        engine.SetValue(name, value);
    }

    /// <inheritdoc />
    public object GetValue(string name)
    {
        return engine.GetValue(name).ToObject();
    }

    /// <inheritdoc />
    public T GetValue<T>(string name)
    {
        return (T) Convert.ChangeType(engine.GetValue(name).ToObject(), typeof(T));
    }

    /// <inheritdoc />
    public void Execute(string script)
    {
        engine.Execute(script);
    }

    /// <inheritdoc />
    public object Evaluate(string script)
    {
        return engine.Evaluate(script).ToObject();
    }

    /// <inheritdoc />
    public T Evaluate<T>(string script)
    {
        object result = null;

        try
        {
            result = Evaluate(script);
            if (result is Newtonsoft.Json.Linq.JToken jToken)
            {
                return jToken.ToObject<T>();
            }

            return result is not IConvertible ? default : (T) Convert.ChangeType(result, typeof(T));
        }
        catch (Exception exception) when (exception is InvalidCastException or FormatException)
        {
            logger.LogError(exception, "An error occurred while casting the result of a JavaScript snippet. Tried to cast '{Result}' to type '{GenericType}'", result ?? "null", typeof(T));
            return default;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "An error occurred while executing a JavaScript snippet.");
            return default;
        }
    }

    /// <inheritdoc />
    public object Invoke(string functionName, params object[] arguments)
    {
        return engine.Invoke(functionName, arguments).ToObject();
    }

    /// <inheritdoc />
    public T Invoke<T>(string functionName, params object[] arguments)
    {
        object result = null;

        try
        {
            result = Invoke(functionName, arguments);
            return result is not IConvertible ? default : (T) Convert.ChangeType(result, typeof(T));
        }
        catch (Exception exception) when (exception is InvalidCastException or FormatException)
        {
            logger.LogError(exception, "An error occurred while casting the result of a JavaScript function. Tried to cast '{Result}' to type '{GenericType}'", result ?? "null", typeof(T));
            return default;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "An error occurred while executing a JavaScript function.");
            return default;
        }
    }

    #region Disposing

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposing) return;

        engine?.Dispose();
        engine = null;
    }

    #endregion
}