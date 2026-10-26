"""Run the generated rollback logic against a fake vconftool, without a TV."""
import importlib.util
import os
from pathlib import Path
import shutil
import subprocess
import tempfile
import unittest

ROOT = Path(__file__).resolve().parents[1]
spec = importlib.util.spec_from_file_location('planner', ROOT / 'tools/make_preview_plan.py')
planner = importlib.util.module_from_spec(spec)
spec.loader.exec_module(planner)
git_bash = Path(os.environ.get('ProgramFiles', 'C:/Program Files')) / 'Git/bin/bash.exe'
SHELL = str(git_bash) if os.name == 'nt' and git_bash.exists() else shutil.which('sh')
KEY = 'memory/volume-app/key_guide_status'


@unittest.skipUnless(SHELL, 'A POSIX shell is needed for generated rollback tests')
class VolumeGuideTests(unittest.TestCase):
    def run_restore(self, before, current, read_fails=False):
        base = ROOT / '.local/test-tmp'
        base.mkdir(parents=True, exist_ok=True)
        with tempfile.TemporaryDirectory(dir=base) as directory:
            backup = Path(directory)
            self.assertTrue(backup.resolve().is_relative_to(base.resolve()))
            if before is not None:
                (backup / 'volume-guide-before.txt').write_text(before, encoding='utf-8')
            script = '''set -eu
vconftool() {
 if test "$1" = get; then
  test "$READ_FAILS" = 0 || return 1
  printf '%s\n' "$CURRENT"
 else
  printf '%s\n' "$*"
 fi
}
''' + planner.VOLUME_GUIDE_FUNCTIONS + '\nrestore_volume_guide "$1"\n'
            result = subprocess.run(
                [SHELL, '-c', script, '--', backup.as_posix()],
                env=dict(os.environ, CURRENT=current, READ_FAILS=str(int(read_fails))),
                capture_output=True, text=True, check=True,
            )
            return result.stdout.strip()

    def test_restore_only_changed_boolean(self):
        for value in ('True', 'true', '1'):
            with self.subTest(value=value):
                self.assertEqual(self.run_restore('0', f'{KEY}, value = {value} (Bool)'),
                                 f'set -f -t bool {KEY} 0')

    def test_preserve_unmodified_or_missing_baseline(self):
        for before in ('1', 'unavailable', None):
            with self.subTest(before=before):
                self.assertEqual(self.run_restore(before, f'{KEY}, value = True (Bool)'), '')

    def test_reject_type_drift_and_ambiguous_output(self):
        for current in (f'{KEY}, value = 1 (Int)', f'{KEY}, value = True (String)',
                        f'{KEY}, value = 10 (Bool)', f'{KEY}, value = False (Bool)',
                        'another/key, value = True (Bool)', '', 'error: value = True'):
            with self.subTest(current=current):
                self.assertEqual(self.run_restore('0', current), '')

    def test_failed_read_does_not_write(self):
        self.assertEqual(self.run_restore('0', '', read_fails=True), '')


if __name__ == '__main__':
    unittest.main()
