"""Explicit single-TV transport for a separately obtained samsung-tv-root 0.0.12 release."""
import argparse,asyncio,datetime,ipaddress,json,os,pathlib,sys

def main():
 ap=argparse.ArgumentParser(description=__doc__)
 for name in ('release','sdb','tv-ip','pc-ip','commands'):ap.add_argument('--'+name,required=True)
 a=ap.parse_args();tv=ipaddress.IPv4Address(a.tv_ip);pc=ipaddress.IPv4Address(a.pc_ip)
 if tv.is_multicast or pc.is_multicast or tv.is_unspecified or pc.is_unspecified:raise SystemExit('Use explicit unicast device addresses')
 release=pathlib.Path(a.release).resolve();sdb=pathlib.Path(a.sdb).resolve();source=release/'source/src';payload=release/'_internal/payloads/common'
 if not source.is_dir() or not payload.is_dir() or not sdb.is_file():raise SystemExit('Expected the separately obtained 0.0.12 release layout and a real SDB executable')
 commands=json.loads(pathlib.Path(a.commands).read_text(encoding='utf-8-sig'))
 if not isinstance(commands,list) or not commands or any(not isinstance(c,str) or len(c.encode())>65536 for c in commands):raise SystemExit('Commands must be a nonempty JSON string array; max 65536 bytes each')
 out=pathlib.Path('.local/sessions');out.mkdir(parents=True,exist_ok=True);lock=out.parent/'tv-session.lock'
 try:lock.mkdir()
 except FileExistsError:raise SystemExit('Another session owns .local/tv-session.lock; inspect it before retrying')
 (lock/'owner.json').write_text(json.dumps({'pid':os.getpid(),'started_utc':datetime.datetime.now(datetime.timezone.utc).isoformat()}))
 record={'tool_version':'0.0.12','capture_utc':datetime.datetime.now(datetime.timezone.utc).isoformat()};failed=True
 try:
  sys.path.insert(0,str(source));os.environ['SDB']=str(sdb)
  from samsung_tv_root.archive_root import probe
  e=asyncio.run(probe(str(tv),callback_host=str(pc),bind_host=str(pc),payloads=payload,commands=tuple(commands)));record['evidence']=e
  failed=not(e.get('authenticated') and e.get('uid')==0 and e.get('euid')==0 and e.get('system_files_unchanged') is True and e.get('staging_cleaned') is True and all(c.get('exit_code')==0 for c in e.get('commands',[])))
 except Exception as exc:record['error']=str(exc)
 finally:
  target=out/(datetime.datetime.now(datetime.timezone.utc).strftime('%Y%m%dT%H%M%S%fZ')+'.json');target.write_text(json.dumps(record,indent=2),encoding='utf-8');(lock/'owner.json').unlink();lock.rmdir()
 print('Private result:',target)
 print('PASS' if not failed else 'FAIL: inspect the private record; do not continue to another mutation')
 raise SystemExit(1 if failed else 0)
if __name__=='__main__':main()
