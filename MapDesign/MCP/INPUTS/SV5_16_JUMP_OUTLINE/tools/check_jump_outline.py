from __future__ import annotations

import argparse
import csv
import json
import re
import sys
from pathlib import Path

TASK = "SV5_16_JUMP_OUTLINE"
GEN_REL = Path("MapDesign/MCP/GENERATED") / TASK
BASE_REL = Path("MapDesign/MCP/GENERATED/SV5_15_JUMP_GRAB")
WIDTH = 24
HEIGHT = 32
DEPTHS = {
    "JS00_ENTRY_SOLID": {0: 1, 1: 1, 2: 0},
    "JS02_SOLID": {12: 1, 13: 2, 14: 1},
    "JS04_SOLID": {14: 2, 15: 1},
    "JS06_SOLID": {4: 2, 5: 2, 6: 1},
    "JS08_SOLID": {3: 2, 4: 1},
}


def load_csv(path: Path, columns: list[str], errors: list[str]) -> list[dict[str, str]]:
    if not path.is_file():
        errors.append("missing export: " + path.as_posix())
        return []
    with path.open("r", encoding="utf-8-sig", newline="") as handle:
        reader = csv.DictReader(handle)
        fields = reader.fieldnames or []
        missing = [column for column in columns if column not in fields]
        if missing:
            errors.append(path.name + " missing columns: " + ",".join(missing))
        return list(reader)


def json_file(path: Path, errors: list[str]) -> dict:
    if not path.is_file():
        errors.append("missing export: " + path.as_posix())
        return {}
    try:
        value = json.loads(path.read_text(encoding="utf-8-sig"))
        if not isinstance(value, dict):
            raise ValueError("root must be object")
        return value
    except Exception as exc:
        errors.append("invalid JSON " + path.name + ": " + str(exc))
        return {}


def integer(row: dict[str, str], key: str, errors: list[str], owner: str) -> int:
    try:
        return int(row[key])
    except Exception:
        errors.append(owner + " invalid integer " + key)
        return 0


def flag(value: str) -> bool:
    return value.strip().lower() in {"true", "1", "yes"}


def normalized(rows: list[dict[str, str]]) -> list[tuple[tuple[str, str], ...]]:
    return sorted(tuple(sorted(row.items())) for row in rows)


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--project-root", default=".")
    parser.add_argument("--output")
    args = parser.parse_args()
    root = Path(args.project_root).resolve()
    gen = root / GEN_REL
    base = root / BASE_REL
    output = Path(args.output).resolve() if args.output else gen / "independent_jump_outline_audit.json"
    errors: list[str] = []

    geometry_path = root / "Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/Sv5JumpOutlineGeometry.cs"
    export_path = root / "Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/Sv5JumpOutlineExport.cs"
    test_path = root / "Assets/_Game/Tests/EditMode/Map/SV5/Sv5JumpOutlineTests.cs"
    doc_path = root / "MapDesign/MCP/SV5/24_JUMP_OUTLINE_V5.md"
    for path_value in (geometry_path, export_path, test_path, doc_path):
        if not path_value.is_file():
            errors.append("missing owned file: " + path_value.relative_to(root).as_posix())
    runtime = "\n".join(path_value.read_text(encoding="utf-8-sig", errors="replace")
                          for path_value in (geometry_path, export_path) if path_value.is_file())
    for symbol in ("Sv5JumpOutlineGeometry", "Sv5JumpOutlinePlan", "Sv5JumpOutlineExport",
                   "Sv5JumpGrabGeometry.CreateCanonicalLocalFixture"):
        if symbol not in runtime:
            errors.append("missing runtime symbol: " + symbol)
    for pattern in (r"\bSectorId\b", r"\bsector_id\b", r"\bsector_index\b", r"48\s*[xX×]\s*32"):
        if re.search(pattern, runtime):
            errors.append("retired partition symbol in SV5_16 runtime: " + pattern)

    summary = json_file(gen / "jump_outline.json", errors)
    validation = json_file(gen / "jump_outline_validation.json", errors)
    base_summary = json_file(base / "jump_grab.json", errors)
    supports = load_csv(base / "supports.csv",
        ["support_id", "kind", "x", "y", "width", "height", "top_y", "top_x_min", "top_x_max"], errors)
    base_cells = load_csv(base / "support_cells.csv",
        ["support_id", "x", "y", "kind", "collision"], errors)
    depths = load_csv(gen / "deformation_depths.csv",
        ["owner_support_id", "x", "support_bottom_y", "depth", "emitted_cell_count"], errors)
    outline_cells = load_csv(gen / "outline_cells.csv",
        ["outline_cell_id", "owner_support_id", "x", "y", "depth", "collision"], errors)
    final_rows = load_csv(gen / "final_occupancy.csv",
        ["x", "y", "collision", "owner_id", "source", "support_kind"], errors)
    route_links = load_csv(gen / "route_links.csv",
        ["order", "link_id", "source_support_id", "target_support_id", "mode", "direction",
         "takeoff_x", "takeoff_y", "landing_x", "landing_y", "gap_air", "rise",
         "grab_edge_id", "required_route"], errors)
    base_route_links = load_csv(base / "route_links.csv",
        ["order", "link_id", "source_support_id", "target_support_id", "mode", "direction",
         "takeoff_x", "takeoff_y", "landing_x", "landing_y", "gap_air", "rise",
         "grab_edge_id", "required_route"], errors)
    route_clearance = load_csv(gen / "route_clearance.csv",
        ["support_id", "foot_x", "foot_y", "body_x", "body_y", "head_x", "head_y", "clear"], errors)
    grab_edges = load_csv(gen / "grab_edges.csv",
        ["grab_edge_id", "link_id", "target_support_id", "contact_x", "contact_y", "face",
         "approach_direction", "hang_body_x", "hang_body_y", "pull_up_x", "pull_up_y",
         "exposed", "hang_body_clear", "pull_up_clear", "safe"], errors)
    base_grab_edges = load_csv(base / "grab_edges.csv", list(grab_edges[0].keys()) if grab_edges else
        ["grab_edge_id", "link_id", "target_support_id"], errors)
    grab_clearance = load_csv(gen / "grab_clearance.csv",
        ["grab_edge_id", "hang_body_x", "hang_body_y", "hang_head_x", "hang_head_y",
         "pull_up_foot_x", "pull_up_foot_y", "pull_up_head_x", "pull_up_head_y", "all_air"], errors)
    base_grab_clearance = load_csv(base / "grab_clearance.csv",
        list(grab_clearance[0].keys()) if grab_clearance else ["grab_edge_id"], errors)
    actions = load_csv(gen / "grab_action_sequence.csv",
        ["order", "action", "player_x", "player_y", "contact_x", "contact_y", "support_id"], errors)
    base_actions = load_csv(base / "grab_action_sequence.csv",
        list(actions[0].keys()) if actions else ["order", "action"], errors)

    supports_by_id = {row.get("support_id", ""): row for row in supports}
    actual_depths: dict[str, dict[int, int]] = {}
    for row in depths:
        owner = row.get("owner_support_id", "")
        x = integer(row, "x", errors, owner)
        depth = integer(row, "depth", errors, owner)
        actual_depths.setdefault(owner, {})[x] = depth
        support = supports_by_id.get(owner)
        if not support or support.get("kind") != "SOLID":
            errors.append(owner + " backing owner is missing or not SOLID")
            continue
        if integer(row, "support_bottom_y", errors, owner) != integer(support, "y", errors, owner):
            errors.append(owner + " support_bottom_y mismatch")
        if integer(row, "emitted_cell_count", errors, owner) != depth:
            errors.append(owner + " emitted count does not equal depth")
    if actual_depths != DEPTHS:
        errors.append("deformation depth table differs from exact profile")

    expected_outline: dict[tuple[int, int], tuple[str, int]] = {}
    for owner, columns in DEPTHS.items():
        support = supports_by_id.get(owner)
        if not support:
            continue
        bottom_y = integer(support, "y", errors, owner)
        for x, depth in columns.items():
            for amount in range(1, depth + 1):
                point = (x, bottom_y - amount)
                if point in expected_outline:
                    errors.append("profile produces duplicate outline cell: " + str(point))
                expected_outline[point] = (owner, amount)
    actual_outline: dict[tuple[int, int], tuple[str, int]] = {}
    cell_ids: set[str] = set()
    for row in outline_cells:
        cell_id = row.get("outline_cell_id", "")
        owner = row.get("owner_support_id", "")
        point = (integer(row, "x", errors, owner), integer(row, "y", errors, owner))
        depth = integer(row, "depth", errors, owner)
        if not cell_id or cell_id in cell_ids:
            errors.append("missing or duplicate outline_cell_id: " + cell_id)
        cell_ids.add(cell_id)
        if point in actual_outline:
            errors.append("duplicate outline coordinate: " + str(point))
        actual_outline[point] = (owner, depth)
        if row.get("collision") != "SOLID":
            errors.append(cell_id + " is not full SOLID collision")
        if not (0 <= point[0] < WIDTH and 0 <= point[1] < HEIGHT):
            errors.append(cell_id + " outside local canvas")
    if len(actual_outline) != 17 or actual_outline != expected_outline:
        errors.append("outline cell set differs from exact 17-cell profile")

    base_occupancy: dict[tuple[int, int], tuple[str, str, str]] = {}
    for row in base_cells:
        owner = row.get("support_id", "")
        point = (integer(row, "x", errors, owner), integer(row, "y", errors, owner))
        base_occupancy[point] = (row.get("collision", ""), owner, row.get("kind", ""))
    if set(base_occupancy) & set(actual_outline):
        errors.append("outline overlaps immutable base support cells")
    expected_final = dict(base_occupancy)
    for point, (owner, _) in actual_outline.items():
        expected_final[point] = ("SOLID", owner, "SOLID")
    actual_final: dict[tuple[int, int], tuple[str, str, str]] = {}
    for row in final_rows:
        owner = row.get("owner_id", "")
        point = (integer(row, "x", errors, owner), integer(row, "y", errors, owner))
        value = (row.get("collision", ""), owner, row.get("support_kind", ""))
        expected_source = "OUTLINE_BACKING" if point in actual_outline else "BASE_SUPPORT"
        if row.get("source") != expected_source:
            errors.append("final occupancy source mismatch: " + str(point))
        if point in actual_final:
            errors.append("duplicate final occupancy: " + str(point))
        actual_final[point] = value
    if len(actual_final) != 44 or actual_final != expected_final:
        errors.append("final occupancy is not the exact 27+17 cell union")

    solid_points = {point for point, value in actual_final.items() if value[0] == "SOLID"}
    six_by_six = 0
    for y in range(HEIGHT - 5):
        for x in range(WIDTH - 5):
            if all((x + dx, y + dy) in solid_points for dx in range(6) for dy in range(6)):
                six_by_six += 1
    if six_by_six:
        errors.append("final occupancy contains filled 6x6 SOLID window")

    if normalized(route_links) != normalized(base_route_links):
        errors.append("SV5_15 ordered route links changed")
    if normalized(grab_edges) != normalized(base_grab_edges):
        errors.append("SV5_15 grab edge changed or a new edge was added")
    if normalized(grab_clearance) != normalized(base_grab_clearance):
        errors.append("SV5_15 grab clearance changed")
    if normalized(actions) != normalized(base_actions):
        errors.append("SV5_15 grab action sequence changed")

    clearance_by_id = {row.get("support_id", ""): row for row in route_clearance}
    if set(clearance_by_id) != set(supports_by_id) or len(route_clearance) != 10:
        errors.append("route clearance must contain exactly all ten supports")
    for support_id, support in supports_by_id.items():
        row = clearance_by_id.get(support_id)
        if not row:
            continue
        foot = (integer(row, "foot_x", errors, support_id), integer(row, "foot_y", errors, support_id))
        body = (integer(row, "body_x", errors, support_id), integer(row, "body_y", errors, support_id))
        head = (integer(row, "head_x", errors, support_id), integer(row, "head_y", errors, support_id))
        x = integer(support, "x", errors, support_id)
        width = integer(support, "width", errors, support_id)
        top_y = integer(support, "top_y", errors, support_id)
        if foot[1] != top_y or not x <= foot[0] < x + width:
            errors.append(support_id + " route foot is unsupported")
        if body != foot or head != (foot[0], foot[1] + 1):
            errors.append(support_id + " route clearance coordinates mismatch")
        if body in actual_final or head in actual_final or not flag(row.get("clear", "")):
            errors.append(support_id + " route body/head is not AIR")

    required_grab_air = {(16, 6), (16, 7), (15, 7), (15, 8)}
    blocked_grab = sorted(required_grab_air & set(actual_final))
    if blocked_grab:
        errors.append("outline blocks accepted Grab AIR: " + str(blocked_grab))

    canvas = summary.get("canvas", {}) if isinstance(summary, dict) else {}
    if canvas.get("width") != WIDTH or canvas.get("height") != HEIGHT or canvas.get("scope") != "LOCAL_JUMP_ROOM_NOT_SECTOR":
        errors.append("summary canvas mismatch")
    if summary.get("base_fixture_digest") != base_summary.get("fixture_digest"):
        errors.append("SV5_15 base fixture digest mismatch")
    expected_counts = {"base_support_cells": 27, "outline_cells": 17, "final_occupancy_cells": 44}
    for key, expected in expected_counts.items():
        if summary.get(key) != expected:
            errors.append("summary count mismatch: " + key)
    if summary.get("rectangular_room_shell_created") is not False:
        errors.append("rectangular room shell was invented")
    if summary.get("outline_faces_automatically_grabbable") is not False:
        errors.append("outline faces were automatically made grabbable")
    if summary.get("reverse_completion_required") is not False:
        errors.append("reverse completion was invented")
    readiness = summary.get("readiness", {}) if isinstance(summary, dict) else {}
    expected_readiness = {
        "jump_contract_ready": True, "jump_solid_geometry_ready": True,
        "jump_grab_geometry_ready": True, "jump_outline_ready": True,
        "composed_geometry_ready": False, "player_verified": False,
    }
    for key, expected in expected_readiness.items():
        if readiness.get(key) is not expected:
            errors.append("readiness mismatch: " + key)
    performance = summary.get("performance", {}) if isinstance(summary, dict) else {}
    if performance.get("whole_world_builds_in_new_targeted_tests") != 0:
        errors.append("new targeted tests build the whole world")
    if performance.get("whole_world_searches_in_new_targeted_tests") != 0:
        errors.append("new targeted tests search the whole world")
    if validation.get("status") != "PASS" or validation.get("errors") != []:
        errors.append("jump_outline_validation.json is not PASS with zero errors")

    svg_path = gen / "preview/jump_outline.svg"
    if not svg_path.is_file():
        errors.append("missing export: preview/jump_outline.svg")
    else:
        svg = svg_path.read_text(encoding="utf-8-sig", errors="replace")
        for label in ("24x32", "base SOLID", "ONE_WAY", "outline SOLID", "JUMP_GRAB",
                      "0-2 cell downward deformation", "ONE_WAY unchanged",
                      "no rectangular room shell", "PLAYER verification deferred"):
            if label not in svg:
                errors.append("SVG missing visible label: " + label)

    status = "PASS_INDEPENDENT_JUMP_OUTLINE" if not errors else "FAIL_INDEPENDENT_JUMP_OUTLINE"
    audit = {
        "schema": "SV5_16_JUMP_OUTLINE_AUDIT/v1",
        "status": status,
        "canvas": {"width": WIDTH, "height": HEIGHT},
        "base_support_cells": len(base_cells),
        "outline_cells": len(outline_cells),
        "final_occupancy_cells": len(final_rows),
        "backed_solid_supports": len(actual_depths),
        "one_way_backing_cells": sum(1 for value in actual_outline.values()
                                      if supports_by_id.get(value[0], {}).get("kind") == "ONE_WAY"),
        "filled_solid_6x6_windows": six_by_six,
        "route_clearance_pass": sum(1 for row in route_clearance if flag(row.get("clear", ""))),
        "grab_air_blocked": [list(value) for value in blocked_grab],
        "whole_world_builds_in_new_targeted_tests": performance.get("whole_world_builds_in_new_targeted_tests"),
        "whole_world_searches_in_new_targeted_tests": performance.get("whole_world_searches_in_new_targeted_tests"),
        "errors": errors,
    }
    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_text(json.dumps(audit, ensure_ascii=False, indent=2) + "\n", encoding="utf-8", newline="\n")
    print(status)
    for error in errors:
        print(error)
    return 0 if not errors else 1


if __name__ == "__main__":
    sys.exit(main())
