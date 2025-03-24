using System;

namespace GeeksCoreLibrary.Modules.GclReplacements.Models;

public ref struct IfStatementParts()
{
    public ReadOnlySpan<char> Operator { get; set; } = default;
    public ReadOnlySpan<char> LeftOperand { get; set; } = default;
    public ReadOnlySpan<char> RightOperand { get; set; } = default;
    public ReadOnlySpan<char> TrueBranchValue { get; set; } = default;
    public ReadOnlySpan<char> FalseBranchValue { get; set; } = default;
    public int ScannedUntil { get; set; } = 0;
    public int NextFoundTrueBranchIfIndex { get; set; } = -1;
    public int NextFoundFalseBranchIfIndex { get; set; } = -1;
}