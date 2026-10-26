import importlib.util,pathlib,unittest
p=pathlib.Path(__file__).resolve().parents[1]/'tools/make_catalog.py';spec=importlib.util.spec_from_file_location('catalog',p);m=importlib.util.module_from_spec(spec);spec.loader.exec_module(m)
class CatalogTests(unittest.TestCase):
 def test_selection_order_baseline_and_unicode(self):
  rows=[{'app_id':'a','app_label':'Vídeo','app_icon':'/apps/a.png'},{'app_id':'b','app_label':'B','app_icon':''}]
  data=m.make_catalog(rows,['system','b','a','a'],['b','a'])
  self.assertEqual([x['Id'] for x in data['Apps']],['b','a']);self.assertEqual(data['Apps'][1]['Name'],'Vídeo');self.assertEqual(data['BaselineIds'],['a','b','system'])
 def test_reject_unknown(self):
  with self.assertRaises(ValueError):m.make_catalog([],[],['missing'])
 def test_reject_duplicate_and_empty(self):
  for selected in ([],['a','a']):
   with self.assertRaises(ValueError):m.make_catalog([{'app_id':'a'}],[],selected)
if __name__=='__main__':unittest.main()
