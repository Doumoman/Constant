from __future__ import annotations

import argparse
import hashlib
import json
import re
import subprocess
import sys
from pathlib import Path

BASE = "660d0c58ec0f65cbbbe200716d0e3c36b0ea4ba7"
TASK = "SV5_20_JUMP_PLAYER"
STATUS_REL = "MapDesign/MCP/06_IMPLEMENTATION_STATUS.md"
MASTER_REL = "MapDesign/MCP/MASTER_IMPLEMENTATION_TASK_LIST.md"


def digest(data: bytes) -> str:
    return hashlib.sha256(data).hexdigest()


def same(path: Path, spec: dict) -> bool:
    return path.is_file() and len(path.read_bytes()) == spec["bytes"] and digest(path.read_bytes()) == spec["sha256"]


def run(root: Path, *args: str, check: bool = True) -> subprocess.CompletedProcess[str]:
    return subprocess.run(list(args), cwd=root, text=True, encoding="utf-8",
                          errors="replace", stdout=subprocess.PIPE,
                          stderr=subprocess.PIPE, check=check)


def git(root: Path, *args: str) -> str:
    return run(root, "git", *args).stdout.strip()


def roots() -> tuple[Path, Path]:
    package = Path(__file__).resolve().parent
    return package, package.parents[3]


def counts(text: str) -> dict[str, int]:
    answer = {"COMPLETE": 0, "CURRENT": 0, "LOCKED": 0}
    for state in re.findall(r"^\| [^|]+ \| (COMPLETE|CURRENT|LOCKED) \|$", text, re.M):
        answer[state] += 1
    return answer


def current(text: str) -> str:
    match = re.search(r"## Current Task\s*```text\s*([^\r\n]+)", text)
    return match.group(1).strip() if match else "<MISSING>"


def verify_package(package: Path, manifest_sha: str, errors: list[str]) -> None:
    manifest_path = package / "FILES.json"
    if not manifest_path.is_file():
        errors.append("FILES.json missing")
        return
    if not re.fullmatch(r"[0-9a-f]{64}", manifest_sha) or digest(manifest_path.read_bytes()) != manifest_sha:
        errors.append("FILES.json SHA mismatch")
        return
    try:
        manifest = json.loads(manifest_path.read_text(encoding="utf-8"))
    except Exception as exc:
        errors.append("invalid FILES.json: " + str(exc))
        return
    if manifest.get("schema") != "SV5_20_JUMP_PLAYER_FIX01_FILES/v1" or manifest.get("base_commit") != BASE:
        errors.append("package schema/base mismatch")
    seen: set[str] = set()
    for entry in manifest.get("files", []):
        rel = entry.get("path", "")
        candidate = (package / rel).resolve()
        try:
            candidate.relative_to(package)
        except ValueError:
            errors.append("unsafe package path: " + rel)
            continue
        if rel in seen or not same(candidate, entry):
            errors.append("package byte mismatch: " + rel)
        seen.add(rel)
    required = {"README.md", "AMENDMENT.md", "GEOMETRY_PATCH.json",
                "SOURCE_LOCK.json", "VERIFY.py", "tools/check_jump_player_fix01.py"}
    for rel in sorted(required - seen):
        errors.append("required package entry not manifested: " + rel)


def verify_base_blob(project: Path, entry: dict, errors: list[str]) -> None:
    rel = entry["path"]
    try:
        oid = git(project, "rev-parse", f"{BASE}:{rel}")
        raw = subprocess.run(["git", "cat-file", "blob", oid], cwd=project,
                             stdout=subprocess.PIPE, stderr=subprocess.PIPE,
                             check=True).stdout
        work = (project / rel).read_bytes()
    except Exception as exc:
        errors.append("base/read-only file unavailable: " + rel + ":" + str(exc))
        return
    if (oid != entry.get("git_blob_oid") or digest(raw) != entry.get("sha256")
            or len(raw) != entry.get("bytes") or work != raw):
        errors.append("immutable predecessor mismatch: " + rel)


def verify_dynamic_base_path(project: Path, rel: str, errors: list[str]) -> None:
    try:
        oid = git(project, "rev-parse", f"{BASE}:{rel}")
        raw = subprocess.run(["git", "cat-file", "blob", oid], cwd=project,
                             stdout=subprocess.PIPE, stderr=subprocess.PIPE,
                             check=True).stdout
        work = (project / rel).read_bytes()
    except Exception as exc:
        errors.append("Player input unavailable: " + rel + ":" + str(exc))
        return
    if not oid or work != raw:
        errors.append("Player input changed: " + rel)


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--manifest-sha", required=True)
    mode = parser.add_mutually_exclusive_group(required=True)
    mode.add_argument("--check", action="store_true")
    mode.add_argument("--post-readonly", action="store_true")
    args = parser.parse_args()
    package, project = roots()
    errors: list[str] = []
    verify_package(package, args.manifest_sha, errors)
    try:
        lock = json.loads((package / "SOURCE_LOCK.json").read_text(encoding="utf-8"))
    except Exception as exc:
        lock = {}
        errors.append("invalid SOURCE_LOCK.json: " + str(exc))
    try:
        head = git(project, "rev-parse", "HEAD")
    except Exception as exc:
        head = "<UNAVAILABLE>"
        errors.append("Git unavailable: " + str(exc))
    if head != BASE or lock.get("base_commit") != BASE:
        errors.append(f"HEAD/base mismatch: expected {BASE}, actual {head}")

    status_path = project / STATUS_REL
    master_path = project / MASTER_REL
    if not status_path.is_file() or not master_path.is_file():
        errors.append("Status or Master missing")
        status = master = ""
    else:
        status = status_path.read_text(encoding="utf-8-sig")
        master = master_path.read_text(encoding="utf-8-sig")
    if counts(status) != {"COMPLETE": 267, "CURRENT": 1, "LOCKED": 27}:
        errors.append("active Status counts mismatch: " + repr(counts(status)))
    if current(status) != TASK:
        errors.append("Current Task mismatch: " + current(status))
    if status.count("| SV5_20_JUMP_PLAYER | CURRENT |") != 1:
        errors.append("SV5_20 active row mismatch")
    if status.count("| SV5_21_LIBRARY | LOCKED |") != 1:
        errors.append("SV5_21 must remain LOCKED")
    if master.count("| 20 | SV5_20_JUMP_PLAYER | LOCKED |") != 1:
        errors.append("Master membership mismatch")

    for name in ("original_package_manifest", "installed_task", "archive_task"):
        spec = lock.get(name, {})
        path = project / spec.get("path", "__missing__")
        if not spec or not same(path, spec):
            errors.append(name + " byte mismatch")
    task_spec, archive_spec = lock.get("installed_task", {}), lock.get("archive_task", {})
    if task_spec and archive_spec:
        tp, ap = project / task_spec["path"], project / archive_spec["path"]
        if tp.is_file() and ap.is_file() and tp.read_bytes() != ap.read_bytes():
            errors.append("installed Task and Archive differ")

    for entry in lock.get("base_files", []):
        verify_base_blob(project, entry, errors)
    for rel in lock.get("base_commit_readonly_paths", []):
        verify_dynamic_base_path(project, rel, errors)

    evidence = lock.get("failure_evidence", {})
    found = []
    for rel in evidence.get("paths", []):
        path = project / rel
        if same(path, evidence):
            raw = path.read_text(encoding="utf-8-sig", errors="replace")
            if evidence.get("required_failure_token", "") in raw:
                found.append(rel)
    if not found:
        errors.append("byte-exact MAIN_MX_02 failure evidence missing")

    staged = run(project, "git", "diff", "--cached", "--quiet", check=False)
    if staged.returncode != 0:
        errors.append("staged changes are not allowed before Finalize")

    if args.post_readonly:
        required_outputs = [
            "player_geometry_fix01.json", "player_geometry_patch.csv",
            "player_composed_occupancy.csv", "player_effective_links.csv",
            "independent_jump_player_fix01_audit.json",
        ]
        for name in required_outputs:
            if not (project / "MapDesign/MCP/GENERATED/SV5_20_JUMP_PLAYER" / name).is_file():
                errors.append("missing FIX01 output: " + name)

    if errors:
        print(json.dumps({"status": "BLOCKED", "phase": "post" if args.post_readonly else "check",
                          "errors": errors}, ensure_ascii=False, indent=2))
        return 1
    print(json.dumps({
        "status": "PASS_FIX01_POST_READONLY" if args.post_readonly else "PASS_FIX01_ACTIVE_TASK_PREFLIGHT",
        "task": TASK,
        "base_commit": BASE,
        "current_task_preserved": True,
        "failure_evidence_paths": found,
        "native_apply_or_lifecycle_mutation_performed": False,
    }, ensure_ascii=False, indent=2))
    return 0


if __name__ == "__main__":
    sys.exit(main())

