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
REQUIRED_FILES = (
    "hub_shell.json",
    "hub_connections.csv",
    "hub_connection_cells.csv",
    "hub_connection_checks.csv",
    "constraint_sources.json",
    "hub_validation.json",
    "preview/hub_connection_fix.svg",
)
REQUIRED_CATEGORIES = {
    "PROTECTED", "TYPE0", "PROGRESSION_GATE", "CORE",
    "RESERVATION", "INFILL", "LOOP", "SIDEPATH",
}
ALLOWED_DIRECTIONS = {"HUB_TO_EXTERNAL", "EXTERNAL_TO_HUB", "BIDIRECTIONAL"}
ALLOWED_ENDPOINT_CONTACT = {"INFILL", "LOOP", "SIDEPATH"}
FORBIDDEN_TEXT = ("SectorId", "sector_id", "sector_index")


def sha(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def rows(path: Path) -> list[dict[str, str]]:
    with path.open("r", encoding="utf-8-sig", newline="") as stream:
        return list(csv.DictReader(stream))


def boolean(value: str, label: str, errors: list[str]) -> bool:
    lowered = str(value).strip().lower()
    if lowered in {"true", "1"}:
        return True
    if lowered in {"false", "0"}:
        return False
    errors.append(f"{label}: invalid boolean {value!r}")
    return False


def integer(row: dict[str, str], key: str, label: str, errors: list[str]) -> int:
    try:
        return int(row[key])
    except Exception:
        errors.append(f"{label}: invalid integer field {key}")
        return 0


def require_fields(table: list[dict[str, str]], fields: set[str], label: str,
                   errors: list[str]) -> None:
    actual = set(table[0].keys()) if table else set()
    missing = fields - actual
    if missing:
        errors.append(f"{label}: missing columns {sorted(missing)}")


def load_constraint_sources(project: Path, generated: Path, profile_dir: Path,
                            errors: list[str]) -> tuple[dict[tuple[int, int], set[str]], dict]:
    manifest_path = profile_dir / "constraint_sources.json"
    try:
        manifest = json.loads(manifest_path.read_text(encoding="utf-8"))
    except Exception as exc:
        errors.append(f"{profile_dir.name}: invalid constraint_sources.json: {exc}")
        return {}, {}
    if manifest.get("schema") != "SV5_11_FIX01_CONSTRAINT_SOURCES/v1":
        errors.append(f"{profile_dir.name}: constraint source schema mismatch")
    if manifest.get("profile") != profile_dir.name:
        errors.append(f"{profile_dir.name}: constraint source profile mismatch")
    union: dict[tuple[int, int], set[str]] = defaultdict(set)
    categories: set[str] = set()
    for index, source in enumerate(manifest.get("sources", [])):
        label = f"{profile_dir.name}:constraint[{index}]"
        category = source.get("category", "")
        categories.add(category)
        if category not in REQUIRED_CATEGORIES:
            errors.append(f"{label}: unknown category {category!r}")
        rel = source.get("path", "")
        path = (project / rel).resolve()
        try:
            path.relative_to(project)
        except ValueError:
            errors.append(f"{label}: path escapes project root")
            continue
        if not path.is_file():
            errors.append(f"{label}: source missing: {rel}")
            continue
        expected = source.get("sha256", "")
        if not re.fullmatch(r"[0-9a-f]{64}", expected) or sha(path) != expected:
            errors.append(f"{label}: source SHA mismatch: {rel}")
            continue
        if path.is_relative_to(generated) and source.get("source_kind") != "TESTED_BASELINE_SNAPSHOT":
            errors.append(f"{label}: current generated source requires TESTED_BASELINE_SNAPSHOT")
        x_key = source.get("x_column", "x")
        y_key = source.get("y_column", "y")
        try:
            source_rows = rows(path)
        except Exception as exc:
            errors.append(f"{label}: cannot parse CSV: {exc}")
            continue
        predicate = source.get("filter")
        for row in source_rows:
            if predicate:
                column = predicate.get("column")
                allowed = set(predicate.get("values", []))
                if row.get(column) not in allowed:
                    continue
            try:
                point = (int(row[x_key]), int(row[y_key]))
            except Exception:
                errors.append(f"{label}: invalid coordinate row")
                break
            union[point].add(category)
    missing = REQUIRED_CATEGORIES - categories
    if missing:
        errors.append(f"{profile_dir.name}: missing constraint categories {sorted(missing)}")
    return union, manifest


def check_profile(project: Path, generated: Path, name: str) -> dict:
    errors: list[str] = []
    folder = generated / name
    for rel in REQUIRED_FILES:
        if not (folder / rel).is_file():
            errors.append(f"{name}: missing {rel}")
    if errors:
        return {"profile": name, "status": "FAIL", "errors": errors}

    connections = rows(folder / "hub_connections.csv")
    cells = rows(folder / "hub_connection_cells.csv")
    checks = rows(folder / "hub_connection_checks.csv")
    require_fields(connections, {
        "connection_id", "port_id", "socket_id", "external_room_id",
        "external_space_group_id", "port_x", "port_y", "external_x", "external_y",
        "direction", "centerline_cells", "route_verified", "protected_overlap",
        "type0_overlap", "progression_bypass", "plan_digest",
    }, f"{name}:connections", errors)
    require_fields(cells, {
        "connection_id", "cell_role", "sequence", "x", "y", "before_value",
        "final_value", "head_value", "support_value", "changed", "ownership",
        "plan_digest",
    }, f"{name}:cells", errors)
    require_fields(checks, {"connection_id", "check_id", "status", "detail", "plan_digest"},
                   f"{name}:checks", errors)
    if errors:
        return {"profile": name, "status": "FAIL", "errors": errors}

    if not 4 <= len(connections) <= 6:
        errors.append(f"{name}: connection count {len(connections)} is outside 4..6")
    ids = [row["connection_id"] for row in connections]
    if len(ids) != len(set(ids)):
        errors.append(f"{name}: duplicate connection id")
    if len({row["external_room_id"] for row in connections}) != len(connections):
        errors.append(f"{name}: external Room IDs are not distinct")
    if len({row["external_space_group_id"] for row in connections}) != len(connections):
        errors.append(f"{name}: external SpaceGroup IDs are not distinct")
    digests = {row["plan_digest"] for row in connections + cells + checks}
    if len(digests) != 1 or not all(re.fullmatch(r"[0-9a-f]{64}", value or "") for value in digests):
        errors.append(f"{name}: plan digest set is invalid: {sorted(digests)}")

    constraints, source_manifest = load_constraint_sources(project, generated, folder, errors)
    by_connection: dict[str, list[dict[str, str]]] = defaultdict(list)
    for row in cells:
        by_connection[row["connection_id"]].append(row)
    checks_by_connection: dict[str, dict[str, str]] = defaultdict(dict)
    for row in checks:
        key = row["connection_id"]
        check_id = row["check_id"]
        if check_id in checks_by_connection[key]:
            errors.append(f"{name}:{key}: duplicate check {check_id}")
        checks_by_connection[key][check_id] = row["status"]

    required_checks = {
        "ACTUAL_CELL_ROUTE", "CARDINAL_CENTERLINE", "HEAD_CLEARANCE",
        "MOVEMENT_WITNESS", "NO_PROTECTED_BODY", "NO_TYPE0",
        "NO_PROGRESSION_BYPASS", "EXTERNAL_ANCHOR_UNCHANGED", "OWNERSHIP_UNIQUE",
    }
    total_changed = 0
    total_centerline = 0
    for connection in connections:
        cid = connection["connection_id"]
        label = f"{name}:{cid}"
        if connection["direction"] not in ALLOWED_DIRECTIONS:
            errors.append(f"{label}: invalid direction")
        if not boolean(connection["route_verified"], label + ":route_verified", errors):
            errors.append(f"{label}: route is not verified")
        own_rows = by_connection.get(cid, [])
        centerline = [row for row in own_rows if row["cell_role"] == "CENTERLINE"]
        centerline.sort(key=lambda row: integer(row, "sequence", label, errors))
        declared = integer(connection, "centerline_cells", label, errors)
        if len(centerline) != declared or not 2 <= len(centerline) <= 120:
            errors.append(f"{label}: centerline count mismatch/out of range")
        sequences = [integer(row, "sequence", label, errors) for row in centerline]
        if sequences != list(range(len(centerline))):
            errors.append(f"{label}: centerline sequence is not contiguous from zero")
        points = [(integer(row, "x", label, errors), integer(row, "y", label, errors))
                  for row in centerline]
        if len(points) != len(set(points)):
            errors.append(f"{label}: centerline self-intersects")
        if points:
            port = (integer(connection, "port_x", label, errors), integer(connection, "port_y", label, errors))
            external = (integer(connection, "external_x", label, errors), integer(connection, "external_y", label, errors))
            if points[0] != port or points[-1] != external:
                errors.append(f"{label}: centerline endpoints do not match port/external anchors")
            if any(abs(a[0] - b[0]) + abs(a[1] - b[1]) != 1 for a, b in zip(points, points[1:])):
                errors.append(f"{label}: non-cardinal centerline step")
            external_categories = constraints.get(external, set())
            if external_categories - ALLOWED_ENDPOINT_CONTACT:
                errors.append(f"{label}: forbidden external-anchor categories {sorted(external_categories)}")
        body = set(points[1:-1]) if len(points) >= 2 else set()
        body_categories = {category for point in body for category in constraints.get(point, set())}
        protected = bool(body_categories & {"PROTECTED", "CORE", "RESERVATION", "INFILL", "LOOP", "SIDEPATH"})
        type0 = "TYPE0" in body_categories
        bypass = "PROGRESSION_GATE" in body_categories
        if protected:
            errors.append(f"{label}: protected body intersection {sorted(body_categories)}")
        if type0:
            errors.append(f"{label}: Type0 body intersection")
        if bypass:
            errors.append(f"{label}: progression-gate body intersection")
        if boolean(connection["protected_overlap"], label + ":protected_overlap", errors) != protected:
            errors.append(f"{label}: exported protected_overlap disagrees with recomputation")
        if boolean(connection["type0_overlap"], label + ":type0_overlap", errors) != type0:
            errors.append(f"{label}: exported type0_overlap disagrees with recomputation")
        if boolean(connection["progression_bypass"], label + ":progression_bypass", errors) != bypass:
            errors.append(f"{label}: exported progression_bypass disagrees with recomputation")

        seen_cell_roles: set[tuple[int, int, str]] = set()
        for row in own_rows:
            point = (integer(row, "x", label, errors), integer(row, "y", label, errors))
            identity = (point[0], point[1], row["cell_role"])
            if identity in seen_cell_roles:
                errors.append(f"{label}: duplicate cell-role row {identity}")
            seen_cell_roles.add(identity)
            role = row["cell_role"]
            if role in {"CENTERLINE", "HEAD_CLEARANCE"} and row["final_value"] != "AIR":
                errors.append(f"{label}: {role} final value is not AIR at {point}")
            if role == "CENTERLINE" and row["head_value"] != "AIR":
                errors.append(f"{label}: centerline head clearance is not AIR at {point}")
            if role == "SUPPORT" and row["final_value"] != "SOLID":
                errors.append(f"{label}: support final value is not SOLID at {point}")
            changed = boolean(row["changed"], label + ":changed", errors)
            if changed:
                total_changed += 1
                if row["ownership"] != "HUB_CONNECTION_ACTUAL":
                    errors.append(f"{label}: changed cell lacks HUB_CONNECTION_ACTUAL ownership at {point}")
                if point in constraints:
                    errors.append(f"{label}: changed cell intersects constraint at {point}")
            elif row["ownership"] == "HUB_CONNECTION_ACTUAL":
                errors.append(f"{label}: unchanged cell falsely claims connection ownership at {point}")
        missing_checks = required_checks - set(checks_by_connection.get(cid, {}))
        if missing_checks:
            errors.append(f"{label}: missing checks {sorted(missing_checks)}")
        failed_checks = {key for key, value in checks_by_connection.get(cid, {}).items() if value != "PASS"}
        if failed_checks:
            errors.append(f"{label}: non-PASS checks {sorted(failed_checks)}")
        total_centerline += len(centerline)

    unknown_cell_connections = set(by_connection) - set(ids)
    if unknown_cell_connections:
        errors.append(f"{name}: cells reference unknown connections {sorted(unknown_cell_connections)}")

    try:
        validation = json.loads((folder / "hub_validation.json").read_text(encoding="utf-8"))
        shell = json.loads((folder / "hub_shell.json").read_text(encoding="utf-8"))
    except Exception as exc:
        errors.append(f"{name}: invalid JSON output: {exc}")
        validation, shell = {}, {}
    if validation.get("success") is not True:
        errors.append(f"{name}: validation.success is not true")
    if validation.get("connection_count") != len(connections):
        errors.append(f"{name}: validation connection count mismatch")
    if validation.get("physical_product_success") is not True:
        errors.append(f"{name}: post-Hub physical product did not pass")
    if validation.get("physical_state_count") != 9 or validation.get("resource_order_count") != 6:
        errors.append(f"{name}: physical product coverage is not 9x6")
    if validation.get("global_product_runs") != 1:
        errors.append(f"{name}: global product run count is not one")
    if validation.get("whole_world_copy_per_candidate") != 0 or validation.get("whole_world_bfs_per_candidate") != 0:
        errors.append(f"{name}: per-candidate whole-world work is nonzero")
    for key in ("tree_grab_geometry_ready", "composed_geometry_ready", "player_verified"):
        if validation.get(key) is not False and shell.get(key) is not False:
            errors.append(f"{name}: {key} must remain false")
    if total_changed <= 0:
        errors.append(f"{name}: no actual changed connection cell was exported")

    for rel in REQUIRED_FILES:
        if rel.endswith((".csv", ".json")):
            text = (folder / rel).read_text(encoding="utf-8-sig")
            found = [token for token in FORBIDDEN_TEXT if token in text]
            if found:
                errors.append(f"{name}:{rel}: retired identifiers {found}")
    return {
        "profile": name,
        "status": "FAIL" if errors else "PASS",
        "connections": len(connections),
        "centerline_cells": total_centerline,
        "changed_cells": total_changed,
        "constraint_cells": len(constraints),
        "constraint_sources": len(source_manifest.get("sources", [])),
        "plan_digest": next(iter(digests)) if len(digests) == 1 else None,
        "errors": errors,
    }


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--project-root", required=True)
    parser.add_argument("--generated", default="MapDesign/MCP/GENERATED/SV5_11_FIX01")
    parser.add_argument("--output")
    args = parser.parse_args()
    project = Path(args.project_root).resolve()
    generated = (project / args.generated).resolve()
    try:
        generated.relative_to(project)
    except ValueError:
        print(json.dumps({"status": "FAIL", "errors": ["generated path escapes project root"]}, indent=2))
        return 1
    results = [check_profile(project, generated, profile) for profile in PROFILES]
    errors = [error for result in results for error in result["errors"]]
    report = {
        "schema": "SV5_11_FIX01_INDEPENDENT_AUDIT/v1",
        "status": "PASS_INDEPENDENT_HUB_CONNECTIONS" if not errors else "FAIL_INDEPENDENT_HUB_CONNECTIONS",
        "profiles": results,
        "errors": errors,
    }
    encoded = (json.dumps(report, ensure_ascii=False, indent=2) + "\n")
    if args.output:
        output = (project / args.output).resolve()
        try:
            output.relative_to(project)
        except ValueError:
            print(json.dumps({"status": "FAIL", "errors": ["output path escapes project root"]}, indent=2))
            return 1
        output.parent.mkdir(parents=True, exist_ok=True)
        output.write_text(encoded, encoding="utf-8", newline="\n")
    print(encoded, end="")
    return 1 if errors else 0


if __name__ == "__main__":
    sys.exit(main())
