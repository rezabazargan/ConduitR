
BenchmarkDotNet v0.13.12, Windows 11 (10.0.26100.4946)
Intel Core Ultra 7 155H, 1 CPU, 22 logical and 16 physical cores
.NET SDK 9.0.100
  [Host]     : .NET 8.0.11 (8.0.1124.51707), X64 RyuJIT AVX2
  DefaultJob : .NET 8.0.11 (8.0.1124.51707), X64 RyuJIT AVX2


 Method                          | Mean      | Error    | StdDev    | Ratio | Gen0   | Allocated | Alloc Ratio |
-------------------------------- |----------:|---------:|----------:|------:|-------:|----------:|------------:|
 'ConduitR.Publish (2 handlers)' | 338.45 ns | 6.720 ns | 10.851 ns |  1.00 | 0.0372 |     472 B |        1.00 |
 'MediatR.Publish (2 handlers)'  |  87.87 ns | 1.476 ns |  1.309 ns |  0.26 | 0.0299 |     376 B |        0.80 |
