"""Build a private catalog from two CSV exports; no TV connection or writes."""
import argparse,csv,json,pathlib

def make_catalog(rows,baseline,selected):
 by_id={row['app_id']:row for row in rows}
 if not selected or len(selected)!=len(set(selected)):
  raise ValueError('Select at least one app, without duplicate IDs')
 unknown=set(selected)-by_id.keys()
 if unknown:raise ValueError('Unknown app IDs: '+', '.join(sorted(unknown)))
 return {'Apps':[{'Id':i,'Name':by_id[i].get('app_label') or i,'Icon':by_id[i].get('app_icon') or ''} for i in selected],'BaselineIds':sorted(set(baseline))}

def main():
 ap=argparse.ArgumentParser(description=__doc__);ap.add_argument('--apps',required=True,type=pathlib.Path);ap.add_argument('--baseline',required=True,type=pathlib.Path);ap.add_argument('--include',action='append',required=True);ap.add_argument('--output',required=True,type=pathlib.Path);a=ap.parse_args()
 with a.apps.open(encoding='utf-8-sig',newline='') as f:rows=list(csv.DictReader(f))
 with a.baseline.open(encoding='utf-8-sig',newline='') as f:ids=[r['app_id'] for r in csv.DictReader(f)]
 data=make_catalog(rows,ids,a.include)
 if a.output.exists():raise SystemExit('Refuse to overwrite existing catalog; choose another output path')
 a.output.parent.mkdir(parents=True,exist_ok=True);a.output.write_text(json.dumps(data,ensure_ascii=False,indent=2),encoding='utf-8');print('Catalog saved locally')
if __name__=='__main__':main()
