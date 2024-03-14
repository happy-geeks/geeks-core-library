using System;
using NLua;

namespace GeeksCoreLibrary.Modules.ScriptInterpreters.Lua.Interfaces;

public interface ILuaService : IDisposable
{
    /// <summary>
    /// Access the state of the Lua interpreter.
    /// </summary>
    /// <returns></returns>
    public NLua.Lua GetState();

    /// <summary>
    /// Execute a Lua script and return the result.
    /// </summary>
    /// <param name="script">The Lua script to execute.</param>
    /// <returns>A <see cref="object[]"/> containing the result of the script.</returns>
    public object[] ExecuteScript(string script);

    /// <summary>
    /// Execute a Lua script and return the result.
    /// </summary>
    /// <param name="script">The Lua script to execute.</param>
    /// <returns>A <see cref="T"/> containing the result of the script.</returns>
    public T ExecuteScript<T>(string script);

    /// <summary>
    /// Create a function from a script.
    /// </summary>
    /// <param name="script">The Lua script that will create the function.</param>
    /// <param name="functionName">The name of the function that the script will create.</param>
    /// <returns>The <see cref="LuaFunction"/> that was created.</returns>
    /// <exception cref="ArgumentNullException">If either <paramref name="script"/> or <paramref name="functionName"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">If the name passed in <paramref name="functionName"/> can not be found in the script.</exception>
    LuaFunction CreateFunction(string script, string functionName);

    /// <summary>
    /// Get a function from the state.
    /// </summary>
    LuaFunction GetFunction(string functionName);

    /// <summary>
    /// Call the given <paramref name="function"/> with the given arguments.
    /// </summary>
    object[] CallFunction(LuaFunction function, params object[] args);

    /// <summary>
    /// Call a function by name with the given arguments.
    /// </summary>
    object[] CallFunction(string functionName, params object[] args);
}