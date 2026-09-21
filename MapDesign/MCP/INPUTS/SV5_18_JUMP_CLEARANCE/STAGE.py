from __future__ import annotations

import argparse
import hashlib
import json
import os
import re
import subprocess
import sys
from pathlib import Path

TASK = "SV5_18_JUMP_CLEARANCE"
BASE = "7c8281c1527f55c2d9a1f7d4c5c03f424cf8663b"
STATUS_REL = "MapDesign/MCP/06_IMPLEMENTATION_STATUS.md"
MASTER_REL = "MapDesign/MCP/MASTER_IMPLEMENTATION_TASK_LIST.md"
INBOX_REL = "MapDesign/MCP_INBOX/SV5_18_JUMP_CLEARANCE.md"
TASK_REL = "MapDesign/MCP/TASKS/SV5_18_JUMP_CLEARANCE.md"
ARCHIVE_REL = "MapDesign/MCP_ARCHIVE/SV5_18_JUMP_CLEARANCE.md"
RESULT_REL = "MapDesign/MCP/REPORTS/SV5_18_JUMP_CLEARANCE_RESULT.md"
GENERATED_REL = "MapDesign/MCP/GENERATED/SV5_18_JUMP_CLEARANCE"


def digest(data: bytes) -> str:
    return hashlib.sha256(data).hexdigest()


def sha(path: Path) -> str:
    return digest(path.read_bytes())


def command(root: Path, *args: str, check: bool = True) -> subprocess.CompletedProcess[str]:
    return subprocess.run(list(args), cwd=root, text=True, encoding="utf-8",
                          errors="replace", stdout=subprocess.PIPE,
                          stderr=subprocess.PIPE, check=check)


def git(root: Path, *args: str) -> str:
    return command(root, "git", *args).stdout.strip()


def roots() -> tuple[Path, Path]:
    package = Path(__file__).resolve().parent
    return package, package.parents[3]


def add(errors: list[str], condition: bool, message: str) -> None:
    if not condition:
        errors.append(message)


def verify_package(package: Path, expected_sha: str, errors: list[str]) -> dict:
    path = package / "FILES.json"
    add(errors, path.is_file(), "FILES.json missing")
    if not path.is_file():
        return {}
    add(errors, bool(re.fullmatch(r"[0-9a-f]{64}", expected_sha)),
        "--manifest-sha must be 64 lowercase hex")
    add(errors, sha(path) == expected_sha, "FILES.json SHA mismatch")
    try:
        manifest = json.loads(path.read_text(encoding="utf-8"))
    except Exception as exc:
        errors.append(f"invalid FILES.json: {exc}")
        return {}
    add(errors, manifest.get("schema") == "SV5_18_JUMP_CLEARANCE_FILES/v1",
        "manifest schema mismatch")
    add(errors, manifest.get("base_commit") == BASE, "manifest base mismatch")
    seen: set[str] = set()
    for entry in manifest.get("files", []):
        rel = entry.get("path", "")
        add(errors, rel not in seen, f"duplicate package entry: {rel}")
        seen.add(rel)
        candidate = (package / rel).resolve()
        try:
            candidate.relative_to(package)
        except ValueError:
            errors.append(f"unsafe package path: {rel}")
            continue
        add(errors, candidate.is_file(), f"package file missing: {rel}")
        if candidate.is_file():
            add(errors, sha(candidate) == entry.get("sha256") and
                candidate.stat().st_size == entry.get("bytes"),
                f"package byte mismatch: {rel}")
    required = {
        "README.md", "CONTRACT.md", "CLEARANCE_PROFILE.json", "SOURCE_LOCK.json",
        "SV5_18_JUMP_CLEARANCE.md", "STAGE.py", "tools/check_jump_clearance.py",
    }
    for rel in sorted(required - seen):
        errors.append("required package entry not manifested: " + rel)
    return manifest


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
                                 stdout=subprocess.PIPE, stderr=subprocess.PIPE,
                                 check=True).stdout
        except Exception as exc:
            errors.append(f"locked Git blob unavailable: {rel}: {exc}")
            continue
        add(errors, oid == entry.get("git_blob_oid"), f"Git blob OID mismatch: {rel}")
        add(errors, digest(raw) == entry.get("sha256"), f"Git blob SHA mismatch: {rel}")
        add(errors, len(raw) == entry.get("bytes"), f"Git blob size mismatch: {rel}")


def state_counts(text: str) -> dict[str, int]:
    answer = {"COMPLETE": 0, "CURRENT": 0, "LOCKED": 0}
    for value in re.findall(r"^\| [^|]+ \| (COMPLETE|CURRENT|LOCKED) \|$", text, re.M):
        answer[value] += 1
    return answer


def current_task(text: str) -> str:
    match = re.search(r"## Current Task\s*```text\s*([^\r\n]+)", text)
    return match.group(1).strip() if match else "<MISSING>"


def inbox_candidates(project: Path) -> list[str]:
    folder = project / "MapDesign/MCP_INBOX"
    if not folder.is_dir():
        return []
    answer: list[str] = []
    for path in folder.iterdir():
        if path.is_file() and path.suffix.lower() == ".md":
            answer.append(path.name)
        elif path.is_dir() and not (path / ".APPLIED").exists():
            answer.append(path.name + "/")
    return sorted(answer)


def verify_state(project: Path, package: Path, phase: str,
                 errors: list[str]) -> dict:
    status_path = project / STATUS_REL
    master_path = project / MASTER_REL
    add(errors, status_path.is_file(), "Status missing")
    add(errors, master_path.is_file(), "Master missing")
    if not status_path.is_file() or not master_path.is_file():
        return {}
    status = status_path.read_text(encoding="utf-8-sig")
    master = master_path.read_text(encoding="utf-8-sig")
    applied = phase in {"applied", "post"}
    expected = {"COMPLETE": 265, "CURRENT": 1 if applied else 0,
                "LOCKED": 29 if applied else 30}
    add(errors, state_counts(status) == expected,
        f"Status counts mismatch: {state_counts(status)}")
    add(errors, current_task(status) == (TASK if applied else "NONE"),
        f"Current Task mismatch: {current_task(status)}")
    add(errors, status.count("| SV5_17_JUMP_RECIPES | COMPLETE |") == 1,
        "SV5_17 predecessor row mismatch")
    add(errors, status.count(f"| {TASK} | {'CURRENT' if applied else 'LOCKED'} |") == 1,
        "SV5_18 row mismatch")
    add(errors, status.count("| SV5_19_JUMP_RECOVERY | LOCKED |") == 1,
        "SV5_19 must remain LOCKED")
    add(errors, master.count("| 18 | SV5_18_JUMP_CLEARANCE | LOCKED |") == 1,
        "SV5_18 Master membership mismatch")
    task_bytes = (package / "SV5_18_JUMP_CLEARANCE.md").read_bytes()
    if phase == "before":
        add(errors, not inbox_candidates(project),
            "native inbox is not empty: " + ", ".join(inbox_candidates(project)))
        collisions = [rel for rel in (INBOX_REL, TASK_REL, ARCHIVE_REL,
                      RESULT_REL, GENERATED_REL) if (project / rel).exists()]
        add(errors, not collisions, "destination collision: " + ", ".join(collisions))
    elif phase == "staged":
        inbox = project / INBOX_REL
        add(errors, inbox.is_file() and inbox.read_bytes() == task_bytes,
            "staged inbox bytes mismatch")
        add(errors, inbox_candidates(project) == ["SV5_18_JUMP_CLEARANCE.md"],
            "inbox candidate set is not exactly one")
        add(errors, not (project / TASK_REL).exists() and not (project / ARCHIVE_REL).exists(),
            "stage helper fabricated installed Task or Archive")
    else:
        for rel in (TASK_REL, ARCHIVE_REL):
            installed = project / rel
            add(errors, installed.is_file() and installed.read_bytes() == task_bytes,
                f"installed/archive byte mismatch: {rel}")
        add(errors, not (project / INBOX_REL).exists(), "inbox source remains after Apply")
        add(errors, not inbox_candidates(project), "unexpected inbox candidate after Apply")
    return {"counts": state_counts(status), "current_task": current_task(status),
            "status_sha256": sha(status_path), "master_sha256": sha(master_path),
            "task_sha256": digest(task_bytes)}


def owned_paths(lock: dict) -> list[str]:
    answer = [entry["path"] for entry in lock.get("files", [])]
    answer += [
        INBOX_REL, TASK_REL, ARCHIVE_REL, RESULT_REL, GENERATED_REL,
        "Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/Sv5JumpClearance.cs",
        "Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/Sv5JumpClearance.cs.meta",
        "Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/Sv5JumpClearanceExport.cs",
        "Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/Sv5JumpClearanceExport.cs.meta",
        "Assets/_Game/Tests/EditMode/Map/SV5/Sv5JumpClearanceTests.cs",
        "Assets/_Game/Tests/EditMode/Map/SV5/Sv5JumpClearanceTests.cs.meta",
        "MapDesign/MCP/SV5/26_JUMP_CLEARANCE_V5.md",
    ]
    return answer


def verify_clean_owned(project: Path, lock: dict, errors: list[str]) -> None:
    proc = command(project, "git", "status", "--porcelain=v1", "--untracked-files=all",
                   "--", *owned_paths(lock), check=False)
    add(errors, proc.returncode == 0, "owned-path git status failed")
    if proc.stdout.strip():
        errors.append("task-owned worktree overlap:\n" + proc.stdout.rstrip())


def verify_immutable(project: Path, lock: dict, errors: list[str]) -> None:
    paths = [entry["path"] for entry in lock.get("files", [])
             if entry["role"].startswith("IMMUTABLE")]
    proc = command(project, "git", "status", "--porcelain=v1", "--untracked-files=all",
                   "--", *paths, check=False)
    add(errors, proc.returncode == 0 and not proc.stdout.strip(),
        "immutable predecessor path changed:\n" + proc.stdout.rstrip())


def atomic_write(path: Path, data: bytes) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    temp = path.with_name(path.name + ".SV5_18.tmp")
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
        verify_clean_owned(project, lock, errors)
    elif phase in {"applied", "post"}:
        verify_immutable(project, lock, errors)
    if errors:
        print(json.dumps({"status": "BLOCKED", "phase": phase, "errors": errors},
                         ensure_ascii=False, indent=2))
        return 1
    if args.stage:
        path = project / INBOX_REL
        task_bytes = (package / "SV5_18_JUMP_CLEARANCE.md").read_bytes()
        atomic_write(path, task_bytes)
        post: list[str] = []
        state = verify_state(project, package, "staged", post)
        if post:
            if path.exists() and path.read_bytes() == task_bytes:
                path.unlink()
            print(json.dumps({"status": "BLOCKED_STAGE_ROLLED_BACK", "errors": post},
                             ensure_ascii=False, indent=2))
            return 1
        label = "PASS_STAGED_SINGLE_TASK"
    elif args.check:
        label = "PASS_STAGE_PREFLIGHT_READONLY"
    elif args.verify_staged:
        label = "PASS_STAGED_SINGLE_TASK"
    elif args.verify_applied:
        label = "PASS_NATIVE_APPLY"
    else:
        label = "PASS_POST_READONLY"
    print(json.dumps({"status": label, "base_commit": BASE, "state": state,
                      "implementation_or_tests_executed_by_helper": False},
                     ensure_ascii=False, indent=2))
    return 0


if __name__ == "__main__":
    try:
        sys.exit(main())
    except Exception as exc:
        print(json.dumps({"status": "BLOCKED", "errors": [str(exc)]},
                         ensure_ascii=False, indent=2))
        sys.exit(1)

