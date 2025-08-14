
BenchmarkDotNet v0.13.12, Windows 11 (10.0.26100.4946)
Intel Core Ultra 7 155H, 1 CPU, 22 logical and 16 physical cores
.NET SDK 9.0.100
  [Host]     : .NET 8.0.11 (8.0.1124.51707), X64 RyuJIT AVX2
  DefaultJob : .NET 8.0.11 (8.0.1124.51707), X64 RyuJIT AVX2


 Method                            | Count | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
---------------------------------- |------ |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
 **'ConduitR.CreateStream consume N'** | **16**    |  **14.57 μs** |  **0.169 μs** |  **0.158 μs** |  **1.00** |    **0.00** | **0.0610** |     **795 B** |        **1.00** |
 'MediatR.CreateStream consume N'  | 16    |  21.03 μs |  0.404 μs |  0.358 μs |  1.44 |    0.03 | 0.0610 |    1018 B |        1.28 |
                                   |       |           |           |           |       |         |        |           |             |
 **'ConduitR.CreateStream consume N'** | **256**   | **170.36 μs** |  **2.696 μs** |  **2.522 μs** |  **1.00** |    **0.00** |      **-** |     **832 B** |        **1.00** |
 'MediatR.CreateStream consume N'  | 256   | 212.64 μs |  3.142 μs |  2.939 μs |  1.25 |    0.03 |      - |    1032 B |        1.24 |
                                   |       |           |           |           |       |         |        |           |             |
 **'ConduitR.CreateStream consume N'** | **1024**  | **660.45 μs** | **13.073 μs** | **23.904 μs** |  **1.00** |    **0.00** |      **-** |     **833 B** |        **1.00** |
 'MediatR.CreateStream consume N'  | 1024  | 788.72 μs | 10.173 μs |  9.516 μs |  1.20 |    0.04 |      - |    1033 B |        1.24 |
