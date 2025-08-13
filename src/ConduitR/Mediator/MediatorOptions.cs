namespace ConduitR;

public enum PublishStrategy
{
    Parallel = 0,
    Sequential = 1,
    StopOnFirstException = 2
}

public sealed class MediatorOptions
{
    public PublishStrategy PublishStrategy { get; init; } = PublishStrategy.Parallel;
}
