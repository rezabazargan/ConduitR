```

BenchmarkDotNet v0.13.12, Windows 11 (10.0.26100.4946)
Intel Core Ultra 7 155H, 1 CPU, 22 logical and 16 physical cores
.NET SDK 9.0.100
  [Host]     : .NET 8.0.11 (8.0.1124.51707), X64 RyuJIT AVX2
  DefaultJob : .NET 8.0.11 (8.0.1124.51707), X64 RyuJIT AVX2


```
| Method         | Mean     | Error    | StdDev   | Median   | Gen0   | Allocated |
|--------------- |---------:|---------:|---------:|---------:|-------:|----------:|
| Send_WithCache | 504.6 ns | 19.70 ns | 56.83 ns | 480.4 ns | 0.0305 |     384 B |
