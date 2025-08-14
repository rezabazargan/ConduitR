
BenchmarkDotNet v0.13.12, Windows 11 (10.0.26100.4946)
Intel Core Ultra 7 155H, 1 CPU, 22 logical and 16 physical cores
.NET SDK 9.0.100
  [Host]     : .NET 8.0.11 (8.0.1124.51707), X64 RyuJIT AVX2
  DefaultJob : .NET 8.0.11 (8.0.1124.51707), X64 RyuJIT AVX2


 Method        | Mean     | Error   | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
-------------- |---------:|--------:|---------:|------:|--------:|-------:|----------:|------------:|
 ConduitR.Send | 315.5 ns | 6.27 ns |  7.93 ns |  1.00 |    0.00 | 0.0381 |     480 B |        1.00 |
 MediatR.Send  | 379.7 ns | 7.55 ns | 11.52 ns |  1.20 |    0.05 | 0.1125 |    1416 B |        2.95 |
