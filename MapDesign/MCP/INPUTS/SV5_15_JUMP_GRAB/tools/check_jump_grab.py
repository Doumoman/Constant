from __future__ import annotations

import argparse
import csv
import json
import re
import sys
from pathlib import Path

TASK = "SV5_15_JUMP_GRAB"
GEN_REL = Path("MapDesign/MCP/GENERATED") / TASK
BASE_GEN_REL = Path("MapDesign/MCP/GENERATED/SV5_14_JUMP_SOLID")
WIDTH = 24
HEIGHT = 32
MOVED_ID = "JS04_SOLID"
GRAB_LINK_ID = "JS_LINK_03"
GRAB_EDGE_ID = "JS04_RIGHT_GRAB"


def load_csv(path: Path, columns: list[str], errors: list[str]) -> list[dict[str, str]]:
    if not path.is_file():
        errors.append("missing export: " + path.as_posix())
        return []
    with path.open("r", encoding="utf-8-sig", newline="") as handle:
        reader = csv.DictReader(handle)
        names = reader.fieldnames or []
        missing = [name for name in columns if name not in names]
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
            raise ValueError("root must be an object")
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


def point(row: dict[str, str], x: str, y: str, errors: list[str], owner: str) -> tuple[int, int]:
    return integer(row, x, errors, owner), integer(row, y, errors, owner)


def support_values(row: dict[str, str], errors: list[str]) -> tuple:
    sid = row.get("support_id", "")
    return (
        sid,
        row.get("kind", ""),
        integer(row, "x", errors, sid),
        integer(row, "y", errors, sid),
        integer(row, "width", errors, sid),
        integer(row, "height", errors, sid),
        integer(row, "top_y", errors, sid),
        integer(row, "top_x_min", errors, sid),
        integer(row, "top_x_max", errors, sid),
        flag(row.get("active_route", "")),
        flag(row.get("decorative_only", "")),
        flag(row.get("required_route", "")),
    )


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--project-root", default=".")
    parser.add_argument("--output")
    args = parser.parse_args()
    root = Path(args.project_root).resolve()
    gen = root / GEN_REL
    base_gen = root / BASE_GEN_REL
    output = Path(args.output).resolve() if args.output else gen / "independent_jump_grab_audit.json"
    errors: list[str] = []

    geometry_path = root / "Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/Sv5JumpGrabGeometry.cs"
    export_path = root / "Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/Sv5JumpGrabExport.cs"
    test_path = root / "Assets/_Game/Tests/EditMode/Map/SV5/Sv5JumpGrabTests.cs"
    doc_path = root / "MapDesign/MCP/SV5/23_JUMP_GRAB_V5.md"
    for path_value in (geometry_path, export_path, test_path, doc_path):
        if not path_value.is_file():
            errors.append("missing owned file: " + path_value.relative_to(root).as_posix())
    runtime = "\n".join(path_value.read_text(encoding="utf-8-sig", errors="replace")
                          for path_value in (geometry_path, export_path) if path_value.is_file())
    for symbol in ("Sv5JumpGrabGeometry", "Sv5JumpGrabPlan", "Sv5JumpGrabExport",
                   "Sv5JumpSolidGeometry.CreateCanonicalLocalFixture", "Sv5JumpContract.Measure"):
        if symbol not in runtime:
            errors.append("missing runtime symbol: " + symbol)
    for pattern in (r"\bSectorId\b", r"\bsector_id\b", r"\bsector_index\b", r"48\s*[xX×]\s*32"):
        if re.search(pattern, runtime):
            errors.append("retired partition symbol in SV5_15 runtime: " + pattern)

    summary = json_file(gen / "jump_grab.json", errors)
    validation = json_file(gen / "jump_grab_validation.json", errors)
    base_summary = json_file(base_gen / "jump_solid.json", errors)
    columns = ["support_id", "kind", "x", "y", "width", "height", "top_y",
               "top_x_min", "top_x_max", "active_route", "decorative_only", "required_route"]
    supports = load_csv(gen / "supports.csv", columns, errors)
    base_supports = load_csv(base_gen / "supports.csv", columns, errors)
    cells = load_csv(gen / "support_cells.csv",
        ["support_id", "x", "y", "kind", "collision"], errors)
    base_links = load_csv(base_gen / "route_links.csv",
        ["order", "link_id", "source_support_id", "target_support_id"], errors)
    links = load_csv(gen / "route_links.csv",
        ["order", "link_id", "source_support_id", "target_support_id", "mode", "direction",
         "takeoff_x", "takeoff_y", "landing_x", "landing_y", "gap_air", "rise",
         "grab_edge_id", "required_route"], errors)
    grab_edges = load_csv(gen / "grab_edges.csv",
        ["grab_edge_id", "link_id", "target_support_id", "contact_x", "contact_y", "face",
         "approach_direction", "hang_body_x", "hang_body_y", "pull_up_x", "pull_up_y",
         "exposed", "hang_body_clear", "pull_up_clear", "safe"], errors)
    clearances = load_csv(gen / "grab_clearance.csv",
        ["grab_edge_id", "hang_body_x", "hang_body_y", "hang_head_x", "hang_head_y",
         "pull_up_foot_x", "pull_up_foot_y", "pull_up_head_x", "pull_up_head_y", "all_air"], errors)
    actions = load_csv(gen / "grab_action_sequence.csv",
        ["order", "action", "player_x", "player_y", "contact_x", "contact_y", "support_id"], errors)

    base_by_id = {row.get("support_id", ""): row for row in base_supports}
    by_id = {row.get("support_id", ""): row for row in supports}
    if len(base_supports) != 10 or len(supports) != 10 or len(by_id) != 10:
        errors.append("support count must remain exactly ten")
    if set(by_id) != set(base_by_id):
        errors.append("derived support ID set differs from SV5_14")
    for sid in sorted(set(by_id) & set(base_by_id)):
        actual = list(support_values(by_id[sid], errors))
        expected = list(support_values(base_by_id[sid], errors))
        if sid == MOVED_ID:
            expected[3] += 1
            expected[6] += 1
        if actual != expected:
            errors.append(sid + " differs from the exact allowed SV5_14 transformation")

    expected_cells: dict[tuple[int, int], tuple[str, str, str]] = {}
    for row in supports:
        sid = row.get("support_id", "")
        kind = row.get("kind", "")
        x = integer(row, "x", errors, sid)
        y = integer(row, "y", errors, sid)
        width = integer(row, "width", errors, sid)
        height = integer(row, "height", errors, sid)
        if x < 0 or y < 0 or x + width > WIDTH or y + height > HEIGHT:
            errors.append(sid + " outside 24x32 local canvas")
        for cy in range(y, y + height):
            for cx in range(x, x + width):
                key = (cx, cy)
                if key in expected_cells:
                    errors.append("overlapping support cell: " + str(key))
                expected_cells[key] = (sid, kind, "SOLID" if kind == "SOLID" else "TOP_ONLY")
    actual_cells: dict[tuple[int, int], tuple[str, str, str]] = {}
    for row in cells:
        sid = row.get("support_id", "")
        key = point(row, "x", "y", errors, sid)
        value = (sid, row.get("kind", ""), row.get("collision", ""))
        if key in actual_cells:
            errors.append("duplicate emitted support cell: " + str(key))
        actual_cells[key] = value
    if actual_cells != expected_cells:
        errors.append("emitted support cells do not exactly match derived rectangles")
    for old in ((14, 5), (15, 5)):
        if old in actual_cells:
            errors.append("old JS04 cell was not cleared: " + str(old))
    for moved in ((14, 6), (15, 6)):
        if actual_cells.get(moved) != (MOVED_ID, "SOLID", "SOLID"):
            errors.append("moved JS04 SOLID cell missing: " + str(moved))

    sorted_base_links = sorted(base_links, key=lambda row: integer(row, "order", errors, "base_link"))
    sorted_links = sorted(links, key=lambda row: integer(row, "order", errors, "link"))
    if len(sorted_links) != 9 or len(sorted_base_links) != 9:
        errors.append("route must contain exactly nine links")
    grab_links: list[dict[str, str]] = []
    rise_values: list[int] = []
    gap_values: list[int] = []
    for index, row in enumerate(sorted_links):
        lid = row.get("link_id", "")
        if index < len(sorted_base_links):
            base = sorted_base_links[index]
            if (integer(row, "order", errors, lid), lid, row.get("source_support_id"), row.get("target_support_id")) != (
                index, base.get("link_id"), base.get("source_support_id"), base.get("target_support_id")):
                errors.append(lid + " breaks the accepted ordered route")
        source = by_id.get(row.get("source_support_id", ""))
        target = by_id.get(row.get("target_support_id", ""))
        if not source or not target:
            errors.append(lid + " references missing support")
            continue
        direction = row.get("direction", "")
        sx = integer(source, "x", errors, lid)
        sw = integer(source, "width", errors, lid)
        tx = integer(target, "x", errors, lid)
        tw = integer(target, "width", errors, lid)
        if direction == "LEFT_TO_RIGHT":
            gap = max(0, tx - (sx + sw - 1) - 1)
        elif direction == "RIGHT_TO_LEFT":
            gap = max(0, sx - (tx + tw - 1) - 1)
        else:
            errors.append(lid + " invalid direction")
            gap = -1
        rise = integer(target, "top_y", errors, lid) - integer(source, "top_y", errors, lid)
        declared_gap = integer(row, "gap_air", errors, lid)
        declared_rise = integer(row, "rise", errors, lid)
        gap_values.append(declared_gap)
        rise_values.append(declared_rise)
        if declared_gap != gap:
            errors.append(lid + " gap formula mismatch")
        if declared_rise != rise:
            errors.append(lid + " rise formula mismatch")
        takeoff = point(row, "takeoff_x", "takeoff_y", errors, lid)
        landing = point(row, "landing_x", "landing_y", errors, lid)
        if takeoff[1] != integer(source, "top_y", errors, lid) or not sx <= takeoff[0] < sx + sw:
            errors.append(lid + " takeoff is unsupported")
        if landing[1] != integer(target, "top_y", errors, lid) or not tx <= landing[0] < tx + tw:
            errors.append(lid + " landing is unsupported")
        if not flag(row.get("required_route", "")):
            errors.append(lid + " is not marked required")
        if row.get("mode") == "JUMP_GRAB":
            grab_links.append(row)
            if lid != GRAB_LINK_ID or declared_gap != 2 or declared_rise != 2:
                errors.append("the sole JUMP_GRAB link is not the exact required link")
            if takeoff != (18, 5) or landing != (15, 7) or direction != "RIGHT_TO_LEFT":
                errors.append("the JUMP_GRAB movement coordinates or direction changed")
            if row.get("grab_edge_id") != GRAB_EDGE_ID:
                errors.append(lid + " missing exact grab edge reference")
        elif row.get("mode") == "JUMP":
            if declared_rise < 0 or declared_rise > 1 or row.get("grab_edge_id", ""):
                errors.append(lid + " invalid normal JUMP")
        else:
            errors.append(lid + " invalid mode")
    if len(grab_links) != 1:
        errors.append("exactly one JUMP_GRAB link is required")

    if len(grab_edges) != 1:
        errors.append("exactly one grab edge is required")
    else:
        edge = grab_edges[0]
        expected_edge = {
            "grab_edge_id": GRAB_EDGE_ID,
            "link_id": GRAB_LINK_ID,
            "target_support_id": MOVED_ID,
            "face": "RIGHT",
            "approach_direction": "RIGHT_TO_LEFT",
        }
        for key, value in expected_edge.items():
            if edge.get(key) != value:
                errors.append("grab edge mismatch: " + key)
        contact = point(edge, "contact_x", "contact_y", errors, GRAB_EDGE_ID)
        hang = point(edge, "hang_body_x", "hang_body_y", errors, GRAB_EDGE_ID)
        pull = point(edge, "pull_up_x", "pull_up_y", errors, GRAB_EDGE_ID)
        if contact != (15, 6) or actual_cells.get(contact) != (MOVED_ID, "SOLID", "SOLID"):
            errors.append("grab contact is not the actual target SOLID corner")
        if hang != (16, 6) or hang in actual_cells:
            errors.append("hang body is not the exposed adjacent AIR cell")
        if pull != (15, 7) or pull in actual_cells or actual_cells.get((15, 6), (None,))[0] != MOVED_ID:
            errors.append("pull-up destination is not supported AIR")
        for key in ("exposed", "hang_body_clear", "pull_up_clear", "safe"):
            if not flag(edge.get(key, "")):
                errors.append("grab edge flag false: " + key)

    if len(clearances) != 1:
        errors.append("exactly one grab clearance record is required")
    else:
        row = clearances[0]
        expected_points = {
            "hang_body": (16, 6), "hang_head": (16, 7),
            "pull_up_foot": (15, 7), "pull_up_head": (15, 8),
        }
        for name, expected in expected_points.items():
            actual = point(row, name + "_x", name + "_y", errors, GRAB_EDGE_ID)
            if actual != expected or actual in actual_cells or not (0 <= actual[0] < WIDTH and 0 <= actual[1] < HEIGHT):
                errors.append(name + " is not the exact required AIR cell")
        if row.get("grab_edge_id") != GRAB_EDGE_ID or not flag(row.get("all_air", "")):
            errors.append("grab clearance proof mismatch")

    sorted_actions = sorted(actions, key=lambda row: integer(row, "order", errors, "action"))
    expected_actions = [
        (0, "TAKEOFF", (18, 5), (-1, -1), "JS03_ONE_WAY"),
        (1, "CONTACT_HANG", (16, 6), (15, 6), MOVED_ID),
        (2, "PULL_UP", (15, 7), (15, 6), MOVED_ID),
        (3, "LAND", (15, 7), (-1, -1), MOVED_ID),
    ]
    actual_actions = [(integer(row, "order", errors, "action"), row.get("action", ""),
                       point(row, "player_x", "player_y", errors, "action"),
                       point(row, "contact_x", "contact_y", errors, "action"),
                       row.get("support_id", "")) for row in sorted_actions]
    if actual_actions != expected_actions:
        errors.append("grab action sequence does not match TAKEOFF/CONTACT_HANG/PULL_UP/LAND")

    canvas = summary.get("canvas", {}) if isinstance(summary, dict) else {}
    if canvas.get("width") != WIDTH or canvas.get("height") != HEIGHT or canvas.get("scope") != "LOCAL_JUMP_ROOM_NOT_SECTOR":
        errors.append("summary canvas mismatch")
    if summary.get("base_fixture_digest") != base_summary.get("fixture_digest"):
        errors.append("SV5_14 base fixture digest mismatch")
    if summary.get("reverse_completion_required") is not False:
        errors.append("reverse completion was invented")
    if summary.get("ladder_logic_created") is not False or summary.get("one_way_grab_allowed") is not False:
        errors.append("ladder or ONE_WAY Grab behavior was invented")
    readiness = summary.get("readiness", {}) if isinstance(summary, dict) else {}
    expected_readiness = {
        "jump_contract_ready": True, "jump_solid_geometry_ready": True,
        "jump_grab_geometry_ready": True, "composed_geometry_ready": False,
        "player_verified": False,
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
        errors.append("jump_grab_validation.json is not PASS with zero errors")

    svg_path = gen / "preview/jump_grab.svg"
    if not svg_path.is_file():
        errors.append("missing export: preview/jump_grab.svg")
    else:
        svg = svg_path.read_text(encoding="utf-8-sig", errors="replace")
        for label in ("24x32", "JUMP_GRAB", "SOLID face only", "ONE_WAY is not grabbable",
                      "TAKEOFF", "CONTACT", "HANG", "PULL_UP", "LAND",
                      "PLAYER verification deferred"):
            if label not in svg:
                errors.append("SVG missing visible label: " + label)

    status = "PASS_INDEPENDENT_JUMP_GRAB" if not errors else "FAIL_INDEPENDENT_JUMP_GRAB"
    audit = {
        "schema": "SV5_15_JUMP_GRAB_AUDIT/v1",
        "status": status,
        "canvas": {"width": WIDTH, "height": HEIGHT},
        "support_count": len(supports),
        "support_cell_count": len(cells),
        "route_link_count": len(sorted_links),
        "jump_grab_link_count": len(grab_links),
        "grab_edge_count": len(grab_edges),
        "grab_link_id": grab_links[0].get("link_id") if len(grab_links) == 1 else None,
        "gap_air_range": [min(gap_values), max(gap_values)] if gap_values else [],
        "rise_range": [min(rise_values), max(rise_values)] if rise_values else [],
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
