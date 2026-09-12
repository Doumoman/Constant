#!/usr/bin/env python3
"""Independent SV5_09 CSV/JSON audit. Reads exports only; imports no production code."""
import argparse
import csv
import hashlib
import json
import sys
from collections import Counter
from pathlib import Path

def need(ok, message):
    if not ok:
        raise ValueError(message)

def truth(value):
    return str(value).strip().lower() in ("1", "true", "yes")

def rows(path):
    with path.open(encoding="utf-8-sig", newline="") as f:
        return list(csv.DictReader(f))

def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()

def audit_variant(root):
    links_path = root / "loop_links.csv"
    cells_path = root / "loop_cells.csv"
    candidates_path = root / "loop_candidates.csv"
    loops_path = root / "loops.json"
    validation_path = root / "loop_validation.json"
    for p in (links_path, cells_path, candidates_path, loops_path, validation_path):
        need(p.is_file(), "missing export: " + str(p))
    links, cells, candidates = rows(links_path), rows(cells_path), rows(candidates_path)
    required_link = {"loop_id", "type", "sector_id", "from_room", "to_room", "length",
                     "baseline_cost", "new_cost", "alternate_path_exists", "cycle_delta", "bypass_count"}
    required_cell = {"loop_id", "order", "x", "y", "role", "body_air", "head_air", "support_solid",
                     "protected", "world_inside"}
    required_candidate = {"candidate_id", "stable_rank", "stable_hash", "status", "reason"}
    need(not links or required_link.issubset(links[0]), "loop_links columns differ")
    need(not cells or required_cell.issubset(cells[0]), "loop_cells columns differ")
    need(not candidates or required_candidate.issubset(candidates[0]), "loop_candidates columns differ")
    ids = [r["loop_id"] for r in links]
    need(len(ids) == len(set(ids)), "duplicate loop id")
    pairs = [(r["from_room"], r["to_room"]) for r in links]
    need(len({tuple(sorted(p)) for p in pairs}) == len(pairs), "duplicate unordered endpoint pair")
    by_loop = {}
    for row in cells:
        if row["role"] == "CENTERLINE":
            by_loop.setdefault(row["loop_id"], []).append(row)
    type_counts = Counter()
    lengths = []
    sectors = set()
    for link in links:
        loop_id = link["loop_id"]
        center = sorted(by_loop.get(loop_id, []), key=lambda r: int(r["order"]))
        length = int(link["length"])
        need(4 <= length <= 24 and len(center) == length, "length/centerline mismatch: " + loop_id)
        need([int(r["order"]) for r in center] == list(range(length)), "non-contiguous order: " + loop_id)
        points = [(int(r["x"]), int(r["y"])) for r in center]
        need(len(points) == len(set(points)), "self-intersecting centerline: " + loop_id)
        need(all(abs(a[0]-b[0]) + abs(a[1]-b[1]) == 1 for a, b in zip(points, points[1:])),
             "non-cardinal centerline: " + loop_id)
        need(all(truth(r["body_air"]) and truth(r["head_air"]) and truth(r["support_solid"])
                 and not truth(r["protected"]) and truth(r["world_inside"]) for r in center),
             "clearance/support/protection failure: " + loop_id)
        need(truth(link["alternate_path_exists"]) and int(link["cycle_delta"]) == 1,
             "not an actual cycle: " + loop_id)
        need(int(link["bypass_count"]) == 0, "progress bypass: " + loop_id)
        old, new = int(link["baseline_cost"]), int(link["new_cost"])
        kind = link["type"]
        need(kind in ("LOOP", "RANDOM_SHORTCUT"), "unknown type: " + kind)
        need((kind == "RANDOM_SHORTCUT") == (new < old), "cost classification mismatch: " + loop_id)
        type_counts[kind] += 1
        lengths.append(length)
        sectors.add(link["sector_id"])
    need(len(links) >= 16, "accepted loop minimum not met")
    need(len(sectors) >= 12, "sector spread minimum not met")
    per_sector = Counter(r["sector_id"] for r in links)
    need(max(per_sector.values(), default=0) <= 3, "sector cap exceeded")
    validation = json.loads(validation_path.read_text(encoding="utf-8-sig"))
    failed = [k for k, v in validation.get("checks", {}).items() if v is not True]
    need(not failed, "loop_validation false checks: " + ",".join(failed))
    return {
        "accepted": len(links), "candidate_count": len(candidates), "distinct_sectors": len(sectors),
        "max_length": max(lengths, default=0), "type_counts": dict(type_counts), "bypass_count": 0,
        "file_sha256": {p.name: digest(p) for p in (links_path, cells_path, candidates_path, loops_path, validation_path)}
    }

def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--default", type=Path, required=True)
    ap.add_argument("--repeat", type=Path, required=True)
    ap.add_argument("--output", type=Path, required=True)
    a = ap.parse_args()
    result = {"format": "sv5_09_independent_loop_audit_v1", "status": "PASS",
              "default": audit_variant(a.default), "repeat": audit_variant(a.repeat)}
    a.output.parent.mkdir(parents=True, exist_ok=True)
    a.output.write_text(json.dumps(result, ensure_ascii=False, indent=2) + "\n", encoding="utf-8", newline="\n")
    print("PASS_INDEPENDENT_LOOPS")

if __name__ == "__main__":
    try:
        main()
    except (OSError, ValueError, KeyError, TypeError) as e:
        print("FAIL_INDEPENDENT_LOOPS: " + str(e), file=sys.stderr)
        sys.exit(2)
