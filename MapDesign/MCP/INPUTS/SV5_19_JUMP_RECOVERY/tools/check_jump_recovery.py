from __future__ import annotations

import argparse
import csv
import hashlib
import json
import re
import sys
from collections import defaultdict
from pathlib import Path

TASK = "SV5_19_JUMP_RECOVERY"
INPUT_DIGEST = "48c3754b8109aa0601760567010b9aece67c5305907dfb76c7b155b3d4c37f13"
RECIPES = ("JUMP012_MIXED_R0", "JUMP012_MIXED_MX")


def rows(path: Path) -> list[dict[str, str]]:
    with path.open("r", encoding="utf-8-sig", newline="") as handle:
        return list(csv.DictReader(handle))


def iv(row: dict[str, str], key: str) -> int:
    return int(row[key])


def yes(value: str) -> bool:
    return str(value).strip().lower() == "true"


def sha(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def check_columns(label: str, data: list[dict[str, str]], required: set[str],
                  errors: list[str]) -> None:
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
    out = root / "MapDesign/MCP/GENERATED/SV5_19_JUMP_RECOVERY"
    required_files = [
        base / "recipe_occupancy.csv",
        clear / "jump_clearance.json",
        clear / "clearance_links.csv",
        clear / "clearance_trace.csv",
        clear / "clearance_grab_contacts.csv",
        out / "jump_recovery.json",
        out / "recovery_overlay.csv",
        out / "recovery_miss_probes.csv",
        out / "recovery_routes.csv",
        out / "recovery_links.csv",
        out / "recovery_trace.csv",
        out / "recovery_validation.json",
        out / "preview/jump_recovery.svg",
    ]
    errors: list[str] = []
    for path in required_files:
        if not path.is_file():
            errors.append("MISSING:" + str(path.relative_to(root)).replace("\\", "/"))
    if errors:
        print("FAIL_INDEPENDENT_JUMP_RECOVERY")
        print("\n".join(errors))
        return 1

    try:
        base_occ_rows = rows(base / "recipe_occupancy.csv")
        clearance = json.loads((clear / "jump_clearance.json").read_text(encoding="utf-8-sig"))
        main_links = rows(clear / "clearance_links.csv")
        main_trace = rows(clear / "clearance_trace.csv")
        main_grabs = rows(clear / "clearance_grab_contacts.csv")
        overlay_rows = rows(out / "recovery_overlay.csv")
        probe_rows = rows(out / "recovery_miss_probes.csv")
        route_rows = rows(out / "recovery_routes.csv")
        recovery_links = rows(out / "recovery_links.csv")
        recovery_trace = rows(out / "recovery_trace.csv")
        summary = json.loads((out / "jump_recovery.json").read_text(encoding="utf-8-sig"))
        validation = json.loads((out / "recovery_validation.json").read_text(encoding="utf-8-sig"))
    except Exception as exc:
        print("FAIL_INDEPENDENT_JUMP_RECOVERY")
        print("PARSE:" + str(exc))
        return 1

    check_columns("OVERLAY_COLUMNS", overlay_rows, {
        "recipe_id", "group_id", "x", "y", "collision", "role", "owner_id"
    }, errors)
    check_columns("PROBE_COLUMNS", probe_rows, {
        "recipe_id", "failed_link_order", "source_link_id", "probe_order",
        "source_sample_order", "origin_x", "origin_y", "catch_source",
        "catch_group_id", "catch_x", "catch_y", "landing_body_x",
        "landing_body_y", "landing_head_x", "landing_head_y", "fall_distance",
        "caught"
    }, errors)
    check_columns("ROUTE_COLUMNS", route_rows, {
        "recipe_id", "route_id", "failed_link_order", "checkpoint_link_order",
        "route_kind", "start_x", "start_y", "end_x", "end_y",
        "recovery_link_ids", "recovery_pass", "reverse_required", "diagnostic"
    }, errors)
    check_columns("RECOVERY_LINK_COLUMNS", recovery_links, {
        "recipe_id", "route_id", "link_order", "recovery_link_id", "mode",
        "source_x", "source_y", "target_x", "target_y", "rise", "trace_states"
    }, errors)
    check_columns("RECOVERY_TRACE_COLUMNS", recovery_trace, {
        "recipe_id", "route_id", "recovery_link_order", "recovery_link_id",
        "sample_order", "phase", "body_x", "body_y", "head_x", "head_y",
        "body_clear", "head_clear", "contract_accepted"
    }, errors)

    if clearance.get("status") != "PASS" or clearance.get("clearance_digest") != INPUT_DIGEST:
        errors.append("SV5_18_CLEARANCE_IDENTITY")
    ready18 = clearance.get("readiness", {})
    if not all(ready18.get(k) is True for k in
               ("JumpRecipeReady", "ComposedGeometryReady", "SweptClearanceReady")):
        errors.append("SV5_18_NOT_READY")

    base_occ: dict[str, set[tuple[int, int]]] = defaultdict(set)
    base_collision: dict[tuple[str, int, int], str] = {}
    for row in base_occ_rows:
        point = (iv(row, "x"), iv(row, "y"))
        base_occ[row["recipe_id"]].add(point)
        base_collision[(row["recipe_id"], point[0], point[1])] = row["collision"]

    overlay: dict[str, set[tuple[int, int]]] = defaultdict(set)
    overlay_collision: dict[tuple[str, int, int], str] = {}
    groups: dict[tuple[str, str], list[tuple[int, int]]] = defaultdict(list)
    group_owners: dict[tuple[str, str], set[str]] = defaultdict(set)
    for row in overlay_rows:
        recipe = row["recipe_id"]
        point = (iv(row, "x"), iv(row, "y"))
        if recipe not in RECIPES or not (0 <= point[0] < 24 and 0 <= point[1] < 32):
            errors.append("OVERLAY_OUT_OF_SCOPE:" + repr((recipe, point)))
        if row["collision"] not in {"SOLID", "TOP_ONLY"} or row["role"] != "RECOVERY_CATCH":
            errors.append("OVERLAY_TYPE:" + repr((recipe, point)))
        if point in base_occ[recipe] or point in overlay[recipe]:
            errors.append("OVERLAY_OCCUPANCY_COLLISION:" + repr((recipe, point)))
        overlay[recipe].add(point)
        overlay_collision[(recipe, point[0], point[1])] = row["collision"]
        groups[(recipe, row["group_id"])].append(point)
        group_owners[(recipe, row["group_id"])].add(row["owner_id"])

    for recipe in RECIPES:
        recipe_groups = [points for (r, _), points in groups.items() if r == recipe]
        if not (3 <= len(recipe_groups) <= 9) or len(overlay[recipe]) > 36:
            errors.append(f"OVERLAY_DENSITY:{recipe}:{len(recipe_groups)}:{len(overlay[recipe])}")
        for points in recipe_groups:
            xs = sorted(x for x, _ in points)
            ys = {y for _, y in points}
            if len(ys) != 1 or not (2 <= len(xs) <= 5) or xs != list(range(xs[0], xs[-1] + 1)):
                errors.append("OVERLAY_GROUP_SHAPE:" + repr((recipe, points)))
    if any(len(owners) != 1 or "" in owners for owners in group_owners.values()):
        errors.append("OVERLAY_GROUP_OWNER")

    mirror_overlay = {(23 - x, y, overlay_collision[(RECIPES[0], x, y)])
                      for x, y in overlay[RECIPES[0]]}
    actual_mirror_overlay = {(x, y, overlay_collision[(RECIPES[1], x, y)])
                             for x, y in overlay[RECIPES[1]]}
    if mirror_overlay != actual_mirror_overlay:
        errors.append("OVERLAY_MIRROR")

    main_footprint: dict[str, set[tuple[int, int]]] = defaultdict(set)
    main_trace_by_link: dict[tuple[str, int], list[dict[str, str]]] = defaultdict(list)
    for row in main_trace:
        recipe = row["recipe_id"]
        main_trace_by_link[(recipe, iv(row, "link_order"))].append(row)
        main_footprint[recipe].add((iv(row, "body_x"), iv(row, "body_y")))
        main_footprint[recipe].add((iv(row, "head_x"), iv(row, "head_y")))
    for recipe in RECIPES:
        overlap = overlay[recipe] & main_footprint[recipe]
        if overlap:
            errors.append("OVERLAY_MAIN_TRACE_OVERLAP:" + repr((recipe, sorted(overlap))))
    for row in main_grabs:
        point = (iv(row, "contact_x"), iv(row, "contact_y"))
        if point in overlay[row["recipe_id"]]:
            errors.append("OVERLAY_GRAB_CONTACT_OVERLAP:" + repr((row["recipe_id"], point)))

    combined = {recipe: base_occ[recipe] | overlay[recipe] for recipe in RECIPES}
    for recipe in RECIPES:
        solid = {p for p in base_occ[recipe]
                 if base_collision.get((recipe, p[0], p[1])) == "SOLID"}
        solid |= {p for p in overlay[recipe]
                  if overlay_collision.get((recipe, p[0], p[1])) == "SOLID"}
        for x in range(19):
            for y in range(27):
                if all((xx, yy) in solid for xx in range(x, x + 6)
                       for yy in range(y, y + 6)):
                    errors.append(f"SOLID_FILL_6X6:{recipe}:{x}:{y}")

    link_by_key = {(r["recipe_id"], iv(r, "order")): r for r in main_links}
    probe_by_key = {(r["recipe_id"], iv(r, "failed_link_order")): r for r in probe_rows}
    if len(probe_rows) != 18 or len(probe_by_key) != 18 or set(probe_by_key) != set(link_by_key):
        errors.append(f"PROBE_SET:{len(probe_rows)}/{len(probe_by_key)}")
    expected_origins: dict[tuple[str, int], tuple[int, int, int]] = {}
    for key, trace in main_trace_by_link.items():
        air = sorted((r for r in trace if r["phase"] == "AIR"), key=lambda r: iv(r, "sample_order"))
        if not air:
            errors.append("NO_AIR_SAMPLE:" + repr(key))
            continue
        selected = air[(len(air) - 1) // 2]
        expected_origins[key] = (iv(selected, "sample_order"), iv(selected, "body_x"), iv(selected, "body_y"))

    landing_by_key: dict[tuple[str, int], tuple[int, int]] = {}
    for key, probe in probe_by_key.items():
        expected = expected_origins.get(key)
        actual = (iv(probe, "source_sample_order"), iv(probe, "origin_x"), iv(probe, "origin_y"))
        if expected != actual or iv(probe, "probe_order") != 0:
            errors.append("PROBE_ORIGIN:" + repr(key))
        if probe["source_link_id"] != link_by_key[key]["source_link_id"] or not yes(probe["caught"]):
            errors.append("PROBE_BINDING:" + repr(key))
        x, origin_y = actual[1], actual[2]
        first = next((y for y in range(origin_y - 1, -1, -1) if (x, y) in combined[key[0]]), None)
        catch = (iv(probe, "catch_x"), iv(probe, "catch_y"))
        if first is None or catch != (x, first):
            errors.append("FIRST_CATCH:" + repr(key))
            continue
        landing = (x, first + 1)
        head = (x, first + 2)
        landing_by_key[key] = landing
        if (iv(probe, "landing_body_x"), iv(probe, "landing_body_y")) != landing:
            errors.append("CATCH_LANDING_BODY:" + repr(key))
        if (iv(probe, "landing_head_x"), iv(probe, "landing_head_y")) != head:
            errors.append("CATCH_LANDING_HEAD:" + repr(key))
        if landing in combined[key[0]] or head in combined[key[0]]:
            errors.append("BLOCKED_CATCH_FOOTPRINT:" + repr(key))
        if iv(probe, "fall_distance") != origin_y - landing[1] or iv(probe, "fall_distance") < 0:
            errors.append("FALL_DISTANCE:" + repr(key))
        source = "RECOVERY" if catch in overlay[key[0]] else "BASE"
        if probe["catch_source"] != source:
            errors.append("CATCH_SOURCE:" + repr(key))
        if source == "RECOVERY" and (key[0], probe["catch_group_id"]) not in groups:
            errors.append("CATCH_GROUP:" + repr(key))
        for body_y in range(origin_y, landing[1] - 1, -1):
            if (x, body_y) in combined[key[0]] or (x, body_y + 1) in combined[key[0]]:
                errors.append("BLOCKED_FALL_RAY:" + repr((key, body_y)))

    route_by_key = {(r["recipe_id"], iv(r, "failed_link_order")): r for r in route_rows}
    if len(route_rows) != 18 or len(route_by_key) != 18 or set(route_by_key) != set(link_by_key):
        errors.append(f"ROUTE_SET:{len(route_rows)}/{len(route_by_key)}")
    links_by_route: dict[tuple[str, str], list[dict[str, str]]] = defaultdict(list)
    for row in recovery_links:
        links_by_route[(row["recipe_id"], row["route_id"])].append(row)
    traces_by_link: dict[tuple[str, str, int], list[dict[str, str]]] = defaultdict(list)
    for row in recovery_trace:
        traces_by_link[(row["recipe_id"], row["route_id"], iv(row, "recovery_link_order"))].append(row)

    checkpoint_orders: dict[str, set[int]] = defaultdict(set)
    for key, route in route_by_key.items():
        recipe, failed = key
        checkpoint = iv(route, "checkpoint_link_order")
        checkpoint_orders[recipe].add(checkpoint)
        if checkpoint > failed or checkpoint < 0 or checkpoint > 8:
            errors.append("CHECKPOINT_ORDER:" + repr(key))
            continue
        if route["route_id"] == "" or not yes(route["recovery_pass"]) or yes(route["reverse_required"]) or route["diagnostic"]:
            errors.append("ROUTE_FLAGS:" + repr(key))
        start = landing_by_key.get(key)
        end = (iv(link_by_key[(recipe, checkpoint)], "takeoff_x"),
               iv(link_by_key[(recipe, checkpoint)], "takeoff_y"))
        declared_start = (iv(route, "start_x"), iv(route, "start_y"))
        declared_end = (iv(route, "end_x"), iv(route, "end_y"))
        if start != declared_start or end != declared_end:
            errors.append("ROUTE_ENDPOINT:" + repr(key))
        route_links = sorted(links_by_route.get((recipe, route["route_id"]), []),
                             key=lambda r: iv(r, "link_order"))
        ids = [r["recovery_link_id"] for r in route_links]
        declared_ids = [value for value in route["recovery_link_ids"].split(";") if value]
        if ids != declared_ids or [iv(r, "link_order") for r in route_links] != list(range(len(route_links))):
            errors.append("ROUTE_LINK_LIST:" + repr(key))
        if route["route_kind"] == "ALREADY_AT_CHECKPOINT":
            if start != end or route_links:
                errors.append("DIRECT_ROUTE_INVALID:" + repr(key))
            continue
        if route["route_kind"] != "LINKED" or not route_links:
            errors.append("LINKED_ROUTE_INVALID:" + repr(key))
            continue
        if (iv(route_links[0], "source_x"), iv(route_links[0], "source_y")) != start or (
                iv(route_links[-1], "target_x"), iv(route_links[-1], "target_y")) != end:
            errors.append("RECOVERY_LINK_ENDPOINT:" + repr(key))
        for a, b in zip(route_links, route_links[1:]):
            if (iv(a, "target_x"), iv(a, "target_y")) != (iv(b, "source_x"), iv(b, "source_y")):
                errors.append("RECOVERY_LINK_DISCONTINUITY:" + repr(key))
        for link in route_links:
            order = iv(link, "link_order")
            source_point = (iv(link, "source_x"), iv(link, "source_y"))
            target_point = (iv(link, "target_x"), iv(link, "target_y"))
            rise = target_point[1] - source_point[1]
            mode = link["mode"]
            if iv(link, "rise") != rise or mode not in {"WALK", "JUMP", "DROP"}:
                errors.append("RECOVERY_LINK_MODE:" + repr((key, order)))
            if (mode == "WALK" and rise != 0) or (mode == "JUMP" and rise > 1) or (mode == "DROP" and rise > 0):
                errors.append("RECOVERY_LINK_RISE:" + repr((key, order)))
            for point, label in ((source_point, "SOURCE"), (target_point, "TARGET")):
                if (point[0], point[1] - 1) not in combined[recipe]:
                    errors.append(f"UNSUPPORTED_{label}:" + repr((key, order, point)))
            trace = sorted(traces_by_link.get((recipe, route["route_id"], order), []),
                           key=lambda r: iv(r, "sample_order"))
            if not trace or len(trace) > 16 or iv(link, "trace_states") != len(trace):
                errors.append("RECOVERY_TRACE_COUNT:" + repr((key, order)))
                continue
            points = [(iv(r, "body_x"), iv(r, "body_y")) for r in trace]
            if points[0] != source_point or points[-1] != target_point:
                errors.append("RECOVERY_TRACE_ENDPOINT:" + repr((key, order)))
            if trace[0]["phase"] != "SOURCE" or trace[-1]["phase"] != "TARGET":
                errors.append("RECOVERY_TRACE_PHASE:" + repr((key, order)))
            for index, row in enumerate(trace):
                body = points[index]
                head = (iv(row, "head_x"), iv(row, "head_y"))
                if head != (body[0], body[1] + 1) or body in combined[recipe] or head in combined[recipe]:
                    errors.append("RECOVERY_TRACE_BLOCKED:" + repr((key, order, index)))
                if not yes(row["body_clear"]) or not yes(row["head_clear"]) or not yes(row["contract_accepted"]):
                    errors.append("RECOVERY_TRACE_FALSE_FLAG:" + repr((key, order, index)))
            for index, (a, b) in enumerate(zip(points, points[1:])):
                dx, dy = b[0] - a[0], b[1] - a[1]
                if (dx == 0 and dy == 0) or abs(dx) > 1 or abs(dy) > 1:
                    errors.append("RECOVERY_NON_LOCAL_STEP:" + repr((key, order, index)))
                if dx and dy:
                    for corner in ((b[0], a[1]), (a[0], b[1])):
                        if corner in combined[recipe] or (corner[0], corner[1] + 1) in combined[recipe]:
                            errors.append("RECOVERY_DIAGONAL_CLIP:" + repr((key, order, index, corner)))

    if set(traces_by_link) != {(r["recipe_id"], r["route_id"], iv(r, "link_order")) for r in recovery_links}:
        errors.append("RECOVERY_TRACE_LINK_SET")
    for recipe in RECIPES:
        if len(checkpoint_orders[recipe]) < 2:
            errors.append("CHECKPOINT_DIVERSITY:" + recipe)

    for failed in range(9):
        a, b = route_by_key.get((RECIPES[0], failed)), route_by_key.get((RECIPES[1], failed))
        pa, pb = probe_by_key.get((RECIPES[0], failed)), probe_by_key.get((RECIPES[1], failed))
        if not a or not b or not pa or not pb:
            continue
        if (iv(pb, "origin_x") != 23 - iv(pa, "origin_x") or iv(pb, "origin_y") != iv(pa, "origin_y") or
                iv(pb, "catch_x") != 23 - iv(pa, "catch_x") or iv(pb, "catch_y") != iv(pa, "catch_y")):
            errors.append("PROBE_MIRROR:" + str(failed))
        if (iv(b, "checkpoint_link_order") != iv(a, "checkpoint_link_order") or
                iv(b, "start_x") != 23 - iv(a, "start_x") or iv(b, "start_y") != iv(a, "start_y") or
                iv(b, "end_x") != 23 - iv(a, "end_x") or iv(b, "end_y") != iv(a, "end_y")):
            errors.append("ROUTE_MIRROR:" + str(failed))
        la = sorted(links_by_route.get((RECIPES[0], a["route_id"]), []), key=lambda r: iv(r, "link_order"))
        lb = sorted(links_by_route.get((RECIPES[1], b["route_id"]), []), key=lambda r: iv(r, "link_order"))
        if len(la) != len(lb):
            errors.append("ROUTE_LINK_MIRROR_COUNT:" + str(failed))
        for index, (xa, xb) in enumerate(zip(la, lb)):
            if (iv(xb, "source_x") != 23 - iv(xa, "source_x") or iv(xb, "source_y") != iv(xa, "source_y") or
                    iv(xb, "target_x") != 23 - iv(xa, "target_x") or iv(xb, "target_y") != iv(xa, "target_y") or
                    xb["mode"] != xa["mode"]):
                errors.append("ROUTE_LINK_MIRROR:" + repr((failed, index)))

    expected_ready = {"JumpRecipeReady": True, "ComposedGeometryReady": True,
                      "SweptClearanceReady": True, "RecoveryReady": True,
                      "PlayerVerified": False}
    if summary.get("status") != "PASS" or summary.get("input_clearance_digest") != INPUT_DIGEST:
        errors.append("SUMMARY_IDENTITY")
    if summary.get("recipe_count") != 2 or summary.get("probe_count") != 18 or summary.get("passed_probe_count") != 18 or summary.get("passed_route_count") != 18:
        errors.append("SUMMARY_COUNTS")
    if summary.get("readiness") != expected_ready or summary.get("reverse_required") is not False:
        errors.append("SUMMARY_READINESS")
    if any(summary.get(k) != 0 for k in ("whole_world_builds", "whole_world_searches", "global_endpoint_comparisons")):
        errors.append("GLOBAL_WORK_NOT_ZERO")
    if not re.fullmatch(r"[0-9a-f]{64}", str(summary.get("recovery_digest", ""))):
        errors.append("RECOVERY_DIGEST")
    if validation.get("status") != "PASS" or validation.get("errors") != []:
        errors.append("VALIDATION_NOT_PASS")

    for path in [
        root / "Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/Sv5JumpRecovery.cs",
        root / "Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/Sv5JumpRecoveryExport.cs",
        root / "Assets/_Game/Tests/EditMode/Map/SV5/Sv5JumpRecoveryTests.cs",
    ]:
        if not path.is_file():
            errors.append("OWNED_SOURCE_MISSING:" + path.name)
            continue
        text = path.read_text(encoding="utf-8-sig", errors="replace")
        for token in (r"\bSectorId\b", r"624\s*[x×]\s*416", r"all.?endpoint"):
            if re.search(token, text, re.I):
                errors.append(f"FORBIDDEN_GLOBAL_CONCEPT:{path.name}:{token}")

    audit = {
        "schema": "SV5_19_INDEPENDENT_JUMP_RECOVERY_AUDIT/v1",
        "task": TASK,
        "status": "PASS" if not errors else "FAIL",
        "recipe_count": 2,
        "overlay_cells": len(overlay_rows),
        "overlay_groups": len(groups),
        "probe_count": len(probe_rows),
        "route_count": len(route_rows),
        "recovery_link_count": len(recovery_links),
        "recovery_trace_state_count": len(recovery_trace),
        "whole_world_builds": 0,
        "whole_world_searches": 0,
        "global_endpoint_comparisons": 0,
        "overlay_sha256": sha(out / "recovery_overlay.csv"),
        "probes_sha256": sha(out / "recovery_miss_probes.csv"),
        "routes_sha256": sha(out / "recovery_routes.csv"),
        "errors": errors,
    }
    out.mkdir(parents=True, exist_ok=True)
    (out / "independent_jump_recovery_audit.json").write_text(
        json.dumps(audit, ensure_ascii=False, indent=2) + "\n", encoding="utf-8", newline="\n")
    if errors:
        print("FAIL_INDEPENDENT_JUMP_RECOVERY")
        print("\n".join(errors))
        return 1
    print("PASS_INDEPENDENT_JUMP_RECOVERY")
    print(json.dumps({k: audit[k] for k in ("overlay_cells", "overlay_groups", "probe_count", "route_count", "recovery_link_count")}, ensure_ascii=False))
    return 0


if __name__ == "__main__":
    sys.exit(main())
