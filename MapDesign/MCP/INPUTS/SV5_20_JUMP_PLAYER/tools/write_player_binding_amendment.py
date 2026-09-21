from __future__ import annotations

import argparse
import hashlib
import json
import subprocess
import sys
from pathlib import Path


BASE = "660d0c58ec0f65cbbbe200716d0e3c36b0ea4ba7"
DRIVER = "Assets/_Game/Live/Runtime/Movement/CharacterLiveMovementDriver.cs"
BASE_OID = "8eda20b5a2b1e854cf13c062adea411ca8798411"
BASE_SHA = "df58bb6edca4179cefb3d149baf032f03724f2aac4f5afafb8cc10a66b40a836"
BASE_BYTES = 33883
APPROVED_SHA = "77ac90e6c65f4dc83a104deb1db6c02177b1c50c2797426c6e3b07aeedc0d8f8"
APPROVED_BYTES = 39703


def digest(data: bytes) -> str:
    return hashlib.sha256(data).hexdigest()


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--project-root", default=".")
    args = parser.parse_args()
    root = Path(args.project_root).resolve()
    package = root / "MapDesign/MCP/INPUTS/SV5_20_JUMP_PLAYER"
    source = package / "FIX04_PLAYER_BINDING_AMENDMENT.json"
    output = root / "MapDesign/MCP/GENERATED/SV5_20_JUMP_PLAYER/player_binding_amendment.json"
    errors: list[str] = []

    if not source.is_file():
        errors.append("AMENDMENT_INPUT_MISSING")
    head = subprocess.run(["git", "rev-parse", "HEAD"], cwd=root,
                          text=True, stdout=subprocess.PIPE,
                          stderr=subprocess.PIPE).stdout.strip()
    if head != BASE:
        errors.append(f"HEAD_MISMATCH:{head}")

    oid_proc = subprocess.run(["git", "rev-parse", f"{BASE}:{DRIVER}"], cwd=root,
                              text=True, stdout=subprocess.PIPE,
                              stderr=subprocess.PIPE)
    oid = oid_proc.stdout.strip()
    if oid != BASE_OID:
        errors.append(f"BASE_OID_MISMATCH:{oid}")
    blob_proc = subprocess.run(["git", "cat-file", "blob", oid], cwd=root,
                               stdout=subprocess.PIPE, stderr=subprocess.PIPE)
    base_raw = blob_proc.stdout
    if digest(base_raw) != BASE_SHA or len(base_raw) != BASE_BYTES:
        errors.append("BASE_DRIVER_BYTES")

    driver = root / DRIVER
    if not driver.is_file():
        errors.append("APPROVED_DRIVER_MISSING")
    else:
        current = driver.read_bytes()
        if digest(current) != APPROVED_SHA or len(current) != APPROVED_BYTES:
            errors.append("APPROVED_DRIVER_BYTES")

    if errors:
        print(json.dumps({"status": "BLOCKED", "errors": errors}, indent=2))
        return 1

    payload = source.read_bytes()
    output.parent.mkdir(parents=True, exist_ok=True)
    temp = output.with_name(output.name + ".FIX04.tmp")
    if temp.exists():
        print(json.dumps({"status": "BLOCKED", "errors": ["TEMP_COLLISION"]}, indent=2))
        return 1
    try:
        temp.write_bytes(payload)
        temp.replace(output)
    finally:
        if temp.exists():
            temp.unlink()

    print(json.dumps({
        "status": "PASS_PLAYER_BINDING_AMENDMENT_WRITTEN",
        "path": str(output.relative_to(root)).replace("\\", "/"),
        "sha256": digest(payload),
        "approved_driver_sha256": APPROVED_SHA,
    }, indent=2))
    return 0


if __name__ == "__main__":
    sys.exit(main())
