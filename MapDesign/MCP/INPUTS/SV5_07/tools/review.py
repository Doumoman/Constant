"""Package the completed SV5_07 commit, not a partial or whole-project snapshot."""
import hashlib
import json
import pathlib
import re
import subprocess
import zipfile

ROOT = pathlib.Path(__file__).resolve().parents[5]
OUT = ROOT / 'MapDesign/MCP/GENERATED/SV5_07'

def git(*args):
    return subprocess.check_output(['git', '-C', str(ROOT), *args])

def sha(data):
    return hashlib.sha256(data).hexdigest()

def main():
    commit = git('rev-parse', 'HEAD').decode().strip()
    parent = git('rev-parse', 'HEAD^').decode().strip()
    assert parent == 'c96f76b8097c0baaa7e5d5e7a92465e38124ad2a', 'Unexpected predecessor'
    assert 'SV5_07_DIVERSITY' in git('log', '-1', '--format=%s').decode()
    status = (ROOT / 'MapDesign/MCP/06_IMPLEMENTATION_STATUS.md').read_text(encoding='utf-8-sig')
    rows = dict(re.findall(r'^\|\s*([A-Z][A-Z0-9_]+)\s*\|\s*(COMPLETE|CURRENT|LOCKED)\s*\|\s*$', status, re.M))
    assert len(rows) == 290 and list(rows.values()).count('COMPLETE') == 250
    assert list(rows.values()).count('LOCKED') == 40 and 'CURRENT' not in rows.values()
    assert rows['SV5_08_INFILL'] == 'LOCKED' and rows['SV5_07_DIVERSITY'] == 'COMPLETE'
    assert re.search(r'## Current Task\s+```text\s+NONE\s+```', status)
    report = (ROOT / 'MapDesign/MCP/REPORTS/SV5_07_DIVERSITY_RESULT.md').read_text(encoding='utf-8')
    assert re.search(r'^STATUS: PASS$', report, re.M)
    assert not (OUT / '_work').exists(), 'Own temporary work must be cleaned first'
    prefix = git('rev-parse', '--show-prefix').decode().strip()
    repo_paths = git('diff-tree', '--no-commit-id', '--name-only', '-r', 'HEAD').decode().splitlines()
    assert all(p.startswith(prefix) for p in repo_paths)
    paths = [p[len(prefix):] for p in repo_paths]
    refs = ['MapDesign/MCP/MASTER_IMPLEMENTATION_TASK_LIST.md',
            'MapDesign/MCP/00_MCP_ENTRYPOINT.md', 'MapDesign/MCP/05_CHANGE_CONTROL_RULES.md',
            'MapDesign/MCP/08_STATUS_FINALIZE_RULES.md']
    paths = sorted(set(paths + refs))
    manifest_path = OUT / '_REVIEW_MANIFEST.json'
    zip_path = OUT / 'SV5_07_REVIEW.zip'
    assert not manifest_path.exists() and not zip_path.exists(), 'Never overwrite an existing review'
    entries = []
    for path in paths:
        assert '/_work/' not in path and not path.lower().endswith('.zip')
        local = ROOT / path
        assert local.is_file() and not local.is_symlink()
        raw = local.read_bytes()
        blob = git('show', commit + ':' + prefix + path)
        oid = git('rev-parse', commit + ':' + prefix + path).decode().strip()
        entries.append({'path': path, 'role': 'READ_REFERENCE' if path in refs else 'TASK_OWNED',
                        'raw_bytes': len(raw), 'raw_sha256': sha(raw),
                        'git_blob_bytes': len(blob), 'git_blob_sha256': sha(blob), 'git_blob_oid': oid})
    manifest = {'task': 'SV5_07_DIVERSITY', 'status': 'PASS_REVIEW', 'commit': commit, 'parent': parent,
                'raw_and_blob_hashes_are_separate': True, 'excluded': ['self', 'zip', '_work', 'legacy exports', 'unrelated dirty'],
                'files': entries}
    data = (json.dumps(manifest, ensure_ascii=False, indent=2) + '\n').encode('utf-8')
    with manifest_path.open('xb') as stream:
        stream.write(data)
    with zipfile.ZipFile(zip_path, 'x', zipfile.ZIP_DEFLATED, compresslevel=9) as archive:
        for entry in entries:
            archive.write(ROOT / entry['path'], entry['path'])
        archive.writestr('MapDesign/MCP/GENERATED/SV5_07/_REVIEW_MANIFEST.json', data)
    with zipfile.ZipFile(zip_path) as archive:
        assert archive.testzip() is None
        assert len(archive.namelist()) == len(entries) + 1
        for entry in entries:
            assert sha(archive.read(entry['path'])) == entry['raw_sha256']
    raw = zip_path.read_bytes()
    print(json.dumps({'path': str(zip_path), 'sha256': sha(raw), 'bytes': len(raw),
                      'entries': len(entries)+1, 'commit': commit, 'parent': parent}, indent=2))

if __name__ == '__main__':
    main()
