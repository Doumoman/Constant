from __future__ import annotations

import argparse
import csv
import hashlib
import json
import sys
import xml.etree.ElementTree as ET
from collections import defaultdict
from pathlib import Path


PROFILES = ("default", "repeat")
TREE_ROLES = {
    "TRUNK_CLIMB",
    "BRANCH_PLATFORM",
    "DECORATIVE_BRANCH",
    "LEAF_DECORATION",
}


def truth(value: object) -> bool:
    return str(value).strip().lower() in {"1", "true", "yes"}


def integer(value: object, label: str, errors: list[str]) -> int:
    try:
        return int(value)
    except Exception:
        errors.append(f"{label}: expected integer, got {value!r}")
        return 0


def read_json(path: Path, errors: list[str]) -> dict:
    if not path.is_file():
        errors.append(f"missing: {path.as_posix()}")
        return {}
    try:
        return json.loads(path.read_text(encoding="utf-8-sig"))
    except Exception as exc:
        errors.append(f"invalid JSON {path.as_posix()}: {exc}")
        return {}


def read_csv(path: Path, required: set[str], errors: list[str]) -> list[dict[str, str]]:
    if not path.is_file():
        errors.append(f"missing: {path.as_posix()}")
        return []
    try:
        with path.open("r", encoding="utf-8-sig", newline="") as handle:
            reader = csv.DictReader(handle)
            fields = set(reader.fieldnames or [])
            missing = required - fields
            if missing:
                errors.append(
                    f"{path.as_posix()}: missing columns {sorted(missing)}"
                )
                return []
            return list(reader)
    except Exception as exc:
        errors.append(f"invalid CSV {path.as_posix()}: {exc}")
        return []


def cell_path(value: str, label: str, errors: list[str]) -> list[tuple[int, int]]:
    result: list[tuple[int, int]] = []
    try:
        for token in value.split(";"):
            x, y = token.split(":")
            result.append((int(x), int(y)))
    except Exception:
        errors.append(f"{label}: invalid cell_path")
        return []
    if not result:
        errors.append(f"{label}: empty cell_path")
        return []
    if len(result) != len(set(result)):
        errors.append(f"{label}: repeated cell in path")
    for first, second in zip(result, result[1:]):
        if abs(first[0] - second[0]) + abs(first[1] - second[1]) != 1:
            errors.append(f"{label}: non-cardinal path step {first}->{second}")
    return result


def geometry_digest(rows: list[dict[str, str]]) -> str:
    records = []
    for row in rows:
        if row.get("role") not in TREE_ROLES:
            continue
        records.append(
            "|".join(
                [
                    row.get("x", ""),
                    row.get("y", ""),
                    row.get("role", ""),
                    row.get("collision_type", ""),
                    row.get("top_only", ""),
                    row.get("grab_enabled", ""),
                    row.get("movement_enabled", ""),
                    row.get("visual_only", ""),
                ]
            )
        )
    return hashlib.sha256(("\n".join(sorted(records)) + "\n").encode()).hexdigest()


def filled_six_by_six(cells: set[tuple[int, int]]) -> bool:
    if not cells:
        return False
    for x, y in cells:
        if all((x + dx, y + dy) in cells for dx in range(6) for dy in range(6)):
            return True
    return False


def maximum_contiguous_row_width(points: set[tuple[int, int]], y: int) -> int:
    xs = sorted(x for x, row_y in points if row_y == y)
    best = current = 0
    previous = None
    for x in xs:
        current = current + 1 if previous is not None and x == previous + 1 else 1
        best = max(best, current)
        previous = x
    return best


def contiguous_width_at(points: set[tuple[int, int]], point: tuple[int, int]) -> int:
    x, y = point
    if point not in points:
        return 0
    left = x
    right = x
    while (left - 1, y) in points:
        left -= 1
    while (right + 1, y) in points:
        right += 1
    return right - left + 1


def longest_straight_run(paths: dict[str, list[tuple[int, int]]]) -> int:
    best = 0
    for path in paths.values():
        previous_direction = None
        current = 0
        for first, second in zip(path, path[1:]):
            direction = (second[0] - first[0], second[1] - first[1])
            current = current + 1 if direction == previous_direction else 1
            best = max(best, current)
            previous_direction = direction
    return best


def check_profile(root: Path, profile: str) -> tuple[dict, list[str]]:
    base = root / profile
    errors: list[str] = []
    canopy = read_json(base / "tree_canopy.json", errors)
    tree_validation = read_json(base / "tree_validation.json", errors)
    hub_validation = read_json(base / "hub_validation.json", errors)

    cells = read_csv(
        base / "tree_cells.csv",
        {
            "x",
            "y",
            "role",
            "collision_type",
            "final_value",
            "top_only",
            "grab_enabled",
            "movement_enabled",
            "visual_only",
        },
        errors,
    )
    limbs = read_csv(
        base / "tree_limbs.csv",
        {
            "limb_id",
            "parent_limb_id",
            "generation",
            "start_x",
            "start_y",
            "end_x",
            "end_y",
            "progress_cells",
            "leads_to_fork",
            "terminal",
            "cell_path",
        },
        errors,
    )
    forks = read_csv(
        base / "tree_forks.csv",
        {
            "fork_id",
            "parent_limb_id",
            "generation",
            "x",
            "y",
            "child_limb_ids",
            "substantial",
        },
        errors,
    )
    platforms = read_csv(
        base / "tree_platforms.csv",
        {
            "platform_id",
            "start_x",
            "end_x",
            "y",
            "top_only",
            "grabbable",
            "landing_valid",
        },
        errors,
    )
    surfaces = read_csv(
        base / "tree_surfaces.csv",
        {"x", "y", "collision_role", "branch_platform_grab"},
        errors,
    )
    movement = read_csv(
        base / "tree_movement_witness.csv",
        {"route_id", "sequence", "from_x", "from_y", "to_x", "to_y", "state"},
        errors,
    )
    recovery = read_csv(
        base / "tree_recovery_witness.csv",
        {"route_id", "sequence", "from_x", "from_y", "to_x", "to_y"},
        errors,
    )

    if canopy.get("schema") != "SV5_12_FIX01_TREE_CANOPY/v1":
        errors.append(f"{profile}: unexpected tree_canopy schema")
    envelope = canopy.get("tree_envelope", {})
    hub_inner = canopy.get("hub_inner_volume", {})
    ex = integer(envelope.get("x"), f"{profile}.envelope.x", errors)
    ey = integer(envelope.get("y"), f"{profile}.envelope.y", errors)
    ew = integer(envelope.get("width"), f"{profile}.envelope.width", errors)
    eh = integer(envelope.get("height"), f"{profile}.envelope.height", errors)
    if not 8 <= ew <= 10:
        errors.append(f"{profile}: envelope width {ew} not in 8..10")
    if not 22 <= eh <= 26:
        errors.append(f"{profile}: envelope height {eh} not in 22..26")
    root_axis = integer(canopy.get("root_axis_x"), f"{profile}.root_axis_x", errors)
    if ew and not ex <= root_axis < ex + ew:
        errors.append(f"{profile}: root axis outside envelope")
    hx = integer(hub_inner.get("x"), f"{profile}.hub_inner.x", errors)
    hy = integer(hub_inner.get("y"), f"{profile}.hub_inner.y", errors)
    hw = integer(hub_inner.get("width"), f"{profile}.hub_inner.width", errors)
    hh = integer(hub_inner.get("height"), f"{profile}.hub_inner.height", errors)
    if hw != 12 or hh != 30:
        errors.append(f"{profile}: Hub inner size {hw}x{hh} is not 12x30")
    if ew and eh and not (
        hx <= ex
        and hy <= ey
        and ex + ew <= hx + hw
        and ey + eh <= hy + hh
    ):
        errors.append(f"{profile}: tree envelope is outside Hub inner volume")

    tree_cells: dict[str, set[tuple[int, int]]] = defaultdict(set)
    for row in cells:
        role = row.get("role", "")
        if role not in TREE_ROLES:
            continue
        point = (
            integer(row.get("x"), f"{profile}.tree_cells.x", errors),
            integer(row.get("y"), f"{profile}.tree_cells.y", errors),
        )
        tree_cells[role].add(point)
        if ew and eh and not (ex <= point[0] < ex + ew and ey <= point[1] < ey + eh):
            errors.append(f"{profile}: tree cell outside envelope {point}")
        if role == "BRANCH_PLATFORM":
            if not truth(row.get("top_only")) or truth(row.get("grab_enabled")):
                errors.append(f"{profile}: branch platform collision contract violated at {point}")
        elif role == "TRUNK_CLIMB":
            if not truth(row.get("grab_enabled")) or not truth(row.get("movement_enabled")):
                errors.append(f"{profile}: trunk climb contract violated at {point}")
        else:
            if not truth(row.get("visual_only")):
                errors.append(f"{profile}: decorative role is not visual-only at {point}")

    trunk = tree_cells["TRUNK_CLIMB"]
    if not trunk:
        errors.append(f"{profile}: no TRUNK_CLIMB cells")
        band_widths = [0.0, 0.0, 0.0]
    else:
        min_y = min(y for _, y in trunk)
        root_width = len({x for x, y in trunk if y == min_y})
        if not 4 <= root_width <= 5:
            errors.append(f"{profile}: root width {root_width} not in 4..5")
        band_widths = []
        for band in range(3):
            start = ey + (eh * band) // 3
            end = ey + (eh * (band + 1)) // 3
            widths = [
                maximum_contiguous_row_width(trunk, y)
                for y in range(start, end)
                if any(row_y == y for _, row_y in trunk)
            ]
            band_widths.append(sum(widths) / len(widths) if widths else 0.0)
        base_widths = [
            maximum_contiguous_row_width(trunk, y)
            for y in range(min_y, min_y + 3)
        ]
        if not base_widths or sum(base_widths) / len(base_widths) < 4:
            errors.append(f"{profile}: bottom three-row trunk average below 4")
    solid = tree_cells["TRUNK_CLIMB"] | tree_cells["BRANCH_PLATFORM"]
    if filled_six_by_six(solid):
        errors.append(f"{profile}: 6x6 solid fill detected")

    limb_by_id: dict[str, dict[str, str]] = {}
    limb_paths: dict[str, list[tuple[int, int]]] = {}
    for row in limbs:
        limb_id = row.get("limb_id", "")
        if not limb_id or limb_id in limb_by_id:
            errors.append(f"{profile}: duplicate/empty limb_id {limb_id!r}")
            continue
        limb_by_id[limb_id] = row
        path = cell_path(row.get("cell_path", ""), f"{profile}.{limb_id}", errors)
        limb_paths[limb_id] = path
        if path:
            declared_start = (
                integer(row.get("start_x"), f"{profile}.{limb_id}.start_x", errors),
                integer(row.get("start_y"), f"{profile}.{limb_id}.start_y", errors),
            )
            declared_end = (
                integer(row.get("end_x"), f"{profile}.{limb_id}.end_x", errors),
                integer(row.get("end_y"), f"{profile}.{limb_id}.end_y", errors),
            )
            if path[0] != declared_start or path[-1] != declared_end:
                errors.append(f"{profile}.{limb_id}: declared endpoints mismatch path")
            if not set(path).issubset(trunk):
                errors.append(f"{profile}.{limb_id}: limb path is not bound to trunk cells")
            progress = integer(row.get("progress_cells"), f"{profile}.{limb_id}.progress", errors)
            if progress != len(path) - 1:
                errors.append(f"{profile}.{limb_id}: progress does not equal ordered path")
        parent = row.get("parent_limb_id", "")
        generation = integer(row.get("generation"), f"{profile}.{limb_id}.generation", errors)
        if parent:
            if parent not in limb_by_id and parent not in {r.get("limb_id") for r in limbs}:
                errors.append(f"{profile}.{limb_id}: missing parent limb {parent}")
            elif parent in {r.get("limb_id") for r in limbs}:
                parent_row = next(r for r in limbs if r.get("limb_id") == parent)
                parent_generation = integer(
                    parent_row.get("generation"),
                    f"{profile}.{parent}.generation",
                    errors,
                )
                if generation != parent_generation + 1:
                    errors.append(f"{profile}.{limb_id}: generation does not follow parent")

    all_limb_cells = {point for path in limb_paths.values() for point in path}
    straight_run = longest_straight_run(limb_paths)
    if straight_run > 5:
        errors.append(f"{profile}: straight limb run {straight_run} above 5")
    if all_limb_cells:
        min_x = min(x for x, _ in all_limb_cells)
        max_x = max(x for x, _ in all_limb_cells)
        span = max_x - min_x + 1
        vertical_span = (
            max(y for _, y in all_limb_cells)
            - min(y for _, y in all_limb_cells)
            + 1
        )
        left_reach = root_axis - min_x
        right_reach = max_x - root_axis
        if span < 8:
            errors.append(f"{profile}: actual limb span {span} below 8")
        if vertical_span < 20:
            errors.append(f"{profile}: actual limb vertical span {vertical_span} below 20")
        if left_reach < 3 or right_reach < 3:
            errors.append(
                f"{profile}: limb reach left/right {left_reach}/{right_reach} below 3/3"
            )
    else:
        span = vertical_span = left_reach = right_reach = 0

    fork_parent_ids = {row.get("parent_limb_id", "") for row in forks}
    substantial_forks = 0
    second_generation_forks = 0
    for row in forks:
        fork_id = row.get("fork_id", "")
        parent_id = row.get("parent_limb_id", "")
        children = [item for item in row.get("child_limb_ids", "").split(";") if item]
        if len(children) < 2:
            errors.append(f"{profile}.{fork_id}: fork has fewer than two children")
        if parent_id not in limb_by_id:
            errors.append(f"{profile}.{fork_id}: missing parent limb")
            continue
        parent_path = limb_paths.get(parent_id, [])
        point = (
            integer(row.get("x"), f"{profile}.{fork_id}.x", errors),
            integer(row.get("y"), f"{profile}.{fork_id}.y", errors),
        )
        if parent_path and point != parent_path[-1]:
            errors.append(f"{profile}.{fork_id}: fork is not at parent endpoint")
        if truth(row.get("substantial")):
            substantial_forks += 1
            parent_generation = integer(
                limb_by_id[parent_id].get("generation"),
                f"{profile}.{parent_id}.generation",
                errors,
            )
            if parent_generation >= 1:
                second_generation_forks += 1
        for child in children:
            child_row = limb_by_id.get(child)
            if child_row is None:
                errors.append(f"{profile}.{fork_id}: missing child limb {child}")
                continue
            if child_row.get("parent_limb_id") != parent_id:
                errors.append(f"{profile}.{fork_id}: child {child} parent mismatch")
            path = limb_paths.get(child, [])
            if path and path[0] != point:
                errors.append(f"{profile}.{fork_id}: child {child} does not start at fork")
            progress = max(0, len(path) - 1)
            if progress < 3 and child not in fork_parent_ids:
                errors.append(f"{profile}.{fork_id}: one-cell/fake child limb {child}")
    if substantial_forks < 3:
        errors.append(f"{profile}: substantial forks {substantial_forks} below 3")
    if second_generation_forks < 1:
        errors.append(f"{profile}: no second-generation fork")

    terminal_rows = [row for row in limbs if truth(row.get("terminal"))]
    terminal_points = {
        limb_paths[row["limb_id"]][-1]
        for row in terminal_rows
        if limb_paths.get(row["limb_id"])
    }
    if not 4 <= len(terminal_points) <= 6:
        errors.append(f"{profile}: distinct climb endpoints {len(terminal_points)} not in 4..6")
    for point in terminal_points:
        width = contiguous_width_at(trunk, point)
        if width > 2:
            errors.append(f"{profile}: terminal stem at {point} is {width} cells thick")

    platform_ids: set[str] = set()
    platform_cells: set[tuple[int, int]] = set()
    normalized_platforms: list[tuple[int, int, int]] = []
    for row in platforms:
        platform_id = row.get("platform_id", "")
        if not platform_id or platform_id in platform_ids:
            errors.append(f"{profile}: duplicate/empty platform_id {platform_id!r}")
            continue
        platform_ids.add(platform_id)
        start = integer(row.get("start_x"), f"{profile}.{platform_id}.start_x", errors)
        end = integer(row.get("end_x"), f"{profile}.{platform_id}.end_x", errors)
        y = integer(row.get("y"), f"{profile}.{platform_id}.y", errors)
        if start > end:
            errors.append(f"{profile}.{platform_id}: reversed platform span")
            start, end = end, start
        if not truth(row.get("top_only")) or truth(row.get("grabbable")):
            errors.append(f"{profile}.{platform_id}: platform is not landing-only/no-grab")
        if not truth(row.get("landing_valid")):
            errors.append(f"{profile}.{platform_id}: invalid landing")
        cells_for_platform = {(x, y) for x in range(start, end + 1)}
        if not cells_for_platform.issubset(tree_cells["BRANCH_PLATFORM"]):
            errors.append(f"{profile}.{platform_id}: platform export not bound to branch cells")
        platform_cells.update(cells_for_platform)
        normalized_platforms.append((start, end, y))
    if not 7 <= len(platform_ids) <= 10:
        errors.append(f"{profile}: platform count {len(platform_ids)} not in 7..10")
    if platform_cells != tree_cells["BRANCH_PLATFORM"]:
        errors.append(f"{profile}: platform rows do not exactly cover branch-platform cells")
    for index, first in enumerate(normalized_platforms):
        for second in normalized_platforms[index + 1 :]:
            first_len = first[1] - first[0] + 1
            second_len = second[1] - second[0] + 1
            mirrored_centers = first[0] + first[1] + second[0] + second[1] == 4 * root_axis
            if first[2] == second[2] and first_len == second_len and mirrored_centers:
                errors.append(f"{profile}: same-height mirror-symmetric platform pair")

    for row in surfaces:
        if row.get("collision_role") == "BRANCH_PLATFORM" or truth(
            row.get("branch_platform_grab")
        ):
            errors.append(f"{profile}: branch platform appears in grab surfaces")

    route_ids = {row.get("route_id", "") for row in movement if row.get("route_id")}
    recovery_ids = {row.get("route_id", "") for row in recovery if row.get("route_id")}
    if len(route_ids) < 2:
        errors.append(f"{profile}: itemless route count {len(route_ids)} below 2")
    if len(recovery_ids) < 3:
        errors.append(f"{profile}: recovery route count {len(recovery_ids)} below 3")
    for row in movement:
        if row.get("state") == "JUMP_GRAB":
            rise = integer(row.get("to_y"), f"{profile}.movement.to_y", errors) - integer(
                row.get("from_y"), f"{profile}.movement.from_y", errors
            )
            if rise > 2:
                errors.append(f"{profile}: initial JUMP_GRAB rise {rise} above 2")

    if tree_validation and not truth(tree_validation.get("success", tree_validation.get("status") == "PASS")):
        errors.append(f"{profile}: tree validation is not PASS")
    if not truth(canopy.get("tree_canopy_fix_ready")):
        errors.append(f"{profile}: TreeCanopyFixReady is false")
    if not truth(canopy.get("tree_grab_geometry_ready")):
        errors.append(f"{profile}: TreeGrabGeometryReady regressed")
    if truth(canopy.get("composed_geometry_ready")) or truth(canopy.get("player_verified")):
        errors.append(f"{profile}: future readiness flag was claimed")
    if integer(canopy.get("local_search_margin_cells"), f"{profile}.local_margin", errors) > 2:
        errors.append(f"{profile}: local search margin above 2")
    if integer(canopy.get("whole_world_copy_per_candidate"), f"{profile}.canopy_copies", errors) != 0:
        errors.append(f"{profile}: canopy whole-world copy is nonzero")
    if integer(canopy.get("whole_world_bfs_per_candidate"), f"{profile}.canopy_bfs", errors) != 0:
        errors.append(f"{profile}: canopy whole-world BFS is nonzero")
    if truth(canopy.get("all_endpoint_pair_comparison")):
        errors.append(f"{profile}: all-endpoint-pair comparison was used")
    if integer(canopy.get("generation_milliseconds"), f"{profile}.tree_generation_ms", errors) > 100:
        errors.append(f"{profile}: tree generation exceeds 100 ms")
    if hub_validation:
        if not truth(hub_validation.get("success")):
            errors.append(f"{profile}: Hub validation is not successful")
        if not truth(hub_validation.get("physical_product_success")):
            errors.append(f"{profile}: physical product failed")
        if integer(hub_validation.get("physical_state_count"), f"{profile}.states", errors) != 9:
            errors.append(f"{profile}: physical state count is not 9")
        if integer(hub_validation.get("resource_order_count"), f"{profile}.orders", errors) != 6:
            errors.append(f"{profile}: resource order count is not 6")
        if integer(hub_validation.get("whole_world_copy_per_candidate"), f"{profile}.copies", errors) != 0:
            errors.append(f"{profile}: whole-world copy per candidate is nonzero")
        if integer(hub_validation.get("whole_world_bfs_per_candidate"), f"{profile}.bfs", errors) != 0:
            errors.append(f"{profile}: whole-world BFS per candidate is nonzero")
        if integer(hub_validation.get("global_product_runs"), f"{profile}.product_runs", errors) != 1:
            errors.append(f"{profile}: global product runs is not one")
        total_ms = float(hub_validation.get("timings_ms", {}).get("total_build_ms", 0))
        if total_ms > 45000:
            errors.append(f"{profile}: profile build {total_ms} ms exceeds 45000")

    for name in ("tree_grab_overview.svg", "tree_collision_overlay.svg"):
        svg = base / "preview" / name
        if not svg.is_file():
            errors.append(f"missing: {svg.as_posix()}")
            continue
        try:
            ET.parse(svg)
        except Exception as exc:
            errors.append(f"{profile}: invalid {name}: {exc}")
            continue
        text = svg.read_text(encoding="utf-8-sig")
        if "BRANCH_PLATFORM" not in text or "no grab" not in text.lower():
            errors.append(f"{profile}: {name} lacks landing-only/no-grab legend")

    summary = {
        "profile": profile,
        "status": "PASS" if not errors else "FAIL",
        "envelope": {"x": ex, "y": ey, "width": ew, "height": eh},
        "root_axis_x": root_axis,
        "root_width": (
            len({x for x, y in trunk if y == min(y for _, y in trunk)})
            if trunk
            else 0
        ),
        "lower_middle_upper_max_stem_width_average": band_widths,
        "limb_horizontal_span": span,
        "limb_vertical_span": vertical_span,
        "left_reach": left_reach,
        "right_reach": right_reach,
        "substantial_forks": substantial_forks,
        "second_generation_forks": second_generation_forks,
        "climb_endpoints": len(terminal_points),
        "longest_straight_limb_run": straight_run,
        "branch_platforms": len(platform_ids),
        "itemless_routes": len(route_ids),
        "recovery_routes": len(recovery_ids),
        "geometry_digest": geometry_digest(cells),
        "overview_sha256": (
            hashlib.sha256((base / "preview/tree_grab_overview.svg").read_bytes()).hexdigest()
            if (base / "preview/tree_grab_overview.svg").is_file()
            else ""
        ),
        "errors": errors,
    }
    return summary, errors


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--root", default=".")
    parser.add_argument(
        "--generated",
        default="MapDesign/MCP/GENERATED/SV5_12_FIX01",
    )
    args = parser.parse_args()
    project = Path(args.root).resolve()
    generated = (project / args.generated).resolve()
    summaries = []
    errors: list[str] = []
    for profile in PROFILES:
        summary, profile_errors = check_profile(generated, profile)
        summaries.append(summary)
        errors.extend(profile_errors)
    if len(summaries) == 2:
        if summaries[0]["geometry_digest"] == summaries[1]["geometry_digest"]:
            errors.append("default/repeat canonical tree geometry is identical")
        if summaries[0]["overview_sha256"] == summaries[1]["overview_sha256"]:
            errors.append("default/repeat overview SVG bytes are identical")
    result = {
        "schema": "SV5_12_FIX01_TREE_CANOPY_AUDIT/v1",
        "status": "PASS_INDEPENDENT_TREE_CANOPY_FIX" if not errors else "FAIL_INDEPENDENT_TREE_CANOPY_FIX",
        "profiles": summaries,
        "errors": errors,
    }
    output = generated / "independent_tree_canopy_audit.json"
    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_text(
        json.dumps(result, indent=2, ensure_ascii=False) + "\n",
        encoding="utf-8",
        newline="\n",
    )
    print(json.dumps(result, indent=2, ensure_ascii=False))
    return 0 if not errors else 1


if __name__ == "__main__":
    sys.exit(main())
