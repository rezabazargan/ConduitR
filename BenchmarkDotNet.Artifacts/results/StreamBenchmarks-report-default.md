
BenchmarkDotNet v0.13.12, Windows 11 (10.0.26100.4946)
Intel Core Ultra 7 155H, 1 CPU, 22 logical and 16 physical cores
.NET SDK 9.0.100
  [Host]     : .NET 8.0.11 (8.0.1124.51707), X64 RyuJIT AVX2
  DefaultJob : .NET 8.0.11 (8.0.1124.51707), X64 RyuJIT AVX2


 Method                             | Mean     | Error    | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
----------------------------------- |---------:|---------:|---------:|------:|--------:|-------:|----------:|------------:|
 'ConduitR.CreateStream consume 16' | 18.40 μs | 0.404 μs | 1.192 μs |  1.00 |    0.00 | 0.0610 |     801 B |        1.00 |
 'MediatR.CreateStream consume 16'  | 28.45 μs | 0.568 μs | 1.134 μs |  1.53 |    0.12 | 0.0610 |    1027 B |        1.28 |
