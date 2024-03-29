using System;
using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;
using GeeksCoreLibrary.Core.Extensions;
using GeeksCoreLibrary.Modules.ScriptInterpreters.Interfaces;
using GeeksCoreLibrary.Modules.ScriptInterpreters.JavaScript.Interfaces;
using Jint;

namespace GeeksCoreLibrary.Modules.ScriptInterpreters.JavaScript.Services;

public class JavaScriptService : IScriptInterpretersService, IJavaScriptService, ITransientService
{
    private Engine engine;

    public JavaScriptService()
    {
        engine = new Engine(new Options
        {
            Strict = true,
            StringCompilationAllowed = false
        });

        // Add GCL methods to the state.
        var gcl = new
        {
            ConvertToSeo = new Func<string, string>(StringExtensions.ConvertToSeo)
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
        return (T)Convert.ChangeType(engine.GetValue(name).ToObject(), typeof(T));
    }

    /// <inheritdoc />
    public object ExecuteScript(string script)
    {
        return engine.Execute(script);
    }

    /// <inheritdoc />
    public T ExecuteScript<T>(string script)
    {
        return default;
    }

    /// <inheritdoc />
    public object ExecuteFunction(string functionName, params object[] arguments)
    {
        var result = engine.Invoke(functionName, arguments);
        return result is Jint.Native.JsUndefined or Jint.Native.JsNull ? null : result.ToObject();
    }

    /// <inheritdoc />
    public T ExecuteFunction<T>(string functionName, params object[] arguments)
    {
        var result = engine.Invoke(functionName, arguments);
        return result is Jint.Native.JsUndefined or Jint.Native.JsNull
            ? default
            : (T)Convert.ChangeType(result.ToObject(), typeof(T));
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