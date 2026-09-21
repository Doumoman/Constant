from __future__ import annotations

import argparse
import hashlib
import json
import os
import re
import subprocess
import sys
from pathlib import Path

TASK = "SV5_19_JUMP_RECOVERY"
BASE = "47ffdcf930a6a0c2fbaf6a3edce369a138cfc339"
STATUS_REL = "MapDesign/MCP/06_IMPLEMENTATION_STATUS.md"
MASTER_REL = "MapDesign/MCP/MASTER_IMPLEMENTATION_TASK_LIST.md"
INBOX_REL = "MapDesign/MCP_INBOX/SV5_19_JUMP_RECOVERY.md"
TASK_REL = "MapDesign/MCP/TASKS/SV5_19_JUMP_RECOVERY.md"
ARCHIVE_REL = "MapDesign/MCP_ARCHIVE/SV5_19_JUMP_RECOVERY.md"
RESULT_REL = "MapDesign/MCP/REPORTS/SV5_19_JUMP_RECOVERY_RESULT.md"
GENERATED_REL = "MapDesign/MCP/GENERATED/SV5_19_JUMP_RECOVERY"


def digest(data: bytes) -> str:
    return hashlib.sha256(data).hexdigest()


def sha(path: Path) -> str:
    return digest(path.read_bytes())


def run(root: Path, *args: str, check: bool = True) -> subprocess.CompletedProcess[str]:
    return subprocess.run(list(args), cwd=root, text=True, encoding="utf-8",
                          errors="replace", stdout=subprocess.PIPE,
                          stderr=subprocess.PIPE, check=check)


def git(root: Path, *args: str) -> str:
    return run(root, "git", *args).stdout.strip()


def roots() -> tuple[Path, Path]:
    package = Path(__file__).resolve().parent
    return package, package.parents[3]


def add(errors: list[str], ok: bool, message: str) -> None:
    if not ok:
        errors.append(message)


def verify_package(package: Path, expected_sha: str, errors: list[str]) -> None:
    path = package / "FILES.json"
    add(errors, path.is_file(), "FILES.json missing")
    if not path.is_file():
        return
    add(errors, bool(re.fullmatch(r"[0-9a-f]{64}", expected_sha)),
        "--manifest-sha must be 64 lowercase hex")
    add(errors, sha(path) == expected_sha, "FILES.json SHA mismatch")
    try:
        manifest = json.loads(path.read_text(encoding="utf-8"))
    except Exception as exc:
        errors.append(f"invalid FILES.json: {exc}")
        return
    add(errors, manifest.get("schema") == "SV5_19_JUMP_RECOVERY_FILES/v1",
        "manifest schema mismatch")
    add(errors, manifest.get("base_commit") == BASE, "manifest base mismatch")
    seen: set[str] = set()
    for entry in manifest.get("files", []):
        rel = entry.get("path", "")
        add(errors, rel not in seen, "duplicate package entry: " + rel)
        seen.add(rel)
        candidate = (package / rel).resolve()
        try:
            candidate.relative_to(package)
        except ValueError:
            errors.append("unsafe package path: " + rel)
            continue
        add(errors, candidate.is_file(), "package file missing: " + rel)
        if candidate.is_file():
            add(errors, sha(candidate) == entry.get("sha256") and
                candidate.stat().st_size == entry.get("bytes"),
                "package byte mismatch: " + rel)
    required = {"README.md", "CONTRACT.md", "RECOVERY_PROFILE.json",
                "SOURCE_LOCK.json", "SV5_19_JUMP_RECOVERY.md", "STAGE.py",
                "tools/check_jump_recovery.py"}
    for rel in sorted(required - seen):
        errors.append("required package entry not manifested: " + rel)


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
        add(errors, oid == entry.get("git_blob_oid"), "Git blob OID mismatch: " + rel)
        add(errors, digest(raw) == entry.get("sha256"), "Git blob SHA mismatch: " + rel)
        add(errors, len(raw) == entry.get("bytes"), "Git blob size mismatch: " + rel)


def counts(text: str) -> dict[str, int]:
    answer = {"COMPLETE": 0, "CURRENT": 0, "LOCKED": 0}
    for state in re.findall(r"^\| [^|]+ \| (COMPLETE|CURRENT|LOCKED) \|$", text, re.M):
        answer[state] += 1
    return answer


def current(text: str) -> str:
    match = re.search(r"## Current Task\s*```text\s*([^\r\n]+)", text)
    return match.group(1).strip() if match else "<MISSING>"


def candidates(project: Path) -> list[str]:
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
    status_path, master_path = project / STATUS_REL, project / MASTER_REL
    add(errors, status_path.is_file(), "Status missing")
    add(errors, master_path.is_file(), "Master missing")
    if not status_path.is_file() or not master_path.is_file():
        return {}
    status = status_path.read_text(encoding="utf-8-sig")
    master = master_path.read_text(encoding="utf-8-sig")
    applied = phase in {"applied", "post"}
    expected = {"COMPLETE": 266, "CURRENT": 1 if applied else 0,
                "LOCKED": 28 if applied else 29}
    add(errors, counts(status) == expected, f"Status counts mismatch: {counts(status)}")
    add(errors, current(status) == (TASK if applied else "NONE"),
        f"Current Task mismatch: {current(status)}")
    add(errors, status.count("| SV5_18_JUMP_CLEARANCE | COMPLETE |") == 1,
        "SV5_18 predecessor row mismatch")
    add(errors, status.count(f"| {TASK} | {'CURRENT' if applied else 'LOCKED'} |") == 1,
        "SV5_19 row mismatch")
    add(errors, status.count("| SV5_20_JUMP_PLAYER | LOCKED |") == 1,
        "SV5_20 must remain LOCKED")
    add(errors, master.count("| 19 | SV5_19_JUMP_RECOVERY | LOCKED |") == 1,
        "SV5_19 Master membership mismatch")
    task_bytes = (package / "SV5_19_JUMP_RECOVERY.md").read_bytes()
    if phase == "before":
        add(errors, not candidates(project),
            "native inbox is not empty: " + ", ".join(candidates(project)))
        collision = [rel for rel in (INBOX_REL, TASK_REL, ARCHIVE_REL,
                     RESULT_REL, GENERATED_REL) if (project / rel).exists()]
        add(errors, not collision, "destination collision: " + ", ".join(collision))
    elif phase == "staged":
        inbox = project / INBOX_REL
        add(errors, inbox.is_file() and inbox.read_bytes() == task_bytes,
            "staged inbox bytes mismatch")
        add(errors, candidates(project) == ["SV5_19_JUMP_RECOVERY.md"],
            "inbox candidate set is not exactly one")
        add(errors, not (project / TASK_REL).exists() and not (project / ARCHIVE_REL).exists(),
            "stage helper fabricated Task or Archive")
    else:
        for rel in (TASK_REL, ARCHIVE_REL):
            path = project / rel
            add(errors, path.is_file() and path.read_bytes() == task_bytes,
                "installed/archive byte mismatch: " + rel)
        add(errors, not (project / INBOX_REL).exists(), "inbox remains after Apply")
        add(errors, not candidates(project), "unexpected inbox candidate after Apply")
    return {"counts": counts(status), "current_task": current(status),
            "status_sha256": sha(status_path), "master_sha256": sha(master_path),
            "task_sha256": digest(task_bytes)}


def task_owned(lock: dict) -> list[str]:
    return [entry["path"] for entry in lock.get("files", [])] + [
        INBOX_REL, TASK_REL, ARCHIVE_REL, RESULT_REL, GENERATED_REL,
        "Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/Sv5JumpRecovery.cs",
        "Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/Sv5JumpRecovery.cs.meta",
        "Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/Sv5JumpRecoveryExport.cs",
        "Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/Sv5JumpRecoveryExport.cs.meta",
        "Assets/_Game/Tests/EditMode/Map/SV5/Sv5JumpRecoveryTests.cs",
        "Assets/_Game/Tests/EditMode/Map/SV5/Sv5JumpRecoveryTests.cs.meta",
        "MapDesign/MCP/SV5/27_JUMP_RECOVERY_V5.md",
    ]


def verify_clean_owned(project: Path, lock: dict, errors: list[str]) -> None:
    proc = run(project, "git", "status", "--porcelain=v1", "--untracked-files=all",
               "--", *task_owned(lock), check=False)
    add(errors, proc.returncode == 0, "owned-path git status failed")
    if proc.stdout.strip():
        errors.append("task-owned worktree overlap:\n" + proc.stdout.rstrip())


def verify_immutable(project: Path, lock: dict, errors: list[str]) -> None:
    paths = [entry["path"] for entry in lock.get("files", [])
             if entry["role"].startswith("IMMUTABLE")]
    proc = run(project, "git", "status", "--porcelain=v1", "--untracked-files=all",
               "--", *paths, check=False)
    add(errors, proc.returncode == 0 and not proc.stdout.strip(),
        "immutable predecessor path changed:\n" + proc.stdout.rstrip())


def atomic_write(path: Path, data: bytes) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    temp = path.with_name(path.name + ".SV5_19.tmp")
    if temp.exists():
        raise RuntimeError("temporary collision: " + str(temp))
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
    lock = json.loads((package / "SOURCE_LOCK.json").read_text(encoding="utf-8"))
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
        data = (package / "SV5_19_JUMP_RECOVERY.md").read_bytes()
        atomic_write(path, data)
        post: list[str] = []
        state = verify_state(project, package, "staged", post)
        if post:
            if path.exists() and path.read_bytes() == data:
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

