from __future__ import annotations

import argparse
import csv
import hashlib
import json
import re
import sys
import xml.etree.ElementTree as ET
from collections import defaultdict
from pathlib import Path


WIDTH = 20
HEIGHT = 24
LAYOUTS = 4
ALLOWED_BASES = {"AIR", "SOLID", "TOP_ONLY"}
FORBIDDEN_SOURCE = (
    "SectorId", "48x32", "48×32", "624x416", "624×416",
    "CharacterLiveMovementDriver", "Rigidbody2D", "Collider2D",
    "Physics2D", "InputAction", "MovePosition"
)


def sha(data: bytes) -> str:
    return hashlib.sha256(data).hexdigest()


def read_json(path: Path, errors: list[str]) -> dict:
    try:
        return json.loads(path.read_text(encoding="utf-8"))
    except Exception as exc:
        errors.append(f"JSON:{path.name}:{exc}")
        return {}


def read_csv(path: Path, header: list[str], errors: list[str]) -> list[dict[str, str]]:
    try:
        with path.open("r", encoding="utf-8-sig", newline="") as handle:
            reader = csv.DictReader(handle)
            if reader.fieldnames != header:
                errors.append(f"HEADER:{path.name}:{reader.fieldnames}")
                return []
            return list(reader)
    except Exception as exc:
        errors.append(f"CSV:{path.name}:{exc}")
        return []


def integer(row: dict[str, str], key: str, label: str, errors: list[str]) -> int:
    try:
        return int(row[key])
    except Exception:
        errors.append(f"INT:{label}:{key}")
        return 0


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--project-root", default=".")
    parser.add_argument("--output", default=(
        "MapDesign/MCP/GENERATED/SV5_21_LIBRARY/independent_library_audit.json"
    ))
    args = parser.parse_args()
    root = Path(args.project_root).resolve()
    generated = root / "MapDesign/MCP/GENERATED/SV5_21_LIBRARY"
    errors: list[str] = []
    required = [
        "library.json", "library_cells.csv", "library_micro_slots.csv",
        "library_platforms.csv", "library_ports.csv",
        "library_stair_reservations.csv", "library_decor.csv",
        "library_validation.json", "seed_comparison.json",
        "targeted_results.xml", "preview/library.svg"
    ]
    for rel in required:
        if not (generated / rel).is_file():
            errors.append("MISSING:" + rel)
    if errors:
        print(json.dumps({"status": "FAIL_INDEPENDENT_LIBRARY", "errors": errors}, indent=2))
        return 1

    library = read_json(generated / "library.json", errors)
    validation = read_json(generated / "library_validation.json", errors)
    comparison = read_json(generated / "seed_comparison.json", errors)
    cells = read_csv(generated / "library_cells.csv",
        ["layout_id", "x", "y", "base", "source_kind", "source_id"], errors)
    slots = read_csv(generated / "library_micro_slots.csv",
        ["layout_id", "slot_x", "slot_y", "pattern_id", "variant"], errors)
    platforms = read_csv(generated / "library_platforms.csv",
        ["layout_id", "platform_id", "min_x", "max_x", "y", "base", "role"], errors)
    ports = read_csv(generated / "library_ports.csv",
        ["layout_id", "port_id", "side", "x", "y", "width", "height", "flow"], errors)
    stairs = read_csv(generated / "library_stair_reservations.csv",
        ["layout_id", "stair_id", "direction", "start_x", "start_y",
         "end_x", "end_y", "run", "rise", "min_clearance"], errors)
    decor = read_csv(generated / "library_decor.csv",
        ["layout_id", "decor_id", "kind", "min_x", "min_y", "max_x",
         "max_y", "collidable"], errors)

    layouts = library.get("layouts", [])
    ids = [item.get("layout_id") for item in layouts]
    if library.get("schema") != "SV5_21_LIBRARY/v1":
        errors.append("LIBRARY_SCHEMA")
    if library.get("layout_count") != LAYOUTS or len(layouts) != LAYOUTS:
        errors.append("LAYOUT_COUNT")
    if len(set(ids)) != LAYOUTS or any(not item for item in ids):
        errors.append("LAYOUT_IDS")
    for item in layouts:
        if item.get("width") != WIDTH or item.get("height") != HEIGHT:
            errors.append("LAYOUT_DIMENSION:" + str(item.get("layout_id")))

    cells_by: dict[str, dict[tuple[int, int], str]] = defaultdict(dict)
    for row in cells:
        lid = row["layout_id"]
        x = integer(row, "x", lid, errors)
        y = integer(row, "y", lid, errors)
        if lid not in ids or not (0 <= x < WIDTH and 0 <= y < HEIGHT):
            errors.append(f"CELL_BOUNDS:{lid}:{x}:{y}")
            continue
        if row["base"] not in ALLOWED_BASES:
            errors.append(f"CELL_BASE:{lid}:{x}:{y}:{row['base']}")
        if (x, y) in cells_by[lid]:
            errors.append(f"CELL_DUPLICATE:{lid}:{x}:{y}")
        cells_by[lid][(x, y)] = row["base"]
    for lid in ids:
        if len(cells_by[lid]) != WIDTH * HEIGHT:
            errors.append(f"CELL_COMPLETENESS:{lid}:{len(cells_by[lid])}")

    signatures: dict[str, tuple[str, ...]] = {}
    for lid in ids:
        grid = cells_by[lid]
        signature = tuple(grid.get((x, y), "MISSING")
                          for y in range(HEIGHT) for x in range(WIDTH))
        mirror = tuple(grid.get((WIDTH - 1 - x, y), "MISSING")
                       for y in range(HEIGHT) for x in range(WIDTH))
        signatures[lid] = signature
        if signature == mirror:
            errors.append("SELF_MIRROR:" + lid)
        for y0 in range(HEIGHT - 5):
            for x0 in range(WIDTH - 5):
                if all(grid.get((x, y)) == "SOLID"
                       for y in range(y0, y0 + 6)
                       for x in range(x0, x0 + 6)):
                    errors.append(f"SOLID_6X6:{lid}:{x0}:{y0}")
    for i, left in enumerate(ids):
        for right in ids[i + 1:]:
            if signatures[left] == signatures[right]:
                errors.append(f"DUPLICATE_LAYOUT:{left}:{right}")
            right_grid = cells_by[right]
            right_mirror = tuple(right_grid.get((WIDTH - 1 - x, y), "MISSING")
                                 for y in range(HEIGHT) for x in range(WIDTH))
            if signatures[left] == right_mirror:
                errors.append(f"MIRROR_PAIR:{left}:{right}")

    slots_by: dict[str, set[tuple[int, int]]] = defaultdict(set)
    for row in slots:
        lid = row["layout_id"]
        sx = integer(row, "slot_x", lid, errors)
        sy = integer(row, "slot_y", lid, errors)
        slots_by[lid].add((sx, sy))
        if not (0 <= sx < 5 and 0 <= sy < 6) or not row["pattern_id"]:
            errors.append(f"MICRO_SLOT:{lid}:{sx}:{sy}")
    expected_slots = {(x, y) for y in range(6) for x in range(5)}
    for lid in ids:
        if slots_by[lid] != expected_slots:
            errors.append("MICRO_SLOT_COMPLETENESS:" + lid)

    platform_by: dict[str, list[tuple[int, int, int]]] = defaultdict(list)
    for row in platforms:
        lid = row["layout_id"]
        x0 = integer(row, "min_x", lid, errors)
        x1 = integer(row, "max_x", lid, errors)
        y = integer(row, "y", lid, errors)
        if row["base"] not in {"SOLID", "TOP_ONLY"} or not (0 <= x0 <= x1 < WIDTH):
            errors.append("PLATFORM_GEOMETRY:" + lid)
        if not (4 <= x1 - x0 + 1 <= 10):
            errors.append("PLATFORM_LENGTH:" + lid + ":" + row["platform_id"])
        if row["role"] == "PRIMARY_TIER":
            platform_by[lid].append((y, x0, x1))
    for lid in ids:
        tiers = sorted(platform_by[lid])
        if len(tiers) < 5:
            errors.append("PRIMARY_TIER_COUNT:" + lid)
            continue
        ys = sorted({item[0] for item in tiers})
        if any(not (3 <= b - a <= 5) for a, b in zip(ys, ys[1:])):
            errors.append("PRIMARY_TIER_GAP:" + lid)
        centers_by_y: dict[int, list[float]] = defaultdict(list)
        for y, a, b in tiers:
            centers_by_y[y].append((a + b) / 2.0)
        centers = [sum(centers_by_y[y]) / len(centers_by_y[y])
                   for y in sorted(centers_by_y)]
        signs = [1 if b > a else -1 if b < a else 0
                 for a, b in zip(centers, centers[1:])]
        reversals = sum(1 for a, b in zip(signs, signs[1:]) if a and b and a != b)
        if reversals < 3:
            errors.append(f"ZIGZAG_REVERSALS:{lid}:{reversals}")

    ports_by: dict[str, list[dict[str, str]]] = defaultdict(list)
    for row in ports:
        ports_by[row["layout_id"]].append(row)
    for lid in ids:
        rows = ports_by[lid]
        left = [r for r in rows if r["side"] == "LEFT"]
        right = [r for r in rows if r["side"] == "RIGHT"]
        if len(left) != 1 or len(right) != 1:
            errors.append("REQUIRED_PORTS:" + lid)
            continue
        for row in left + right:
            if integer(row, "height", lid, errors) != 2 or row["flow"] != "REQUIRED":
                errors.append("PORT_SHAPE:" + lid)
        if integer(left[0], "y", lid, errors) == integer(right[0], "y", lid, errors):
            errors.append("PORT_HEIGHT_SYMMETRY:" + lid)

    stairs_by: dict[str, list[dict[str, str]]] = defaultdict(list)
    for row in stairs:
        stairs_by[row["layout_id"]].append(row)
    for lid in ids:
        rows = stairs_by[lid]
        if not (2 <= len(rows) <= 4):
            errors.append(f"STAIR_COUNT:{lid}:{len(rows)}")
        for row in rows:
            sx = integer(row, "start_x", lid, errors)
            sy = integer(row, "start_y", lid, errors)
            ex = integer(row, "end_x", lid, errors)
            ey = integer(row, "end_y", lid, errors)
            run_value = integer(row, "run", lid, errors)
            rise = integer(row, "rise", lid, errors)
            clearance = integer(row, "min_clearance", lid, errors)
            if row["direction"] not in {"UP_LEFT", "UP_RIGHT"}:
                errors.append("STAIR_DIRECTION:" + lid)
            if not (0 <= sx < WIDTH and 0 <= ex < WIDTH and
                    0 <= sy < HEIGHT and 0 <= ey < HEIGHT):
                errors.append("STAIR_BOUNDS:" + lid)
            if sx == ex or sy == ey or not (3 <= rise <= 5) or run_value < rise:
                errors.append("STAIR_DIAGONAL:" + lid + ":" + row["stair_id"])
            if clearance < 2:
                errors.append("STAIR_CLEARANCE:" + lid + ":" + row["stair_id"])
            if cells_by[lid].get((sx, sy)) != "AIR" or cells_by[lid].get((ex, ey)) != "AIR":
                errors.append("STAIR_NOT_AIR:" + lid + ":" + row["stair_id"])

    decor_union: dict[str, set[tuple[int, int]]] = defaultdict(set)
    for row in decor:
        lid = row["layout_id"]
        if row["kind"] != "BOOKSHELF" or row["collidable"].lower() != "false":
            errors.append("DECOR_SEMANTICS:" + lid + ":" + row["decor_id"])
        x0 = integer(row, "min_x", lid, errors)
        y0 = integer(row, "min_y", lid, errors)
        x1 = integer(row, "max_x", lid, errors)
        y1 = integer(row, "max_y", lid, errors)
        if not (0 <= x0 <= x1 < WIDTH and 0 <= y0 <= y1 < HEIGHT):
            errors.append("DECOR_BOUNDS:" + lid)
            continue
        decor_union[lid].update((x, y) for y in range(y0, y1 + 1)
                                for x in range(x0, x1 + 1))
    for lid in ids:
        coverage = len(decor_union[lid]) / float(WIDTH * HEIGHT)
        if not (0.55 <= coverage <= 0.80):
            errors.append(f"DECOR_COVERAGE:{lid}:{coverage:.6f}")

    readiness = validation.get("readiness", {})
    expected_readiness = {
        "LibraryShellReady": True,
        "StairDataReady": False,
        "StairMotorReady": False,
        "LibraryTraversalReady": False,
        "WorldPlaced": False,
    }
    if validation.get("status") != "PASS" or readiness != expected_readiness:
        errors.append("READINESS")
    for key in ("whole_world_builds", "whole_world_searches",
                "global_endpoint_comparisons", "sector_partitions"):
        if validation.get(key) != 0:
            errors.append("FORBIDDEN_SCOPE:" + key)
    if comparison.get("different_occupancy_count", 0) < 2:
        errors.append("SEED_DIVERSITY")

    try:
        result = ET.parse(generated / "targeted_results.xml").getroot()
        if (result.get("result") != "Passed" or int(result.get("failed", "0")) != 0 or
                int(result.get("skipped", "0")) != 0 or
                int(result.get("passed", "0")) < 12):
            errors.append("TARGETED_RESULTS")
    except Exception as exc:
        errors.append("TARGETED_XML:" + str(exc))

    svg = (generated / "preview/library.svg").read_text(encoding="utf-8")
    if not re.search(r'viewBox=["\']0 0 624 416["\']', svg):
        errors.append("SVG_VIEWBOX")
    for token in ("SOLID", "TOP_ONLY", "STAIR_RESERVED", "PORT", "BOOKSHELF"):
        if token not in svg:
            errors.append("SVG_LEGEND:" + token)

    source_paths = [
        root / "Assets/_Game/Map/Runtime/WorldGeneration/TerrainClusters/Sv5LibraryLayout.cs",
        root / "Assets/_Game/Map/Runtime/WorldGeneration/TerrainClusters/Sv5LibraryExport.cs",
    ]
    for path in source_paths:
        if not path.is_file():
            errors.append("SOURCE_MISSING:" + path.name)
            continue
        text = path.read_text(encoding="utf-8-sig")
        for token in FORBIDDEN_SOURCE:
            if token in text:
                errors.append(f"FORBIDDEN_SOURCE:{path.name}:{token}")

    audit = {
        "schema": "SV5_21_INDEPENDENT_LIBRARY_AUDIT/v1",
        "task": "SV5_21_LIBRARY",
        "status": "PASS_INDEPENDENT_LIBRARY" if not errors else "FAIL_INDEPENDENT_LIBRARY",
        "layout_count": len(ids),
        "cell_count": len(cells),
        "micro_slot_count": len(slots),
        "stair_reservation_count": len(stairs),
        "whole_world_builds": validation.get("whole_world_builds"),
        "errors": errors,
        "source_sha256": {
            path.name: sha(path.read_bytes()) for path in source_paths if path.is_file()
        }
    }
    output = root / args.output
    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_text(json.dumps(audit, ensure_ascii=False, indent=2) + "\n",
                      encoding="utf-8", newline="\n")
    print(json.dumps(audit, ensure_ascii=False, indent=2))
    return 0 if not errors else 1


if __name__ == "__main__":
    sys.exit(main())
