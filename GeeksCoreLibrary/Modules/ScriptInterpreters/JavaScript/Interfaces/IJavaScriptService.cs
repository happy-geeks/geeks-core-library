using System;

namespace GeeksCoreLibrary.Modules.ScriptInterpreters.JavaScript.Interfaces;

/// <summary>
/// Interface for a JavaScript interpreter service.
/// </summary>
public interface IJavaScriptService : IDisposable
{
    /// <summary>
    /// Set a value in the JavaScript engine.
    /// </summary>
    /// <param name="name">The name of the global object.</param>
    /// <param name="value">The value of the global object.</param>
    void SetValue(string name, object value);

    /// <summary>
    /// Get a value from the JavaScript engine.
    /// </summary>
    /// <param name="name">The name of the JavaScript object to get the value of.</param>
    /// <returns>The value of the object.</returns>
    object GetValue(string name);

    /// <summary>
    /// Get a value from the JavaScript engine.
    /// </summary>
    /// <param name="name">The name of the JavaScript object to get the value of.</param>
    /// <returns>The value of the object.</returns>
    T GetValue<T>(string name);

    /// <summary>
    /// Execute a script statement. Use this to, for example, create a function which can later be invoked using the <see cref="Invoke{T}(string, object[])"/> method.
    /// </summary>
    /// <param name="script">The script to execute.</param>
    void Execute(string script);

    /// <summary>
    /// Evaluate a script statement and return the result.
    /// </summary>
    /// <param name="script">The script to execute.</param>
    /// <returns>An <see cref="Object"/> containing the result of the script.</returns>
    object Evaluate(string script);

    /// <summary>
    /// Evaluate a script statement and return the result. If the result is null or does not implement the <see cref="IConvertible"/> interface, it will return the default value of <see cref="T"/>.
    /// </summary>
    /// <param name="script">The script to execute.</param>
    /// <returns>A <see cref="T"/> containing the result of the script.</returns>
    T Evaluate<T>(string script);

    /// <summary>
    /// Invoke a function with the given name and arguments.
    /// </summary>
    /// <param name="functionName">The name of the function.</param>
    /// <param name="arguments">The arguments to pass to the function.</param>
    /// <returns>An <see cref="Object"/> representing the result of the function. The returned value will return null if the function wasn't found or if the function returned undefined or null.</returns>
    object Invoke(string functionName, params object[] arguments);

    /// <summary>
    /// Invoke a function with the given name and arguments.
    /// </summary>
    /// <param name="functionName">The name of the function.</param>
    /// <param name="arguments">The arguments to pass to the function.</param>
    /// <returns>A <see cref="T"/> representing the result of the function. The returned value will return null if the function wasn't found or if the function returned undefined or null.</returns>
    T Invoke<T>(string functionName, params object[] arguments);
}