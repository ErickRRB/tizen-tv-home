import importlib.util,json,pathlib,tempfile,unittest
root=pathlib.Path(__file__).resolve().parents[1];spec=importlib.util.spec_from_file_location('planner',root/'tools/make_preview_plan.py');m=importlib.util.module_from_spec(spec);spec.loader.exec_module(m)
class PreviewPlanTests(unittest.TestCase):
 def setUp(self):
  self.base=root/'.local/test-tmp';self.base.mkdir(parents=True,exist_ok=True);self.tmp=tempfile.TemporaryDirectory(dir=self.base);self.stage=pathlib.Path(self.tmp.name);self.profile=json.loads((root/'profiles/qn75qn70fagczb-1301.json').read_text())
  for n in ['BixbyPromotion.dll','BixbyPromotion.runtimeconfig.json','HomeProbe.dll','HomeProbe.runtimeconfig.json','res/quiet-home/catalog.json']:
   p=self.stage/n;p.parent.mkdir(parents=True,exist_ok=True);p.write_text(json.dumps({'Apps':[{'Id':'test'}],'BaselineIds':['test']}) if n.endswith('catalog.json') else 'synthetic fixture, never executed')
 def tearDown(self):
  self.assertTrue(self.stage.resolve().is_relative_to(self.base.resolve()));self.tmp.cleanup()
 def test_plan_does_not_assign_home_or_connect(self):
  p=m.make_plan(self.profile,self.stage,'synthetic-preview');self.assertFalse(json.loads(p['manifest.json'])['home_changed']);self.assertNotIn('vconftool set',p['apply-preview.sh']);self.assertIn('restore-preview.sh',p);self.assertTrue(all(len(c.encode())<=65536 for c in json.loads(p['stage.json'])))
 def test_reject_target_name_injection(self):
  for name in ['../outside','x;id','x y','/tmp/x']:
   with self.assertRaises(ValueError):m.make_plan(self.profile,self.stage,name)
 def test_reject_unknown_device(self):
  p=dict(self.profile,model='OTHER')
  with self.assertRaises(ValueError):m.make_plan(p,self.stage,'preview')
 def test_reject_reference_dll(self):
  (self.stage/'Tizen.NUI.dll').write_bytes(b'not a real assembly')
  with self.assertRaises(ValueError):m.make_plan(self.profile,self.stage,'preview')
if __name__=='__main__':unittest.main()
