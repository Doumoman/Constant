#!/usr/bin/env python3
"""Independent SV5_11 hub-shell export checker. It imports no production C#."""
import argparse
import csv
import hashlib
import json
import re
import sys
from pathlib import Path


class Failure(Exception):
    pass


def need(condition, message):
    if not condition:
        raise Failure(message)


def load_json(path):
    return json.loads(path.read_text(encoding="utf-8-sig"))


def load_csv(path):
    with path.open("r", encoding="utf-8-sig", newline="") as handle:
        return list(csv.DictReader(handle))


def integer(row, key):
    return int(row[key])


def truth(value):
    return str(value).strip().lower() in ("1", "true", "yes")


def digest_rows(rows, fields):
    lines = []
    for row in sorted(rows, key=lambda item: tuple(item[field] for field in fields)):
        lines.append("|".join(row[field] for field in fields))
    return hashlib.sha256("\n".join(lines).encode("utf-8")).hexdigest()


def check_profile(root):
    required = [
        "hub_shell.json", "hub_candidates.csv", "hub_cells.csv", "hub_sockets.csv",
        "hub_ports.csv", "hub_connections.csv", "hub_validation.json", "preview/hub_shell.svg"
    ]
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

    need(len(cells) > 0, "hub has no 1x1 cells")
    coords = {(integer(row, "x"), integer(row, "y")) for row in cells}
    need(len(coords) == len(cells), "duplicate hub cell")
    need(all(integer(row, "micro_x") == integer(row, "x") // 4 for row in cells), "bad micro_x owner")
    need(all(integer(row, "micro_y") == integer(row, "y") // 4 for row in cells), "bad micro_y owner")
    need(all(row["cell_role"] in ("AIR", "SOLID", "RESERVED_TREE", "PORT_NECK") for row in cells), "bad cell role")

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
    external = {(row["external_room_id"], row["external_space_group_id"]) for row in connections}
    rooms = {row["external_room_id"] for row in connections}
    groups = {row["external_space_group_id"] for row in connections}
    need(len(external) == len(connections), "duplicate external space counted")
    need(len(rooms) == len(connections), "duplicate external RoomId counted")
    need(len(groups) == len(connections), "duplicate external SpaceGroupId counted")
    need(all(not truth(row["protected_overlap"]) for row in connections), "protected overlap")
    need(all(not truth(row["type0_overlap"]) for row in connections), "Type0 overlap")
    need(all(not truth(row["progression_bypass"]) for row in connections), "progression bypass")

    tree_cells = [row for row in cells if row["cell_role"] == "RESERVED_TREE"]
    need(len(tree_cells) > 0, "tree slot not reserved")
    by_x = {}
    for row in tree_cells:
        by_x.setdefault(integer(row, "x"), []).append(integer(row, "y"))
    need(any(max(values) - min(values) + 1 == len(set(values)) for values in by_x.values()), "tree slot lacks vertical continuity")

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

    serialized = digest_rows(cells, ["hub_id", "x", "y", "cell_role", "micro_x", "micro_y"])
    need(serialized == shell["cell_digest"], "cell digest mismatch")
    need(validation["status"] == "PASS", "production validation failed")
    need(validation["active_sector_symbols"] == 0, "retired Sector dependency detected")
    need(validation["whole_world_copy_per_candidate"] == 0, "whole-world candidate copy detected")
    need(validation["whole_world_bfs_per_candidate"] == 0, "whole-world candidate BFS detected")

    return {
        "profile": root.name,
        "hub_id": shell["hub_id"],
        "cells": len(cells),
        "sockets": len(sockets),
        "ports": len(ports),
        "distinct_external_spaces": len(external),
        "cell_digest": serialized,
    }


def check_retired_symbols(source_root):
    patterns = [
        r"\bSectorId\b", r"\bSv5SidepathSector\b", r"\bSv5SidepathSectorPair\b",
        r"sector_index\.csv", r"sector_pairs\.csv"
    ]
    roots = [
        source_root / "Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning",
        source_root / "Assets/_Game/Tests/EditMode/Map/SV5"
    ]
    matches = []
    for root in roots:
        if not root.exists():
            continue
        for path in root.rglob("*.cs"):
            text = path.read_text(encoding="utf-8-sig")
            for pattern in patterns:
                if re.search(pattern, text, re.I):
                    matches.append(str(path.relative_to(source_root)) + ":" + pattern)
    need(not matches, "retired Sector symbol found: " + ", ".join(matches[:8]))


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--default", type=Path, required=True)
    parser.add_argument("--repeat", type=Path, required=True)
    parser.add_argument("--source-root", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    try:
        check_retired_symbols(args.source_root)
        results = [check_profile(args.default), check_profile(args.repeat)]
        need(results[0]["hub_id"] != "" and results[1]["hub_id"] != "", "empty HubId")
        payload = {"status": "PASS_INDEPENDENT_HUB_SHELL", "profiles": results}
        args.output.parent.mkdir(parents=True, exist_ok=True)
        args.output.write_text(json.dumps(payload, indent=2, sort_keys=True) + "\n", encoding="utf-8", newline="\n")
        print("PASS_INDEPENDENT_HUB_SHELL")
    except (Failure, OSError, ValueError, KeyError, json.JSONDecodeError) as error:
        print("FAIL_INDEPENDENT_HUB_SHELL:", error, file=sys.stderr)
        raise SystemExit(2)


if __name__ == "__main__":
    main()
