#!/usr/bin/env python3
"""Reproduce two SV5_06_FIX03 physical-plan failures from exact exported evidence."""
from __future__ import annotations

from collections import Counter, deque
from pathlib import Path
import argparse
import csv
import hashlib
import json
import sys


EXPECTED = {
    "connections.csv": "6838d9de552b3f8687f05219d2dae2b836c5c49b6ad0e4d268afcc3a5b4ddacb",
    "ports.csv": "c09b68bc08ee7868ffe9b2f79024de9a83fe3e988560c26c0b1a340b3330eb0b",
    "contact_checks.csv": "a2b96265678392e8eb2008ff712c6f174e17a0fcb904bd9067d7b548b41c1f1e",
    "gate_geometry.json": "99f4bab5835490763a1a06aabf687e67ba016ded217425cc34888865a0245953",
    "state_proofs.json": "4dca6e805f34f09509c3d4517da556eedc64e1b039735e23c116d35895399770",
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
    generated = args.project_root.resolve() / "MapDesign/MCP/GENERATED/SV5_06_FIX03"
    for name, expected in EXPECTED.items():
        path = generated / name
        if not path.is_file() or sha(path) != expected:
            raise RuntimeError("Exact FIX03 evidence mismatch: " + name)

    connections = read_rows(generated / "connections.csv")
    by_id = {row["connection_id"]: row for row in connections}
    ports = {row["port_id"]: row for row in read_rows(generated / "ports.csv")}
    contacts = read_rows(generated / "contact_checks.csv")
    gates = json.loads((generated / "gate_geometry.json").read_text(encoding="utf-8-sig"))["gates"]
    gate_by_boundary = {gate["boundary_id"]: gate for gate in gates}

    conditional = [row for row in contacts if row["crossing"] == "ConditionalGate"]
    coverage = Counter()
    unsupported_examples = []
    for row in conditional:
        owner = gate_by_boundary.get(row["boundary_id"])
        kind = row["kind"]
        supported = False
        if owner is not None:
            if kind == "SHARED":
                point = (int(row["first_x"]), int(row["first_y"]))
                supported = point in {tuple(item) for item in owner["blocking_cells"]}
            elif kind == "FACE":
                contact_face = face((int(row["first_x"]), int(row["first_y"])),
                                    (int(row["second_x"]), int(row["second_y"])))
                supported = contact_face in {face(tuple(item["first"]), tuple(item["second"]))
                                             for item in owner["blocking_faces"]}
        coverage[(kind, supported)] += 1
        if not supported and len(unsupported_examples) < 8:
            unsupported_examples.append({
                "contact_id": row["contact_id"], "kind": kind,
                "first": [int(row["first_x"]), int(row["first_y"])],
                "second": [int(row["second_x"]), int(row["second_y"])],
                "boundary_id": row["boundary_id"],
                "owner_gate_id": owner["gate_id"] if owner else "",
            })
    expected_coverage = Counter({("FACE", False): 157, ("SHARED", False): 61, ("FACE", True): 7})
    if coverage != expected_coverage:
        raise RuntimeError("FIX03 local-barrier counts changed: " + repr(coverage))

    passage = set()
    for row in connections:
        passage.update(points(row["centerline"]))
        passage.update(points(row["aperture_cells"]))

    def reachable(row, state, active_gates):
        blocked_cells = set()
        blocked_faces = set()
        for gate in active_gates:
            if gate_open(gate, state):
                continue
            blocked_cells.update(tuple(item) for item in gate["blocking_cells"])
            blocked_faces.update(face(tuple(item["first"]), tuple(item["second"]))
                                 for item in gate["blocking_faces"])
        starts = [item for item in points(ports[row["from_port_id"]]["boundary_cells"])
                  if item in passage and item not in blocked_cells]
        goals = set(points(ports[row["to_port_id"]]["boundary_cells"])) & passage
        queue = deque(starts)
        seen = set(starts)
        while queue:
            current = queue.popleft()
            if current in goals:
                return True
            for nxt in ((current[0] - 1, current[1]), (current[0] + 1, current[1]),
                        (current[0], current[1] - 1), (current[0], current[1] + 1)):
                if (nxt not in passage or nxt in blocked_cells or face(current, nxt) in blocked_faces
                        or nxt in seen):
                    continue
                seen.add(nxt)
                queue.append(nxt)
        return False

    initial = (0, False, False, False)
    deepstar_ids = ["SV5_CORE_CONN_93f752cf7b8f813c", "SV5_CORE_CONN_be0aa26aa7cc3b03"]
    deepstar = []
    for connection_id in deepstar_ids:
        row = by_id[connection_id]
        item = {
            "connection_id": connection_id,
            "condition": row["condition"],
            "without_gates": reachable(row, initial, []),
            "all_closed_gates": reachable(row, initial, gates),
            "boss_only": reachable(row, initial, [gate for gate in gates if gate["predicate"] == "BOSS_GATED_EXIT_APPROACH"]),
            "forge_only": reachable(row, initial, [gate for gate in gates if gate["predicate"] == "FORGE_GATED_SEAL_APPROACH"]),
            "seal_only": reachable(row, initial, [gate for gate in gates if gate["predicate"] == "SEAL_GATED_BOSS_APPROACH"]),
            "boss_plus_forge": reachable(row, initial, [gate for gate in gates if gate["predicate"] in
                                                          {"BOSS_GATED_EXIT_APPROACH", "FORGE_GATED_SEAL_APPROACH"}]),
        }
        deepstar.append(item)
        if item != {
            "connection_id": connection_id, "condition": row["condition"], "without_gates": True,
            "all_closed_gates": False, "boss_only": True, "forge_only": True, "seal_only": True,
            "boss_plus_forge": False,
        }:
            raise RuntimeError("FIX03 DeepStar lock changed: " + repr(item))

    proofs = json.loads((generated / "state_proofs.json").read_text(encoding="utf-8-sig"))
    first_order = proofs["actual_projection"][0]
    if (first_order["resource_order"] != "DeepStarYeast>CondensedCoefficientSap>MooncoreOre"
            or not first_order["success"]):
        raise RuntimeError("FIX03 logical first-order proof changed")

    optional = []
    for connection_id in ["SV5_OPTIONAL_01", "SV5_OPTIONAL_RETURN_TO_START", "SV5_OPTIONAL_TO_VILLAGE"]:
        row = by_id[connection_id]
        optional.append({"connection_id": connection_id,
                         "without_gates": reachable(row, initial, []),
                         "all_closed_gates": reachable(row, initial, gates)})
    if any(item["without_gates"] is not True or item["all_closed_gates"] is not False for item in optional):
        raise RuntimeError("FIX03 optional collateral-lock counts changed: " + repr(optional))

    print(json.dumps({
        "status": "EXPECTED_FIX03_FAILURES_REPRODUCED",
        "scope": "EXACT_EXPORTED_PLAN;GLOBAL_CARDINAL_PASSAGE_GRAPH;NOT_UNITY_OR_PLAYER",
        "local_barrier_coverage": {
            "conditional_contacts": len(conditional),
            "supported": coverage[("FACE", True)],
            "unsupported": coverage[("FACE", False)] + coverage[("SHARED", False)],
            "face_supported": coverage[("FACE", True)],
            "face_unsupported": coverage[("FACE", False)],
            "shared_supported": coverage[("SHARED", True)],
            "shared_unsupported": coverage[("SHARED", False)],
            "unsupported_examples": unsupported_examples,
        },
        "deepstar_initial_lock": deepstar,
        "logical_first_order_claim": {"resource_order": first_order["resource_order"],
                                      "success": first_order["success"]},
        "optional_collateral_locks": optional,
    }, ensure_ascii=False, indent=2))


if __name__ == "__main__":
    try:
        main()
    except (OSError, ValueError, KeyError, RuntimeError) as exc:
        print(json.dumps({"status": "BLOCKED", "reason": str(exc)}, ensure_ascii=False))
        sys.exit(2)
