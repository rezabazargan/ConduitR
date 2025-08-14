```

BenchmarkDotNet v0.13.12, Windows 11 (10.0.26100.4946)
Intel Core Ultra 7 155H, 1 CPU, 22 logical and 16 physical cores
.NET SDK 9.0.100
  [Host]     : .NET 8.0.11 (8.0.1124.51707), X64 RyuJIT AVX2
  DefaultJob : .NET 8.0.11 (8.0.1124.51707), X64 RyuJIT AVX2


```
| Method                      | BehaviorCount | Mean     | Error   | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|---------------------------- |-------------- |---------:|--------:|---------:|------:|--------:|-------:|----------:|------------:|
| **&#39;ConduitR.Send + behaviors&#39;** | **0**             | **312.2 ns** | **6.26 ns** |  **9.74 ns** |  **1.00** |    **0.00** | **0.0381** |     **480 B** |        **1.00** |
| &#39;MediatR.Send + behaviors&#39;  | 0             | 373.4 ns | 7.46 ns | 16.21 ns |  1.20 |    0.06 | 0.1125 |    1416 B |        2.95 |
|                             |               |          |         |          |       |         |        |           |             |
| **&#39;ConduitR.Send + behaviors&#39;** | **1**             | **336.1 ns** | **6.67 ns** |  **6.24 ns** |  **1.00** |    **0.00** | **0.0534** |     **672 B** |        **1.00** |
| &#39;MediatR.Send + behaviors&#39;  | 1             | 402.2 ns | 8.04 ns | 10.16 ns |  1.20 |    0.04 | 0.1240 |    1560 B |        2.32 |
|                             |               |          |         |          |       |         |        |           |             |
| **&#39;ConduitR.Send + behaviors&#39;** | **2**             | **368.5 ns** | **6.51 ns** |  **6.09 ns** |  **1.00** |    **0.00** | **0.0644** |     **808 B** |        **1.00** |
| &#39;MediatR.Send + behaviors&#39;  | 2             | 405.5 ns | 8.00 ns | 12.93 ns |  1.11 |    0.03 | 0.1354 |    1704 B |        2.11 |
