"""Read-only SV5_08 preservation and actual XML/source evidence audit."""
import argparse
import hashlib
import json
from pathlib import Path
import subprocess
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[5]

def digest(path):
    data = path.read_bytes()
    return {"sha256": hashlib.sha256(data).hexdigest(), "bytes": len(data)}

def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--xml")
    args = parser.parse_args()
    command = ["python", "-X", "utf8", str(ROOT / "MapDesign/MCP/INPUTS/SV5_08/STAGE.py"),
               "--mode", "post-readonly", "--project-root", str(ROOT), "--expected-manifest-sha",
               "b1893ce4937c6800c7cf6ecea6b862d3b1a6beb0c15c91c164cdb8c0c892363d"]
    result = subprocess.run(command, cwd=ROOT, text=True, encoding="utf-8", capture_output=True)
    if result.returncode:
        raise SystemExit(result.stdout + result.stderr)
    binding = json.loads((ROOT / "MapDesign/MCP/GENERATED/SV5_08/BINDING.json").read_text(encoding="utf-8"))
    snapshot_meta = binding["non_owned_dirty_snapshot"]
    snapshot_path = ROOT / snapshot_meta["path"]
    snapshot_bytes = snapshot_path.read_bytes()
    if len(snapshot_bytes) != snapshot_meta["bytes"] or hashlib.sha256(snapshot_bytes).hexdigest() != snapshot_meta["sha256"]:
        raise SystemExit("NON_OWNED_DIRTY_SNAPSHOT_MISMATCH")
    dirty_items = json.loads(snapshot_bytes.decode("utf-8"))["items"]
    mismatch = []
    for item in dirty_items:
        path = ROOT / item["path"]
        actual = digest(path)["sha256"] if path.is_file() else "MISSING"
        if actual.lower() != item["sha"].lower():
            mismatch.append({"path": item["path"], "expected": item["sha"], "actual": actual})
    if mismatch:
        raise SystemExit(json.dumps(mismatch, indent=2))
    allowed = set(binding["write_existing"] + binding["write_new"] + binding["helpers"])
    allowed.update(item["path"] for item in dirty_items)
    allowed.update("MapDesign/MCP/INPUTS/SV5_08/" + name for name in
                   ("CONTRACT.md", "FILES.json", "INFILL_PROFILE.json", "PROTOCOL_APPEND.md",
                    "SOURCE_LOCK.json", "STAGE.py", "SV5_08_INFILL.md"))
    allowed.update(("MapDesign/MCP/06_IMPLEMENTATION_STATUS.md", "MapDesign/MCP/TASKS/SV5_08_INFILL.md",
                    "MapDesign/MCP_ARCHIVE/SV5_08_INFILL.md", "MapDesign/MCP_INBOX/SV5_08_INFILL.md",
                    "MapDesign/MCP/REPORTS/SV5_08_INFILL_RESULT.md"))
    status = subprocess.run(["git", "-c", "core.autocrlf=false", "-c", "core.eol=lf", "status",
                             "--porcelain=v1", "-z", "--untracked-files=all"], cwd=ROOT,
                            capture_output=True, check=True).stdout.decode("utf-8")
    entries = iter(status.split("\0"))
    changed = []
    for entry in entries:
        if not entry:
            continue
        changed.append(entry[3:])
        if "R" in entry[:2] or "C" in entry[:2]:
            changed.append(next(entries))
    unexpected = sorted(set(name for name in changed if name not in allowed and not any(
        name.startswith(prefix.rstrip("/") + "/") for prefix in binding["write_roots"])))
    if unexpected:
        raise SystemExit(json.dumps({"unexpected_non_owned_changes": unexpected}, indent=2))
    sources = {}
    for name in binding["write_existing"] + binding["write_new"]:
        path = ROOT / name
        if path.is_file() and path.suffix == ".cs":
            sources[name] = digest(path)
    output = {"stage": json.loads(result.stdout),
              "initial_non_owned_dirty_entries": snapshot_meta["initial_audit_entries"],
              "non_owned_dirty_unique_paths_checked": len(dirty_items),
              "non_owned_mismatch": 0, "unexpected_non_owned_changes": [], "current_source_bytes": sources}
    if args.xml:
        path = (ROOT / args.xml).resolve()
        path.relative_to((ROOT / "MapDesign/MCP/GENERATED/SV5_08").resolve())
        xml = ET.parse(path).getroot()
        output["xml"] = {"path": str(path.relative_to(ROOT)), **digest(path), "run": xml.attrib,
                         "tests": [{"fullname": t.get("fullname"), "result": t.get("result"),
                                    "message": t.findtext("failure/message")} for t in xml.iter("test-case")],
                         "provenance": "Compare current_source_bytes with the pre-run snapshot; never infer source match from timestamps."}
    print(json.dumps(output, indent=2, ensure_ascii=False))

if __name__ == "__main__":
    main()
