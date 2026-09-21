from __future__ import annotations

import argparse
import csv
import hashlib
import json
import re
import sys
from collections import Counter
from pathlib import Path

TASK = "SV5_17_JUMP_RECIPES"
GEN = Path("MapDesign/MCP/GENERATED") / TASK
BASE16 = Path("MapDesign/MCP/GENERATED/SV5_16_JUMP_OUTLINE")
BASE15 = Path("MapDesign/MCP/GENERATED/SV5_15_JUMP_GRAB")
PACKAGE = Path("MapDesign/MCP/INPUTS") / TASK
R0 = "JUMP012_MIXED_R0"
MX = "JUMP012_MIXED_MX"
RECIPES = {R0: "R0", MX: "MIRROR_X"}
SEGMENTS = [
    (0, "SEG_ASCENT_A", "THREE_PLUS_ONE_JUMPS", ["JS_LINK_00", "JS_LINK_01", "JS_LINK_02"]),
    (1, "SEG_GRAB_TURN", "PLUS_TWO_GRAB_THEN_LEVEL", ["JS_LINK_03", "JS_LINK_04"]),
    (2, "SEG_ASCENT_B", "TWO_PLUS_ONE_JUMPS", ["JS_LINK_05", "JS_LINK_06"]),
    (3, "SEG_ASCENT_C", "TWO_PLUS_ONE_JUMPS", ["JS_LINK_07", "JS_LINK_08"]),
]
BEFORE_SHA = "9cf9f0de9e144951ccc63c4264dfb4ddfa4f1f611ff293e8df15dc28eefad02c"
AFTER_SHA = "36c4f5a585b15505e8a91d59bd519a212cb02ae2faa8b9cca493e7722e43f7e1"
INTENTS = {
    "MIXED_SOLID_ONE_WAY", "PLUS_ONE_JUMP", "PLUS_TWO_JUMP_GRAB",
    "LEVEL_CONNECTION", "IRREGULAR_SOLID_BACKING",
}


def load_json(path: Path) -> dict:
    return json.loads(path.read_text(encoding="utf-8-sig"))


def load_csv(path: Path) -> list[dict[str, str]]:
    with path.open("r", encoding="utf-8-sig", newline="") as handle:
        return list(csv.DictReader(handle))


def integer(row: dict[str, str], key: str) -> int:
    return int(row[key])


def truth(value: str) -> bool:
    return value.strip().lower() == "true"


def cell_rows(rows: list[dict[str, str]], errors: list[str], label: str) -> list[str]:
    grid: dict[tuple[int, int], str] = {}
    for row in rows:
        try:
            key = (integer(row, "x"), integer(row, "y"))
            if key in grid:
                errors.append(f"{label}: duplicate cell {key}")
            grid[key] = row["cell"]
        except Exception as exc:
            errors.append(f"{label}: malformed row: {exc}")
    expected = {(x, y) for y in range(32) for x in range(24)}
    if set(grid) != expected:
        errors.append(f"{label}: grid coordinate set mismatch")
    return ["".join(grid.get((x, y), "?") for x in range(24)) for y in range(32)]


def mirror_x(value: int) -> int:
    return 23 - value


def flip_lr(value: str) -> str:
    return {"LEFT_TO_RIGHT": "RIGHT_TO_LEFT", "RIGHT_TO_LEFT": "LEFT_TO_RIGHT",
            "LEFT": "RIGHT", "RIGHT": "LEFT"}.get(value, value)


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--project-root", default=".")
    parser.add_argument("--output", default=str(GEN / "independent_jump_recipe_audit.json"))
    args = parser.parse_args()
    root = Path(args.project_root).resolve()
    errors: list[str] = []

    required = [
        GEN / "jump_recipes.json", GEN / "recipe_catalog.csv", GEN / "recipe_segments.csv",
        GEN / "recipe_supports.csv", GEN / "recipe_occupancy.csv", GEN / "recipe_links.csv",
        GEN / "recipe_grab_edges.csv", GEN / "reference_012_before.csv",
        GEN / "reference_012_draft_after.csv", GEN / "reference_012_changes.csv",
        GEN / "reference_012_correspondence.csv", GEN / "jump_recipe_validation.json",
        GEN / "preview/jump_recipes.svg", PACKAGE / "JUMP_RECIPE_PROFILE.json",
        PACKAGE / "REFERENCE_012_PROFILE.json", BASE15 / "supports.csv",
        BASE16 / "final_occupancy.csv", BASE16 / "route_links.csv", BASE16 / "grab_edges.csv",
    ]
    for rel in required:
        if not (root / rel).is_file():
            errors.append(f"missing required file: {rel.as_posix()}")
    if errors:
        print("FAIL_INDEPENDENT_JUMP_RECIPES\n" + "\n".join(errors))
        return 1

    source_requirements = {
        "Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/Sv5JumpRecipeCatalog.cs":
            ["Sv5JumpRecipeCatalog", "JUMP012_MIXED_R0", "JUMP012_MIXED_MX", "MIRROR_X"],
        "Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/Sv5JumpRecipeExport.cs":
            ["Sv5JumpRecipeExport", "recipe_catalog.csv", "reference_012_changes.csv"],
        "Assets/_Game/Tests/EditMode/Map/SV5/Sv5JumpRecipeTests.cs":
            ["Sv5JumpRecipe", "JUMP012_MIXED_R0", "JUMP012_MIXED_MX"],
    }
    combined = ""
    for rel, symbols in source_requirements.items():
        path = root / rel
        if not path.is_file():
            errors.append(f"missing source: {rel}")
            continue
        text = path.read_text(encoding="utf-8-sig")
        combined += "\n" + text
        for symbol in symbols:
            if symbol not in text:
                errors.append(f"missing source symbol {symbol}: {rel}")
    if re.search(r"\bSectorId\b|\bsector_id\b|\bsector_index\b|48x32", combined):
        errors.append("forbidden Sector-era identifier in SV5_17-owned source")
    test_path = root / "Assets/_Game/Tests/EditMode/Map/SV5/Sv5JumpRecipeTests.cs"
    test_count = len(re.findall(r"\[Test(?:Case)?(?:\([^\]]*\))?\]", test_path.read_text(encoding="utf-8-sig"))) if test_path.is_file() else 0
    if test_count < 30:
        errors.append(f"local test count {test_count} < 30")

    profile = load_json(root / PACKAGE / "JUMP_RECIPE_PROFILE.json")
    reference = load_json(root / PACKAGE / "REFERENCE_012_PROFILE.json")
    if profile.get("base_fixture_digest") != "0ac93bf0ddc1a203baa47517096516c2df98d67978d3c80131a13edcedd5df9c":
        errors.append("profile base fixture digest mismatch")
    if {v.get("recipe_id"): v.get("transform") for v in profile.get("variants", [])} != RECIPES:
        errors.append("profile recipe set mismatch")

    base_supports = load_csv(root / BASE15 / "supports.csv")
    base_occupancy = load_csv(root / BASE16 / "final_occupancy.csv")
    base_links = load_csv(root / BASE16 / "route_links.csv")
    base_grabs = load_csv(root / BASE16 / "grab_edges.csv")
    catalog = load_csv(root / GEN / "recipe_catalog.csv")
    supports = load_csv(root / GEN / "recipe_supports.csv")
    occupancy = load_csv(root / GEN / "recipe_occupancy.csv")
    links = load_csv(root / GEN / "recipe_links.csv")
    grabs = load_csv(root / GEN / "recipe_grab_edges.csv")
    segments = load_csv(root / GEN / "recipe_segments.csv")

    if len(catalog) != 2 or {r.get("recipe_id"): r.get("transform") for r in catalog} != RECIPES:
        errors.append("recipe catalog must contain exact R0 and MIRROR_X rows")
    expected_catalog = {
        "supports": 10, "base_support_cells": 27, "outline_cells": 17,
        "final_occupancy_cells": 44, "route_links": 9,
        "plus_one_jump_links": 7, "plus_two_jump_grab_links": 1,
        "level_jump_links": 1, "grab_edges": 1,
    }
    for row in catalog:
        for key, expected in expected_catalog.items():
            try:
                if integer(row, key) != expected:
                    errors.append(f"{row.get('recipe_id')}: {key} != {expected}")
            except Exception:
                errors.append(f"{row.get('recipe_id')}: malformed {key}")
        if not truth(row.get("static_recipe_only", "")):
            errors.append(f"{row.get('recipe_id')}: static_recipe_only must be true")
        if truth(row.get("production_equivalence_to_reference_012", "")) or truth(row.get("player_verified", "")):
            errors.append(f"{row.get('recipe_id')}: false production/Player claim")

    base_support_by_id = {r["support_id"]: r for r in base_supports}
    for recipe_id, transform in RECIPES.items():
        rows = [r for r in supports if r.get("recipe_id") == recipe_id]
        if len(rows) != 10 or len({r.get("source_support_id") for r in rows}) != 10:
            errors.append(f"{recipe_id}: support count/source identity mismatch")
        for row in rows:
            src = base_support_by_id.get(row.get("source_support_id", ""))
            if src is None:
                errors.append(f"{recipe_id}: unknown source support")
                continue
            for key in ("kind", "height"):
                if row.get(key) != src.get(key):
                    errors.append(f"{recipe_id}: support {key} mutation {src['support_id']}")
            if transform == "R0":
                fields = ("x", "y", "width", "top_y", "top_x_min", "top_x_max")
                if any(row.get(k) != src.get(k) for k in fields):
                    errors.append(f"{recipe_id}: R0 support mutation {src['support_id']}")
            else:
                expected = {
                    "x": mirror_x(integer(src, "x") + integer(src, "width") - 1),
                    "y": integer(src, "y"), "width": integer(src, "width"),
                    "top_y": integer(src, "top_y"),
                    "top_x_min": mirror_x(integer(src, "top_x_max")),
                    "top_x_max": mirror_x(integer(src, "top_x_min")),
                }
                if any(integer(row, k) != v for k, v in expected.items()):
                    errors.append(f"{recipe_id}: mirror support mismatch {src['support_id']}")

    base_occ = {(integer(r, "x"), integer(r, "y"), r["collision"], r["owner_id"], r["source"], r["support_kind"])
                for r in base_occupancy}
    for recipe_id, transform in RECIPES.items():
        rows = [r for r in occupancy if r.get("recipe_id") == recipe_id]
        actual = {(integer(r, "x"), integer(r, "y"), r["collision"], r["source_owner_id"], r["source"], r["support_kind"])
                  for r in rows}
        expected = base_occ if transform == "R0" else {
            (mirror_x(x), y, collision, owner, source, kind)
            for x, y, collision, owner, source, kind in base_occ
        }
        if len(rows) != 44 or len(actual) != 44 or actual != expected:
            errors.append(f"{recipe_id}: occupancy is not exact transformed SV5_16 set")

    base_link_by_id = {r["link_id"]: r for r in base_links}
    for recipe_id, transform in RECIPES.items():
        rows = sorted((r for r in links if r.get("recipe_id") == recipe_id), key=lambda r: integer(r, "order"))
        if len(rows) != 9 or [integer(r, "order") for r in rows] != list(range(9)):
            errors.append(f"{recipe_id}: link count/order mismatch")
            continue
        mix = Counter()
        for row in rows:
            src = base_link_by_id.get(row.get("source_link_id", ""))
            if src is None:
                errors.append(f"{recipe_id}: unknown source link")
                continue
            same = ["mode", "gap_air", "rise", "required_route"]
            if any(row.get(k) != src.get(k) for k in same):
                errors.append(f"{recipe_id}: link scalar mutation {src['link_id']}")
            expected_direction = src["direction"] if transform == "R0" else flip_lr(src["direction"])
            expected_coords = {
                "takeoff_x": integer(src, "takeoff_x") if transform == "R0" else mirror_x(integer(src, "takeoff_x")),
                "takeoff_y": integer(src, "takeoff_y"),
                "landing_x": integer(src, "landing_x") if transform == "R0" else mirror_x(integer(src, "landing_x")),
                "landing_y": integer(src, "landing_y"),
            }
            if row.get("direction") != expected_direction or any(integer(row, k) != v for k, v in expected_coords.items()):
                errors.append(f"{recipe_id}: transformed link mismatch {src['link_id']}")
            rise = integer(row, "rise")
            if rise > 2:
                errors.append(f"{recipe_id}: rise above 2")
            mix["plus1" if row["mode"] == "JUMP" and rise == 1 else
                "plus2grab" if row["mode"] == "JUMP_GRAB" and rise == 2 else
                "level" if row["mode"] == "JUMP" and rise == 0 else "other"] += 1
        if mix != Counter({"plus1": 7, "plus2grab": 1, "level": 1}):
            errors.append(f"{recipe_id}: movement mixture mismatch {dict(mix)}")

    base_grab = base_grabs[0] if len(base_grabs) == 1 else None
    for recipe_id, transform in RECIPES.items():
        rows = [r for r in grabs if r.get("recipe_id") == recipe_id]
        if base_grab is None or len(rows) != 1:
            errors.append(f"{recipe_id}: explicit Grab count mismatch")
            continue
        row = rows[0]
        if row.get("source_grab_edge_id") != base_grab["grab_edge_id"]:
            errors.append(f"{recipe_id}: Grab source identity mismatch")
        expected_face = base_grab["face"] if transform == "R0" else flip_lr(base_grab["face"])
        expected_direction = base_grab["approach_direction"] if transform == "R0" else flip_lr(base_grab["approach_direction"])
        expected_coords = {}
        for xk, yk in (("contact_x", "contact_y"), ("hang_body_x", "hang_body_y"), ("pull_up_x", "pull_up_y")):
            expected_coords[xk] = integer(base_grab, xk) if transform == "R0" else mirror_x(integer(base_grab, xk))
            expected_coords[yk] = integer(base_grab, yk)
        if row.get("face") != expected_face or row.get("approach_direction") != expected_direction:
            errors.append(f"{recipe_id}: Grab face/direction mismatch")
        if any(integer(row, k) != v for k, v in expected_coords.items()):
            errors.append(f"{recipe_id}: Grab coordinate mismatch")

    for recipe_id in RECIPES:
        rows = sorted((r for r in segments if r.get("recipe_id") == recipe_id), key=lambda r: integer(r, "segment_order"))
        if len(rows) != 4:
            errors.append(f"{recipe_id}: segment count mismatch")
            continue
        flattened: list[str] = []
        for row, (order, seg_id, purpose, source_links) in zip(rows, SEGMENTS):
            actual_links = [v for v in row.get("source_link_ids", "").split(";") if v]
            if integer(row, "segment_order") != order or row.get("segment_id") != seg_id or row.get("purpose") != purpose or actual_links != source_links:
                errors.append(f"{recipe_id}: segment mismatch {seg_id}")
            flattened.extend(actual_links)
        if flattened != [f"JS_LINK_{i:02d}" for i in range(9)]:
            errors.append(f"{recipe_id}: segments do not partition ordered route")

    before_rows = cell_rows(load_csv(root / GEN / "reference_012_before.csv"), errors, "reference before")
    after_rows = cell_rows(load_csv(root / GEN / "reference_012_draft_after.csv"), errors, "reference draft")
    before_bytes = ("\n".join(before_rows) + "\n").encode()
    after_bytes = ("\n".join(after_rows) + "\n").encode()
    if hashlib.sha256(before_bytes).hexdigest() != BEFORE_SHA:
        errors.append("reference before digest mismatch")
    if hashlib.sha256(after_bytes).hexdigest() != AFTER_SHA:
        errors.append("reference draft-after digest mismatch")
    before_counts = Counter("".join(before_rows)); after_counts = Counter("".join(after_rows))
    if before_counts != Counter({"A": 546, "S": 176, "O": 46}):
        errors.append(f"reference before counts mismatch {dict(before_counts)}")
    if after_counts != Counter({"A": 491, "S": 249, "O": 28}):
        errors.append(f"reference draft counts mismatch {dict(after_counts)}")
    expected_changes = {(x, y, before_rows[y][x], after_rows[y][x]) for y in range(32) for x in range(24)
                        if before_rows[y][x] != after_rows[y][x]}
    change_rows = load_csv(root / GEN / "reference_012_changes.csv")
    actual_changes = {(integer(r, "x"), integer(r, "y"), r["before"], r["draft_after"]) for r in change_rows}
    if len(change_rows) != 141 or len(actual_changes) != 141 or actual_changes != expected_changes:
        errors.append("reference #012 change set is not exact 141-cell diff")
    if reference.get("production_equivalence_claimed") is not False or reference.get("player_verified") is not False:
        errors.append("reference profile makes false production/Player claim")

    corr = load_csv(root / GEN / "reference_012_correspondence.csv")
    if len(corr) != 5 or {r.get("intent_id") for r in corr} != INTENTS:
        errors.append("reference correspondence intent set mismatch")
    for row in corr:
        if row.get("relationship") != "DESIGN_LINEAGE_ONLY" or truth(row.get("production_equivalence", "")) or truth(row.get("player_verified", "")):
            errors.append(f"false correspondence claim: {row.get('intent_id')}")

    summary = load_json(root / GEN / "jump_recipes.json")
    validation = load_json(root / GEN / "jump_recipe_validation.json")
    readiness = summary.get("readiness", {})
    if summary.get("recipe_count") != 2 or summary.get("catalog_route_links") != 18 or summary.get("reference_012_changed_cells") != 141:
        errors.append("jump_recipes summary totals mismatch")
    expected_readiness = {
        "jump_recipe_ready": True, "composed_geometry_ready": True,
        "swept_clearance_ready": False, "recovery_ready": False, "player_verified": False,
    }
    for key, value in expected_readiness.items():
        if readiness.get(key) is not value:
            errors.append(f"readiness mismatch: {key}")
    performance = summary.get("performance", {})
    if performance.get("whole_world_builds_in_new_targeted_tests") != 0 or performance.get("whole_world_searches_in_new_targeted_tests") != 0:
        errors.append("whole-world work reported in local recipe tests")
    if validation.get("status") != "PASS" or validation.get("errors") not in ([], None):
        errors.append("jump_recipe_validation is not PASS")

    svg = (root / GEN / "preview/jump_recipes.svg").read_text(encoding="utf-8-sig")
    for label in ["JUMP012_MIXED_R0", "JUMP012_MIXED_MX", "+1 x7", "+2 Grab x1",
                  "level x1", "REFERENCE ONLY", "141 cells", "NOT PRODUCTION"]:
        if label not in svg:
            errors.append(f"SVG missing label: {label}")

    audit = {
        "schema": "SV5_17_JUMP_RECIPE_AUDIT/v1",
        "status": "PASS_INDEPENDENT_JUMP_RECIPES" if not errors else "FAIL_INDEPENDENT_JUMP_RECIPES",
        "recipes": 2,
        "segments_per_recipe": 4,
        "supports_per_recipe": 10,
        "occupancy_per_recipe": 44,
        "route_links_per_recipe": 9,
        "movement_mixture_per_recipe": {"plus_one_jump": 7, "plus_two_jump_grab": 1, "level_jump": 1},
        "explicit_grab_edges_per_recipe": 1,
        "reference_before_sha256": hashlib.sha256(before_bytes).hexdigest(),
        "reference_draft_after_sha256": hashlib.sha256(after_bytes).hexdigest(),
        "reference_changed_cells": len(actual_changes),
        "production_equivalence_claimed": False,
        "player_verified": False,
        "local_test_attributes": test_count,
        "whole_world_builds_in_new_targeted_tests": 0,
        "whole_world_searches_in_new_targeted_tests": 0,
        "errors": errors,
    }
    output = Path(args.output)
    if not output.is_absolute():
        output = root / output
    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_text(json.dumps(audit, ensure_ascii=False, indent=2) + "\n", encoding="utf-8", newline="\n")
    if errors:
        print("FAIL_INDEPENDENT_JUMP_RECIPES\n" + "\n".join(errors))
        return 1
    print("PASS_INDEPENDENT_JUMP_RECIPES")
    return 0


if __name__ == "__main__":
    sys.exit(main())
