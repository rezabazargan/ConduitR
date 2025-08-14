
BenchmarkDotNet v0.13.12, Windows 11 (10.0.26100.4946)
Intel Core Ultra 7 155H, 1 CPU, 22 logical and 16 physical cores
.NET SDK 9.0.100
  [Host]     : .NET 8.0.11 (8.0.1124.51707), X64 RyuJIT AVX2
  DefaultJob : .NET 8.0.11 (8.0.1124.51707), X64 RyuJIT AVX2


 Method                      | BehaviorCount | Mean     | Error   | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
---------------------------- |-------------- |---------:|--------:|---------:|------:|--------:|-------:|----------:|------------:|
 **'ConduitR.Send + behaviors'** | **0**             | **314.1 ns** | **6.23 ns** | **12.29 ns** |  **1.00** |    **0.00** | **0.0381** |     **480 B** |        **1.00** |
 'MediatR.Send + behaviors'  | 0             | 404.6 ns | 7.91 ns | 10.56 ns |  1.27 |    0.05 | 0.1125 |    1416 B |        2.95 |
                             |               |          |         |          |       |         |        |           |             |
 **'ConduitR.Send + behaviors'** | **1**             | **338.7 ns** | **5.66 ns** |  **5.56 ns** |  **1.00** |    **0.00** | **0.0534** |     **672 B** |        **1.00** |
 'MediatR.Send + behaviors'  | 1             | 423.2 ns | 8.48 ns | 16.34 ns |  1.27 |    0.06 | 0.1240 |    1560 B |        2.32 |
                             |               |          |         |          |       |         |        |           |             |
 **'ConduitR.Send + behaviors'** | **2**             | **367.8 ns** | **4.65 ns** |  **4.35 ns** |  **1.00** |    **0.00** | **0.0644** |     **808 B** |        **1.00** |
 'MediatR.Send + behaviors'  | 2             | 450.5 ns | 8.84 ns |  7.83 ns |  1.23 |    0.03 | 0.1354 |    1704 B |        2.11 |
