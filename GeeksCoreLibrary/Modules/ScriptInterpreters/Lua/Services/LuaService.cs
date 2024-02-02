using System;
using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;
using GeeksCoreLibrary.Modules.ScriptInterpreters.Lua.Interfaces;
using NLua;

namespace GeeksCoreLibrary.Modules.ScriptInterpreters.Lua.Services;

public class LuaService : ILuaService, ITransientService
{
    private NLua.Lua state = new();

    public LuaFunction CreateFunction(string script, string functionName)
    {
        state.DoString(script);
        if (state[functionName] is not LuaFunction scriptFunction) throw new ArgumentOutOfRangeException(nameof(functionName), $"The function {functionName} could not be found in the script.");

        return scriptFunction;
    }

    public LuaFunction GetFunction(string functionName)
    {
        return state[functionName] as LuaFunction;
    }

    public object[] CallFunction(LuaFunction function, params object[] args)
    {
        if (function == null) throw new ArgumentNullException(nameof(function));

        return function.Call(args);
    }

    public object[] CallFunction(string functionName, params object[] args)
    {
        if (state[functionName] is not LuaFunction scriptFunction) throw new ArgumentOutOfRangeException(nameof(functionName), $"The function {functionName} could not be found in the state.");
        return scriptFunction.Call(args);
    }

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