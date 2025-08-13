namespace ConduitR.Abstractions;

/// <summary>Represents a void-like unit type for requests without a return value.</summary>
public readonly struct Unit
{
    public static readonly Unit Value = new();
    public override string ToString() => "()";
}
