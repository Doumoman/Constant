from __future__ import annotations

import argparse
import hashlib
import json
import os
import re
import subprocess
import sys
from pathlib import Path

TASK = "SV5_11_FIX01"
PATCH = "SV5_11_FIX01_REG"
BASE = "0ab603362b93047a45c6f30bac8e28d57ac57f93"
STATUS_REL = "MapDesign/MCP/06_IMPLEMENTATION_STATUS.md"
MASTER_REL = "MapDesign/MCP/MASTER_IMPLEMENTATION_TASK_LIST.md"
INBOX_REL = "MapDesign/MCP_INBOX/SV5_11_FIX01.md"
TASK_REL = "MapDesign/MCP/TASKS/SV5_11_FIX01.md"
ARCHIVE_REL = "MapDesign/MCP_ARCHIVE/SV5_11_FIX01.md"
RESULT_REL = "MapDesign/MCP/REPORTS/SV5_11_FIX01_RESULT.md"
REG_DIR_REL = "MapDesign/MCP/GENERATED/SV5_11_FIX01_REG"


def sha_bytes(data: bytes) -> str:
    return hashlib.sha256(data).hexdigest()


def sha_file(path: Path) -> str:
    return sha_bytes(path.read_bytes())


def run(root: Path, *args: str, check: bool = True) -> subprocess.CompletedProcess[str]:
    return subprocess.run(
        list(args), cwd=root, text=True, encoding="utf-8", errors="replace",
        stdout=subprocess.PIPE, stderr=subprocess.PIPE, check=check,
    )


def git(root: Path, *args: str) -> str:
    return run(root, "git", *args).stdout.strip()


def fail(errors: list[str], message: str) -> None:
    errors.append(message)


def package_root() -> tuple[Path, Path]:
    package = Path(__file__).resolve().parent
    project = package.parents[3]
    return package, project


def verify_package(package: Path, expected_manifest_sha: str, errors: list[str]) -> dict:
    manifest_path = package / "FILES.json"
    if not manifest_path.is_file():
        fail(errors, "package manifest missing")
        return {}
    if not re.fullmatch(r"[0-9a-f]{64}", expected_manifest_sha):
        fail(errors, "--manifest-sha must be 64 lowercase hex")
    elif sha_file(manifest_path) != expected_manifest_sha:
        fail(errors, "package manifest SHA mismatch")
    try:
        manifest = json.loads(manifest_path.read_text(encoding="utf-8"))
    except Exception as exc:
        fail(errors, f"invalid package manifest: {exc}")
        return {}
    if manifest.get("schema") != "SV5_11_FIX01_REG_FILES/v1":
        fail(errors, "unexpected package manifest schema")
    seen: set[str] = set()
    for entry in manifest.get("files", []):
        rel = entry.get("path", "")
        if rel in seen:
            fail(errors, f"duplicate package path: {rel}")
        seen.add(rel)
        path = (package / rel).resolve()
        try:
            path.relative_to(package)
        except ValueError:
            fail(errors, f"unsafe package path: {rel}")
            continue
        if not path.is_file():
            fail(errors, f"package file missing: {rel}")
            continue
        if sha_file(path) != entry.get("sha256") or path.stat().st_size != entry.get("bytes"):
            fail(errors, f"package file byte mismatch: {rel}")
    required = {
        "README.md", "REGISTRATION.md", "CONTRACT.md", "SOURCE_LOCK.json",
        "HUB_CONNECTION_PROFILE.json", "SV5_11_FIX01.md", "REGISTER.py",
        "tools/check_hub_connection_fix.py",
    }
    for rel in sorted(required - seen):
        fail(errors, f"required package entry not manifested: {rel}")
    return manifest


def source_lock(package: Path) -> dict:
    return json.loads((package / "SOURCE_LOCK.json").read_text(encoding="utf-8"))


def verify_git_objects(project: Path, lock: dict, errors: list[str]) -> None:
    try:
        head = git(project, "rev-parse", "HEAD")
    except Exception as exc:
        fail(errors, f"not a readable Git worktree: {exc}")
        return
    if head != BASE:
        fail(errors, f"HEAD mismatch: expected {BASE}, actual {head}")
    if lock.get("base_commit") != BASE:
        fail(errors, "SOURCE_LOCK base commit mismatch")
    for entry in lock.get("files", []):
        rel = entry["path"]
        spec = f"{BASE}:{rel}"
        try:
            oid = git(project, "rev-parse", spec)
            raw = subprocess.run(
                ["git", "cat-file", "blob", oid], cwd=project,
                stdout=subprocess.PIPE, stderr=subprocess.PIPE, check=True,
            ).stdout
        except Exception as exc:
            fail(errors, f"locked Git blob unavailable: {rel}: {exc}")
            continue
        if oid != entry["git_blob_oid"]:
            fail(errors, f"Git blob OID mismatch: {rel}")
        if sha_bytes(raw) != entry["sha256"] or len(raw) != entry["bytes"]:
            fail(errors, f"Git blob byte mismatch: {rel}")


def status_counts(text: str) -> dict[str, int]:
    counts = {"COMPLETE": 0, "CURRENT": 0, "LOCKED": 0}
    for state in re.findall(r"^\| [^|]+ \| (COMPLETE|CURRENT|LOCKED) \|$", text, re.M):
        counts[state] += 1
    return counts


def current_task(text: str) -> str:
    match = re.search(r"## Current Task\s*```text\s*([^\r\n]+)", text)
    return match.group(1).strip() if match else "<MISSING>"


def native_candidates(project: Path) -> list[str]:
    inbox = project / "MapDesign/MCP_INBOX"
    if not inbox.is_dir():
        return []
    result: list[str] = []
    for path in inbox.iterdir():
        if path.is_file() and path.suffix.lower() == ".md":
            result.append(path.name)
        elif path.is_dir() and not (path / ".APPLIED").exists():
            result.append(path.name + "/")
    return sorted(result)


def worktree_overlap(project: Path, lock: dict, errors: list[str]) -> None:
    checked = [entry["path"] for entry in lock.get("files", [])]
    checked += [TASK_REL, ARCHIVE_REL, RESULT_REL, REG_DIR_REL, INBOX_REL]
    proc = run(
        project, "git", "status", "--porcelain=v1", "--untracked-files=all", "--", *checked,
        check=False,
    )
    if proc.returncode != 0:
        fail(errors, "git status failed for owned-path overlap check")
    elif proc.stdout.strip():
        fail(errors, "task-owned worktree overlap:\n" + proc.stdout.rstrip())


def verify_before(project: Path, package: Path, lock: dict, errors: list[str]) -> dict:
    status_path = project / STATUS_REL
    master_path = project / MASTER_REL
    if not status_path.is_file() or not master_path.is_file():
        fail(errors, "Status or Master is missing")
        return {}
    status = status_path.read_text(encoding="utf-8-sig")
    master = master_path.read_text(encoding="utf-8-sig")
    counts = status_counts(status)
    expected = {"COMPLETE": 257, "CURRENT": 0, "LOCKED": 36}
    if counts != expected:
        fail(errors, f"before-state counts mismatch: {counts}")
    if current_task(status) != "NONE":
        fail(errors, f"Current Task is not NONE: {current_task(status)}")
    if status.count("| SV5_11_HUB_SHELL | COMPLETE |") != 1:
        fail(errors, "SV5_11 COMPLETE row mismatch")
    if status.count("| SV5_12_TREE_GRAB | LOCKED |") != 1:
        fail(errors, "SV5_12 LOCKED row mismatch")
    if TASK in status or TASK in master:
        fail(errors, "SV5_11_FIX01 is already registered")
    if master.count("| 11 | SV5_11_HUB_SHELL | LOCKED |") != 1:
        fail(errors, "SV5_11 Master anchor mismatch")
    if master.count("| 12 | SV5_12_TREE_GRAB | LOCKED |") != 1:
        fail(errors, "SV5_12 Master anchor mismatch")
    collisions = [rel for rel in [INBOX_REL, TASK_REL, ARCHIVE_REL, RESULT_REL, REG_DIR_REL]
                  if (project / rel).exists()]
    if collisions:
        fail(errors, "registration collision: " + ", ".join(collisions))
    candidates = native_candidates(project)
    if candidates:
        fail(errors, "native inbox is not empty: " + ", ".join(candidates))
    worktree_overlap(project, lock, errors)
    return {"counts": counts, "status_sha256": sha_file(status_path),
            "master_sha256": sha_file(master_path)}


def replace_exact(data: bytes, before_lf: str, after_lf: str, label: str) -> bytes:
    newline = b"\r\n" if b"\r\n" in data else b"\n"
    before = before_lf.replace("\n", newline.decode("ascii")).encode("utf-8")
    after = after_lf.replace("\n", newline.decode("ascii")).encode("utf-8")
    if data.count(before) != 1:
        raise RuntimeError(f"exact {label} anchor count is not one")
    return data.replace(before, after, 1)


def atomic_write(path: Path, data: bytes) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    temp = path.with_name(path.name + ".SV5_11_FIX01.tmp")
    if temp.exists():
        raise RuntimeError(f"temporary collision: {temp}")
    try:
        temp.write_bytes(data)
        os.replace(temp, path)
    finally:
        if temp.exists():
            temp.unlink()


def apply_registration(project: Path, package: Path, manifest_sha: str, before: dict) -> dict:
    status_path = project / STATUS_REL
    master_path = project / MASTER_REL
    inbox_path = project / INBOX_REL
    reg_dir = project / REG_DIR_REL
    record_path = reg_dir / "REGISTRATION_RECORD.md"
    receipt_path = reg_dir / "REGISTRATION_RECEIPT.json"
    old_status = status_path.read_bytes()
    old_master = master_path.read_bytes()
    new_status = replace_exact(
        old_status,
        "| SV5_11_HUB_SHELL | COMPLETE |\n| SV5_12_TREE_GRAB | LOCKED |",
        "| SV5_11_HUB_SHELL | COMPLETE |\n| SV5_11_FIX01 | LOCKED |\n| SV5_12_TREE_GRAB | LOCKED |",
        "Status",
    )
    new_master = replace_exact(
        old_master,
        "| 11 | SV5_11_HUB_SHELL | LOCKED |\n| 12 | SV5_12_TREE_GRAB | LOCKED |",
        "| 11 | SV5_11_HUB_SHELL | LOCKED |\n| 11.F1 | SV5_11_FIX01 | LOCKED |\n| 12 | SV5_12_TREE_GRAB | LOCKED |",
        "Master",
    )
    task_bytes = (package / "SV5_11_FIX01.md").read_bytes()
    record_bytes = (package / "REGISTRATION.md").read_bytes()
    created: list[Path] = []
    try:
        atomic_write(status_path, new_status)
        atomic_write(master_path, new_master)
        atomic_write(inbox_path, task_bytes)
        created.append(inbox_path)
        reg_dir.mkdir(parents=True, exist_ok=False)
        atomic_write(record_path, record_bytes)
        created.append(record_path)
        receipt = {
            "schema": "SV5_11_FIX01_REG_RECEIPT/v1",
            "patch_id": PATCH,
            "status": "REGISTERED_LOCKED_TASK_NOT_APPLIED",
            "base_commit": BASE,
            "package_manifest_sha256": manifest_sha,
            "bound_task_sha256": sha_bytes(task_bytes),
            "status_before_sha256": before["status_sha256"],
            "status_after_sha256": sha_bytes(new_status),
            "master_before_sha256": before["master_sha256"],
            "master_after_sha256": sha_bytes(new_master),
            "state_before": before["counts"],
            "state_after": {"COMPLETE": 257, "CURRENT": 0, "LOCKED": 37},
            "current_task": "NONE",
            "inbox_candidate": INBOX_REL,
            "native_apply_execution_finalize_commit": "NOT_RUN_BY_REGISTRATION_HELPER",
        }
        atomic_write(receipt_path, (json.dumps(receipt, indent=2, ensure_ascii=False) + "\n").encode("utf-8"))
        created.append(receipt_path)
        errors: list[str] = []
        verify_registered(project, package, applied=False, errors=errors)
        if errors:
            raise RuntimeError("; ".join(errors))
        return receipt
    except Exception:
        for path in reversed(created):
            if path.exists():
                path.unlink()
        if reg_dir.exists() and not any(reg_dir.iterdir()):
            reg_dir.rmdir()
        atomic_write(status_path, old_status)
        atomic_write(master_path, old_master)
        raise


def verify_immutable_worktree(project: Path, lock: dict, errors: list[str]) -> None:
    immutable = [entry["path"] for entry in lock.get("files", [])
                 if entry["role"].startswith("IMMUTABLE")]
    proc = run(project, "git", "status", "--porcelain=v1", "--untracked-files=all", "--", *immutable,
               check=False)
    if proc.returncode != 0 or proc.stdout.strip():
        fail(errors, "immutable predecessor paths changed:\n" + proc.stdout.rstrip())


def verify_registered(project: Path, package: Path, applied: bool, errors: list[str]) -> dict:
    status_path = project / STATUS_REL
    master_path = project / MASTER_REL
    task_bytes = (package / "SV5_11_FIX01.md").read_bytes()
    status = status_path.read_text(encoding="utf-8-sig") if status_path.is_file() else ""
    master = master_path.read_text(encoding="utf-8-sig") if master_path.is_file() else ""
    if status.count(f"| {TASK} | {'CURRENT' if applied else 'LOCKED'} |") != 1:
        fail(errors, "registered Task Status row mismatch")
    if master.count("| 11.F1 | SV5_11_FIX01 | LOCKED |") != 1:
        fail(errors, "registered Task Master row mismatch")
    if status.count("| SV5_11_HUB_SHELL | COMPLETE |") != 1 or status.count("| SV5_12_TREE_GRAB | LOCKED |") != 1:
        fail(errors, "neighbor state changed")
    expected_counts = {"COMPLETE": 257, "CURRENT": 1 if applied else 0,
                       "LOCKED": 36 if applied else 37}
    if status_counts(status) != expected_counts:
        fail(errors, f"registered counts mismatch: {status_counts(status)}")
    if current_task(status) != (TASK if applied else "NONE"):
        fail(errors, f"registered Current Task mismatch: {current_task(status)}")
    if applied:
        for rel in [TASK_REL, ARCHIVE_REL]:
            path = project / rel
            if not path.is_file() or path.read_bytes() != task_bytes:
                fail(errors, f"installed/archive Task byte mismatch: {rel}")
        if (project / INBOX_REL).exists():
            fail(errors, "inbox Task remains after native Apply")
        if native_candidates(project):
            fail(errors, "unexpected native inbox candidate after Apply")
    else:
        inbox = project / INBOX_REL
        if not inbox.is_file() or inbox.read_bytes() != task_bytes:
            fail(errors, "staged inbox Task byte mismatch")
        if native_candidates(project) != ["SV5_11_FIX01.md"]:
            fail(errors, "staged inbox candidate set is not exactly one")
        if (project / TASK_REL).exists() or (project / ARCHIVE_REL).exists():
            fail(errors, "registration helper fabricated Task or Archive")
    receipt = project / REG_DIR_REL / "REGISTRATION_RECEIPT.json"
    record = project / REG_DIR_REL / "REGISTRATION_RECORD.md"
    if not receipt.is_file() or not record.is_file():
        fail(errors, "registration receipt or record missing")
    return {"counts": status_counts(status), "current_task": current_task(status),
            "task_sha256": sha_bytes(task_bytes)}


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--manifest-sha", required=True)
    modes = parser.add_mutually_exclusive_group(required=True)
    modes.add_argument("--check", action="store_true")
    modes.add_argument("--apply-registration", action="store_true")
    modes.add_argument("--verify-staged", action="store_true")
    modes.add_argument("--verify-task-inputs", action="store_true")
    modes.add_argument("--post-readonly", action="store_true")
    parser.add_argument("--approval")
    args = parser.parse_args()
    package, project = package_root()
    errors: list[str] = []
    verify_package(package, args.manifest_sha, errors)
    lock = source_lock(package)
    verify_git_objects(project, lock, errors)

    if args.check or args.apply_registration:
        before = verify_before(project, package, lock, errors)
        if args.apply_registration and args.approval != PATCH:
            fail(errors, f"--approval must be exactly {PATCH}")
        if errors:
            print(json.dumps({"status": "BLOCKED", "errors": errors}, ensure_ascii=False, indent=2))
            return 1
        if args.check:
            print(json.dumps({"status": "PASS_REGISTRATION_PREFLIGHT_READONLY", "base_commit": BASE,
                              "before": before, "native_apply_run": False}, ensure_ascii=False, indent=2))
            return 0
        try:
            receipt = apply_registration(project, package, args.manifest_sha, before)
        except Exception as exc:
            print(json.dumps({"status": "BLOCKED_ROLLED_BACK", "errors": [str(exc)]},
                             ensure_ascii=False, indent=2))
            return 1
        print(json.dumps({"status": "REGISTERED_LOCKED_TASK_NOT_APPLIED", "receipt": receipt},
                         ensure_ascii=False, indent=2))
        return 0

    verify_registered(project, package, applied=args.verify_task_inputs or args.post_readonly, errors=errors)
    verify_immutable_worktree(project, lock, errors)
    label = "PASS_STAGED_REGISTRATION" if args.verify_staged else (
        "PASS_TASK_INPUTS" if args.verify_task_inputs else "PASS_POST_READONLY")
    print(json.dumps({"status": "BLOCKED" if errors else label, "base_commit": BASE,
                      "native_apply_checked": bool(args.verify_task_inputs or args.post_readonly),
                      "implementation_or_tests_executed_by_helper": False, "errors": errors},
                     ensure_ascii=False, indent=2))
    return 1 if errors else 0


if __name__ == "__main__":
    try:
        sys.exit(main())
    except Exception as exc:
        print(json.dumps({"status": "BLOCKED", "errors": [str(exc)]}, ensure_ascii=False, indent=2))
        sys.exit(1)
