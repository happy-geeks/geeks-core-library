using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;

namespace GeeksCoreLibrary.Modules.Templates.Services;

public class NullView : IView
{
    public static readonly NullView Instance = new();
    public string Path => String.Empty;
    public Task RenderAsync(ViewContext context)
    {
        if (context == null) { throw new ArgumentNullException(nameof(context)); }
        return Task.CompletedTask;
    }
}