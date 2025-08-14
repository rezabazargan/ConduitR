
BenchmarkDotNet v0.13.12, Windows 11 (10.0.26100.4946)
Intel Core Ultra 7 155H, 1 CPU, 22 logical and 16 physical cores
.NET SDK 9.0.100
  [Host]     : .NET 8.0.11 (8.0.1124.51707), X64 RyuJIT AVX2
  DefaultJob : .NET 8.0.11 (8.0.1124.51707), X64 RyuJIT AVX2


 Method                             | Mean     | Error    | StdDev   | Median   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
----------------------------------- |---------:|---------:|---------:|---------:|------:|--------:|-------:|----------:|------------:|
 'ConduitR.CreateStream consume 16' | 14.56 μs | 0.282 μs | 0.302 μs | 14.53 μs |  1.00 |    0.00 | 0.0610 |     791 B |        1.00 |
 'MediatR.CreateStream consume 16'  | 20.85 μs | 0.412 μs | 0.822 μs | 20.44 μs |  1.44 |    0.07 | 0.0610 |    1019 B |        1.29 |
