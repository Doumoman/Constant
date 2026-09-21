from __future__ import annotations

import argparse
import csv
import hashlib
import json
import sys
import xml.etree.ElementTree as ET
from collections import defaultdict
from pathlib import Path

PATCH_ID = "SV5_20_FIX03_LOCAL_PHYSICAL_CLEARANCE"
POLICY_ID = "WALKABLE_FRONTIER_AND_GRAB_EXIT_V2"
INPUT_DIGEST = "f8b2e69686dbd9b7a29b82e4e4a8ca2c46c6183a005a5373e986e04b21f4288b"
RECIPES = ("JUMP012_MIXED_R0", "JUMP012_MIXED_MX")

REMOVE = {
    ("JUMP012_MIXED_R0", 14, 4), ("JUMP012_MIXED_R0", 14, 5),
    ("JUMP012_MIXED_R0", 14, 6), ("JUMP012_MIXED_R0", 3, 7),
    ("JUMP012_MIXED_R0", 3, 8), ("JUMP012_MIXED_R0", 4, 8),
    ("JUMP012_MIXED_MX", 9, 4), ("JUMP012_MIXED_MX", 9, 5),
    ("JUMP012_MIXED_MX", 9, 6), ("JUMP012_MIXED_MX", 20, 7),
    ("JUMP012_MIXED_MX", 20, 8), ("JUMP012_MIXED_MX", 19, 8),
}
CONVERT = {
    ("JUMP012_MIXED_R0", 3, 9), ("JUMP012_MIXED_R0", 4, 9),
    ("JUMP012_MIXED_MX", 20, 9), ("JUMP012_MIXED_MX", 19, 9),
}
BORROWED = {
    ("JUMP012_MIXED_R0", 16, 4): ("TOP_ONLY", "SV5_19_RECOVERY", "RG_GRAB_CATCH"),
    ("JUMP012_MIXED_R0", 17, 4): ("TOP_ONLY", "SV5_19_RECOVERY", "RG_GRAB_CATCH"),
    ("JUMP012_MIXED_MX", 6, 4): ("TOP_ONLY", "SV5_19_RECOVERY", "RG_GRAB_CATCH"),
    ("JUMP012_MIXED_MX", 7, 4): ("TOP_ONLY", "SV5_19_RECOVERY", "RG_GRAB_CATCH"),
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


def nunit_pass(path: Path, total: int, errors: list[str]) -> None:
    try:
        attrs = ET.parse(path).getroot().attrib
        if (int(attrs.get("total", -1)) != total or int(attrs.get("passed", -1)) != total
                or int(attrs.get("failed", -1)) != 0 or int(attrs.get("skipped", -1)) != 0
                or attrs.get("result") != "Passed"):
            errors.append("MAIN_DIAGNOSTIC_NUNIT:" + repr(attrs))
    except Exception as exc:
        errors.append("MAIN_DIAGNOSTIC_XML:" + str(exc))


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
        base / "recipe_occupancy.csv", clear / "clearance_links.csv",
        recovery / "recovery_overlay.csv", out / "player_geometry_fix03.json",
        out / "player_geometry_patch.csv", out / "player_composed_occupancy.csv",
        out / "player_effective_links.csv", out / "player_scheduler_audit.json",
        out / "player_main_link_diagnostic_results.xml", out / "jump_player.json",
        out / "player_cases.csv", out / "player_trace.csv",
        out / "player_validation.json", out / "preview/jump_player.svg",
        out / "targeted_playmode_before_fix03.xml",
    ]
    errors: list[str] = []
    for path in required:
        if not path.is_file():
            errors.append("MISSING:" + str(path.relative_to(root)).replace("\\", "/"))
    if errors:
        print("FAIL_INDEPENDENT_JUMP_PLAYER_FIX03")
        print("\n".join(errors))
        return 1

    try:
        base_occ = rows(base / "recipe_occupancy.csv")
        recovery_overlay = rows(recovery / "recovery_overlay.csv")
        clear_links = rows(clear / "clearance_links.csv")
        patch_rows = rows(out / "player_geometry_patch.csv")
        composed_rows = rows(out / "player_composed_occupancy.csv")
        effective_rows = rows(out / "player_effective_links.csv")
        cases = rows(out / "player_cases.csv")
        traces = rows(out / "player_trace.csv")
        fix = json.loads((out / "player_geometry_fix03.json").read_text(encoding="utf-8-sig"))
        scheduler = json.loads((out / "player_scheduler_audit.json").read_text(encoding="utf-8-sig"))
        summary = json.loads((out / "jump_player.json").read_text(encoding="utf-8-sig"))
        validation = json.loads((out / "player_validation.json").read_text(encoding="utf-8-sig"))
    except Exception as exc:
        print("FAIL_INDEPENDENT_JUMP_PLAYER_FIX03")
        print("PARSE:" + str(exc))
        return 1

    columns("PATCH_COLUMNS", patch_rows, {
        "recipe_id", "op", "x", "y", "old_collision", "new_collision",
        "old_owner_id", "new_owner_id", "reason"
    }, errors)
    columns("COMPOSED_COLUMNS", composed_rows, {
        "recipe_id", "x", "y", "collision", "source_layer", "owner_id"
    }, errors)
    columns("EFFECTIVE_LINK_COLUMNS", effective_rows, {
        "recipe_id", "order", "source_link_id", "mode", "takeoff_x",
        "takeoff_y", "landing_x", "landing_y", "walk_to_next_takeoff_x",
        "walk_to_next_takeoff_y", "geometry_patch_id"
    }, errors)
    columns("CASE_COLUMNS", cases, {
        "case_id", "recipe_id", "case_kind", "main_link_order",
        "effective_takeoff_x", "effective_takeoff_y", "effective_landing_x",
        "effective_landing_y", "geometry_patch_id", "outcome",
        "teleports_after_start"
    }, errors)
    columns("TRACE_SCHEDULER_COLUMNS", traces, {
        "case_id", "step", "input_jump", "input_move_x", "body_x", "body_y",
        "velocity_x", "velocity_y", "is_grounded", "is_grabbing",
        "grounded_before_step", "walkable_frontier_x", "frontier_distance",
        "jump_threshold", "scheduler_policy_id", "event"
    }, errors)

    actual_remove: set[tuple[str, int, int]] = set()
    actual_convert: set[tuple[str, int, int]] = set()
    for row in patch_rows:
        try:
            key = (row["recipe_id"], iv(row, "x"), iv(row, "y"))
        except Exception:
            errors.append("PATCH_PARSE:" + repr(row))
            continue
        if row["op"] == "REMOVE":
            actual_remove.add(key)
            if row["old_collision"] != "SOLID" or row["new_collision"] != "AIR":
                errors.append("BAD_REMOVE:" + repr(key))
        elif row["op"] == "CONVERT":
            actual_convert.add(key)
            if row["old_collision"] != "SOLID" or row["new_collision"] != "TOP_ONLY":
                errors.append("BAD_CONVERT:" + repr(key))
        else:
            errors.append("BAD_OP:" + repr(key))
        if row["old_owner_id"] not in {"JS04_SOLID", "JS08_SOLID"}:
            errors.append("BAD_OLD_OWNER:" + repr(key))
    if len(patch_rows) != 16 or actual_remove != REMOVE or actual_convert != CONVERT:
        errors.append("EXACT_16_CELL_PATCH")
    if actual_remove & actual_convert:
        errors.append("DUPLICATE_PATCH_CELL")
    mirror = lambda key: (RECIPES[1], 23 - key[1], key[2])
    if {mirror(k) for k in actual_remove if k[0] == RECIPES[0]} != {k for k in actual_remove if k[0] == RECIPES[1]}:
        errors.append("REMOVE_NOT_EXACT_MIRROR")
    if {mirror(k) for k in actual_convert if k[0] == RECIPES[0]} != {k for k in actual_convert if k[0] == RECIPES[1]}:
        errors.append("CONVERT_NOT_EXACT_MIRROR")

    expected: dict[tuple[str, int, int], tuple[str, str, str]] = {}
    for row in base_occ:
        key = (row["recipe_id"], iv(row, "x"), iv(row, "y"))
        expected[key] = (row["collision"], "SV5_17_BASE", row["source_owner_id"])
    for row in recovery_overlay:
        key = (row["recipe_id"], iv(row, "x"), iv(row, "y"))
        if key in expected:
            errors.append("RECOVERY_BASE_OVERLAP:" + repr(key))
        expected[key] = (row["collision"], "SV5_19_RECOVERY", row["owner_id"])
    for key in REMOVE:
        if expected.get(key, (None,))[0] != "SOLID":
            errors.append("REMOVE_SOURCE_MISMATCH:" + repr(key))
        expected.pop(key, None)
    for key in CONVERT:
        if expected.get(key, (None,))[0] != "SOLID":
            errors.append("CONVERT_SOURCE_MISMATCH:" + repr(key))
        expected[key] = ("TOP_ONLY", "SV5_20_FIX03", "SV5_20_FIX03_JS08_PASS_THROUGH")
    actual: dict[tuple[str, int, int], tuple[str, str, str]] = {}
    for row in composed_rows:
        key = (row["recipe_id"], iv(row, "x"), iv(row, "y"))
        if key in actual:
            errors.append("COMPOSED_DUPLICATE:" + repr(key))
        actual[key] = (row["collision"], row["source_layer"], row["owner_id"])
    if actual != expected:
        errors.append(f"COMPOSED_OCCUPANCY_MISMATCH:{len(expected)}/{len(actual)}")
    for key, value in BORROWED.items():
        if actual.get(key) != value:
            errors.append("BORROWED_SUPPORT_IDENTITY:" + repr(key))
    for key in CONVERT:
        if actual.get(key) != ("TOP_ONLY", "SV5_20_FIX03", "SV5_20_FIX03_JS08_PASS_THROUGH"):
            errors.append("JS08_PASS_THROUGH_IDENTITY:" + repr(key))

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
            if got != OVERRIDES[key]:
                errors.append("LINK02_OVERRIDE:" + repr(key))
        else:
            got = tuple(iv(row, k) for k in ("takeoff_x", "takeoff_y", "landing_x", "landing_y"))
            want = tuple(int(base_row[k]) for k in ("takeoff_x", "takeoff_y", "landing_x", "landing_y"))
            if got != want:
                errors.append("NON_LINK02_ENDPOINT_CHANGED:" + repr(key))

    if (fix.get("schema") != "SV5_20_JUMP_PLAYER_FIX03/v1" or fix.get("status") != "PASS"
            or fix.get("patch_id") != PATCH_ID or fix.get("input_recovery_digest") != INPUT_DIGEST):
        errors.append("FIX_SUMMARY_IDENTITY")
    for key, value in (("changed_cell_count", 16), ("removed_cell_count", 12),
                       ("converted_cell_count", 4), ("added_cell_count", 0)):
        if fix.get(key) != value:
            errors.append("FIX_SUMMARY_COUNT:" + key)
    for key in ("exact_mirror_x", "all_physical_cases_passed"):
        if fix.get(key) is not True:
            errors.append("FIX_SUMMARY_TRUE:" + key)
    for key in ("top_only_grabbable", "player_tuning_changed", "predecessor_files_changed",
                "reverse_required", "items_used"):
        if fix.get(key) is not False:
            errors.append("FIX_SUMMARY_FALSE:" + key)

    if (scheduler.get("schema") != "SV5_20_PLAYER_SCHEDULER_AUDIT/v2"
            or scheduler.get("policy_id") != POLICY_ID or scheduler.get("status") != "PASS"):
        errors.append("SCHEDULER_IDENTITY")
    for key in ("walkable_frontier_uses_contiguous_support", "body_head_clearance_required",
                "internal_cell_boundary_rejected", "grab_space_exit_verified",
                "all_main_links_diagnosed"):
        if scheduler.get(key) is not True:
            errors.append("SCHEDULER_TRUE:" + key)
    for key in ("hardcoded_step_used", "per_link_magic_frame_used", "coyote_required_for_pass",
                "post_start_teleport_used", "player_tuning_changed"):
        if scheduler.get(key) is not False:
            errors.append("SCHEDULER_FALSE:" + key)
    if scheduler.get("formerly_failed_gate") != "6/6 PASS" or scheduler.get("main_link_diagnostic") != "18/18 PASS":
        errors.append("SCHEDULER_GATE_COUNTS")
    nunit_pass(out / "player_main_link_diagnostic_results.xml", 18, errors)

    case_by_id = {r["case_id"]: r for r in cases}
    main_cases = [r for r in cases if r["case_kind"] == "MAIN_LINK"]
    if len(main_cases) != 18:
        errors.append("MAIN_CASE_COUNT:" + str(len(main_cases)))
    for row in cases:
        if row["geometry_patch_id"] != PATCH_ID or row["outcome"] != "PASS" or iv(row, "teleports_after_start") != 0:
            errors.append("CASE_PATCH_OR_OUTCOME:" + row["case_id"])
    trace_by_case: dict[str, list[dict[str, str]]] = defaultdict(list)
    for row in traces:
        if row["case_id"] in case_by_id:
            trace_by_case[row["case_id"]].append(row)
    for case in main_cases:
        trace = sorted(trace_by_case.get(case["case_id"], []), key=lambda r: iv(r, "step"))
        jump_rows = [r for r in trace if yes(r["input_jump"])]
        if not trace or not jump_rows:
            errors.append("MAIN_NO_JUMP_TRACE:" + case["case_id"])
            continue
        first = jump_rows[0]
        if (not yes(first["grounded_before_step"])
                or fv(first, "frontier_distance") > fv(first, "jump_threshold") + 1e-4
                or first["scheduler_policy_id"] != POLICY_ID):
            errors.append("BAD_FRONTIER_JUMP:" + case["case_id"])
        if min(fv(r, "body_y") for r in trace) < -2.0001:
            errors.append("FAIL_FAST_BOUNDARY:" + case["case_id"])
        order = iv(case, "main_link_order")
        if order == 3:
            events = [r["event"] for r in trace]
            if not all(token in events for token in ("GRAB_ENTER", "GRAB_HOLD", "GRAB_SPACE_EXIT", "PASS_TARGET")):
                errors.append("GRAB_EVENT_CHAIN:" + case["case_id"])
            exit_index = next((i for i, r in enumerate(trace) if r["event"] == "GRAB_SPACE_EXIT"), None)
            if exit_index is None or not any(fv(r, "velocity_y") > 0 for r in trace[exit_index:exit_index + 3]):
                errors.append("GRAB_EXIT_NO_VERTICAL_IMPULSE:" + case["case_id"])

    before = out / "targeted_playmode_before_fix03.xml"
    if (before.stat().st_size != 76428
            or sha(before) != "640170e9e2dc6e7c27a659d00dc43840ec0d9ef84033cee8c846e80902fc60ba"):
        errors.append("BEFORE_FIX03_NOT_BYTE_EXACT")
    if summary.get("status") != "PASS" or summary.get("input_recovery_digest") != INPUT_DIGEST:
        errors.append("PLAYER_SUMMARY_NOT_PASS")
    if summary.get("PlayerVerified", summary.get("readiness", {}).get("PlayerVerified")) is not True:
        errors.append("PLAYER_NOT_VERIFIED")
    if validation.get("status") != "PASS" or validation.get("errors") not in ([], None):
        errors.append("PLAYER_VALIDATION_NOT_PASS")

    svg = (out / "preview/jump_player.svg").read_text(encoding="utf-8-sig")
    for token in (PATCH_ID, "REMOVED 12", "CONVERTED 4", POLICY_ID,
                  "JS_LINK_02", "JS_LINK_03", "JS_LINK_06"):
        if token not in svg:
            errors.append("SVG_FIX03_TOKEN:" + token)

    if errors:
        print("FAIL_INDEPENDENT_JUMP_PLAYER_FIX03")
        print("\n".join(errors))
        return 1
    audit = {
        "schema": "SV5_20_INDEPENDENT_JUMP_PLAYER_FIX03_AUDIT/v1",
        "task": "SV5_20_JUMP_PLAYER", "patch_id": PATCH_ID,
        "scheduler_policy_id": POLICY_ID,
        "status": "PASS_INDEPENDENT_JUMP_PLAYER_FIX03",
        "removed_cells": 12, "converted_cells": 4, "added_cells": 0,
        "formerly_failed_gate": "6/6 PASS", "main_link_diagnostic": "18/18 PASS",
        "physical_cases": len(cases), "trace_states": len(traces),
        "player_tuning_changed": False, "predecessor_files_changed": False,
        "whole_world_builds": 0, "global_endpoint_comparisons": 0,
        "patch_sha256": sha(out / "player_geometry_patch.csv"),
        "composed_occupancy_sha256": sha(out / "player_composed_occupancy.csv"),
        "effective_links_sha256": sha(out / "player_effective_links.csv"),
        "scheduler_audit_sha256": sha(out / "player_scheduler_audit.json"),
        "main_diagnostic_xml_sha256": sha(out / "player_main_link_diagnostic_results.xml"),
        "errors": [],
    }
    audit_path = out / "independent_jump_player_fix03_audit.json"
    audit_path.write_text(json.dumps(audit, ensure_ascii=False, indent=2) + "\n",
                          encoding="utf-8", newline="\n")
    print("PASS_INDEPENDENT_JUMP_PLAYER_FIX03")
    print(json.dumps(audit, ensure_ascii=False, sort_keys=True))
    return 0


if __name__ == "__main__":
    sys.exit(main())
