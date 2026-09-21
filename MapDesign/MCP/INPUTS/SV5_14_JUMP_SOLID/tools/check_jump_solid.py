from __future__ import annotations

import argparse
import csv
import json
import re
import sys
from pathlib import Path

TASK = "SV5_14_JUMP_SOLID"
GEN_REL = Path("MapDesign/MCP/GENERATED") / TASK
WIDTH = 24
HEIGHT = 32


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


def number(row: dict[str, str], key: str, errors: list[str], owner: str) -> int:
    try:
        return int(row[key])
    except Exception:
        errors.append(owner + " invalid integer " + key)
        return 0


def flag(value: str) -> bool:
    return value.strip().lower() in {"true", "1", "yes"}


def json_file(path: Path, errors: list[str]) -> dict:
    if not path.is_file():
        errors.append("missing export: " + path.as_posix())
        return {}
    try:
        value = json.loads(path.read_text(encoding="utf-8-sig"))
        return value if isinstance(value, dict) else {}
    except Exception as exc:
        errors.append("invalid JSON " + path.name + ": " + str(exc))
        return {}


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--project-root", default=".")
    parser.add_argument("--output")
    args = parser.parse_args()
    root = Path(args.project_root).resolve()
    gen = root / GEN_REL
    output = Path(args.output).resolve() if args.output else gen / "independent_jump_solid_audit.json"
    errors: list[str] = []

    runtime_paths = [
        root / "Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/Sv5JumpSolidGeometry.cs",
        root / "Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/Sv5JumpSolidExport.cs",
    ]
    test_path = root / "Assets/_Game/Tests/EditMode/Map/SV5/Sv5JumpSolidTests.cs"
    doc_path = root / "MapDesign/MCP/SV5/22_JUMP_SOLID_V5.md"
    for path in runtime_paths + [test_path, doc_path]:
        if not path.is_file():
            errors.append("missing owned file: " + path.relative_to(root).as_posix())
    runtime = "\n".join(path.read_text(encoding="utf-8-sig", errors="replace")
                         for path in runtime_paths if path.is_file())
    for symbol in ("Sv5JumpSolidGeometry", "Sv5JumpSolidPlan", "Sv5JumpContract",
                   "Sv5JumpSolidExport", "CreateCanonicalLocalFixture"):
        if symbol not in runtime:
            errors.append("missing runtime symbol: " + symbol)
    if "Sv5JumpContract.Measure" not in runtime:
        errors.append("route geometry does not consume Sv5JumpContract.Measure")
    for pattern in (r"\bSectorId\b", r"\bsector_id\b", r"\bsector_index\b", r"48\s*[xX×]\s*32"):
        if re.search(pattern, runtime):
            errors.append("retired partition symbol in SV5_14 runtime: " + pattern)

    summary = json_file(gen / "jump_solid.json", errors)
    validation = json_file(gen / "jump_solid_validation.json", errors)
    supports = load_csv(gen / "supports.csv",
        ["support_id", "kind", "x", "y", "width", "height", "top_y", "top_x_min",
         "top_x_max", "active_route", "decorative_only", "required_route"], errors)
    cells = load_csv(gen / "support_cells.csv",
        ["support_id", "x", "y", "kind", "collision"], errors)
    sequence = load_csv(gen / "route_support_sequence.csv",
        ["order", "support_id", "entry", "exit"], errors)
    links = load_csv(gen / "route_links.csv",
        ["order", "link_id", "source_support_id", "target_support_id", "mode", "direction",
         "takeoff_x", "takeoff_y", "landing_x", "landing_y", "gap_air", "rise",
         "required_route"], errors)
    clearance = load_csv(gen / "support_clearance.csv",
        ["support_id", "foot_x", "foot_y", "body_x", "body_y", "head_x", "head_y", "clear"], errors)
    solid_use = load_csv(gen / "solid_use.csv",
        ["support_id", "as_takeoff_count", "as_landing_count", "counted_active_solid"], errors)

    by_support: dict[str, dict[str, str]] = {}
    expected_cells: dict[tuple[int, int], tuple[str, str]] = {}
    required_ids: set[str] = set()
    solid_required: set[str] = set()
    one_way_required: set[str] = set()
    for row in supports:
        sid = row.get("support_id", "")
        if not sid or sid in by_support:
            errors.append("missing or duplicate support_id: " + sid)
        by_support[sid] = row
        kind = row.get("kind", "")
        if kind not in {"SOLID", "ONE_WAY"}:
            errors.append(sid + " invalid support kind")
        x = number(row, "x", errors, sid)
        y = number(row, "y", errors, sid)
        width = number(row, "width", errors, sid)
        height = number(row, "height", errors, sid)
        top_y = number(row, "top_y", errors, sid)
        if width < 2 or width > 4:
            errors.append(sid + " support width outside 2..4")
        if kind == "SOLID" and (height < 1 or height > 3):
            errors.append(sid + " SOLID height outside 1..3")
        if kind == "ONE_WAY" and height != 1:
            errors.append(sid + " ONE_WAY height must be one")
        if x < 0 or y < 0 or x + width > WIDTH or y + height > HEIGHT:
            errors.append(sid + " support outside 24x32 canvas")
        if top_y != y + height:
            errors.append(sid + " top_y mismatch")
        if number(row, "top_x_min", errors, sid) != x or number(row, "top_x_max", errors, sid) != x + width - 1:
            errors.append(sid + " top interval mismatch")
        for cy in range(y, y + height):
            for cx in range(x, x + width):
                point = (cx, cy)
                if point in expected_cells:
                    errors.append("overlapping support cell: " + str(point))
                expected_cells[point] = (sid, kind)
        if flag(row.get("required_route", "")):
            required_ids.add(sid)
            if not flag(row.get("active_route", "")) or flag(row.get("decorative_only", "")):
                errors.append(sid + " required support is inactive or decorative")
            if kind == "SOLID":
                solid_required.add(sid)
            elif kind == "ONE_WAY":
                one_way_required.add(sid)

    actual_cells: dict[tuple[int, int], tuple[str, str]] = {}
    solid_points: set[tuple[int, int]] = set()
    for row in cells:
        sid = row.get("support_id", "")
        point = (number(row, "x", errors, sid), number(row, "y", errors, sid))
        value = (sid, row.get("kind", ""))
        if point in actual_cells:
            errors.append("duplicate emitted support cell: " + str(point))
        actual_cells[point] = value
        expected = expected_cells.get(point)
        if expected != value:
            errors.append("extra or mismatched support cell: " + str(point))
        collision = row.get("collision", "")
        if value[1] == "SOLID":
            solid_points.add(point)
            if collision != "SOLID":
                errors.append(sid + " SOLID cell collision mismatch")
        elif value[1] == "ONE_WAY" and collision != "TOP_ONLY":
            errors.append(sid + " ONE_WAY cell collision mismatch")
    for point, value in expected_cells.items():
        if actual_cells.get(point) != value:
            errors.append("missing emitted support cell: " + str(point))

    six_by_six = 0
    for y in range(0, HEIGHT - 5):
        for x in range(0, WIDTH - 5):
            if all((x + dx, y + dy) in solid_points for dx in range(6) for dy in range(6)):
                six_by_six += 1
    if six_by_six:
        errors.append("6x6 all-SOLID windows: " + str(six_by_six))

    sequence_sorted = sorted(sequence, key=lambda row: number(row, "order", errors, "sequence"))
    sequence_ids = [row.get("support_id", "") for row in sequence_sorted]
    if len(sequence_ids) < 10:
        errors.append("required route has fewer than 10 supports")
    if len(sequence_ids) != len(set(sequence_ids)):
        errors.append("required support sequence repeats IDs")
    if set(sequence_ids) != required_ids:
        errors.append("required support flags and sequence disagree")
    if sequence_sorted:
        if not flag(sequence_sorted[0].get("entry", "")) or any(flag(row.get("entry", "")) for row in sequence_sorted[1:]):
            errors.append("entry marker mismatch")
        if not flag(sequence_sorted[-1].get("exit", "")) or any(flag(row.get("exit", "")) for row in sequence_sorted[:-1]):
            errors.append("exit marker mismatch")
    if len(solid_required) < 4:
        errors.append("fewer than four active required SOLID supports")
    if len(one_way_required) < 3:
        errors.append("fewer than three active required ONE_WAY supports")

    links_sorted = sorted(links, key=lambda row: number(row, "order", errors, "link"))
    if len(links_sorted) != max(0, len(sequence_ids) - 1):
        errors.append("route link count does not equal support sequence minus one")
    solid_takeoffs: dict[str, int] = {sid: 0 for sid in solid_required}
    solid_landings: dict[str, int] = {sid: 0 for sid in solid_required}
    rises: list[int] = []
    gaps: list[int] = []
    for index, row in enumerate(links_sorted):
        lid = row.get("link_id", "LINK_" + str(index))
        source_id = row.get("source_support_id", "")
        target_id = row.get("target_support_id", "")
        if index + 1 < len(sequence_ids) and (source_id != sequence_ids[index] or target_id != sequence_ids[index + 1]):
            errors.append(lid + " breaks ordered support sequence")
        source = by_support.get(source_id)
        target = by_support.get(target_id)
        if not source or not target:
            errors.append(lid + " references missing support")
            continue
        if row.get("mode") != "JUMP":
            errors.append(lid + " must remain JUMP until SV5_15")
        if not flag(row.get("required_route", "")):
            errors.append(lid + " is not marked required")
        sx = number(source, "x", errors, lid)
        sw = number(source, "width", errors, lid)
        tx = number(target, "x", errors, lid)
        tw = number(target, "width", errors, lid)
        direction = row.get("direction", "")
        if direction == "LEFT_TO_RIGHT":
            computed_gap = max(0, tx - (sx + sw - 1) - 1)
        elif direction == "RIGHT_TO_LEFT":
            computed_gap = max(0, sx - (tx + tw - 1) - 1)
        else:
            errors.append(lid + " invalid direction")
            computed_gap = -1
        rise = number(target, "top_y", errors, lid) - number(source, "top_y", errors, lid)
        declared_gap = number(row, "gap_air", errors, lid)
        declared_rise = number(row, "rise", errors, lid)
        gaps.append(declared_gap)
        rises.append(declared_rise)
        if declared_gap != computed_gap or declared_gap < 0 or declared_gap > 3:
            errors.append(lid + " gap_air invalid")
        if declared_rise != rise or declared_rise < 0 or declared_rise > 1:
            errors.append(lid + " rise invalid")
        takeoff = (number(row, "takeoff_x", errors, lid), number(row, "takeoff_y", errors, lid))
        landing = (number(row, "landing_x", errors, lid), number(row, "landing_y", errors, lid))
        source_min = number(source, "top_x_min", errors, lid)
        source_max = number(source, "top_x_max", errors, lid)
        target_min = number(target, "top_x_min", errors, lid)
        target_max = number(target, "top_x_max", errors, lid)
        if takeoff[1] != number(source, "top_y", errors, lid) or not source_min <= takeoff[0] <= source_max:
            errors.append(lid + " unsupported takeoff")
        if landing[1] != number(target, "top_y", errors, lid) or not target_min <= landing[0] <= target_max:
            errors.append(lid + " unsupported landing")
        if source_id in solid_takeoffs:
            solid_takeoffs[source_id] += 1
        if target_id in solid_landings:
            solid_landings[target_id] += 1

    route_span = 0
    if sequence_ids and sequence_ids[0] in by_support and sequence_ids[-1] in by_support:
        route_span = number(by_support[sequence_ids[-1]], "top_y", errors, "exit") - number(by_support[sequence_ids[0]], "top_y", errors, "entry")
    if route_span < 8:
        errors.append("required route vertical span below eight")
    if not any(value > 0 for value in rises):
        errors.append("required route has no ascending link")

    use_rows = {row.get("support_id", ""): row for row in solid_use}
    for sid in solid_required:
        row = use_rows.get(sid)
        takeoffs = solid_takeoffs.get(sid, 0)
        landings = solid_landings.get(sid, 0)
        if not row:
            errors.append(sid + " missing SOLID use proof")
            continue
        if number(row, "as_takeoff_count", errors, sid) != takeoffs or number(row, "as_landing_count", errors, sid) != landings:
            errors.append(sid + " SOLID use count mismatch")
        if takeoffs + landings == 0 or not flag(row.get("counted_active_solid", "")):
            errors.append(sid + " counted SOLID is not used by route")

    clearance_rows = {row.get("support_id", ""): row for row in clearance}
    for sid in required_ids:
        support = by_support.get(sid)
        row = clearance_rows.get(sid)
        if not support or not row:
            errors.append(sid + " missing clearance proof")
            continue
        foot = (number(row, "foot_x", errors, sid), number(row, "foot_y", errors, sid))
        body = (number(row, "body_x", errors, sid), number(row, "body_y", errors, sid))
        head = (number(row, "head_x", errors, sid), number(row, "head_y", errors, sid))
        top_min = number(support, "top_x_min", errors, sid)
        top_max = number(support, "top_x_max", errors, sid)
        top_y = number(support, "top_y", errors, sid)
        if foot[1] != top_y or not top_min <= foot[0] <= top_max:
            errors.append(sid + " clearance foot is unsupported")
        if body != (foot[0], foot[1]) or head != (foot[0], foot[1] + 1):
            errors.append(sid + " clearance does not reserve two rows above foot")
        if body in actual_cells or head in actual_cells or not (0 <= head[0] < WIDTH and 0 <= head[1] < HEIGHT):
            errors.append(sid + " reserved clearance is occupied or out of bounds")
        if not flag(row.get("clear", "")):
            errors.append(sid + " clearance flag false")

    canvas = summary.get("canvas", {}) if isinstance(summary, dict) else {}
    if canvas.get("width") != WIDTH or canvas.get("height") != HEIGHT:
        errors.append("jump_solid canvas is not 24x32")
    if summary.get("entry_support_id") != (sequence_ids[0] if sequence_ids else None):
        errors.append("entry support summary mismatch")
    if summary.get("exit_support_id") != (sequence_ids[-1] if sequence_ids else None):
        errors.append("exit support summary mismatch")
    if summary.get("reverse_completion_required") is not False:
        errors.append("reverse completion was invented")
    readiness = summary.get("readiness", {}) if isinstance(summary, dict) else {}
    expected_readiness = {
        "jump_contract_ready": True,
        "jump_solid_geometry_ready": True,
        "jump_grab_geometry_ready": False,
        "composed_geometry_ready": False,
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
        errors.append("jump_solid_validation.json is not PASS with zero errors")

    svg_path = gen / "preview/jump_solid.svg"
    if not svg_path.is_file():
        errors.append("missing export: preview/jump_solid.svg")
    else:
        svg = svg_path.read_text(encoding="utf-8-sig", errors="replace")
        for label in ("24x32", "SOLID", "ONE_WAY", "ENTRY", "EXIT", "takeoff", "landing", "Grab deferred"):
            if label not in svg:
                errors.append("SVG missing visible label: " + label)

    status = "PASS_INDEPENDENT_JUMP_SOLID" if not errors else "FAIL_INDEPENDENT_JUMP_SOLID"
    audit = {
        "schema": "SV5_14_JUMP_SOLID_AUDIT/v1",
        "status": status,
        "canvas": {"width": WIDTH, "height": HEIGHT},
        "support_count": len(supports),
        "support_cell_count": len(cells),
        "required_route_support_count": len(sequence_ids),
        "required_route_link_count": len(links_sorted),
        "active_required_solid_count": len(solid_required),
        "active_required_one_way_count": len(one_way_required),
        "route_vertical_span": route_span,
        "gap_air_range": [min(gaps), max(gaps)] if gaps else [],
        "rise_range": [min(rises), max(rises)] if rises else [],
        "solid_6x6_windows": six_by_six,
        "whole_world_builds_in_new_targeted_tests": performance.get("whole_world_builds_in_new_targeted_tests"),
        "whole_world_searches_in_new_targeted_tests": performance.get("whole_world_searches_in_new_targeted_tests"),
        "errors": errors,
    }
    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_text(json.dumps(audit, ensure_ascii=False, indent=2) + "\n",
                      encoding="utf-8", newline="\n")
    print(status)
    for error in errors:
        print(error)
    return 0 if not errors else 1


if __name__ == "__main__":
    sys.exit(main())
