#!/usr/bin/env python3
"""Reproduce the SV5_06_FIX02 route-label firewall from exact exported evidence."""
from __future__ import annotations

from collections import Counter, defaultdict, deque
from pathlib import Path, PurePosixPath
import argparse
import csv
import hashlib
import json
import sys


EXPECTED = {
    "connections.csv": "b5eba89bc41ab9f91dc172a1970baa04a6d2ff778b27e725cc7e9daa9a978379",
    "ports.csv": "4b26743b9ec9d761d34a8277058ed0acd6b5dc05b9cd061ed17332401413c5a8",
    "contact_checks.csv": "9f1a09897682f5874553aa994fa46173e0357320062dc73884b2ee22598c88c8",
    "gate_geometry.json": "0cab19392162c1dc8aae6dc78295a2719c37e488fdf4068021ad926380d49446",
}
CONDITIONS = {
    "FORGE_GATED_SEAL_APPROACH": ["INITIAL"],
    "SEAL_GATED_BOSS_APPROACH": ["INITIAL", "FORGE"],
    "BOSS_GATED_EXIT_APPROACH": ["INITIAL", "FORGE", "SEAL"],
}
STATES = {
    "INITIAL": (0, False, False, False),
    "FORGE": (7, True, False, False),
    "SEAL": (7, True, True, False),
}


def sha(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def read_rows(path: Path):
    with path.open(encoding="utf-8-sig", newline="") as handle:
        return list(csv.DictReader(handle))


def points(value: str):
    return [tuple(map(int, item.split(":"))) for item in value.split("|") if item]


def face(first, second):
    return tuple(sorted((first, second)))


def gate_open(gate, state):
    predicate = gate["typed_predicate"]
    return (state[0] & predicate["required_resource_mask"] == predicate["required_resource_mask"]
            and (not predicate["requires_forge"] or state[1])
            and (not predicate["requires_seal"] or state[2])
            and (not predicate["requires_boss_complete"] or state[3]))


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--project-root", type=Path, required=True)
    args = parser.parse_args()
    root = args.project_root.resolve()
    generated = root / "MapDesign/MCP/GENERATED/SV5_06_FIX02"
    for name, expected in EXPECTED.items():
        path = generated / name
        if not path.is_file() or sha(path) != expected:
            raise RuntimeError("Exact FIX02 evidence mismatch: " + name)

    connections = read_rows(generated / "connections.csv")
    ports = {row["port_id"]: row for row in read_rows(generated / "ports.csv")}
    contacts = read_rows(generated / "contact_checks.csv")
    gates = json.loads((generated / "gate_geometry.json").read_text(encoding="utf-8-sig"))["gates"]
    separated = [item for item in contacts if item["crossing"] == "Separated"]
    counts = {
        "all": len(contacts),
        "separated": len(separated),
        "shared": sum(item["kind"] == "SHARED" for item in separated),
        "face": sum(item["kind"] == "FACE" for item in separated),
        "empty_boundary": sum(not item["boundary_id"] for item in separated),
        "passage_to_passage": sum("Passage" in item["route_a_kinds"] and
                                  "Passage" in item["route_b_kinds"] for item in separated),
    }
    expected_counts = {"all": 7188, "separated": 225, "shared": 61, "face": 164,
                       "empty_boundary": 225, "passage_to_passage": 93}
    if counts != expected_counts:
        raise RuntimeError("FIX02 contact counts changed: " + repr(counts))

    route = {row["source_graph_edge_id"] or row["connection_id"]: row for row in connections}
    allowed = {key: set(points(row["envelope"])) for key, row in route.items()}
    crossing = defaultdict(list)
    for item in contacts:
        first = (item["route_a"], (int(item["route_a_x"]), int(item["route_a_y"])))
        second = (item["route_b"], (int(item["route_b_x"]), int(item["route_b_y"])))
        crossing[first].append(second)
        crossing[second].append(first)

    def reachable(connection, state, all_contacts):
        key = connection["source_graph_edge_id"] or connection["connection_id"]
        blocked = defaultdict(set)
        for gate in gates:
            if not gate_open(gate, state):
                blocked[gate["source_route_id"]].update(face(tuple(item["first"]), tuple(item["second"]))
                                                        for item in gate["blocking_faces"])
        start = [(key, cell) for cell in points(ports[connection["from_port_id"]]["boundary_cells"])
                 if cell in allowed[key]]
        goals = {(key, cell) for cell in points(ports[connection["to_port_id"]]["boundary_cells"])}
        queue = deque(start)
        seen = set(start)
        while queue:
            current = queue.popleft()
            current_route, cell = current
            if current in goals:
                return True
            for neighbor in ((cell[0] + 1, cell[1]), (cell[0] - 1, cell[1]),
                             (cell[0], cell[1] + 1), (cell[0], cell[1] - 1)):
                candidate = (current_route, neighbor)
                if (neighbor in allowed[current_route] and face(cell, neighbor) not in blocked[current_route]
                        and candidate not in seen):
                    seen.add(candidate)
                    queue.append(candidate)
            if all_contacts:
                for candidate in crossing.get(current, []):
                    if candidate not in seen:
                        seen.add(candidate)
                        queue.append(candidate)
        return False

    by_condition = {row["condition"]: row for row in connections}
    cases = []
    for condition, state_names in CONDITIONS.items():
        connection = by_condition[condition]
        for state_name in state_names:
            state = STATES[state_name]
            isolated = reachable(connection, state, False)
            physical = reachable(connection, state, True)
            cases.append({"condition": condition, "state": state_name,
                          "route_identity_reachable": isolated, "all_contacts_reachable": physical})
            if isolated or not physical:
                raise RuntimeError("Expected route-label firewall bypass was not reproduced: " + repr(cases[-1]))
    print(json.dumps({
        "status": "EXPECTED_FIX02_FAILURE_REPRODUCED",
        "scope": "EXACT_EXPORTED_ACCEPTED_ENVELOPE_AND_GATE_FACES;NOT_UNITY_OR_PLAYER",
        "contact_counts": counts,
        "forbidden_cases": cases,
    }, ensure_ascii=False, indent=2))


if __name__ == "__main__":
    try:
        main()
    except (OSError, ValueError, KeyError, RuntimeError) as exc:
        print(json.dumps({"status": "BLOCKED", "reason": str(exc)}, ensure_ascii=False))
        sys.exit(2)
