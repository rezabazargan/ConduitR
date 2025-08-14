
BenchmarkDotNet v0.13.12, Windows 11 (10.0.26100.4946)
Intel Core Ultra 7 155H, 1 CPU, 22 logical and 16 physical cores
.NET SDK 9.0.100
  [Host]     : .NET 8.0.11 (8.0.1124.51707), X64 RyuJIT AVX2
  DefaultJob : .NET 8.0.11 (8.0.1124.51707), X64 RyuJIT AVX2


 Method                            | Count | Mean      | Error     | StdDev    | Median    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
---------------------------------- |------ |----------:|----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
 **'ConduitR.CreateStream consume N'** | **16**    |  **13.63 μs** |  **0.358 μs** |  **1.015 μs** |  **13.01 μs** |  **1.00** |    **0.00** | **0.0610** |     **782 B** |        **1.00** |
 'MediatR.CreateStream consume N'  | 16    |  20.44 μs |  0.305 μs |  0.255 μs |  20.54 μs |  1.32 |    0.06 | 0.0610 |    1020 B |        1.30 |
                                   |       |           |           |           |           |       |         |        |           |             |
 **'ConduitR.CreateStream consume N'** | **256**   | **167.45 μs** |  **2.387 μs** |  **2.233 μs** | **168.17 μs** |  **1.00** |    **0.00** |      **-** |     **832 B** |        **1.00** |
 'MediatR.CreateStream consume N'  | 256   | 208.07 μs |  3.298 μs |  3.798 μs | 206.87 μs |  1.25 |    0.03 |      - |    1032 B |        1.24 |
                                   |       |           |           |           |           |       |         |        |           |             |
 **'ConduitR.CreateStream consume N'** | **1024**  | **647.95 μs** |  **7.014 μs** |  **6.561 μs** | **647.04 μs** |  **1.00** |    **0.00** |      **-** |     **833 B** |        **1.00** |
 'MediatR.CreateStream consume N'  | 1024  | 815.53 μs | 13.535 μs | 12.660 μs | 813.13 μs |  1.26 |    0.03 |      - |    1033 B |        1.24 |
