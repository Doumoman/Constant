from __future__ import annotations

import argparse
import csv
import json
import re
import sys
from pathlib import Path

TASK = "SV5_13_JUMP_CONTRACT"
GEN = Path("MapDesign/MCP/GENERATED") / TASK


def read_csv(path: Path, required: list[str], errors: list[str]) -> list[dict[str, str]]:
    if not path.is_file():
        errors.append(f"missing export: {path.as_posix()}")
        return []
    with path.open("r", encoding="utf-8-sig", newline="") as handle:
        reader = csv.DictReader(handle)
        fields = reader.fieldnames or []
        missing = [name for name in required if name not in fields]
        if missing:
            errors.append(f"{path.name} missing columns: {missing}")
        return list(reader)


def integer(row: dict[str, str], key: str, errors: list[str], label: str) -> int:
    try:
        return int(row[key])
    except Exception:
        errors.append(f"{label} invalid integer {key}")
        return 0


def truth(value: str) -> bool:
    return value.strip().lower() in {"true", "1", "yes"}


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--project-root", default=".")
    parser.add_argument("--output")
    args = parser.parse_args()
    root = Path(args.project_root).resolve()
    gen = root /GEN
    output = Path(args.output).resolve() if args.output else gen / "independent_jump_contract_audit.json"
    errors: list[str] = []

    owned = [
        root / "Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/Sv5JumpContract.cs",
        root / "Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/Sv5JumpContractExport.cs",
        root / "Assets/_Game/Tests/EditMode/Map/SV5/Sv5JumpContractTests.cs",
        root / "MapDesign/MCP/SV5/21_JUMP_CONTRACT_V5.md",
    ]
    for path in owned:
        if not path.is_file():
            errors.append(f"missing owned file: {path.relative_to(root).as_posix()}")
    source = "\n".join(path.read_text(encoding="utf-8-sig", errors="replace")
                         for path in owned if path.is_file())
    required_symbols = ["Sv5JumpSupportKind", "Sv5JumpValidationState", "Sv5JumpSupport",
                        "Sv5JumpGrabEdge", "Sv5JumpLink", "GapAir", "Rise", "Takeoff",
                        "Landing", "Planned", "StaticScreen", "PlayerVerified"]
    for symbol in required_symbols:
        if symbol not in source:
            errors.append(f"missing contract symbol: {symbol}")
    for pattern in (r"\bSectorId\b", r"\bsector_id\b", r"\bsector_index\b", r"48\s*[xX×]\s*32"):
        if re.search(pattern, source):
            errors.append(f"retired partition symbol in SV5_13-owned scope: {pattern}")

    contract_path = gen / "jump_contract.json"
    validation_path = gen / "contract_validation.json"
    contract = {}
    validation = {}
    for path, target in ((contract_path, "contract"), (validation_path, "validation")):
        if not path.is_file():
            errors.append(f"missing export: {path.relative_to(root).as_posix()}")
            continue
        try:
            value = json.loads(path.read_text(encoding="utf-8-sig"))
            if target == "contract":
                contract = value
            else:
                validation = value
        except Exception as exc:
            errors.append(f"invalid JSON {path.name}: {exc}")

    supports = read_csv(gen / "supports.csv",
        ["support_id", "kind", "x", "y", "width", "height", "top_y", "top_x_min",
         "top_x_max", "active_route", "decorative_only", "validation_state"], errors)
    grabs = read_csv(gen / "grab_edges.csv",
        ["grab_edge_id", "support_id", "contact_x", "contact_y", "face",
         "approach_direction", "hang_body_x", "hang_body_y", "pull_up_x", "pull_up_y", "safe"], errors)
    links = read_csv(gen / "links.csv",
        ["link_id", "source_support_id", "target_support_id", "mode", "direction",
         "takeoff_x", "takeoff_y", "landing_x", "landing_y", "gap_air", "rise",
         "grab_edge_id", "validation_state"], errors)
    proofs = read_csv(gen / "measurement_proofs.csv",
        ["link_id", "source_near_face_x", "target_near_face_x", "computed_gap_air",
         "source_top_y", "target_top_y", "computed_rise", "formula_pass"], errors)
    states = read_csv(gen / "validation_states.csv",
        ["object_id", "object_kind", "state", "player_run_id", "player_result"], errors)

    by_support = {row.get("support_id", ""): row for row in supports}
    by_grab = {row.get("grab_edge_id", ""): row for row in grabs}
    by_proof = {row.get("link_id", ""): row for row in proofs}
    expected = {"J1": (3, 1, "JUMP"), "J2": (4, 1, "JUMP"), "J3": (2, 2, "JUMP_GRAB")}
    seen: dict[str, dict[str, str]] = {}
    for row in links:
        lid = row.get("link_id", "")
        if lid in seen:
            errors.append(f"duplicate link_id: {lid}")
        seen[lid] = row
        source_row = by_support.get(row.get("source_support_id", ""))
        target_row = by_support.get(row.get("target_support_id", ""))
        if not source_row or not target_row:
            errors.append(f"{lid} references missing support")
            continue
        sx = integer(source_row, "x", errors, lid)
        sw = integer(source_row, "width", errors, lid)
        tx = integer(target_row, "x", errors, lid)
        tw = integer(target_row, "width", errors, lid)
        direction = row.get("direction", "")
        if direction == "LEFT_TO_RIGHT":
            source_face, target_face = sx + sw - 1, tx
            computed_gap = max(0, target_face - source_face - 1)
        elif direction == "RIGHT_TO_LEFT":
            source_face, target_face = sx, tx + tw - 1
            computed_gap = max(0, source_face - target_face - 1)
        else:
            errors.append(f"{lid} invalid direction")
            source_face = target_face = computed_gap = 0
        source_top = integer(source_row, "top_y", errors, lid)
        target_top = integer(target_row, "top_y", errors, lid)
        computed_rise = target_top - source_top
        if integer(row, "gap_air", errors, lid) != computed_gap:
            errors.append(f"{lid} gap_air is not boundary-empty-cell count")
        if integer(row, "rise", errors, lid) != computed_rise:
            errors.append(f"{lid} rise is not top-support-height delta")
        proof = by_proof.get(lid)
        if not proof or not truth(proof.get("formula_pass", "")):
            errors.append(f"{lid} missing passing measurement proof")
        elif (integer(proof, "source_near_face_x", errors, lid) != source_face or
              integer(proof, "target_near_face_x", errors, lid) != target_face or
              integer(proof, "computed_gap_air", errors, lid) != computed_gap or
              integer(proof, "source_top_y", errors, lid) != source_top or
              integer(proof, "target_top_y", errors, lid) != target_top or
              integer(proof, "computed_rise", errors, lid) != computed_rise):
            errors.append(f"{lid} measurement proof values disagree")

        rise = integer(row, "rise", errors, lid)
        mode = row.get("mode", "")
        if mode == "JUMP" and rise > 1:
            errors.append(f"{lid} normal jump rise exceeds one")
        if mode == "JUMP_GRAB":
            if rise > 2:
                errors.append(f"{lid} jump-grab rise exceeds two")
            edge = by_grab.get(row.get("grab_edge_id", ""))
            if not edge:
                errors.append(f"{lid} missing grab edge")
            else:
                owner = by_support.get(edge.get("support_id", ""))
                if not owner or owner.get("kind") != "SOLID" or not truth(edge.get("safe", "")):
                    errors.append(f"{lid} grab edge is not a safe SOLID face")
                if owner and owner.get("support_id") != target_row.get("support_id"):
                    errors.append(f"{lid} grab edge does not belong to target")

    for lid, values in expected.items():
        row = seen.get(lid)
        if not row:
            errors.append(f"missing canonical link: {lid}")
        elif (integer(row, "gap_air", errors, lid), integer(row, "rise", errors, lid),
              row.get("mode")) != values:
            errors.append(f"canonical link mismatch: {lid}")
    if "J3" in seen:
        target = by_support.get(seen["J3"].get("target_support_id", ""), {})
        if target.get("kind") != "SOLID":
            errors.append("J3 target is not SOLID")

    active_kinds = {row.get("kind") for row in supports
                    if truth(row.get("active_route", "")) and not truth(row.get("decorative_only", ""))}
    if not {"SOLID", "ONE_WAY"}.issubset(active_kinds):
        errors.append("active examples do not use both SOLID and ONE_WAY")
    one_way_ids = {row.get("support_id") for row in supports if row.get("kind") == "ONE_WAY"}
    for edge in grabs:
        if edge.get("support_id") in one_way_ids:
            errors.append(f"ONE_WAY has grab edge: {edge.get('grab_edge_id')}")

    allowed_states = {"PLANNED", "STATIC_SCREEN", "PLAYER_VERIFIED"}
    for row in states:
        state = row.get("state", "")
        if state not in allowed_states:
            errors.append(f"invalid validation state: {state}")
        if state == "PLAYER_VERIFIED":
            errors.append(f"SV5_13 prematurely claims PLAYER_VERIFIED: {row.get('object_id')}")
    readiness = contract.get("readiness", {}) if isinstance(contract, dict) else {}
    expected_readiness = {"jump_contract_ready": True, "jump_solid_geometry_ready": False,
                          "jump_grab_geometry_ready": False, "composed_geometry_ready": False,
                          "player_verified": False}
    for key, value in expected_readiness.items():
        if readiness.get(key) is not value:
            errors.append(f"readiness mismatch: {key}")
    performance = contract.get("performance", {}) if isinstance(contract, dict) else {}
    if performance.get("whole_world_builds_in_new_targeted_tests") != 0:
        errors.append("new targeted tests build the whole world")
    if validation.get("status") != "PASS" or validation.get("errors") not in ([], None):
        errors.append("contract_validation.json is not PASS with zero errors")
    svg_path = gen / "preview/jump_contract.svg"
    if not svg_path.is_file():
        errors.append("missing export: preview/jump_contract.svg")
    else:
        svg = svg_path.read_text(encoding="utf-8-sig", errors="replace")
        for label in ("SOLID", "ONE_WAY", "JUMP_GRAB", "gap_air", "rise", "takeoff", "landing"):
            if label not in svg:
                errors.append(f"SVG missing visible label: {label}")

    audit = {
        "schema": "SV5_13_JUMP_CONTRACT_AUDIT/v1",
        "status": "PASS_INDEPENDENT_JUMP_CONTRACT" if not errors else "FAIL_INDEPENDENT_JUMP_CONTRACT",
        "canonical_links": {lid: {
            "gap_air": integer(row, "gap_air", [], lid),
            "rise": integer(row, "rise", [], lid),
            "mode": row.get("mode")
        } for lid, row in seen.items() if lid in expected},
        "support_count": len(supports),
        "grab_edge_count": len(grabs),
        "validation_state_count": len(states),
        "player_verified_count": sum(1 for row in states if row.get("state") == "PLAYER_VERIFIED"),
        "whole_world_builds_in_new_targeted_tests": performance.get("whole_world_builds_in_new_targeted_tests"),
        "errors": errors,
    }
    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_text(json.dumps(audit, ensure_ascii=False, indent=2) + "\n", encoding="utf-8", newline="\n")
    print(audit["status"])
    if errors:
        for error in errors:
            print(error)
        return 1
    return 0


if __name__ == "__main__":
    sys.exit(main())
