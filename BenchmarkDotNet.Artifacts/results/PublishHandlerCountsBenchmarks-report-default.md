
BenchmarkDotNet v0.13.12, Windows 11 (10.0.26100.4946)
Intel Core Ultra 7 155H, 1 CPU, 22 logical and 16 physical cores
.NET SDK 9.0.100
  [Host]     : .NET 8.0.11 (8.0.1124.51707), X64 RyuJIT AVX2
  DefaultJob : .NET 8.0.11 (8.0.1124.51707), X64 RyuJIT AVX2


 Method                          | HandlerCount | Mean     | Error    | StdDev   | Ratio | Gen0   | Allocated | Alloc Ratio |
-------------------------------- |------------- |---------:|---------:|---------:|------:|-------:|----------:|------------:|
 **'ConduitR.Publish (N handlers)'** | **2**            | **399.9 ns** |  **5.58 ns** |  **4.95 ns** |  **1.00** | **0.0439** |     **552 B** |        **1.00** |
 'MediatR.Publish (N handlers)'  | 2            | 100.8 ns |  2.01 ns |  3.13 ns |  0.26 | 0.0395 |     496 B |        0.90 |
                                 |              |          |          |          |       |        |           |             |
 **'ConduitR.Publish (N handlers)'** | **5**            | **640.7 ns** | **12.67 ns** | **11.85 ns** |  **1.00** | **0.0696** |     **880 B** |        **1.00** |
 'MediatR.Publish (N handlers)'  | 5            | 157.1 ns |  2.91 ns |  2.72 ns |  0.25 | 0.0682 |     856 B |        0.97 |
                                 |              |          |          |          |       |        |           |             |
 **'ConduitR.Publish (N handlers)'** | **10**           | **999.3 ns** | **19.79 ns** | **31.40 ns** |  **1.00** | **0.1125** |    **1432 B** |        **1.00** |
 'MediatR.Publish (N handlers)'  | 10           | 254.1 ns |  3.23 ns |  3.02 ns |  0.25 | 0.1159 |    1456 B |        1.02 |
