#!/usr/bin/env python3
"""Independent SV5_11 hub-shell checker with task-local retired-symbol scope."""
import argparse
import csv
import hashlib
import json
import re
import subprocess
import sys
from pathlib import Path


BASE_COMMIT = "3408a0ce540f0c9c947b5f2661f68122e06137c3"
BANNED = (
    r"\bSectorId\b",
    r"\bSv5SidepathSector\b",
    r"\bSv5SidepathSectorPair\b",
    r"sector_index\.csv",
    r"sector_pairs\.csv",
)
NEW_OWNED_SOURCE = (
    "Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/Sv5HubShell.cs",
    "Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/Sv5HubShellExport.cs",
    "Assets/_Game/Tests/EditMode/Map/SV5/Sv5HubShellTests.cs",
    "MapDesign/MCP/SV5/19_HUB_SHELL_V5.md",
)
EXISTING_INTEGRATION = (
    "Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/Sv5SpaceGraphPlan.cs",
    "Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/Sv5SpaceGraphPlanner.cs",
    "Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/Sv5SpaceGraphExport.cs",
    "Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/Sv5InfillExport.cs",
    "Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/Sv5SpacePhysicalMovement.cs",
    "MapDesign/MCP/SV5/02_PROTOCOL_V5.md",
)


class Failure(Exception):
    pass


def need(condition, message):
    if not condition:
        raise Failure(message)


def run_git(root, *args):
    process = subprocess.run(
        ["git", "-C", str(root), *args],
        stdout=subprocess.PIPE,
        stderr=subprocess.PIPE,
    )
    need(process.returncode == 0, "git failed: " + " ".join(args))
    return process.stdout.decode("utf-8", errors="replace")


def load_json(path):
    return json.loads(path.read_text(encoding="utf-8-sig"))


def load_csv(path):
    with path.open("r", encoding="utf-8-sig", newline="") as handle:
        return list(csv.DictReader(handle))


def integer(row, key):
    return int(row[key])


def truth(value):
    return str(value).strip().lower() in ("1", "true", "yes")


def banned_matches(text, label):
    return [label + ":" + pattern for pattern in BANNED if re.search(pattern, text, re.I)]


def added_lines(diff_text):
    lines = []
    for line in diff_text.splitlines():
        if line.startswith("+") and not line.startswith("+++"):
            lines.append(line[1:])
    return "\n".join(lines)


def check_retired_symbols(source_root, generated_root, allow_missing_new=False):
    head = run_git(source_root, "rev-parse", "HEAD").strip()
    need(head == BASE_COMMIT, "HEAD changed before SV5_11 Finalize: " + head)
    matches = []
    present_new = 0
    for rel in NEW_OWNED_SOURCE:
        path = source_root / rel
        if not path.exists():
            need(allow_missing_new, "missing SV5_11-owned source: " + rel)
            continue
        present_new += 1
        matches.extend(banned_matches(path.read_text(encoding="utf-8-sig"), rel))

    diff = run_git(
        source_root,
        "diff",
        "--no-ext-diff",
        "--unified=0",
        BASE_COMMIT,
        "--",
        *EXISTING_INTEGRATION,
    )
    matches.extend(banned_matches(added_lines(diff), "SV5_11_ADDED_INTEGRATION_LINES"))

    if generated_root is not None and generated_root.exists():
        for profile in ("default", "repeat"):
            root = generated_root / profile
            if not root.exists():
                continue
            for path in root.rglob("*"):
                if not path.is_file() or path.suffix.lower() not in (".json", ".csv"):
                    continue
                matches.extend(banned_matches(path.read_text(encoding="utf-8-sig"), str(path.relative_to(source_root))))

    need(not matches, "retired Sector symbol introduced by SV5_11: " + ", ".join(matches[:8]))
    return present_new


def digest_rows(rows, fields):
    lines = []
    for row in sorted(rows, key=lambda item: tuple(item[field] for field in fields)):
        lines.append("|".join(row[field] for field in fields))
    return hashlib.sha256("\n".join(lines).encode("utf-8")).hexdigest()


def check_profile(root):
    required = (
        "hub_shell.json",
        "hub_candidates.csv",
        "hub_cells.csv",
        "hub_sockets.csv",
        "hub_ports.csv",
        "hub_connections.csv",
        "hub_validation.json",
        "preview/hub_shell.svg",
    )
    for rel in required:
        need((root / rel).is_file(), "missing export: " + str(root / rel))

    shell = load_json(root / "hub_shell.json")
    validation = load_json(root / "hub_validation.json")
    cells = load_csv(root / "hub_cells.csv")
    sockets = load_csv(root / "hub_sockets.csv")
    ports = load_csv(root / "hub_ports.csv")
    connections = load_csv(root / "hub_connections.csv")

    need(shell["world_width"] == 624 and shell["world_height"] == 416, "wrong world size")
    need(shell["inner_width_cells"] == 12 and shell["inner_height_cells"] == 30, "wrong inner size")
    need(shell["footprint_width_cells"] == 24 and shell["footprint_height_cells"] == 40, "wrong footprint")
    need(shell.get("tree_grab_geometry_ready") is False, "tree grab must remain deferred")
    need(shell.get("composed_geometry_ready") is False, "composed geometry must remain false")
    need(shell.get("player_verified") is False, "player verification must remain false")

    need(cells, "hub has no 1x1 cells")
    coords = {(integer(row, "x"), integer(row, "y")) for row in cells}
    need(len(coords) == len(cells), "duplicate hub cell")
    need(all(integer(row, "micro_x") == integer(row, "x") // 4 for row in cells), "bad micro_x owner")
    need(all(integer(row, "micro_y") == integer(row, "y") // 4 for row in cells), "bad micro_y owner")
    roles = ("AIR", "SOLID", "RESERVED_TREE", "PORT_NECK")
    need(all(row["cell_role"] in roles for row in cells), "bad cell role")

    need(len(sockets) == 6, "six sockets required")
    left = [row for row in sockets if row["side"] == "LEFT"]
    right = [row for row in sockets if row["side"] == "RIGHT"]
    need(len(left) == 3 and len(right) == 3, "three sockets per side required")
    need({row["tier"] for row in left} == {"LOW", "MID", "HIGH"}, "left tiers invalid")
    need({row["tier"] for row in right} == {"LOW", "MID", "HIGH"}, "right tiers invalid")
    need(all(6 <= integer(row, "aperture_height") <= 7 for row in sockets), "aperture outside 6..7")

    active_socket_ids = {row["socket_id"] for row in sockets if truth(row["active"])}
    need(active_socket_ids == {row["socket_id"] for row in ports}, "active socket/port mismatch")
    need(4 <= len(ports) <= 6, "active port count outside 4..6")
    need(len(connections) == len(ports), "port/connection count mismatch")
    need({row["port_id"] for row in ports} == {row["port_id"] for row in connections}, "connection missing port")
    rooms = {row["external_room_id"] for row in connections}
    groups = {row["external_space_group_id"] for row in connections}
    need(len(rooms) == len(connections), "duplicate external RoomId counted")
    need(len(groups) == len(connections), "duplicate external SpaceGroupId counted")
    need(all(not truth(row["protected_overlap"]) for row in connections), "protected overlap")
    need(all(not truth(row["type0_overlap"]) for row in connections), "Type0 overlap")
    need(all(not truth(row["progression_bypass"]) for row in connections), "progression bypass")

    tree_cells = [row for row in cells if row["cell_role"] == "RESERVED_TREE"]
    need(tree_cells, "tree slot not reserved")
    tree_by_x = {}
    for row in tree_cells:
        tree_by_x.setdefault(integer(row, "x"), []).append(integer(row, "y"))
    need(any(max(values) - min(values) + 1 == len(set(values)) for values in tree_by_x.values()), "tree slot lacks vertical continuity")

    passable = {(integer(row, "x"), integer(row, "y")) for row in cells if row["cell_role"] != "SOLID"}
    anchors = {(integer(row, "x"), integer(row, "y")) for row in ports}
    need(anchors <= passable, "port anchor is not a passable hub cell")
    pending = [next(iter(anchors))]
    visited = {pending[0]}
    while pending:
        x, y = pending.pop()
        for other in ((x - 1, y), (x + 1, y), (x, y - 1), (x, y + 1)):
            if other in passable and other not in visited:
                visited.add(other)
                pending.append(other)
    need(anchors <= visited, "active ports do not share one shell-level cell component")

    serialized = digest_rows(cells, ("hub_id", "x", "y", "cell_role", "micro_x", "micro_y"))
    need(serialized == shell["cell_digest"], "cell digest mismatch")
    need(validation["status"] == "PASS", "production validation failed")
    need(validation["retired_grid_symbol_matches"] == 0, "retired grid dependency detected")
    need(validation["whole_world_copy_per_candidate"] == 0, "whole-world candidate copy detected")
    need(validation["whole_world_bfs_per_candidate"] == 0, "whole-world candidate BFS detected")

    return {
        "profile": root.name,
        "hub_id": shell["hub_id"],
        "cells": len(cells),
        "sockets": len(sockets),
        "ports": len(ports),
        "distinct_external_spaces": len(groups),
        "cell_digest": serialized,
    }


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--default", type=Path)
    parser.add_argument("--repeat", type=Path)
    parser.add_argument("--source-root", type=Path, required=True)
    parser.add_argument("--output", type=Path)
    parser.add_argument("--preflight-only", action="store_true")
    args = parser.parse_args()
    try:
        generated_root = None
        if args.default is not None:
            generated_root = args.default.parent
        present_new = check_retired_symbols(
            args.source_root.resolve(),
            generated_root,
            allow_missing_new=args.preflight_only,
        )
        if args.preflight_only:
            print("PASS_CHECKER_SCOPE_PREFLIGHT")
            print(json.dumps({"scope": "SV5_11_TASK_LOCAL", "present_new_owned_sources": present_new}, indent=2))
            return
        need(args.default is not None and args.repeat is not None and args.output is not None, "final mode requires --default, --repeat, and --output")
        results = [check_profile(args.default), check_profile(args.repeat)]
        payload = {
            "status": "PASS_INDEPENDENT_HUB_SHELL",
            "checker_scope": "SV5_11_TASK_LOCAL",
            "predecessor_commit": BASE_COMMIT,
            "profiles": results,
        }
        args.output.parent.mkdir(parents=True, exist_ok=True)
        args.output.write_text(json.dumps(payload, indent=2, sort_keys=True) + "\n", encoding="utf-8", newline="\n")
        print("PASS_INDEPENDENT_HUB_SHELL")
    except (Failure, OSError, ValueError, KeyError, json.JSONDecodeError) as error:
        print("FAIL_INDEPENDENT_HUB_SHELL:", error, file=sys.stderr)
        raise SystemExit(2)


if __name__ == "__main__":
    main()
