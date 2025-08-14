```

BenchmarkDotNet v0.13.12, Windows 11 (10.0.26100.4946)
Intel Core Ultra 7 155H, 1 CPU, 22 logical and 16 physical cores
.NET SDK 9.0.100
  [Host]     : .NET 8.0.11 (8.0.1124.51707), X64 RyuJIT AVX2
  DefaultJob : .NET 8.0.11 (8.0.1124.51707), X64 RyuJIT AVX2


```
| Method                          | Mean      | Error    | StdDev    | Median    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|-------------------------------- |----------:|---------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| &#39;ConduitR.Publish (2 handlers)&#39; | 324.44 ns | 6.830 ns | 18.696 ns | 318.48 ns |  1.00 |    0.00 | 0.0267 |     336 B |        1.00 |
| &#39;MediatR.Publish (2 handlers)&#39;  |  89.02 ns | 1.362 ns |  1.274 ns |  88.87 ns |  0.26 |    0.02 | 0.0299 |     376 B |        1.12 |
