
BenchmarkDotNet v0.13.12, Windows 11 (10.0.26100.4946)
Intel Core Ultra 7 155H, 1 CPU, 22 logical and 16 physical cores
.NET SDK 9.0.100
  [Host]     : .NET 8.0.11 (8.0.1124.51707), X64 RyuJIT AVX2
  DefaultJob : .NET 8.0.11 (8.0.1124.51707), X64 RyuJIT AVX2


 Method                          | HandlerCount | Mean     | Error   | StdDev   | Ratio | Gen0   | Allocated | Alloc Ratio |
-------------------------------- |------------- |---------:|--------:|---------:|------:|-------:|----------:|------------:|
 **'ConduitR.Publish (N handlers)'** | **2**            | **323.0 ns** | **6.30 ns** |  **8.63 ns** |  **1.00** | **0.0291** |     **368 B** |        **1.00** |
 'MediatR.Publish (N handlers)'  | 2            | 103.4 ns | 1.50 ns |  1.33 ns |  0.32 | 0.0395 |     496 B |        1.35 |
                                 |              |          |         |          |       |        |           |             |
 **'ConduitR.Publish (N handlers)'** | **5**            | **370.2 ns** | **7.29 ns** | **11.35 ns** |  **1.00** | **0.0439** |     **552 B** |        **1.00** |
 'MediatR.Publish (N handlers)'  | 5            | 148.8 ns | 2.76 ns |  2.44 ns |  0.40 | 0.0682 |     856 B |        1.55 |
                                 |              |          |         |          |       |        |           |             |
 **'ConduitR.Publish (N handlers)'** | **10**           | **434.9 ns** | **8.32 ns** |  **7.79 ns** |  **1.00** | **0.0687** |     **864 B** |        **1.00** |
 'MediatR.Publish (N handlers)'  | 10           | 249.1 ns | 3.89 ns |  3.24 ns |  0.57 | 0.1159 |    1456 B |        1.69 |
