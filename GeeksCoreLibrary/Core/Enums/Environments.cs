using System;

namespace GeeksCoreLibrary.Core.Enums;

[Flags]
public enum Environments
{
    Hidden = 0,
    Development = 1,
    Test = 2,
    Acceptance = 4,
    Live = 8
}