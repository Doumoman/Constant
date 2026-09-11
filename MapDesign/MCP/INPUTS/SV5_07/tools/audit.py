"""Read-only SV5_07 preservation and XML audit; no expectations are rewritten."""
import hashlib
import json
import pathlib
import subprocess
import sys
import xml.etree.ElementTree as ET

ROOT = pathlib.Path(__file__).resolve().parents[5]
def sha(data):
    return hashlib.sha256(data).hexdigest()

def audit():
    package = ROOT / 'MapDesign/MCP/INPUTS/SV5_07'
    subprocess.run([sys.executable, '-X', 'utf8', str(package / 'STAGE.py'),
                    '--mode', 'post-readonly', '--project-root', str(ROOT),
                    '--expected-manifest-sha', 'c560a05ea06588043c3bce4875838de778de21cc21a09638fecdd1a79fdfd14a'], check=True)
    binding = json.loads((ROOT / 'MapDesign/MCP/GENERATED/SV5_07/BINDING.json').read_text(encoding='utf-8'))
    mismatches = []
    for item in binding['non_owned_baseline']:
        path = ROOT / item['path']
        actual = sha(path.read_bytes()) if path.is_file() else 'MISSING'
        if actual.lower() != item['sha'].lower():
            mismatches.append({'path': item['path'], 'expected': item['sha'], 'actual': actual})
    print(json.dumps({'non_owned_checked': len(binding['non_owned_baseline']), 'mismatches': mismatches}))
    if mismatches:
        raise SystemExit(1)
    if len(sys.argv) > 1:
        path = ROOT / sys.argv[1]
        raw = path.read_bytes()
        xml = ET.fromstring(raw)
        cases = xml.findall('.//test-case')
        print(json.dumps({'xml': str(path.relative_to(ROOT)), 'raw_sha256': sha(raw), 'raw_bytes': len(raw),
                          'lf_sha256': sha(raw.replace(b'\r\n', b'\n')), 'lf_bytes': len(raw.replace(b'\r\n', b'\n')),
                          'run': xml.attrib, 'case_count': len(cases),
                          'failures': [{'name': c.get('fullname'), 'message': c.findtext('failure/message'),
                                        'stack': c.findtext('failure/stack-trace')} for c in cases if c.get('result') != 'Passed'],
                          'output': [{'name': c.get('name'), 'text': c.findtext('output')} for c in cases if c.find('output') is not None]}, ensure_ascii=False))

if __name__ == '__main__':
    audit()
