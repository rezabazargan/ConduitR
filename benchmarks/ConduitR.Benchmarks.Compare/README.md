# ConduitR vs MediatR Benchmarks

## Run
```bash
dotnet run -c Release --project benchmarks/ConduitR.Benchmarks.Compare/ConduitR.Benchmarks.Compare.csproj
```

Results (CSV + Markdown) are saved under:
```
benchmarks/ConduitR.Benchmarks.Compare/BenchmarkDotNet.Artifacts/results/
```

## Plot charts
```bash
python3 tools/plot_benchmarks.py --repo-root .
```
Images will be written to `docs/images/bench-*.png`.
