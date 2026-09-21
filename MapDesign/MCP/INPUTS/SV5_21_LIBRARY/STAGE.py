from __future__ import annotations

import argparse
import hashlib
import json
import os
import re
import subprocess
import sys
from pathlib import Path


TASK = "SV5_21_LIBRARY"
BASE = "b95289b6c2eb727dd107d01ce89279d1556a60f9"
STATUS_REL = "MapDesign/MCP/06_IMPLEMENTATION_STATUS.md"
MASTER_REL = "MapDesign/MCP/MASTER_IMPLEMENTATION_TASK_LIST.md"
INBOX_REL = "MapDesign/MCP_INBOX/SV5_21_LIBRARY.md"
TASK_REL = "MapDesign/MCP/TASKS/SV5_21_LIBRARY.md"
ARCHIVE_REL = "MapDesign/MCP_ARCHIVE/SV5_21_LIBRARY.md"
RESULT_REL = "MapDesign/MCP/REPORTS/SV5_21_LIBRARY_RESULT.md"
GENERATED_REL = "MapDesign/MCP/GENERATED/SV5_21_LIBRARY"


def digest(data: bytes) -> str:
    return hashlib.sha256(data).hexdigest()


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
    add(errors, digest(path.read_bytes()) == expected_sha, "FILES.json SHA mismatch")
    try:
        manifest = json.loads(path.read_text(encoding="utf-8"))
    except Exception as exc:
        errors.append("invalid FILES.json: " + str(exc))
        return
    add(errors, manifest.get("schema") == "SV5_21_LIBRARY_FILES/v1",
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
            raw = candidate.read_bytes()
            add(errors, digest(raw) == entry.get("sha256") and
                len(raw) == entry.get("bytes"), "package byte mismatch: " + rel)
    required = {
        "CONTRACT.md", "LIBRARY_PROFILE.json", "README.md",
        "SOURCE_LOCK.json", "STAGE.py", "SV5_21_LIBRARY.md",
        "tools/check_library.py"
    }
    for rel in sorted(required - seen):
        errors.append("required package entry not manifested: " + rel)


def cat_blob(project: Path, rev: str, rel: str) -> tuple[str, bytes]:
    oid = git(project, "rev-parse", f"{rev}:{rel}")
    raw = subprocess.run(["git", "cat-file", "blob", oid], cwd=project,
                         stdout=subprocess.PIPE, stderr=subprocess.PIPE,
                         check=True).stdout
    return oid, raw


def verify_git(project: Path, lock: dict, errors: list[str]) -> None:
    try:
        head = git(project, "rev-parse", "HEAD")
    except Exception as exc:
        errors.append("Git worktree unavailable: " + str(exc))
        return
    add(errors, head == BASE, f"HEAD mismatch: expected {BASE}, actual {head}")
    add(errors, lock.get("base_commit") == BASE, "SOURCE_LOCK base mismatch")
    for entry in lock.get("files", []):
        rel = entry["path"]
        try:
            oid, raw = cat_blob(project, BASE, rel)
        except Exception as exc:
            errors.append(f"locked Git blob unavailable: {rel}: {exc}")
            continue
        add(errors, oid == entry.get("git_blob_oid"), "Git blob OID mismatch: " + rel)
        add(errors, digest(raw) == entry.get("sha256"), "Git blob SHA mismatch: " + rel)
        add(errors, len(raw) == entry.get("bytes"), "Git blob size mismatch: " + rel)


def verify_immutable_worktree(project: Path, lock: dict, errors: list[str]) -> None:
    for entry in lock.get("files", []):
        if not entry.get("role", "").startswith("IMMUTABLE"):
            continue
        path = project / entry["path"]
        add(errors, path.is_file(), "immutable path missing: " + entry["path"])
        if path.is_file():
            raw = path.read_bytes()
            add(errors, digest(raw) == entry["sha256"] and len(raw) == entry["bytes"],
                "immutable bytes changed: " + entry["path"])


def counts(text: str) -> dict[str, int]:
    return {state: len(re.findall(
        rf"^\| [^|]+ \| {state} \|$", text, re.M
    )) for state in ("COMPLETE", "CURRENT", "LOCKED")}


def current(text: str) -> str:
    match = re.search(r"## Current Task\s*```text\s*([^\r\n]+)", text)
    return match.group(1).strip() if match else "<MISSING>"


def inbox_candidates(project: Path) -> list[str]:
    folder = project / "MapDesign/MCP_INBOX"
    if not folder.is_dir():
        return []
    answer = []
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
    expected = {"COMPLETE": 268, "CURRENT": 1 if applied else 0,
                "LOCKED": 26 if applied else 27}
    add(errors, counts(status) == expected, "Status counts mismatch: " + str(counts(status)))
    add(errors, current(status) == (TASK if applied else "NONE"),
        "Current Task mismatch: " + current(status))
    add(errors, status.count("| SV5_20_JUMP_PLAYER | COMPLETE |") == 1,
        "SV5_20 predecessor row mismatch")
    add(errors, status.count(f"| {TASK} | {'CURRENT' if applied else 'LOCKED'} |") == 1,
        "SV5_21 row mismatch")
    add(errors, status.count("| SV5_22_STAIR_DATA | LOCKED |") == 1,
        "SV5_22 must remain LOCKED")
    add(errors, master.count("| 21 | SV5_21_LIBRARY | LOCKED |") == 1,
        "SV5_21 Master membership mismatch")
    task_bytes = (package / "SV5_21_LIBRARY.md").read_bytes()
    if phase == "staged":
        inbox = project / INBOX_REL
        add(errors, inbox.is_file() and inbox.read_bytes() == task_bytes,
            "staged inbox bytes mismatch")
        add(errors, inbox_candidates(project) == ["SV5_21_LIBRARY.md"],
            "inbox candidate set is not exactly one")
        add(errors, not (project / TASK_REL).exists() and
            not (project / ARCHIVE_REL).exists(),
            "stage helper fabricated Task or Archive")
    elif applied:
        for rel in (TASK_REL, ARCHIVE_REL):
            path = project / rel
            add(errors, path.is_file() and path.read_bytes() == task_bytes,
                "installed/archive byte mismatch: " + rel)
        add(errors, not (project / INBOX_REL).exists(), "inbox remains after Apply")
        add(errors, not inbox_candidates(project), "unexpected inbox candidate after Apply")
    return {
        "counts": counts(status),
        "current_task": current(status),
        "status_sha256": digest(status_path.read_bytes()),
        "master_sha256": digest(master_path.read_bytes()),
        "task_sha256": digest(task_bytes)
    }


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--manifest-sha", required=True)
    modes = parser.add_mutually_exclusive_group(required=True)
    modes.add_argument("--verify-staged", action="store_true")
    modes.add_argument("--verify-applied", action="store_true")
    modes.add_argument("--post-readonly", action="store_true")
    args = parser.parse_args()
    package, project = roots()
    errors: list[str] = []
    verify_package(package, args.manifest_sha, errors)
    lock = json.loads((package / "SOURCE_LOCK.json").read_text(encoding="utf-8"))
    verify_git(project, lock, errors)
    phase = "staged" if args.verify_staged else (
        "post" if args.post_readonly else "applied"
    )
    state = verify_state(project, package, phase, errors)
    if phase in {"applied", "post"}:
        verify_immutable_worktree(project, lock, errors)
    if errors:
        print(json.dumps({"status": "BLOCKED", "phase": phase,
                          "errors": errors}, ensure_ascii=False, indent=2))
        return 1
    label = {
        "staged": "PASS_STAGED_SINGLE_TASK",
        "applied": "PASS_NATIVE_APPLY",
        "post": "PASS_POST_READONLY"
    }[phase]
    print(json.dumps({"status": label, "base_commit": BASE, "state": state,
                      "implementation_or_tests_executed_by_helper": False},
                     ensure_ascii=False, indent=2))
    return 0


if __name__ == "__main__":
    sys.exit(main())
