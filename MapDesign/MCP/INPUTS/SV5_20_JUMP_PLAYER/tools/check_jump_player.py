from __future__ import annotations

import argparse
import csv
import hashlib
import json
import math
import re
import subprocess
import sys
from collections import defaultdict
from pathlib import Path

TASK = "SV5_20_JUMP_PLAYER"
BASE = "660d0c58ec0f65cbbbe200716d0e3c36b0ea4ba7"
INPUT_DIGEST = "f8b2e69686dbd9b7a29b82e4e4a8ca2c46c6183a005a5373e986e04b21f4288b"
RECIPES = ("JUMP012_MIXED_R0", "JUMP012_MIXED_MX")
AMENDMENT = "SV5_20_FIX04_PLAYER_BINDING"
DRIVER_PATH = "Assets/_Game/Live/Runtime/Movement/CharacterLiveMovementDriver.cs"
DRIVER_BASE_OID = "8eda20b5a2b1e854cf13c062adea411ca8798411"
DRIVER_BASE_SHA = "df58bb6edca4179cefb3d149baf032f03724f2aac4f5afafb8cc10a66b40a836"
DRIVER_BASE_BYTES = 33883
DRIVER_APPROVED_SHA = "77ac90e6c65f4dc83a104deb1db6c02177b1c50c2797426c6e3b07aeedc0d8f8"
DRIVER_APPROVED_BYTES = 39703
TARGETED_EDITMODE_SHA = "e60954ca85bf997ec643bfa37ce96c02b0873668aa8e1c89aa1f440ff668c0be"
PHYSICAL_38_SHA = "89e690b032377e35253f88a5f34efc766af3c96bdb8b84b8536dc0db6dafaba0"
PLAYER_PATHS = {
    "Assets/_Game/Live/Prefabs/CharacterLivePlayer.prefab",
    "Assets/_Game/Live/Runtime/Movement/CharacterLiveMovementDriver.cs",
    "Assets/_Game/Live/Runtime/Movement/CharacterLiveMovementSettings.cs",
    "Assets/_Game/Live/Runtime/Movement/CharacterLiveGrabSurface.cs",
    "Assets/_Game/Live/Runtime/Movement/CharacterLiveOneWayPlatform.cs",
    "Assets/_Game/Character/Runtime/Input/CharacterInputSnapshot.cs",
}


def rows(path: Path) -> list[dict[str, str]]:
    with path.open("r", encoding="utf-8-sig", newline="") as handle:
        return list(csv.DictReader(handle))


def yes(value: object) -> bool:
    return str(value).strip().lower() == "true"


def iv(row: dict[str, str], key: str) -> int:
    return int(row[key])


def fv(row: dict[str, str], key: str) -> float:
    return float(row[key])


def sha_bytes(data: bytes) -> str:
    return hashlib.sha256(data).hexdigest()


def sha(path: Path) -> str:
    return sha_bytes(path.read_bytes())


def check_columns(label: str, data: list[dict[str, str]], required: set[str],
                  errors: list[str]) -> None:
    actual = set(data[0]) if data else set()
    if not required.issubset(actual):
        errors.append(label + ":" + ",".join(sorted(required - actual)))


def git_blob(root: Path, rev: str, rel: str) -> tuple[str, bytes]:
    oid = subprocess.run(["git", "rev-parse", f"{rev}:{rel}"], cwd=root,
                         check=True, stdout=subprocess.PIPE,
                         stderr=subprocess.PIPE, text=True).stdout.strip()
    raw = subprocess.run(["git", "cat-file", "blob", oid], cwd=root,
                         check=True, stdout=subprocess.PIPE,
                         stderr=subprocess.PIPE).stdout
    return oid, raw


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--project-root", default=".")
    args = parser.parse_args()
    root = Path(args.project_root).resolve()
    clear = root / "MapDesign/MCP/GENERATED/SV5_18_JUMP_CLEARANCE"
    recovery = root / "MapDesign/MCP/GENERATED/SV5_19_JUMP_RECOVERY"
    out = root / "MapDesign/MCP/GENERATED/SV5_20_JUMP_PLAYER"
    required = [
        clear / "clearance_links.csv",
        recovery / "jump_recovery.json",
        recovery / "recovery_miss_probes.csv",
        recovery / "recovery_routes.csv",
        out / "jump_player.json",
        out / "player_bindings.json",
        out / "player_binding_amendment.json",
        out / "player_measurements.json",
        out / "player_cases.csv",
        out / "player_trace.csv",
        out / "player_validation.json",
        out / "preview/jump_player.svg",
        out / "targeted_editmode_results.xml",
        out / "targeted_playmode_results.xml",
    ]
    errors: list[str] = []
    for path in required:
        if not path.is_file():
            errors.append("MISSING:" + str(path.relative_to(root)).replace("\\", "/"))
    if errors:
        print("FAIL_INDEPENDENT_JUMP_PLAYER")
        print("\n".join(errors))
        return 1

    try:
        main_links = rows(clear / "clearance_links.csv")
        recovery_summary = json.loads((recovery / "jump_recovery.json").read_text(encoding="utf-8-sig"))
        probes = rows(recovery / "recovery_miss_probes.csv")
        routes = rows(recovery / "recovery_routes.csv")
        summary = json.loads((out / "jump_player.json").read_text(encoding="utf-8-sig"))
        bindings = json.loads((out / "player_bindings.json").read_text(encoding="utf-8-sig"))
        amendment = json.loads((out / "player_binding_amendment.json").read_text(encoding="utf-8-sig"))
        measurements = json.loads((out / "player_measurements.json").read_text(encoding="utf-8-sig"))
        cases = rows(out / "player_cases.csv")
        traces = rows(out / "player_trace.csv")
        validation = json.loads((out / "player_validation.json").read_text(encoding="utf-8-sig"))
    except Exception as exc:
        print("FAIL_INDEPENDENT_JUMP_PLAYER")
        print("PARSE:" + str(exc))
        return 1

    check_columns("CASE_COLUMNS", cases, {
        "case_id", "recipe_id", "case_kind", "main_link_order",
        "source_id", "target_id", "checkpoint_link_order", "outcome",
        "actual_player", "actual_driver", "actual_rigidbody2d",
        "actual_collider2d", "used_grab", "used_one_way", "used_item",
        "reverse_required", "teleports_after_start", "fixed_steps"
    }, errors)
    check_columns("TRACE_COLUMNS", traces, {
        "case_id", "step", "input_x", "input_up", "input_down",
        "input_jump", "input_shift", "body_x", "body_y", "velocity_x",
        "velocity_y", "is_grounded", "is_grabbing", "is_climbing",
        "is_dropping_through", "contact_kind", "event"
    }, errors)

    if recovery_summary.get("status") != "PASS" or recovery_summary.get("recovery_digest") != INPUT_DIGEST:
        errors.append("SV5_19_RECOVERY_IDENTITY")
    if summary.get("schema") != "SV5_20_JUMP_PLAYER/v1" or summary.get("task") != TASK:
        errors.append("SUMMARY_SCHEMA")
    if summary.get("status") != "PASS" or summary.get("input_recovery_digest") != INPUT_DIGEST:
        errors.append("SUMMARY_IDENTITY")
    expected_counts = {
        "recipe_count": 2,
        "main_link_case_count": 18,
        "passed_main_link_case_count": 18,
        "full_route_case_count": 2,
        "passed_full_route_case_count": 2,
        "recovery_case_count": 18,
        "passed_recovery_case_count": 18,
    }
    for key, value in expected_counts.items():
        if summary.get(key) != value:
            errors.append(f"SUMMARY_COUNT:{key}:{summary.get(key)}")
    if summary.get("actual_player_prefab") != "Assets/_Game/Live/Prefabs/CharacterLivePlayer.prefab":
        errors.append("ACTUAL_PLAYER_PREFAB")
    for key in ("actual_movement_driver", "actual_rigidbody2d", "actual_capsule_collider2d",
                "fixed_timestep", "itemless"):
        if summary.get(key) is not True:
            errors.append("SUMMARY_TRUE:" + key)
    for key in ("reverse_required",):
        if summary.get(key) is not False:
            errors.append("SUMMARY_FALSE:" + key)
    for key in ("post_start_teleports", "whole_world_builds", "whole_world_searches",
                "global_endpoint_comparisons", "sector_partitions"):
        if summary.get(key) != 0:
            errors.append(f"SUMMARY_ZERO:{key}:{summary.get(key)}")
    if summary.get("jump_plus_grab_max_rise_cells") != 2:
        errors.append("TWO_CELL_CAP")
    ready = summary.get("readiness", {})
    for key in ("JumpRecipeReady", "ComposedGeometryReady", "SweptClearanceReady",
                "RecoveryReady", "PlayerVerified"):
        if ready.get(key) is not True:
            errors.append("READINESS:" + key)

    if bindings.get("schema") != "SV5_20_PLAYER_BINDINGS/v1" or bindings.get("base_commit") != BASE:
        errors.append("BINDING_SCHEMA_OR_BASE")
    expected_amendment = {
        "schema": "SV5_20_PLAYER_BINDING_AMENDMENT/v1",
        "task": TASK,
        "amendment": AMENDMENT,
        "reason": "ACTUAL_CAPSULE_GRAB_CORRECTION",
        "base_commit": BASE,
        "path": DRIVER_PATH,
        "base_git_blob_oid": DRIVER_BASE_OID,
        "base_sha256": DRIVER_BASE_SHA,
        "base_bytes": DRIVER_BASE_BYTES,
        "approved_sha256": DRIVER_APPROVED_SHA,
        "approved_bytes": DRIVER_APPROVED_BYTES,
        "approved_role": "APPROVED_FIX04_RUNTIME_CORRECTION",
        "movement_constants_retuned": False,
        "allowed_behavior": [
            "actual_capsule_outside_grab_probe",
            "nearest_external_physical_surface_filter",
            "actual_capsule_contact_offset_grab_spacing",
            "attached_rigidbody_grab_anchor_ownership",
            "released_collider_upward_escape_only",
            "separation_bounded_collision_restore",
        ],
        "evidence": {
            "targeted_editmode_sha256": TARGETED_EDITMODE_SHA,
            "physical_38_case_sha256": PHYSICAL_38_SHA,
            "rmap03_sha256": "17989038f1387d93e754367cc21b71c8d38a1b5023d9a6fc4405c7ca84659ff4",
        },
    }
    if amendment != expected_amendment:
        errors.append("PLAYER_BINDING_AMENDMENT_CONTENT")
    package_amendment = root / "MapDesign/MCP/INPUTS/SV5_20_JUMP_PLAYER/FIX04_PLAYER_BINDING_AMENDMENT.json"
    if (not package_amendment.is_file() or
            (out / "player_binding_amendment.json").read_bytes() != package_amendment.read_bytes()):
        errors.append("PLAYER_BINDING_AMENDMENT_BYTES")
    if sha(out / "targeted_editmode_results.xml") != TARGETED_EDITMODE_SHA:
        errors.append("TARGETED_EDITMODE_EVIDENCE_SHA")
    if sha(out / "targeted_playmode_results.xml") != PHYSICAL_38_SHA:
        errors.append("PHYSICAL_38_EVIDENCE_SHA")
    bound: dict[str, dict] = {}
    for entry in bindings.get("files", []):
        path = entry.get("path", "")
        if path in bound:
            errors.append("DUPLICATE_BINDING:" + path)
        bound[path] = entry
    if set(bound) != PLAYER_PATHS:
        errors.append("PLAYER_BINDING_SET:" + repr(sorted(set(bound) ^ PLAYER_PATHS)))
    for rel in sorted(PLAYER_PATHS & set(bound)):
        entry = bound[rel]
        try:
            oid, raw = git_blob(root, BASE, rel)
            work = (root / rel).read_bytes()
        except Exception as exc:
            errors.append("PLAYER_BINDING_READ:" + rel + ":" + str(exc))
            continue
        if (entry.get("git_blob_oid") != oid or entry.get("sha256") != sha_bytes(raw)
                or entry.get("bytes") != len(raw) or entry.get("role") != "READ_ONLY"):
            errors.append("PLAYER_BASE_BINDING_BYTES:" + rel)
        if rel == DRIVER_PATH:
            if (oid != DRIVER_BASE_OID or sha_bytes(raw) != DRIVER_BASE_SHA or
                    len(raw) != DRIVER_BASE_BYTES or sha_bytes(work) != DRIVER_APPROVED_SHA or
                    len(work) != DRIVER_APPROVED_BYTES):
                errors.append("PLAYER_AMENDED_BINDING_BYTES:" + rel)
        elif work != raw:
            errors.append("PLAYER_BINDING_BYTES:" + rel)
    observed = measurements.get("observed_player", {})
    for key in ("fixed_delta_time", "capsule_width", "capsule_height", "jump_velocity",
                "run_speed", "walk_speed", "grab_probe", "grab_vertical_window"):
        value = observed.get(key)
        if not isinstance(value, (int, float)) or not math.isfinite(value) or value <= 0:
            errors.append("MEASUREMENT:" + key)
    if measurements.get("source") != "ACTUAL_BASE_COMMIT_PLAYER_COMPONENTS":
        errors.append("MEASUREMENT_SOURCE")
    if measurements.get("retuned") is not False:
        errors.append("PLAYER_RETUNED")

    case_by_id: dict[str, dict[str, str]] = {}
    by_kind: dict[str, list[dict[str, str]]] = defaultdict(list)
    for row in cases:
        cid = row.get("case_id", "")
        if not cid or cid in case_by_id:
            errors.append("CASE_ID:" + cid)
            continue
        case_by_id[cid] = row
        by_kind[row.get("case_kind", "")].append(row)
        if row.get("recipe_id") not in RECIPES or row.get("outcome") != "PASS":
            errors.append("CASE_OUTCOME:" + cid)
        for key in ("actual_player", "actual_driver", "actual_rigidbody2d", "actual_collider2d"):
            if not yes(row.get(key, "")):
                errors.append("CASE_ACTUAL:" + cid + ":" + key)
        if yes(row.get("used_item", "")) or yes(row.get("reverse_required", "")):
            errors.append("CASE_FORBIDDEN_CLAIM:" + cid)
        try:
            if iv(row, "teleports_after_start") != 0 or iv(row, "fixed_steps") <= 0:
                errors.append("CASE_EXECUTION:" + cid)
        except Exception:
            errors.append("CASE_NUMERIC:" + cid)

    if {k: len(v) for k, v in by_kind.items()} != {
            "MAIN_LINK": 18, "FULL_ROUTE": 2, "RECOVERY": 18}:
        errors.append("CASE_KIND_COUNTS:" + repr({k: len(v) for k, v in by_kind.items()}))
    expected_links = {(r, o) for r in RECIPES for o in range(9)}
    main_keys = {(r.get("recipe_id", ""), iv(r, "main_link_order"))
                 for r in by_kind.get("MAIN_LINK", [])}
    recovery_keys = {(r.get("recipe_id", ""), iv(r, "main_link_order"))
                     for r in by_kind.get("RECOVERY", [])}
    if main_keys != expected_links:
        errors.append("MAIN_CASE_SET")
    if recovery_keys != expected_links:
        errors.append("RECOVERY_CASE_SET")
    if {r.get("recipe_id") for r in by_kind.get("FULL_ROUTE", [])} != set(RECIPES):
        errors.append("FULL_ROUTE_SET")

    link_by_key = {(r["recipe_id"], iv(r, "order")): r for r in main_links}
    route_by_key = {(r["recipe_id"], iv(r, "failed_link_order")): r for r in routes}
    probe_by_key = {(r["recipe_id"], iv(r, "failed_link_order")): r for r in probes}
    for row in by_kind.get("MAIN_LINK", []):
        key = (row["recipe_id"], iv(row, "main_link_order"))
        source = link_by_key.get(key)
        if not source or row["source_id"] != source["source_link_id"]:
            errors.append("MAIN_BINDING:" + repr(key))
        if (source and source["mode"] == "JUMP_GRAB") != yes(row["used_grab"]):
            errors.append("MAIN_GRAB_OBSERVATION:" + repr(key))
    for row in by_kind.get("RECOVERY", []):
        key = (row["recipe_id"], iv(row, "main_link_order"))
        route, probe = route_by_key.get(key), probe_by_key.get(key)
        if not route or not probe:
            errors.append("RECOVERY_BINDING:" + repr(key))
            continue
        if iv(row, "checkpoint_link_order") != iv(route, "checkpoint_link_order"):
            errors.append("RECOVERY_CHECKPOINT:" + repr(key))
        if iv(row, "checkpoint_link_order") > key[1]:
            errors.append("LATER_CHECKPOINT:" + repr(key))

    trace_by_case: dict[str, list[dict[str, str]]] = defaultdict(list)
    for row in traces:
        cid = row.get("case_id", "")
        if cid not in case_by_id:
            errors.append("ORPHAN_TRACE:" + cid)
            continue
        trace_by_case[cid].append(row)
        for key in ("body_x", "body_y", "velocity_x", "velocity_y"):
            try:
                if not math.isfinite(fv(row, key)):
                    errors.append("TRACE_FINITE:" + cid + ":" + key)
            except Exception:
                errors.append("TRACE_NUMBER:" + cid + ":" + key)
    for cid, case in case_by_id.items():
        trace = sorted(trace_by_case.get(cid, []), key=lambda r: iv(r, "step"))
        if not trace or [iv(r, "step") for r in trace] != list(range(len(trace))):
            errors.append("TRACE_ORDER:" + cid)
            continue
        if len(trace) != iv(case, "fixed_steps"):
            errors.append("TRACE_COUNT:" + cid)
        events = {r.get("event", "") for r in trace}
        if "START" not in events or "PASS_TARGET" not in events:
            errors.append("TRACE_BOUNDARY_EVENTS:" + cid)
        if case["case_kind"] == "RECOVERY" and not {"RECOVERY_CATCH", "REJOIN"}.issubset(events):
            errors.append("TRACE_RECOVERY_EVENTS:" + cid)
        if case["case_kind"] == "MAIN_LINK" and yes(case["used_grab"]):
            if not {"GRAB_ENTER", "GRAB_EXIT"}.issubset(events):
                errors.append("TRACE_GRAB_EVENTS:" + cid)
            if not any(yes(r["is_grabbing"]) for r in trace):
                errors.append("TRACE_GRAB_STATE:" + cid)

    if validation.get("schema") != "SV5_20_JUMP_PLAYER_VALIDATION/v1":
        errors.append("VALIDATION_SCHEMA")
    if validation.get("status") != "PASS" or validation.get("input_recovery_digest") != INPUT_DIGEST:
        errors.append("VALIDATION_IDENTITY")
    if validation.get("errors") not in ([], None):
        errors.append("VALIDATION_ERRORS")
    for key, value in (("main_links_passed", 18), ("full_routes_passed", 2),
                       ("recovery_cases_passed", 18)):
        if validation.get(key) != value:
            errors.append("VALIDATION_COUNT:" + key)

    owned_sources = [
        root / "Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/Sv5JumpPlayerVerification.cs",
        root / "Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/Sv5JumpPlayerExport.cs",
        root / "Assets/_Game/Tests/EditMode/Map/SV5/Sv5JumpPlayerProofTests.cs",
        root / "Assets/_Game/Tests/PlayMode/Map/SV5/Sv5JumpPlayerPlayModeTests.cs",
    ]
    for path in owned_sources:
        if not path.is_file():
            errors.append("MISSING_OWNED_SOURCE:" + str(path.relative_to(root)).replace("\\", "/"))
            continue
        text = path.read_text(encoding="utf-8-sig")
        for label, pattern in (
            ("SECTOR_ID", r"\bSectorId\b"),
            ("WORLD_WIDTH", r"\b624\b"),
            ("WORLD_HEIGHT", r"\b416\b"),
            ("LEGACY_GRID", r"48\s*[xX×*]\s*32"),
        ):
            if re.search(pattern, text):
                errors.append(label + ":" + str(path.relative_to(root)).replace("\\", "/"))

    svg = (out / "preview/jump_player.svg").read_text(encoding="utf-8-sig")
    for token in (TASK, INPUT_DIGEST[:12], "JUMP012_MIXED_R0", "JUMP012_MIXED_MX",
                  "PlayerVerified=true"):
        if token not in svg:
            errors.append("SVG_TOKEN:" + token)

    if errors:
        print("FAIL_INDEPENDENT_JUMP_PLAYER")
        print("\n".join(errors))
        return 1
    audit = {
        "schema": "SV5_20_INDEPENDENT_JUMP_PLAYER_AUDIT/v2",
        "task": TASK,
        "status": "PASS_INDEPENDENT_JUMP_PLAYER",
        "main_link_cases": 18,
        "full_route_cases": 2,
        "recovery_cases": 18,
        "trace_states": len(traces),
        "player_binding_files": len(PLAYER_PATHS),
        "player_binding_amendment": AMENDMENT,
        "approved_driver_sha256": DRIVER_APPROVED_SHA,
        "binding_amendment_sha256": sha(out / "player_binding_amendment.json"),
        "whole_world_builds": 0,
        "whole_world_searches": 0,
        "global_endpoint_comparisons": 0,
        "sector_partitions": 0,
        "cases_sha256": sha(out / "player_cases.csv"),
        "trace_sha256": sha(out / "player_trace.csv"),
        "errors": [],
    }
    audit_path = out / "independent_jump_player_audit.json"
    audit_path.write_text(json.dumps(audit, ensure_ascii=False, indent=2) + "\n",
                          encoding="utf-8", newline="\n")
    print("PASS_INDEPENDENT_JUMP_PLAYER")
    print(json.dumps(audit, ensure_ascii=False, sort_keys=True))
    return 0


if __name__ == "__main__":
    sys.exit(main())
