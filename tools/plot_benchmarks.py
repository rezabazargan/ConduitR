import argparse, os, glob, re
import pandas as pd
import matplotlib.pyplot as plt
from datetime import datetime
from pathlib import Path
import pandas as pd

def find_all_csvs(root):
    patterns = [
        os.path.join(root, "benchmarks", "ConduitR.Benchmarks.Compare", "**", "BenchmarkDotNet.Artifacts", "results", "*.csv"),
        os.path.join(root, "BenchmarkDotNet.Artifacts", "results", "*.csv"),
    ]
    files = []
    for p in patterns:
        files.extend(glob.glob(p, recursive=True))
    if not files:
        raise SystemExit("No CSV files found under:\n  " + "\n  ".join(patterns))
    # de-dupe identical paths
    return sorted(set(files), key=os.path.getmtime)

def read_csv(csv_path):
    # auto-detect delimiter
    return pd.read_csv(csv_path, engine="python", sep=None)

def to_number(series):
    def clean(x):
        if isinstance(x, (int, float)): return x
        s = str(x)
        s = re.sub(r"[^0-9eE\\.-]", "", s)
        return float(s) if s else 0.0
    return series.map(clean)

def save_bar(x, y, title, ylabel, out_path):
    plt.figure()
    plt.bar(x, y)       # no custom colors/styles
    plt.ylim(bottom=0)  # start at zero so small bars are visible
    plt.title(title)
    plt.ylabel(ylabel)
    plt.xticks(rotation=30, ha="right")
    plt.tight_layout()
    plt.savefig(out_path, dpi=180)
    print("[plot] Wrote:", out_path)

def base_name_from_csv(path):
    # e.g., SendBenchmarks-report.csv -> SendBenchmarks
    stem = Path(path).stem
    return stem.replace("-report", "")

def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--repo-root", default=".", help="Repo root (outputs go to docs/images)")
    args = ap.parse_args()

    csvs = find_all_csvs(args.repo_root)
    out_dir = os.path.join(args.repo_root, "docs", "images")
    os.makedirs(out_dir, exist_ok=True)

    # Per-file charts + collect for summary
    summary_rows = []
    for csv in csvs:
        df = read_csv(csv)
        if "Method" not in df.columns:
            print("[plot] Skipping (no Method column):", csv)
            continue

        df["Method"] = df["Method"].astype(str)\
            .str.replace("ConduitR_", "ConduitR.", regex=False)\
            .str.replace("MediatR_", "MediatR.", regex=False)

        mean_col = next((c for c in df.columns if c.lower().startswith("mean")), None)
        alloc_col = next((c for c in df.columns if c.lower().startswith("allocated")), None)
        if not mean_col or not alloc_col:
            print("[plot] Skipping (missing Mean/Allocated):", csv)
            continue

        bench = base_name_from_csv(csv)
        # Per-file images
        mean_path = os.path.join(out_dir, f"bench-mean-{bench}.png")
        alloc_path = os.path.join(out_dir, f"bench-alloc-{bench}.png")
        save_bar(df["Method"], to_number(df[mean_col]), f"{bench}: Mean time per op", mean_col, mean_path)
        save_bar(df["Method"], to_number(df[alloc_col]), f"{bench}: Allocated bytes per op", alloc_col, alloc_path)

        # For summary
        for _, row in df.iterrows():
            summary_rows.append({
                "Label": f"{bench}::{row['Method']}",
                "Mean": to_number(pd.Series([row[mean_col]])).iloc[0],
                "Allocated": to_number(pd.Series([row[alloc_col]])).iloc[0],
            })

    # Summary charts (all CSVs combined)
    if summary_rows:
        s = pd.DataFrame(summary_rows)
        ts = datetime.utcnow().strftime("%Y%m%dT%H%M%SZ")
        out_mean = os.path.join(out_dir, f"bench-mean-summary-{ts}.png")
        out_alloc = os.path.join(out_dir, f"bench-alloc-summary-{ts}.png")
        save_bar(s["Label"], s["Mean"], "All benchmarks: Mean time per op", "Mean", out_mean)
        save_bar(s["Label"], s["Allocated"], "All benchmarks: Allocated bytes per op", "Allocated", out_alloc)
    else:
        print("[plot] No rows collected for summary")

if __name__ == "__main__":
    main()
