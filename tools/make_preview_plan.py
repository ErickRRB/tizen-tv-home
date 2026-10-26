"""Generate a private, reviewable first-install plan; never connects to the TV."""
import argparse,base64,hashlib,json,pathlib,re

KNOWN_MODEL='QN75QN70FAGCZB'
VOLUME_GUIDE_READ=r'''read_volume_guide() {
 key=memory/volume-app/key_guide_status
 guide=$(vconftool get "$key" 2>/dev/null || true)
 case "$guide" in
  "$key, value = "*" (Bool)"|"$key, value = "*" (bool)"|"$key, value = "*" (Boolean)")
   value=${guide#"$key, value = "}; value=${value% (*}
   case "$value" in
    True|true|1) printf %s 1 ;;
    False|false|0) printf %s 0 ;;
    *) printf %s unavailable ;;
   esac ;;
  *) printf %s unavailable ;;
 esac
}
'''
VOLUME_GUIDE_FUNCTIONS=VOLUME_GUIDE_READ+r'''restore_volume_guide() {
 if test -f "$1/volume-guide-before.txt" && test "$(cat "$1/volume-guide-before.txt")" = 0; then
  if test "$(read_volume_guide)" = 1; then
   vconftool set -f -t bool memory/volume-app/key_guide_status 0
  fi
 fi
}
'''
def sha(data):return hashlib.sha256(data).hexdigest()
def make_plan(profile,stage,name):
 if profile.get('model')!=KNOWN_MODEL or profile.get('firmware')!='T-PTMFUABC-0090-1301.0':raise ValueError('Unreviewed model/firmware. Prepare and review a separate profile; do not rename the TV to bypass this guard.')
 if not re.fullmatch(r'[a-z0-9][a-z0-9-]{0,40}',name):raise ValueError('Invalid backup name')
 for key in ('original_dll_sha256','original_runtimeconfig_sha256'):
  if not re.fullmatch('[0-9a-f]{64}',profile.get(key,'')):raise ValueError('Missing original hash')
 stage=pathlib.Path(stage);files={}
 for path in stage.rglob('*'):
  if path.is_symlink():raise ValueError('Symlinks are not allowed in a stage')
  if path.is_file():
   rel=path.relative_to(stage).as_posix()
   if not re.fullmatch(r'[A-Za-z0-9_./-]+',rel) or any(x in ('','..') for x in rel.split('/')):raise ValueError('Unsafe stage path')
   files[rel]=path.read_bytes()
 if {'apply-preview.sh','restore-preview.sh'} & files.keys():raise ValueError('Reserved generated script in input stage')
 required={'BixbyPromotion.dll','BixbyPromotion.runtimeconfig.json','HomeProbe.dll','HomeProbe.runtimeconfig.json','res/quiet-home/catalog.json'}
 if not required<=files.keys():raise ValueError('Incomplete stage')
 # Fail closed: SDK/reference assemblies never belong in this package.
 allowed_dlls={'BixbyPromotion.dll','HomeProbe.dll'}
 if any(n.lower().endswith('.dll') and n not in allowed_dlls for n in files):raise ValueError('Unexpected DLL; do not package Samsung references')
 catalog=json.loads(files['res/quiet-home/catalog.json']);
 if not catalog.get('Apps') or not catalog.get('BaselineIds'):raise ValueError('A real selected catalog and baseline are required')
 b='/opt/usr/share/quiet-home-backups/'+name;a='/opt/usr/apps/com.samsung.tv.bixby-promotion';h=a+'/bin/BixbyPromotion.dll';rc=a+'/bin/BixbyPromotion.runtimeconfig.json';res=a+'/res/quiet-home';old=profile['original_dll_sha256'];oldrc=profile['original_runtimeconfig_sha256'];new=sha(files['BixbyPromotion.dll']);newrc=sha(files['BixbyPromotion.runtimeconfig.json']);uid=profile.get('application_uid')
 if uid!=5001:raise ValueError('Unreviewed application UID')
 stop="app_launcher -u 5001 -t com.samsung.tv.bixby-promotion || true\nn=0; while app_launcher -u 5001 -r com.samsung.tv.bixby-promotion >/dev/null 2>&1; do n=$((n+1)); test \"$n\" -le 12; sleep 0.2; done\n"
 checks='\n'.join(f'test "$(sha256sum "{b}/staged/{n}" | cut -d \' \' -f1)" = {sha(data)}' for n,data in files.items())
 apply=f'''set -eu
 {VOLUME_GUIDE_READ}
b={b}; a={a}; h={h}; rc={rc}; res={res}
test "$(id -u)" = 0
test ! -e "$b/original"; test ! -e "$b/preview-installed"; test ! -e "$res"; test ! -L "$res"
test -f "$h"; test ! -L "$h"; test -f "$rc"; test ! -L "$rc"
test -s "$b/staged/restore-preview.sh"
test "$(sha256sum "$h" | cut -d ' ' -f1)" = {old}
test "$(sha256sum "$rc" | cut -d ' ' -f1)" = {oldrc}
{checks}
mkdir "$b/original"
cp -a "$h" "$rc" "$b/original/"
test "$(sha256sum "$b/original/BixbyPromotion.dll" | cut -d ' ' -f1)" = {old}
test "$(sha256sum "$b/original/BixbyPromotion.runtimeconfig.json" | cut -d ' ' -f1)" = {oldrc}
vconftool get db/appfw/home_appid > "$b/home-before.txt"
vconftool get db/appfw/startup_appid >> "$b/home-before.txt"
read_volume_guide > "$b/volume-guide-before.txt"
/usr/bin/dotnet "$b/staged/HomeProbe.dll" > "$b/main-and-sources-before.txt"
cp -a "$b/staged/restore-preview.sh" "$b/restore-preview.sh"
{stop}
touch "$b/preview-started"
if test -d "$a/bin/.native_image"; then mv "$a/bin/.native_image" "$b/native-image-original"; else touch "$b/no-native-image-before"; fi
cp -a "$b/staged/res/quiet-home" "$res"
owner=$(stat -c %u:%g "$h")
find "$res" -type d -exec chown "$owner" {{}} + -exec chmod 755 {{}} + -exec chsmack -a User::Pkg::com.samsung.tv.bixby-promotion::RO {{}} +
find "$res" -type f -exec chown "$owner" {{}} + -exec chmod 644 {{}} + -exec chsmack -a User::Pkg::com.samsung.tv.bixby-promotion::RO {{}} +
cp -a "$h" "$h.quiet-new"; cat "$b/staged/BixbyPromotion.dll" > "$h.quiet-new"
cp -a "$rc" "$rc.quiet-new"; cat "$b/staged/BixbyPromotion.runtimeconfig.json" > "$rc.quiet-new"
test "$(sha256sum "$h.quiet-new" | cut -d ' ' -f1)" = {new}
test "$(sha256sum "$rc.quiet-new" | cut -d ' ' -f1)" = {newrc}
mv "$h.quiet-new" "$h"; mv "$rc.quiet-new" "$rc"
touch "$b/preview-installed"
app_launcher -u 5001 -s com.samsung.tv.bixby-promotion
echo PREVIEW_LAUNCHED_HOME_UNCHANGED
'''
 restore=f'''set -eu
{VOLUME_GUIDE_FUNCTIONS}
b={b}; a={a}; h={h}; rc={rc}; res={res}
test "$(id -u)" = 0
test -e "$b/preview-started"; test -e "$b/original/BixbyPromotion.dll"; test ! -e "$b/preview-restored"; test ! -e "$b/removed-custom-resources"; test ! -e "$b/native-image-generated"
test "$(vconftool get db/appfw/home_appid)" != 'db/appfw/home_appid, value = com.samsung.tv.bixby-promotion (String)'
main=$(/usr/bin/dotnet "$b/staged/HomeProbe.dll")
if printf '%s\n' "$main" | grep -Fx 'home_result=0 home=com.samsung.tv.bixby-promotion'; then echo RESTORE_HOME_FIRST; exit 1; fi
current=$(sha256sum "$h" | cut -d ' ' -f1); case "$current" in {old}|{new}) ;; *) exit 1;; esac
current=$(sha256sum "$rc" | cut -d ' ' -f1); case "$current" in {oldrc}|{newrc}) ;; *) exit 1;; esac
test "$(sha256sum "$b/original/BixbyPromotion.dll" | cut -d ' ' -f1)" = {old}
test "$(sha256sum "$b/original/BixbyPromotion.runtimeconfig.json" | cut -d ' ' -f1)" = {oldrc}
{stop}
cp -a "$b/original/BixbyPromotion.dll" "$h.quiet-restore"
cp -a "$b/original/BixbyPromotion.runtimeconfig.json" "$rc.quiet-restore"
mv "$h.quiet-restore" "$h"; mv "$rc.quiet-restore" "$rc"
if test -d "$b/native-image-original"; then
 if test -e "$a/bin/.native_image"; then mv "$a/bin/.native_image" "$b/native-image-generated"; fi
 mv "$b/native-image-original" "$a/bin/.native_image"
elif test -e "$b/no-native-image-before" && test -e "$a/bin/.native_image"; then
 mv "$a/bin/.native_image" "$b/native-image-generated"
fi
if test -d "$res"; then mv "$res" "$b/removed-custom-resources"; fi
restore_volume_guide "$b"
touch "$b/preview-restored"
echo ORIGINAL_PACKAGE_RESTORED_HOME_ROUTING_PRESERVED
'''
 files.update({'apply-preview.sh':apply.encode(),'restore-preview.sh':restore.encode()})
 commands=[f'set -eu; test ! -e {b}; test ! -L /opt/usr/share/quiet-home-backups; mkdir -p /opt/usr/share/quiet-home-backups; mkdir -m 700 {b}; mkdir {b}/staged']
 parents=set()
 for n,data in files.items():
  parent=pathlib.PurePosixPath(n).parent.as_posix()
  if parent!='.' and parent not in parents:commands.append(f'mkdir -p {b}/staged/{parent}');parents.add(parent)
  for offset in range(0,len(data),45056):commands.append(f'printf %s {base64.b64encode(data[offset:offset+45056]).decode()} | base64 -d {">" if offset==0 else ">>"} {b}/staged/{n}')
  commands.append(f'test "$(sha256sum {b}/staged/{n} | cut -d \' \' -f1)" = {sha(data)}')
 return {'stage.json':json.dumps(commands),'apply-preview.json':json.dumps([f"bash -c '. {b}/staged/apply-preview.sh'"]),'restore-preview.json':json.dumps([f"bash -c '. {b}/restore-preview.sh'"]),'apply-preview.sh':apply,'restore-preview.sh':restore,'manifest.json':json.dumps({'model':profile['model'],'firmware':profile['firmware'],'backup':b,'new_dll_sha256':new,'home_changed':False,'first_install_recipe_fresh_tv_not_tested_end_to_end':True},indent=2)}

def main():
 ap=argparse.ArgumentParser(description=__doc__);ap.add_argument('--profile',required=True,type=pathlib.Path);ap.add_argument('--stage',required=True,type=pathlib.Path);ap.add_argument('--name',required=True);ap.add_argument('--output',required=True,type=pathlib.Path);a=ap.parse_args()
 if a.output.exists():raise SystemExit('Refuse to overwrite a plan directory')
 plan=make_plan(json.loads(a.profile.read_text()),a.stage,a.name);a.output.mkdir(parents=True)
 for n,s in plan.items():(a.output/n).write_text(s,encoding='utf-8',newline='\n')
 print('Private first-install plan generated for review; no TV access performed')
if __name__=='__main__':main()
