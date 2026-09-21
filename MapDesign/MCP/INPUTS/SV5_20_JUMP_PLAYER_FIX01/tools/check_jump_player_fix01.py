from __future__ import annotations

import argparse
import csv
import hashlib
import json
import math
import sys
from collections import defaultdict
from pathlib import Path

PATCH_ID = "SV5_20_FIX01_LINK02_CLEARANCE"
INPUT_DIGEST = "f8b2e69686dbd9b7a29b82e4e4a8ca2c46c6183a005a5373e986e04b21f4288b"
RECIPES = ("JUMP012_MIXED_R0", "JUMP012_MIXED_MX")
OPS = {
    ("JUMP012_MIXED_R0", "REMOVE", 14, 4, "SOLID", "AIR"),
    ("JUMP012_MIXED_R0", "REMOVE", 14, 5, "SOLID", "AIR"),
    ("JUMP012_MIXED_R0", "ADD", 17, 4, "AIR", "TOP_ONLY"),
    ("JUMP012_MIXED_MX", "REMOVE", 9, 4, "SOLID", "AIR"),
    ("JUMP012_MIXED_MX", "REMOVE", 9, 5, "SOLID", "AIR"),
    ("JUMP012_MIXED_MX", "ADD", 6, 4, "AIR", "TOP_ONLY"),
}
OVERRIDES = {
    ("JUMP012_MIXED_R0", 2): (13, 4, 17, 5, 18, 5),
    ("JUMP012_MIXED_MX", 2): (10, 4, 6, 5, 5, 5),
}


def rows(path: Path) -> list[dict[str, str]]:
    with path.open("r", encoding="utf-8-sig", newline="") as handle:
        return list(csv.DictReader(handle))


def iv(row: dict[str, str], key: str) -> int:
    return int(row[key])


def fv(row: dict[str, str], key: str) -> float:
    return float(row[key])


def yes(value: object) -> bool:
    return str(value).strip().lower() == "true"


def sha(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def columns(label: str, data: list[dict[str, str]], required: set[str], errors: list[str]) -> None:
    actual = set(data[0]) if data else set()
    if not required.issubset(actual):
        errors.append(label + ":" + ",".join(sorted(required - actual)))


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--project-root", default=".")
    args = parser.parse_args()
    root = Path(args.project_root).resolve()
    base = root / "MapDesign/MCP/GENERATED/SV5_17_JUMP_RECIPES"
    clear = root / "MapDesign/MCP/GENERATED/SV5_18_JUMP_CLEARANCE"
    recovery = root / "MapDesign/MCP/GENERATED/SV5_19_JUMP_RECOVERY"
    out = root / "MapDesign/MCP/GENERATED/SV5_20_JUMP_PLAYER"
    required = [
        base / "recipe_occupancy.csv",
        clear / "clearance_links.csv",
        recovery / "recovery_overlay.csv",
        recovery / "recovery_miss_probes.csv",
        recovery / "recovery_routes.csv",
        out / "player_geometry_fix01.json",
        out / "player_geometry_patch.csv",
        out / "player_composed_occupancy.csv",
        out / "player_effective_links.csv",
        out / "jump_player.json",
        out / "player_cases.csv",
        out / "player_trace.csv",
        out / "player_validation.json",
        out / "preview/jump_player.svg",
    ]
    errors: list[str] = []
    for path in required:
        if not path.is_file():
            errors.append("MISSING:" + str(path.relative_to(root)).replace("\\", "/"))
    if errors:
        print("FAIL_INDEPENDENT_JUMP_PLAYER_FIX01")
        print("\n".join(errors))
        return 1

    try:
        base_occ = rows(base / "recipe_occupancy.csv")
        clear_links = rows(clear / "clearance_links.csv")
        recovery_overlay = rows(recovery / "recovery_overlay.csv")
        patch_rows = rows(out / "player_geometry_patch.csv")
        composed_rows = rows(out / "player_composed_occupancy.csv")
        effective_rows = rows(out / "player_effective_links.csv")
        cases = rows(out / "player_cases.csv")
        traces = rows(out / "player_trace.csv")
        fix = json.loads((out / "player_geometry_fix01.json").read_text(encoding="utf-8-sig"))
        summary = json.loads((out / "jump_player.json").read_text(encoding="utf-8-sig"))
        validation = json.loads((out / "player_validation.json").read_text(encoding="utf-8-sig"))
    except Exception as exc:
        print("FAIL_INDEPENDENT_JUMP_PLAYER_FIX01")
        print("PARSE:" + str(exc))
        return 1

    columns("PATCH_COLUMNS", patch_rows, {
        "recipe_id", "op", "x", "y", "old_collision", "new_collision",
        "owner_id", "reason"
    }, errors)
    columns("COMPOSED_COLUMNS", composed_rows, {
        "recipe_id", "x", "y", "collision", "source_layer", "owner_id"
    }, errors)
    columns("EFFECTIVE_LINK_COLUMNS", effective_rows, {
        "recipe_id", "order", "source_link_id", "mode", "takeoff_x",
        "takeoff_y", "landing_x", "landing_y", "walk_to_next_takeoff_x",
        "walk_to_next_takeoff_y", "geometry_patch_id"
    }, errors)
    columns("CASE_FIX_COLUMNS", cases, {
        "case_id", "recipe_id", "case_kind", "main_link_order",
        "effective_takeoff_x", "effective_takeoff_y", "effective_landing_x",
        "effective_landing_y", "terminal_body_x", "terminal_body_y",
        "geometry_patch_id", "outcome", "used_grab", "teleports_after_start"
    }, errors)
    columns("TRACE_COLUMNS", traces, {
        "case_id", "step", "body_x", "body_y", "is_grabbing", "event"
    }, errors)

    actual_ops = set()
    for row in patch_rows:
        try:
            item = (row["recipe_id"], row["op"], iv(row, "x"), iv(row, "y"),
                    row["old_collision"], row["new_collision"])
        except Exception:
            errors.append("PATCH_PARSE:" + repr(row))
            continue
        actual_ops.add(item)
        if row["owner_id"] != PATCH_ID or row["reason"] != "LINK02_PHYSICAL_CLEARANCE":
            errors.append("PATCH_OWNER_OR_REASON:" + repr(item))
    if actual_ops != OPS or len(patch_rows) != 6:
        errors.append("EXACT_SIX_OPERATIONS:" + repr(sorted(actual_ops ^ OPS)))
    mirrored = {(r, op, 23 - x, y, old, new) for r, op, x, y, old, new in actual_ops
                if r == RECIPES[0]}
    expected_mirror = {(RECIPES[0], op, 23 - x, y, old, new)
                       for r, op, x, y, old, new in actual_ops if r == RECIPES[1]}
    if mirrored != expected_mirror:
        errors.append("PATCH_NOT_EXACT_MIRROR")

    expected: dict[tuple[str, int, int], tuple[str, str, str]] = {}
    for row in base_occ:
        key = (row["recipe_id"], iv(row, "x"), iv(row, "y"))
        if key in expected:
            errors.append("BASE_DUPLICATE:" + repr(key))
        expected[key] = (row["collision"], "SV5_17_BASE", row["source_owner_id"])
    for row in recovery_overlay:
        key = (row["recipe_id"], iv(row, "x"), iv(row, "y"))
        if key in expected:
            errors.append("RECOVERY_BASE_OVERLAP:" + repr(key))
        expected[key] = (row["collision"], "SV5_19_RECOVERY", row["owner_id"])
    for recipe, op, x, y, old, new in OPS:
        key = (recipe, x, y)
        if op == "REMOVE":
            if key not in expected or expected[key][0] != old:
                errors.append("REMOVE_SOURCE_MISMATCH:" + repr(key))
            expected.pop(key, None)
        else:
            if key in expected or old != "AIR":
                errors.append("ADD_SOURCE_MISMATCH:" + repr(key))
            expected[key] = (new, "SV5_20_FIX01", PATCH_ID)
    actual: dict[tuple[str, int, int], tuple[str, str, str]] = {}
    for row in composed_rows:
        key = (row["recipe_id"], iv(row, "x"), iv(row, "y"))
        if key in actual:
            errors.append("COMPOSED_DUPLICATE:" + repr(key))
        actual[key] = (row["collision"], row["source_layer"], row["owner_id"])
    if actual != expected:
        errors.append(f"COMPOSED_OCCUPANCY_MISMATCH:{len(expected)}/{len(actual)}")
    for key in ((RECIPES[0], 14, 6), (RECIPES[1], 9, 6)):
        if actual.get(key, (None,))[0] != "SOLID":
            errors.append("OVERHANG_NOT_PRESERVED:" + repr(key))
    for key in ((RECIPES[0], 17, 4), (RECIPES[1], 6, 4)):
        if actual.get(key) != ("TOP_ONLY", "SV5_20_FIX01", PATCH_ID):
            errors.append("PLATFORM_EXTENSION_TYPE:" + repr(key))

    base_links = {(r["recipe_id"], iv(r, "order")): r for r in clear_links}
    effective = {(r["recipe_id"], iv(r, "order")): r for r in effective_rows}
    expected_keys = {(r, o) for r in RECIPES for o in range(9)}
    if set(base_links) != expected_keys or set(effective) != expected_keys or len(effective_rows) != 18:
        errors.append("EFFECTIVE_LINK_SET")
    for key in sorted(expected_keys):
        base_row, row = base_links.get(key), effective.get(key)
        if not base_row or not row:
            continue
        if row["source_link_id"] != base_row["source_link_id"] or row["mode"] != base_row["mode"]:
            errors.append("LINK_IDENTITY:" + repr(key))
        if key in OVERRIDES:
            got = tuple(iv(row, k) for k in ("takeoff_x", "takeoff_y", "landing_x",
                                             "landing_y", "walk_to_next_takeoff_x",
                                             "walk_to_next_takeoff_y"))
            if got != OVERRIDES[key] or row["geometry_patch_id"] != PATCH_ID:
                errors.append("LINK02_OVERRIDE:" + repr((key, got)))
        else:
            got = tuple(iv(row, k) for k in ("takeoff_x", "takeoff_y", "landing_x", "landing_y"))
            want = tuple(int(base_row[k]) for k in ("takeoff_x", "takeoff_y", "landing_x", "landing_y"))
            if got != want:
                errors.append("NON_LINK02_CHANGED:" + repr(key))

    if (fix.get("schema") != "SV5_20_JUMP_PLAYER_FIX01/v1" or fix.get("status") != "PASS"
            or fix.get("patch_id") != PATCH_ID or fix.get("input_recovery_digest") != INPUT_DIGEST):
        errors.append("FIX_SUMMARY_IDENTITY")
    for key, value in (("changed_cell_count", 6), ("removed_cell_count", 4),
                       ("added_cell_count", 2), ("effective_link_override_count", 2)):
        if fix.get(key) != value:
            errors.append("FIX_SUMMARY_COUNT:" + key)
    for key in ("exact_mirror_x", "all_physical_cases_passed"):
        if fix.get(key) is not True:
            errors.append("FIX_SUMMARY_TRUE:" + key)
    for key in ("player_tuning_changed", "predecessor_files_changed",
                "reverse_required", "items_used"):
        if fix.get(key) is not False:
            errors.append("FIX_SUMMARY_FALSE:" + key)
    for key in ("whole_world_builds", "whole_world_searches",
                "global_endpoint_comparisons", "sector_partitions"):
        if fix.get(key) != 0:
            errors.append("FIX_SUMMARY_ZERO:" + key)

    if summary.get("status") != "PASS" or summary.get("input_recovery_digest") != INPUT_DIGEST:
        errors.append("PLAYER_SUMMARY_NOT_PASS")
    if summary.get("PlayerVerified", summary.get("readiness", {}).get("PlayerVerified")) is not True:
        errors.append("PLAYER_NOT_VERIFIED")
    if validation.get("status") != "PASS" or validation.get("errors") not in ([], None):
        errors.append("PLAYER_VALIDATION_NOT_PASS")

    case_by_key: dict[tuple[str, int], dict[str, str]] = {}
    case_by_id: dict[str, dict[str, str]] = {}
    for row in cases:
        case_by_id[row["case_id"]] = row
        if row["geometry_patch_id"] != PATCH_ID or row["outcome"] != "PASS" or iv(row, "teleports_after_start") != 0:
            errors.append("CASE_PATCH_OR_OUTCOME:" + row["case_id"])
        if row["case_kind"] == "MAIN_LINK":
            case_by_key[(row["recipe_id"], iv(row, "main_link_order"))] = row
    for key, override in OVERRIDES.items():
        row = case_by_key.get(key)
        if not row:
            errors.append("MISSING_LINK02_CASE:" + repr(key))
            continue
        got = tuple(iv(row, k) for k in ("effective_takeoff_x", "effective_takeoff_y",
                                         "effective_landing_x", "effective_landing_y"))
        if got != override[:4] or yes(row["used_grab"]):
            errors.append("LINK02_CASE_BINDING:" + repr(key))
        if (iv(row, "terminal_body_x"), iv(row, "terminal_body_y")) != override[2:4]:
            errors.append("LINK02_TERMINAL:" + repr(key))

    trace_by_case: dict[str, list[dict[str, str]]] = defaultdict(list)
    for row in traces:
        if row["case_id"] in case_by_id:
            trace_by_case[row["case_id"]].append(row)
    for key in OVERRIDES:
        case = case_by_key.get(key)
        if not case:
            continue
        trace = sorted(trace_by_case.get(case["case_id"], []), key=lambda r: iv(r, "step"))
        if not trace:
            errors.append("MISSING_LINK02_TRACE:" + repr(key))
            continue
        ys = [fv(r, "body_y") for r in trace]
        if not all(math.isfinite(y) for y in ys) or max(ys) - ys[0] > 1.50:
            errors.append("LINK02_FAKE_APEX_OR_NONFINITE:" + repr(key))
        events = {r["event"] for r in trace}
        if "START" not in events or "PASS_TARGET" not in events or any(yes(r["is_grabbing"]) for r in trace):
            errors.append("LINK02_TRACE_EVENTS:" + repr(key))

    svg = (out / "preview/jump_player.svg").read_text(encoding="utf-8-sig")
    for token in (PATCH_ID, "REMOVED 4", "TOP_ONLY ADDED 2", "MAIN_MX_02", "MAIN_R0_02"):
        if token not in svg:
            errors.append("SVG_FIX01_TOKEN:" + token)

    if errors:
        print("FAIL_INDEPENDENT_JUMP_PLAYER_FIX01")
        print("\n".join(errors))
        return 1
    audit = {
        "schema": "SV5_20_INDEPENDENT_JUMP_PLAYER_FIX01_AUDIT/v1",
        "task": "SV5_20_JUMP_PLAYER",
        "patch_id": PATCH_ID,
        "status": "PASS_INDEPENDENT_JUMP_PLAYER_FIX01",
        "changed_cells": 6,
        "removed_cells": 4,
        "added_top_only_cells": 2,
        "effective_link_overrides": 2,
        "physical_cases": len(cases),
        "trace_states": len(traces),
        "player_tuning_changed": False,
        "predecessor_files_changed": False,
        "whole_world_builds": 0,
        "global_endpoint_comparisons": 0,
        "patch_sha256": sha(out / "player_geometry_patch.csv"),
        "composed_occupancy_sha256": sha(out / "player_composed_occupancy.csv"),
        "effective_links_sha256": sha(out / "player_effective_links.csv"),
        "errors": [],
    }
    audit_path = out / "independent_jump_player_fix01_audit.json"
    audit_path.write_text(json.dumps(audit, ensure_ascii=False, indent=2) + "\n",
                          encoding="utf-8", newline="\n")
    print("PASS_INDEPENDENT_JUMP_PLAYER_FIX01")
    print(json.dumps(audit, ensure_ascii=False, sort_keys=True))
    return 0


if __name__ == "__main__":
    sys.exit(main())

