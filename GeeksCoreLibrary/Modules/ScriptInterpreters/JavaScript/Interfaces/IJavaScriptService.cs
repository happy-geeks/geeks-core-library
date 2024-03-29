using System;

namespace GeeksCoreLibrary.Modules.ScriptInterpreters.JavaScript.Interfaces;

public interface IJavaScriptService : IDisposable
{
    /// <summary>
    /// Set a value in the JavaScript engine.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="value"></param>
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
    /// Execute a script and return the result.
    /// </summary>
    /// <param name="script">The script to execute.</param>
    /// <returns>An <see cref="object[]"/> containing the result of the script.</returns>
    object ExecuteScript(string script);

    /// <summary>
    /// Execute a script and return the result.
    /// </summary>
    /// <param name="script">The script to execute.</param>
    /// <returns>A <see cref="T"/> containing the result of the script.</returns>
    T ExecuteScript<T>(string script);

    /// <summary>
    /// Execute a function with the given name and arguments.
    /// </summary>
    /// <param name="functionName">The name of the function.</param>
    /// <param name="arguments">The arguments to pass to the function.</param>
    /// <returns>An <see cref="object"/> representing the result of the function. The returned value will return null if the function wasn't found or if the function returned undefined or null.</returns>
    object ExecuteFunction(string functionName, params object[] arguments);

    /// <summary>
    /// Execute a function with the given name and arguments.
    /// </summary>
    /// <param name="functionName">The name of the function.</param>
    /// <param name="arguments">The arguments to pass to the function.</param>
    /// <returns>A <see cref="T"/> representing the result of the function. The returned value will return null if the function wasn't found or if the function returned undefined or null.</returns>
    T ExecuteFunction<T>(string functionName, params object[] arguments);
}