from __future__ import annotations

import argparse
import csv
import hashlib
import json
import re
import sys
from collections import defaultdict
from pathlib import Path

TASK = "SV5_18_JUMP_CLEARANCE"
RECIPES = ("JUMP012_MIXED_R0", "JUMP012_MIXED_MX")
CATALOG_DIGEST = "19b4b3c37a1b1b11856eb1c946ce8ea64cbc16517dbfe9ace2ae66e4a5504022"
CORRECTION_ID = "SV5_18_ENDPOINT_FIX01"
EFFECTIVE_LINK_ENDPOINT_DIGEST = "9ca58b3070662afece14b812aff892ec8e31bac09938a7445a667f3b2aa8db78"
TAKEOFF_X_FIX = {
    ("JUMP012_MIXED_R0", 2): 13,
    ("JUMP012_MIXED_MX", 2): 10,
    ("JUMP012_MIXED_R0", 6): 5,
    ("JUMP012_MIXED_MX", 6): 18,
}


def truth(value: str) -> bool:
    return str(value).strip().lower() == "true"


def rows(path: Path) -> list[dict[str, str]]:
    with path.open("r", encoding="utf-8-sig", newline="") as handle:
        return list(csv.DictReader(handle))


def integer(row: dict[str, str], key: str) -> int:
    return int(row[key])


def digest(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--project-root", default=".")
    args = parser.parse_args()
    root = Path(args.project_root).resolve()
    source = root / "MapDesign/MCP/GENERATED/SV5_17_JUMP_RECIPES"
    out = root / "MapDesign/MCP/GENERATED/SV5_18_JUMP_CLEARANCE"
    required = [
        out / "jump_clearance.json",
        out / "clearance_links.csv",
        out / "clearance_trace.csv",
        out / "clearance_grab_contacts.csv",
        out / "clearance_validation.json",
        out / "preview/jump_clearance.svg",
        source / "recipe_links.csv",
        source / "recipe_occupancy.csv",
        source / "recipe_grab_edges.csv",
    ]
    errors: list[str] = []
    for path in required:
        if not path.is_file():
            errors.append("MISSING:" + str(path.relative_to(root)).replace("\\", "/"))
    if errors:
        print("FAIL_INDEPENDENT_JUMP_CLEARANCE")
        print("\n".join(errors))
        return 1

    try:
        source_links = rows(source / "recipe_links.csv")
        occupied_rows = rows(source / "recipe_occupancy.csv")
        source_grabs = rows(source / "recipe_grab_edges.csv")
        link_rows = rows(out / "clearance_links.csv")
        trace_rows = rows(out / "clearance_trace.csv")
        grab_rows = rows(out / "clearance_grab_contacts.csv")
        summary = json.loads((out / "jump_clearance.json").read_text(encoding="utf-8-sig"))
        validation = json.loads((out / "clearance_validation.json").read_text(encoding="utf-8-sig"))
    except Exception as exc:
        print("FAIL_INDEPENDENT_JUMP_CLEARANCE")
        print("PARSE:" + str(exc))
        return 1

    expected_link_columns = {
        "recipe_id", "order", "source_link_id", "mode", "direction",
        "takeoff_x", "takeoff_y", "landing_x", "landing_y", "trace_states",
        "source_takeoff_x", "source_takeoff_y", "endpoint_adjusted",
        "correction_id", "clearance_pass", "diagnostic",
    }
    expected_trace_columns = {
        "recipe_id", "link_order", "source_link_id", "sample_order", "phase",
        "foot_x", "foot_y", "body_x", "body_y", "head_x", "head_y",
        "body_clear", "head_clear", "contract_accepted",
    }
    expected_grab_columns = {
        "recipe_id", "source_link_id", "source_grab_edge_id", "contact_x",
        "contact_y", "face", "hang_body_x", "hang_body_y", "hang_head_x",
        "hang_head_y", "pull_up_x", "pull_up_y", "pull_up_head_x",
        "pull_up_head_y", "contact_solid", "exposed", "safe", "trace_bound",
    }
    for label, data, columns in (
        ("LINK_COLUMNS", link_rows, expected_link_columns),
        ("TRACE_COLUMNS", trace_rows, expected_trace_columns),
        ("GRAB_COLUMNS", grab_rows, expected_grab_columns),
    ):
        actual = set(data[0].keys()) if data else set()
        if not columns.issubset(actual):
            errors.append(label + ":" + ",".join(sorted(columns - actual)))

    source_by_key = {(r["recipe_id"], int(r["order"])): r for r in source_links}
    link_by_key = {(r["recipe_id"], int(r["order"])): r for r in link_rows}
    source_grab_by_recipe = {r["recipe_id"]: r for r in source_grabs}
    if len(source_links) != 18 or len(source_by_key) != 18:
        errors.append(f"SOURCE_LINK_COUNT:{len(source_links)}/{len(source_by_key)}")
    if len(link_rows) != 18 or len(link_by_key) != 18 or set(link_by_key) != set(source_by_key):
        errors.append(f"CLEARANCE_LINK_SET:{len(link_rows)}/{len(link_by_key)}")

    occupied: dict[str, set[tuple[int, int]]] = defaultdict(set)
    solid: dict[str, set[tuple[int, int]]] = defaultdict(set)
    for row in occupied_rows:
        point = (integer(row, "x"), integer(row, "y"))
        occupied[row["recipe_id"]].add(point)
        if row["collision"] == "SOLID":
            solid[row["recipe_id"]].add(point)

    traces: dict[tuple[str, int], list[dict[str, str]]] = defaultdict(list)
    seen_trace_keys: set[tuple[str, int, int]] = set()
    for row in trace_rows:
        key = (row["recipe_id"], integer(row, "link_order"))
        sample_key = key + (integer(row, "sample_order"),)
        if sample_key in seen_trace_keys:
            errors.append("DUPLICATE_TRACE_SAMPLE:" + repr(sample_key))
        seen_trace_keys.add(sample_key)
        traces[key].append(row)

    for key in sorted(source_by_key):
        source_row = source_by_key[key]
        link = link_by_key.get(key)
        trace = sorted(traces.get(key, []), key=lambda r: integer(r, "sample_order"))
        if link is None:
            continue
        exact_fields = ("recipe_id", "source_link_id", "mode", "direction",
                        "takeoff_y", "landing_x", "landing_y")
        for field in exact_fields:
            if link.get(field) != source_row.get(field):
                errors.append(f"LINK_BINDING:{key}:{field}")
        expected_takeoff_x = TAKEOFF_X_FIX.get(key, integer(source_row, "takeoff_x"))
        if integer(link, "takeoff_x") != expected_takeoff_x:
            errors.append(f"EFFECTIVE_TAKEOFF:{key}:{link.get('takeoff_x')}")
        if (link.get("source_takeoff_x") != source_row.get("takeoff_x") or
                link.get("source_takeoff_y") != source_row.get("takeoff_y")):
            errors.append("SOURCE_TAKEOFF_BINDING:" + repr(key))
        adjusted = key in TAKEOFF_X_FIX
        if truth(link.get("endpoint_adjusted", "")) != adjusted:
            errors.append("ENDPOINT_ADJUSTED_FLAG:" + repr(key))
        expected_correction = CORRECTION_ID if adjusted else ""
        if link.get("correction_id", "") != expected_correction:
            errors.append("ENDPOINT_CORRECTION_ID:" + repr(key))
        if not truth(link.get("clearance_pass", "")) or link.get("diagnostic", ""):
            errors.append("LINK_NOT_PASS:" + repr(key))
        if not trace or len(trace) > 16:
            errors.append(f"TRACE_COUNT:{key}:{len(trace)}")
            continue
        if [integer(r, "sample_order") for r in trace] != list(range(len(trace))):
            errors.append("TRACE_ORDER:" + repr(key))
        if integer(link, "trace_states") != len(trace):
            errors.append("TRACE_DECLARED_COUNT:" + repr(key))
        points = [(integer(r, "body_x"), integer(r, "body_y")) for r in trace]
        start = (expected_takeoff_x, integer(source_row, "takeoff_y"))
        end = (integer(source_row, "landing_x"), integer(source_row, "landing_y"))
        if points[0] != start or points[-1] != end:
            errors.append("TRACE_ENDPOINT:" + repr(key))
        if trace[0]["phase"] != "TAKEOFF":
            errors.append("TRACE_TAKEOFF_PHASE:" + repr(key))
        expected_end_phase = "PULL_UP" if source_row["mode"] == "JUMP_GRAB" else "LANDING"
        if trace[-1]["phase"] != expected_end_phase:
            errors.append("TRACE_END_PHASE:" + repr(key))
        higher = max(start[1], end[1])
        for index, row in enumerate(trace):
            body = (integer(row, "body_x"), integer(row, "body_y"))
            foot = (integer(row, "foot_x"), integer(row, "foot_y"))
            head = (integer(row, "head_x"), integer(row, "head_y"))
            if foot != body or head != (body[0], body[1] + 1):
                errors.append(f"FOOTPRINT:{key}:{index}")
            if not (0 <= body[0] < 24 and 0 <= body[1] < 32 and 0 <= head[1] < 32):
                errors.append(f"OUT_OF_LOCAL_ROOM:{key}:{index}")
            if body in occupied[key[0]] or head in occupied[key[0]]:
                errors.append(f"OCCUPIED_FOOTPRINT:{key}:{index}")
            if not truth(row.get("body_clear", "")) or not truth(row.get("head_clear", "")) or not truth(row.get("contract_accepted", "")):
                errors.append(f"FALSE_CLEARANCE_CLAIM:{key}:{index}")
            if body[1] > higher + 3:
                errors.append(f"APEX:{key}:{index}")
        direction = source_row["direction"]
        for index, (a, b) in enumerate(zip(points, points[1:])):
            dx, dy = b[0] - a[0], b[1] - a[1]
            if (dx == 0 and dy == 0) or abs(dx) > 1 or abs(dy) > 1:
                errors.append(f"NON_LOCAL_STEP:{key}:{index}")
            if direction == "LEFT_TO_RIGHT" and dx < 0:
                errors.append(f"HORIZONTAL_REVERSAL:{key}:{index}")
            if direction == "RIGHT_TO_LEFT" and dx > 0:
                errors.append(f"HORIZONTAL_REVERSAL:{key}:{index}")
            if dx and dy:
                corners = ((b[0], a[1]), (a[0], b[1]))
                for corner in corners:
                    source_grab = source_grab_by_recipe.get(key[0], {})
                    contact = ((int(source_grab["contact_x"]), int(source_grab["contact_y"]))
                               if source_grab else None)
                    intentional_pull_up_contact = (
                        source_row["mode"] == "JUMP_GRAB" and
                        trace[index]["phase"] == "HANG" and
                        trace[index + 1]["phase"] == "PULL_UP" and
                        corner == contact and corner in solid[key[0]])
                    blocked = (corner in occupied[key[0]] or
                               (corner[0], corner[1] + 1) in occupied[key[0]])
                    if blocked and not intentional_pull_up_contact:
                        errors.append(f"DIAGONAL_CORNER_CLIP:{key}:{index}:{corner}")

        phases = [r["phase"] for r in trace]
        if source_row["mode"] == "JUMP_GRAB":
            if integer(source_row, "rise") != 2 or phases.count("HANG") != 1 or phases.count("PULL_UP") != 1:
                errors.append("GRAB_PHASES:" + repr(key))
            elif phases.index("HANG") >= phases.index("PULL_UP"):
                errors.append("GRAB_PHASE_ORDER:" + repr(key))
        elif any(phase not in {"TAKEOFF", "AIR", "LANDING"} for phase in phases):
            errors.append("NORMAL_PHASE_SET:" + repr(key))

    if set(traces) != set(source_by_key):
        errors.append("TRACE_LINK_SET_MISMATCH")

    r0 = {key[1]: sorted(traces[key], key=lambda r: integer(r, "sample_order"))
          for key in traces if key[0] == RECIPES[0]}
    mx = {key[1]: sorted(traces[key], key=lambda r: integer(r, "sample_order"))
          for key in traces if key[0] == RECIPES[1]}
    for order in range(9):
        left, right = r0.get(order, []), mx.get(order, [])
        if len(left) != len(right):
            errors.append(f"MIRROR_COUNT:{order}")
            continue
        for index, (a, b) in enumerate(zip(left, right)):
            if (integer(b, "body_x") != 23 - integer(a, "body_x") or
                    integer(b, "body_y") != integer(a, "body_y") or
                    b["phase"] != a["phase"]):
                errors.append(f"MIRROR_TRACE:{order}:{index}")

    grab_by_recipe = {r["recipe_id"]: r for r in grab_rows}
    if len(grab_rows) != 2 or set(grab_by_recipe) != set(RECIPES):
        errors.append(f"GRAB_COUNT:{len(grab_rows)}")
    for recipe in RECIPES:
        expected = source_grab_by_recipe.get(recipe)
        actual = grab_by_recipe.get(recipe)
        if not expected or not actual:
            continue
        mapping = {
            "source_grab_edge_id": "source_grab_edge_id", "contact_x": "contact_x",
            "contact_y": "contact_y", "face": "face", "hang_body_x": "hang_body_x",
            "hang_body_y": "hang_body_y", "hang_head_x": "hang_head_x",
            "hang_head_y": "hang_head_y", "pull_up_x": "pull_up_x",
            "pull_up_y": "pull_up_y", "pull_up_head_x": "pull_up_head_x",
            "pull_up_head_y": "pull_up_head_y",
        }
        for a, b in mapping.items():
            if actual.get(a) != expected.get(b):
                errors.append(f"GRAB_BINDING:{recipe}:{a}")
        contact = (integer(actual, "contact_x"), integer(actual, "contact_y"))
        if contact not in solid[recipe]:
            errors.append("GRAB_CONTACT_NOT_SOLID:" + recipe)
        if not all(truth(actual.get(k, "")) for k in ("contact_solid", "exposed", "safe", "trace_bound")):
            errors.append("GRAB_FLAGS:" + recipe)

    readiness = summary.get("readiness", {})
    expected_readiness = {
        "JumpRecipeReady": True, "ComposedGeometryReady": True,
        "SweptClearanceReady": True, "RecoveryReady": False,
        "PlayerVerified": False,
    }
    if summary.get("status") != "PASS" or summary.get("input_catalog_digest") != CATALOG_DIGEST:
        errors.append("SUMMARY_IDENTITY")
    if (summary.get("endpoint_correction_id") != CORRECTION_ID or
            summary.get("endpoint_correction_count") != 4 or
            summary.get("effective_link_endpoint_digest") != EFFECTIVE_LINK_ENDPOINT_DIGEST):
        errors.append("SUMMARY_ENDPOINT_CORRECTION")
    if summary.get("recipe_count") != 2 or summary.get("link_count") != 18 or summary.get("passed_link_count") != 18:
        errors.append("SUMMARY_COUNTS")
    if readiness != expected_readiness:
        errors.append("READINESS:" + repr(readiness))
    if any(summary.get(k) != 0 for k in ("whole_world_builds", "whole_world_searches", "global_endpoint_comparisons")):
        errors.append("GLOBAL_WORK_NOT_ZERO")
    if validation.get("status") != "PASS" or validation.get("errors") != []:
        errors.append("VALIDATION_NOT_PASS")

    owned_source = [
        root / "Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/Sv5JumpClearance.cs",
        root / "Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/Sv5JumpClearanceExport.cs",
        root / "Assets/_Game/Tests/EditMode/Map/SV5/Sv5JumpClearanceTests.cs",
    ]
    for path in owned_source:
        if not path.is_file():
            errors.append("OWNED_SOURCE_MISSING:" + path.name)
            continue
        text = path.read_text(encoding="utf-8-sig", errors="replace")
        for token in (r"\bSectorId\b", r"624\s*[x×]\s*416", r"all.?endpoint"):
            if re.search(token, text, re.I):
                errors.append(f"FORBIDDEN_GLOBAL_CONCEPT:{path.name}:{token}")

    audit = {
        "schema": "SV5_18_INDEPENDENT_JUMP_CLEARANCE_AUDIT/v1",
        "task": TASK,
        "status": "PASS" if not errors else "FAIL",
        "recipe_count": 2,
        "link_count": len(link_rows),
        "trace_state_count": len(trace_rows),
        "grab_contact_count": len(grab_rows),
        "endpoint_correction_id": CORRECTION_ID,
        "endpoint_correction_count": len(TAKEOFF_X_FIX),
        "effective_link_endpoint_digest": EFFECTIVE_LINK_ENDPOINT_DIGEST,
        "whole_world_builds": 0,
        "whole_world_searches": 0,
        "global_endpoint_comparisons": 0,
        "clearance_links_sha256": digest(out / "clearance_links.csv"),
        "clearance_trace_sha256": digest(out / "clearance_trace.csv"),
        "errors": errors,
    }
    out.mkdir(parents=True, exist_ok=True)
    (out / "independent_jump_clearance_audit.json").write_text(
        json.dumps(audit, ensure_ascii=False, indent=2) + "\n", encoding="utf-8", newline="\n")
    if errors:
        print("FAIL_INDEPENDENT_JUMP_CLEARANCE")
        print("\n".join(errors))
        return 1
    print("PASS_INDEPENDENT_JUMP_CLEARANCE")
    print(json.dumps({k: audit[k] for k in ("recipe_count", "link_count", "trace_state_count", "grab_contact_count")}, ensure_ascii=False))
    return 0


if __name__ == "__main__":
    sys.exit(main())
