from __future__ import annotations

import argparse
import hashlib
import json
import os
import re
import subprocess
import sys
from pathlib import Path

TASK = "SV5_13_JUMP_CONTRACT"
BASE = "5e09c37146c21e16bb165ebdd47745205e5a0381"
STATUS_REL = "MapDesign/MCP/06_IMPLEMENTATION_STATUS.md"
MASTER_REL = "MapDesign/MCP/MASTER_IMPLEMENTATION_TASK_LIST.md"
INBOX_REL = "MapDesign/MCP_INBOX/SV5_13_JUMP_CONTRACT.md"
TASK_REL = "MapDesign/MCP/TASKS/SV5_13_JUMP_CONTRACT.md"
ARCHIVE_REL = "MapDesign/MCP_ARCHIVE/SV5_13_JUMP_CONTRACT.md"
RESULT_REL = "MapDesign/MCP/REPORTS/SV5_13_JUMP_CONTRACT_RESULT.md"
GENERATED_REL = "MapDesign/MCP/GENERATED/SV5_13_JUMP_CONTRACT"


def digest(data: bytes) -> str:
    return hashlib.sha256(data).hexdigest()


def sha(path: Path) -> str:
    return digest(path.read_bytes())


def command(root: Path, *args: str, check: bool = True) -> subprocess.CompletedProcess[str]:
    return subprocess.run(list(args), cwd=root, text=True, encoding="utf-8", errors="replace",
                          stdout=subprocess.PIPE, stderr=subprocess.PIPE, check=check)


def git(root: Path, *args: str) -> str:
    return command(root, "git", *args).stdout.strip()


def roots() -> tuple[Path, Path]:
    package = Path(__file__).resolve().parent
    return package, package.parents[3]


def add(errors: list[str], condition: bool, message: str) -> None:
    if not condition:
        errors.append(message)


def verify_package(package: Path, expected_sha: str, errors: list[str]) -> None:
    manifest_path = package / "FILES.json"
    add(errors, manifest_path.is_file(), "FILES.json missing")
    if not manifest_path.is_file():
        return
    add(errors, bool(re.fullmatch(r"[0-9a-f]{64}", expected_sha)),
        "--manifest-sha must be 64 lowercase hex")
    add(errors, sha(manifest_path) == expected_sha, "FILES.json SHA mismatch")
    try:
        manifest = json.loads(manifest_path.read_text(encoding="utf-8"))
    except Exception as exc:
        errors.append(f"invalid FILES.json: {exc}")
        return
    add(errors, manifest.get("schema") == "SV5_13_JUMP_CONTRACT_FILES/v1",
        "manifest schema mismatch")
    seen: set[str] = set()
    for entry in manifest.get("files", []):
        rel = entry.get("path", "")
        add(errors, rel not in seen, f"duplicate package entry: {rel}")
        seen.add(rel)
        path = (package / rel).resolve()
        try:
            path.relative_to(package)
        except ValueError:
            errors.append(f"unsafe package path: {rel}")
            continue
        add(errors, path.is_file(), f"package file missing: {rel}")
        if path.is_file():
            add(errors, sha(path) == entry.get("sha256") and path.stat().st_size == entry.get("bytes"),
                f"package byte mismatch: {rel}")
    required = {"README.md", "CONTRACT.md", "JUMP_CONTRACT_PROFILE.json", "SOURCE_LOCK.json",
                "SV5_13_JUMP_CONTRACT.md", "STAGE.py", "tools/check_jump_contract.py"}
    for rel in sorted(required - seen):
        errors.append(f"required package entry not manifested: {rel}")


def load_lock(package: Path) -> dict:
    return json.loads((package / "SOURCE_LOCK.json").read_text(encoding="utf-8"))


def verify_git(project: Path, lock: dict, errors: list[str]) -> None:
    try:
        head = git(project, "rev-parse", "HEAD")
    except Exception as exc:
        errors.append(f"Git worktree unavailable: {exc}")
        return
    add(errors, head == BASE, f"HEAD mismatch: expected {BASE}, actual {head}")
    add(errors, lock.get("base_commit") == BASE, "SOURCE_LOCK base mismatch")
    for entry in lock.get("files", []):
        rel = entry["path"]
        try:
            oid = git(project, "rev-parse", f"{BASE}:{rel}")
            raw = subprocess.run(["git", "cat-file", "blob", oid], cwd=project,
                                 stdout=subprocess.PIPE, stderr=subprocess.PIPE, check=True).stdout
        except Exception as exc:
            errors.append(f"locked Git blob unavailable: {rel}: {exc}")
            continue
        add(errors, oid == entry.get("git_blob_oid"), f"Git blob OID mismatch: {rel}")
        add(errors, digest(raw) == entry.get("sha256"), f"Git blob SHA mismatch: {rel}")
        add(errors, len(raw) == entry.get("bytes"), f"Git blob size mismatch: {rel}")


def counts(text: str) -> dict[str, int]:
    return {state: len(re.findall(r"^\| [^|]+ \| " + state + r" \|$", text, re.M))
            for state in ("COMPLETE", "CURRENT", "LOCKED")}


def current(text: str) -> str:
    match = re.search(r"## Current Task\s*```text\s*([^\r\n]+)", text)
    return match.group(1).strip() if match else "<MISSING>"


def candidates(project: Path) -> list[str]:
    folder = project / "MapDesign/MCP_INBOX"
    if not folder.is_dir():
        return []
    found: list[str] = []
    for path in folder.iterdir():
        if path.is_file() and path.suffix.lower() == ".md":
            found.append(path.name)
        elif path.is_dir() and not (path / ".APPLIED").exists():
            found.append(path.name + "/")
    return sorted(found)


def verify_state(project: Path, package: Path, phase: str, errors: list[str]) -> dict:
    status_path = project / STATUS_REL
    master_path = project / MASTER_REL
    add(errors, status_path.is_file() and master_path.is_file(), "Status or Master missing")
    if not status_path.is_file() or not master_path.is_file():
        return {}
    status = status_path.read_text(encoding="utf-8-sig")
    master = master_path.read_text(encoding="utf-8-sig")
    applied = phase in {"applied", "post"}
    expected = {"COMPLETE": 260, "CURRENT": 1 if applied else 0,
                "LOCKED": 34 if applied else 35}
    add(errors, counts(status) == expected, f"Status counts mismatch: {counts(status)}")
    add(errors, current(status) == (TASK if applied else "NONE"),
        f"Current Task mismatch: {current(status)}")
    add(errors, status.count("| SV5_12_FIX01 | COMPLETE |") == 1,
        "SV5_12_FIX01 predecessor row mismatch")
    add(errors, status.count(f"| {TASK} | {'CURRENT' if applied else 'LOCKED'} |") == 1,
        "SV5_13 row mismatch")
    add(errors, status.count("| SV5_14_JUMP_SOLID | LOCKED |") == 1,
        "SV5_14 must remain LOCKED")
    add(errors, master.count("| 13 | SV5_13_JUMP_CONTRACT | LOCKED |") == 1,
        "SV5_13 Master membership mismatch")
    task_bytes = (package / "SV5_13_JUMP_CONTRACT.md").read_bytes()
    if phase == "before":
        add(errors, not candidates(project), "native inbox is not empty: " + ", ".join(candidates(project)))
        collisions = [rel for rel in [INBOX_REL, TASK_REL, ARCHIVE_REL, RESULT_REL, GENERATED_REL]
                      if (project / rel).exists()]
        add(errors, not collisions, "destination collision: " + ", ".join(collisions))
    elif phase == "staged":
        inbox = project / INBOX_REL
        add(errors, inbox.is_file() and inbox.read_bytes() == task_bytes, "staged inbox bytes mismatch")
        add(errors, candidates(project) == ["SV5_13_JUMP_CONTRACT.md"],
            "inbox candidate set is not exactly one")
        add(errors, not (project / TASK_REL).exists() and not (project / ARCHIVE_REL).exists(),
            "stage helper fabricated installed Task or Archive")
    else:
        for rel in [TASK_REL, ARCHIVE_REL]:
            path = project / rel
            add(errors, path.is_file() and path.read_bytes() == task_bytes,
                f"installed/archive byte mismatch: {rel}")
        add(errors, not (project / INBOX_REL).exists(), "inbox source remains after Apply")
        add(errors, not candidates(project), "unexpected inbox candidate after Apply")
    return {"counts": counts(status), "current_task": current(status),
            "status_sha256": sha(status_path), "master_sha256": sha(master_path),
            "task_sha256": digest(task_bytes)}


def owned_overlap(project: Path, lock: dict, errors: list[str]) -> None:
    paths = [entry["path"] for entry in lock.get("files", [])]
    paths += [INBOX_REL, TASK_REL, ARCHIVE_REL, RESULT_REL, GENERATED_REL,
              "Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/Sv5JumpContract.cs",
              "Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/Sv5JumpContract.cs.meta",
              "Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/Sv5JumpContractExport.cs",
              "Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/Sv5JumpContractExport.cs.meta",
              "Assets/_Game/Tests/EditMode/Map/SV5/Sv5JumpContractTests.cs",
              "Assets/_Game/Tests/EditMode/Map/SV5/Sv5JumpContractTests.cs.meta",
              "MapDesign/MCP/SV5/21_JUMP_CONTRACT_V5.md"]
    proc = command(project, "git", "status", "--porcelain=v1", "--untracked-files=all", "--", *paths,
                   check=False)
    add(errors, proc.returncode == 0, "owned-path git status failed")
    if proc.stdout.strip():
        errors.append("task-owned worktree overlap:\n" + proc.stdout.rstrip())


def verify_immutable(project: Path, lock: dict, errors: list[str]) -> None:
    paths = [entry["path"] for entry in lock.get("files", []) if entry["role"] == "IMMUTABLE"]
    proc = command(project, "git", "status", "--porcelain=v1", "--untracked-files=all", "--", *paths,
                   check=False)
    add(errors, proc.returncode == 0 and not proc.stdout.strip(),
        "immutable predecessor path changed:\n" + proc.stdout.rstrip())


def atomic_write(path: Path, data: bytes) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    temp = path.with_name(path.name + ".SV5_13.tmp")
    if temp.exists():
        raise RuntimeError(f"temporary collision: {temp}")
    try:
        temp.write_bytes(data)
        os.replace(temp, path)
    finally:
        if temp.exists():
            temp.unlink()


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--manifest-sha", required=True)
    modes = parser.add_mutually_exclusive_group(required=True)
    modes.add_argument("--check", action="store_true")
    modes.add_argument("--stage", action="store_true")
    modes.add_argument("--verify-staged", action="store_true")
    modes.add_argument("--verify-applied", action="store_true")
    modes.add_argument("--post-readonly", action="store_true")
    args = parser.parse_args()
    package, project = roots()
    errors: list[str] = []
    verify_package(package, args.manifest_sha, errors)
    lock = load_lock(package)
    verify_git(project, lock, errors)
    phase = "before" if args.check or args.stage else (
        "staged" if args.verify_staged else "post" if args.post_readonly else "applied")
    state = verify_state(project, package, phase, errors)
    if phase == "before":
        owned_overlap(project, lock, errors)
    elif phase in {"applied", "post"}:
        verify_immutable(project, lock, errors)
    if errors:
        print(json.dumps({"status": "BLOCKED", "phase": phase, "errors": errors},
                         ensure_ascii=False, indent=2))
        return 1
    if args.stage:
        path = project / INBOX_REL
        atomic_write(path, (package / "SV5_13_JUMP_CONTRACT.md").read_bytes())
        post_errors: list[str] = []
        verify_state(project, package, "staged", post_errors)
        if post_errors:
            if path.exists() and path.read_bytes() == (package / "SV5_13_JUMP_CONTRACT.md").read_bytes():
                path.unlink()
            print(json.dumps({"status": "BLOCKED_STAGE_ROLLED_BACK", "errors": post_errors},
                             ensure_ascii=False, indent=2))
            return 1
        state = verify_state(project, package, "staged", [])
        label = "PASS_STAGED_SINGLE_TASK"
    else:
        label = {"before": "PASS_STAGE_PREFLIGHT_READONLY",
                 "staged": "PASS_STAGED_SINGLE_TASK",
                 "applied": "PASS_NATIVE_APPLY",
                 "post": "PASS_POST_READONLY"}[phase]
    print(json.dumps({"status": label, "base_commit": BASE, "state": state,
                      "implementation_or_tests_executed_by_helper": False},
                     ensure_ascii=False, indent=2))
    return 0


if __name__ == "__main__":
    try:
        sys.exit(main())
    except Exception as exc:
        print(json.dumps({"status": "BLOCKED", "errors": [str(exc)]}, ensure_ascii=False, indent=2))
        sys.exit(1)
