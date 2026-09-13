from __future__ import annotations

import argparse
import csv
import hashlib
import json
import re
import sys
from collections import defaultdict
from pathlib import Path

PROFILES = ("default", "repeat")
MOVES = {"WALK", "STEP_UP", "STEP_DOWN", "JUMP", "DROP", "JUMP_GRAB"}
FACES = {"LEFT", "RIGHT", "TOP", "BOTTOM"}
FORBIDDEN = ("SectorId", "sector_id", "sector_index")


def read_csv(path: Path) -> list[dict[str, str]]:
    with path.open("r", encoding="utf-8-sig", newline="") as stream:
        return list(csv.DictReader(stream))


def as_int(row: dict[str, str], key: str, label: str, errors: list[str]) -> int:
    try:
        return int(row[key])
    except Exception:
        errors.append(f"{label}: invalid {key}")
        return 0


def as_bool(value: str, label: str, errors: list[str]) -> bool:
    value = str(value).lower()
    if value in {"true", "1"}:
        return True
    if value in {"false", "0"}:
        return False
    errors.append(f"{label}: invalid boolean {value!r}")
    return False


def fields(table: list[dict[str, str]], required: set[str], label: str,
           errors: list[str]) -> None:
    actual = set(table[0]) if table else set()
    missing = required - actual
    if missing:
        errors.append(f"{label}: missing fields {sorted(missing)}")


def movement_edges(table: list[dict[str, str]], id_key: str, solid: set[tuple[int, int]],
                   surfaces: set[tuple[int, int]], label: str, errors: list[str]) -> dict[str, int]:
    groups: dict[str, list[dict[str, str]]] = defaultdict(list)
    for row in table:
        groups[row[id_key]].append(row)
    for route_id, group in groups.items():
        route = f"{label}:{route_id}"
        group.sort(key=lambda row: as_int(row, "sequence", route, errors))
        seq = [as_int(row, "sequence", route, errors) for row in group]
        if seq != list(range(len(group))):
            errors.append(f"{route}: sequence is not contiguous")
        previous_to = None
        for row in group:
            fx, fy = as_int(row, "from_x", route, errors), as_int(row, "from_y", route, errors)
            tx, ty = as_int(row, "to_x", route, errors), as_int(row, "to_y", route, errors)
            dx, dy = tx - fx, ty - fy
            move = row["move_type"]
            if previous_to is not None and previous_to != (fx, fy):
                errors.append(f"{route}: discontinuous movement witness")
            previous_to = (tx, ty)
            if move not in MOVES:
                errors.append(f"{route}: unknown move {move}")
            if not as_bool(row["body_clear"], route + ":body_clear", errors) or not as_bool(
                    row["head_clear"], route + ":head_clear", errors):
                errors.append(f"{route}: blocked body/head clearance")
            if move == "WALK" and not (abs(dx) == 1 and dy == 0):
                errors.append(f"{route}: invalid WALK delta {dx},{dy}")
            elif move in {"STEP_UP", "STEP_DOWN"} and not (abs(dx) == 1 and abs(dy) == 1):
                errors.append(f"{route}: invalid STEP delta {dx},{dy}")
            elif move == "JUMP" and not (1 <= abs(dx) <= 3 and -2 <= dy <= 2):
                errors.append(f"{route}: invalid JUMP delta {dx},{dy}")
            elif move == "DROP" and not (abs(dx) <= 1 and -4 <= dy <= -1):
                errors.append(f"{route}: invalid DROP delta {dx},{dy}")
            elif move == "JUMP_GRAB":
                if not (abs(dx) <= 2 and 1 <= dy <= 2):
                    errors.append(f"{route}: jump-grab rise exceeds two cells: {dx},{dy}")
                cx, cy = as_int(row, "contact_x", route, errors), as_int(row, "contact_y", route, errors)
                if (cx, cy) not in solid or (cx, cy) not in surfaces:
                    errors.append(f"{route}: jump-grab contact is not an exported grabbable solid")
            if move != "JUMP_GRAB" and dy > 2:
                errors.append(f"{route}: upward gain exceeds two cells")
        if not group:
            errors.append(f"{route}: empty witness")
    return {key: len(value) for key, value in groups.items()}


def check_profile(root: Path, generated: Path, name: str) -> dict:
    folder = generated / name
    errors: list[str] = []
    required = ["hub_shell.json", "hub_connections.csv", "hub_connection_cells.csv",
                "hub_connection_movement.csv", "tree_grab.json", "tree_cells.csv",
                "tree_surfaces.csv", "tree_movement_witness.csv", "tree_recovery_witness.csv",
                "tree_validation.json", "preview/tree_grab.svg"]
    for rel in required:
        if not (folder / rel).is_file():
            errors.append(f"{name}: missing {rel}")
    if errors:
        return {"profile": name, "status": "FAIL", "errors": errors}
    shell = json.loads((folder / "hub_shell.json").read_text(encoding="utf-8"))
    tree = json.loads((folder / "tree_grab.json").read_text(encoding="utf-8"))
    validation = json.loads((folder / "tree_validation.json").read_text(encoding="utf-8"))
    connections = read_csv(folder / "hub_connections.csv")
    connection_cells = read_csv(folder / "hub_connection_cells.csv")
    connection_moves = read_csv(folder / "hub_connection_movement.csv")
    tree_cells = read_csv(folder / "tree_cells.csv")
    surface_rows = read_csv(folder / "tree_surfaces.csv")
    routes = read_csv(folder / "tree_movement_witness.csv")
    recovery = read_csv(folder / "tree_recovery_witness.csv")
    fields(connections, {"connection_id", "external_x", "external_y", "route_verified", "plan_digest"},
           name + ":connections", errors)
    fields(connection_cells, {"connection_id", "cell_role", "x", "y", "final_value"},
           name + ":connection_cells", errors)
    movement_fields = {"sequence", "from_x", "from_y", "to_x", "to_y", "move_type",
                       "body_clear", "head_clear", "contact_x", "contact_y", "plan_digest"}
    fields(connection_moves, movement_fields | {"connection_id"}, name + ":connection_moves", errors)
    fields(tree_cells, {"x", "y", "role", "before_value", "final_value", "owner", "plan_digest"},
           name + ":tree_cells", errors)
    fields(surface_rows, {"x", "y", "face", "plan_digest"}, name + ":surfaces", errors)
    fields(routes, movement_fields | {"route_id"}, name + ":routes", errors)
    fields(recovery, movement_fields | {"route_id"}, name + ":recovery", errors)
    if errors:
        return {"profile": name, "status": "FAIL", "errors": errors}

    if not 4 <= len(connections) <= 6:
        errors.append(f"{name}: connection count is not 4..6")
    anchors = [(as_int(row, "external_x", name, errors), as_int(row, "external_y", name, errors))
               for row in connections]
    if len(set(anchors)) != len(connections):
        errors.append(f"{name}: logical connections alias physical anchors")
    if len(set(anchors)) < 4:
        errors.append(f"{name}: fewer than four physical external anchors")
    connection_ids = {row["connection_id"] for row in connections}
    move_ids = {row["connection_id"] for row in connection_moves}
    if connection_ids != move_ids:
        errors.append(f"{name}: connection/movement witness IDs differ")
    if not all(as_bool(row["route_verified"], name + ":route_verified", errors) for row in connections):
        errors.append(f"{name}: unverified Hub connection")

    footprint = shell.get("footprint", {})
    bx, by = int(footprint.get("x", -1)), int(footprint.get("y", -1))
    bw, bh = int(footprint.get("width", 0)), int(footprint.get("height", 0))
    def outside(point: tuple[int, int]) -> bool:
        return not (bx <= point[0] < bx + bw and by <= point[1] < by + bh)
    bodies: dict[str, set[tuple[int, int]]] = defaultdict(set)
    for row in connection_cells:
        if row["cell_role"] == "CENTERLINE":
            point = (as_int(row, "x", name, errors), as_int(row, "y", name, errors))
            if outside(point) and point not in set(anchors):
                bodies[row["connection_id"]].add(point)
    for first in sorted(bodies):
        for second in sorted(bodies):
            if first < second and bodies[first] & bodies[second]:
                errors.append(f"{name}: connection bodies merge outside Hub: {first}, {second}")

    solid = {(as_int(row, "x", name, errors), as_int(row, "y", name, errors))
             for row in tree_cells if row["final_value"] == "SOLID"}
    surfaces: set[tuple[int, int]] = set()
    for row in surface_rows:
        point = (as_int(row, "x", name, errors), as_int(row, "y", name, errors))
        surfaces.add(point)
        if point not in solid or row["face"] not in FACES:
            errors.append(f"{name}: invalid grabbable surface {point}")
    connection_edge_counts = movement_edges(connection_moves, "connection_id", solid, surfaces,
                                             name + ":connection", errors)
    route_counts = movement_edges(routes, "route_id", solid, surfaces, name + ":tree", errors)
    recovery_counts = movement_edges(recovery, "route_id", solid, surfaces, name + ":recovery", errors)
    if len(route_counts) < 2:
        errors.append(f"{name}: fewer than two tree routes")
    if len(recovery_counts) < 3:
        errors.append(f"{name}: fewer than three recovery witnesses")
    signatures = []
    for route_id in sorted(route_counts):
        signatures.append(tuple((row["move_type"], row["contact_x"], row["contact_y"])
                                for row in sorted((r for r in routes if r["route_id"] == route_id),
                                                  key=lambda r: int(r["sequence"]))))
    if len(set(signatures)) < 2:
        errors.append(f"{name}: tree routes have no distinct branch/contact sequence")

    coords = {(as_int(row, "x", name, errors), as_int(row, "y", name, errors)) for row in tree_cells}
    for x, y in sorted(solid):
        if all((x + dx, y + dy) in solid for dx in range(6) for dy in range(6)):
            errors.append(f"{name}: forbidden 6x6 solid fill at {x},{y}")
            break
    if len(coords) != len(tree_cells):
        errors.append(f"{name}: duplicate tree cell coordinates")

    expected = {
        "status": "PASS", "success": True, "distinct_physical_external_anchors": len(set(anchors)),
        "tree_route_count": len(route_counts), "tree_grab_geometry_ready": True,
        "composed_geometry_ready": False, "player_verified": False,
        "physical_product_success": True, "physical_state_count": 9,
        "resource_order_count": 6, "global_product_runs": 1,
        "whole_world_copy_per_candidate": 0, "whole_world_bfs_per_candidate": 0,
    }
    for key, value in expected.items():
        if validation.get(key) != value:
            errors.append(f"{name}: validation {key} expected {value!r}, got {validation.get(key)!r}")
    if tree.get("tree_grab_geometry_ready") is not True or tree.get("player_verified") is not False:
        errors.append(f"{name}: tree readiness boundary mismatch")
    for rel in required:
        if rel.endswith((".csv", ".json")):
            text = (folder / rel).read_text(encoding="utf-8-sig")
            bad = [token for token in FORBIDDEN if token in text]
            if bad:
                errors.append(f"{name}:{rel}: retired identifiers {bad}")
    return {
        "profile": name, "status": "FAIL" if errors else "PASS",
        "logical_connections": len(connections), "physical_external_anchors": len(set(anchors)),
        "connection_movement_witnesses": len(connection_edge_counts),
        "tree_routes": len(route_counts), "recovery_routes": len(recovery_counts),
        "tree_cells": len(tree_cells), "grabbable_surfaces": len(surfaces), "errors": errors,
    }


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--project-root", required=True)
    parser.add_argument("--generated", default="MapDesign/MCP/GENERATED/SV5_12_TREE_GRAB")
    parser.add_argument("--output")
    args = parser.parse_args()
    root = Path(args.project_root).resolve()
    generated = (root / args.generated).resolve()
    try:
        generated.relative_to(root)
    except ValueError:
        print(json.dumps({"status": "FAIL", "errors": ["generated path escapes project root"]}, indent=2))
        return 1
    profiles = [check_profile(root, generated, name) for name in PROFILES]
    errors = [error for profile in profiles for error in profile["errors"]]
    report = {"schema": "SV5_12_TREE_GRAB_INDEPENDENT_AUDIT/v1",
              "status": "PASS_INDEPENDENT_TREE_GRAB" if not errors else "FAIL_INDEPENDENT_TREE_GRAB",
              "profiles": profiles, "errors": errors}
    text = json.dumps(report, ensure_ascii=False, indent=2) + "\n"
    if args.output:
        output = (root / args.output).resolve()
        try:
            output.relative_to(root)
        except ValueError:
            print(json.dumps({"status": "FAIL", "errors": ["output path escapes project root"]}, indent=2))
            return 1
        output.parent.mkdir(parents=True, exist_ok=True)
        output.write_text(text, encoding="utf-8", newline="\n")
    print(text, end="")
    return 1 if errors else 0


if __name__ == "__main__":
    sys.exit(main())
