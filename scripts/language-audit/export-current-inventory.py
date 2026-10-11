"""Сохраняет текущий реестр; старые отчёты не изменяет."""
import argparse
import csv
import json
import os
import re
from collections import Counter
from datetime import datetime
from pathlib import Path

parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('--snapshot-date', required=True, help='Дата нового снимка в формате YYYYMMDD')
parser.add_argument('--inventory-dir', default='.artifacts/language-inventory-current', help='Каталог inventory.json и cli-metadata.json')
parser.add_argument('--output-dir', default='docs/audit/bytecode-only', help='Каталог экспорта')
args = parser.parse_args()
if len(args.snapshot_date) != 8 or not args.snapshot_date.isascii() or not args.snapshot_date.isdigit():
    parser.error('--snapshot-date должна содержать восемь цифр YYYYMMDD')
try:
    datetime.strptime(args.snapshot_date, '%Y%m%d')
except ValueError:
    parser.error('--snapshot-date должна быть существующей календарной датой')
root = Path(__file__).resolve().parents[2]
inventory_dir = (root / args.inventory_dir).resolve()
inventory = json.loads((inventory_dir / 'inventory.json').read_text(encoding='utf-8-sig'))
metadata = json.loads((inventory_dir / 'cli-metadata.json').read_text(encoding='utf-8-sig'))

def registry_signature(row):
    return (row['Name'], tuple((p['Name'], p['Type']) for p in row['Parameters']),
            row['ReturnType'], row['Binding'], row['OperandPolicy'], row['ResultPolicy'])

def metadata_signature(row):
    return (row['name'], tuple((p['name'], p['type']) for p in row['params']),
            row['returnType'], row['bindingKind'], row['operandBindingPolicy'], row['resultTypePolicy'])

assert Counter(map(registry_signature, inventory['Rows'])) == Counter(map(metadata_signature, metadata['builtins'])), 'Реестр и метаданные CLI расходятся'
assert not inventory['Integrity']['MissingVmDispatch'], 'Нет поддержки VM'
assert not inventory['Integrity']['DuplicateSignatures'], 'Повторяющиеся сигнатуры'

audit = (root / args.output_dir).resolve()
if not audit.is_relative_to(root):
    parser.error('--output-dir должен находиться внутри репозитория')
audit.mkdir(parents=True, exist_ok=True)
candidates = {}
registered_forms = {(row['Id'], row['Arity']) for row in inventory['Rows']}
registered_names = {(row['Id'], row['Arity'], row['Name']) for row in inventory['Rows']}
evidence_paths = sorted({
    *list((root / 'docs/audit').glob('*EVIDENCE_????????.csv')),
    *list((root / 'docs/archive/audits-through-20261011').glob('*EVIDENCE_????????.csv')),
})
for path in evidence_paths:
    if path.stem[-8:] > args.snapshot_date:
        continue
    for line, row in enumerate(csv.DictReader(path.open(encoding='utf-8-sig', newline='')), 2):
        identifier = row.get('BuiltinId') or row.get('builtin_id') or row.get('Id')
        if identifier:
            arity = row.get('Arity')
            if arity:
                assert (identifier, int(arity)) in registered_forms, f'{path.name}:{line}: неизвестная форма {identifier}/{arity}'
                if row.get('Name'):
                    assert (identifier, int(arity), row['Name']) in registered_names, f'{path.name}:{line}: имя не соответствует ID/Arity'
            relative = Path(os.path.relpath(path, audit)).as_posix()
            candidates.setdefault(identifier, []).append((row.get('Arity'), f'{relative}:{line}'))

target = audit / f'LANGUAGE_FUNCTION_MATRIX_{args.snapshot_date}.csv'
fields = ['Name', 'Id', 'Arity', 'Parameters', 'DeclaredReturn', 'Binding', 'OperandPolicy',
          'ResultPolicy', 'VmDispatch', 'HistoricalIssue', 'SemanticStatus',
          'CandidateEvidence', 'CandidateLimit', 'TestMentions', 'SampleMentions', 'ManualMentions']
linked = 0
with target.open('w', encoding='utf-8-sig', newline='') as output:
    writer = csv.DictWriter(output, fieldnames=fields)
    writer.writeheader()
    for row in inventory['Rows']:
        links = [link for arity, link in candidates.get(row['Id'], [])
                 if not arity or int(arity) == row['Arity']]
        linked += bool(links)
        writer.writerow({
            **{key: row[key] for key in ['Name', 'Id', 'Arity', 'Binding', 'OperandPolicy', 'ResultPolicy', 'VmDispatch', 'HistoricalIssue']},
            'Parameters': '; '.join(p['Name'] + ': ' + p['Type'] for p in row['Parameters']),
            'DeclaredReturn': row['ReturnType'], 'SemanticStatus': row['Status'],
            'CandidateEvidence': '; '.join(links),
            'CandidateLimit': 'Указатель по ID; при отсутствии Arity нужна отдельная сверка формы. Не подтверждает границы, типы и ожидаемый результат.',
            'TestMentions': '; '.join(p for p in row['Tests'] if not re.search(r'Axiom\.Ide(?!\.(?:Backend|Contracts))|Interpreter', p)),
            'SampleMentions': '; '.join(row['Samples']),
            'ManualMentions': '; '.join(row['Manual']),
        })

summary = {
    'GeneratedAt': inventory['GeneratedAt'], 'Descriptors': inventory['Descriptors'],
    'Names': inventory['Names'], 'EnumIds': inventory['Ids'],
    'RegisteredIds': len({row['Id'] for row in inventory['Rows']}),
    'Reserved': inventory['Reserved'], 'MetadataMatches': True,
    'RowsWithEvidenceCandidates': linked, 'RowsWithoutEvidenceCandidates': len(inventory['Rows']) - linked,
    'MissingVmDispatch': inventory['Integrity']['MissingVmDispatch'],
    'DuplicateSignatures': inventory['Integrity']['DuplicateSignatures'],
}
(audit / f'LANGUAGE_REGISTRY_RECONCILIATION_{args.snapshot_date}.json').write_text(json.dumps(summary, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
print(json.dumps(summary, ensure_ascii=True))
