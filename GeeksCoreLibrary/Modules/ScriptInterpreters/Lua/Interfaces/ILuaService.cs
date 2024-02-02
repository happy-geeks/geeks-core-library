using System;
using NLua;

namespace GeeksCoreLibrary.Modules.ScriptInterpreters.Lua.Interfaces;

public interface ILuaService : IDisposable
{
    /// <summary>
    /// Create a function from a script.
    /// </summary>
    /// <param name="script">The Lua script that will create the function.</param>
    /// <param name="functionName">The name of the function that the script will create.</param>
    /// <returns>The <see cref="LuaFunction"/> that was created.</returns>
    /// <exception cref="ArgumentNullException">If either <paramref name="script"/> or <paramref name="functionName"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">If the name passed in <paramref name="functionName"/> can not be found in the script.</exception>
    LuaFunction CreateFunction(string script, string functionName);

    LuaFunction GetFunction(string functionName);

    object[] CallFunction(LuaFunction function, params object[] args);

    object[] CallFunction(string functionName, params object[] args);
}