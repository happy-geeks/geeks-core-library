using System;
using System.Text;
using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;
using GeeksCoreLibrary.Core.Extensions;
using GeeksCoreLibrary.Modules.ScriptInterpreters.Interfaces;
using GeeksCoreLibrary.Modules.ScriptInterpreters.Lua.Interfaces;
using NLua;

namespace GeeksCoreLibrary.Modules.ScriptInterpreters.Lua.Services;

public class LuaService : IScriptInterpretersService, ILuaService, ITransientService
{
    private NLua.Lua state;

    public LuaService()
    {
        state = new NLua.Lua();
        state.State.Encoding = Encoding.UTF8;

        // Add the JSON module to the state as a global variable.
        state.DoString("json = require('Modules/ScriptInterpreters/Lua/Modules/json')");

        // Add GCL methods to the state.
        var gcl = new
        {
            ConvertToSeo = new Func<string, string>(StringExtensions.ConvertToSeo)
        };

        state["GCL"] = gcl;
    }

    /// <inheritdoc />
    public NLua.Lua GetState()
    {
        return state;
    }

    /// <inheritdoc />
    public object[] ExecuteScript(string script)
    {
        var result = state.DoString(script);
        return result.Length == 0 ? null : result;
    }

    /// <inheritdoc />
    public T ExecuteScript<T>(string script)
    {
        var result = state.DoString(script);
        if (result.Length == 0) return default;

        return (T)Convert.ChangeType(result[0], typeof(T));
    }

    /// <inheritdoc />
    public LuaFunction CreateFunction(string script, string functionName)
    {
        state.DoString(script);
        if (state[functionName] is not LuaFunction scriptFunction) throw new ArgumentOutOfRangeException(nameof(functionName), $"The function {functionName} could not be found in the script.");

        return scriptFunction;
    }

    /// <inheritdoc />
    public LuaFunction GetFunction(string functionName)
    {
        return state[functionName] as LuaFunction;
    }

    /// <inheritdoc />
    public object[] CallFunction(LuaFunction function, params object[] args)
    {
        if (function == null) throw new ArgumentNullException(nameof(function));

        return function.Call(args);
    }

    /// <inheritdoc />
    public object[] CallFunction(string functionName, params object[] args)
    {
        if (state[functionName] is not LuaFunction scriptFunction) throw new ArgumentOutOfRangeException(nameof(functionName), $"The function {functionName} could not be found in the state.");

        return scriptFunction.Call(args);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposing) return;

        state?.Dispose();
        state = null;
    }
}