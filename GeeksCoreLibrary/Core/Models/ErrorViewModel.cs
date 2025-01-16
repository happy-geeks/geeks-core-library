using System;

namespace GeeksCoreLibrary.Core.Models;

public class ErrorViewModel
{
    public string RequestId { get; set; }

    public bool ShowRequestId => !String.IsNullOrEmpty(RequestId);
}