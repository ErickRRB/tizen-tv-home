using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections.Generic;
using Mono.Cecil;

Action Load(string path) {
 using var module=ModuleDefinition.ReadModule(path);
 var method=module.Types.Single(t=>t.Name=="SearchAllView").Methods.Single(m=>m.Name=="HideSearchHandler");
 var dynamic=new DynamicMethod("ReviewedSearchClose",typeof(void),Type.EmptyTypes);
 var il=dynamic.GetILGenerator();
 var instructions=method.Body.Instructions;
 var labels=instructions.ToDictionary(i=>i,i=>il.DefineLabel());
 var ops=typeof(OpCodes).GetFields(BindingFlags.Public|BindingFlags.Static).Where(f=>f.FieldType==typeof(OpCode)).Select(f=>(OpCode)f.GetValue(null)!).ToDictionary(o=>o.Name!);
 MethodInfo Target(MethodReference reference) => reference.Name switch {
  "GetInstance"=>typeof(FakeRouter).GetMethod("GetInstance")!,
  "GetListCount"=>typeof(FakeRouter).GetMethod("GetListCount")!,
  "Back"=>typeof(FakeRouter).GetMethod("Back")!,
  "HideSearch"=>typeof(FakeSearch).GetMethod("HideSearch")!,
  "GetCallerID"=>typeof(State).GetMethod("GetCallerID")!,
  "LaunchAppWithAUL"=>typeof(State).GetMethod("LaunchAppWithAUL")!,
  "Trigger"=>typeof(State).GetMethod("Trigger")!,
  "IsNullOrEmpty"=>typeof(string).GetMethod("IsNullOrEmpty",new[]{typeof(string)})!,
  _=>throw new Exception("Unreviewed call: "+reference.FullName)
 };
 foreach(var i in instructions) {
  il.MarkLabel(labels[i]);var op=ops[i.OpCode.Name];
  switch(i.Operand) {
   case null:il.Emit(op);break;
   case string value:il.Emit(op,value);break;
   case MethodReference reference:il.Emit(op,Target(reference));break;
   case FieldReference field when field.Name=="commonSearch":il.Emit(op,typeof(State).GetField("Search")!);break;
   case Mono.Cecil.Cil.Instruction target:il.Emit(op,labels[target]);break;
   default:throw new Exception("Unreviewed operand: "+i);
  }
 }
 return (Action)dynamic.CreateDelegate(typeof(Action));
}
if(args.Length!=1)throw new ArgumentException("Pass your locally generated patched CommonStore.dll; no Samsung binary is bundled.");
var repaired=Load(args[0]);
foreach(int count in new[]{1,2})foreach(string caller in new[]{"com.samsung.tv.bixby-promotion",""}) {
 State.Reset(count,caller);repaired();
 if(count==1) {
  if(State.Hidden!=1||State.Event!="EVENT_HIDE_APP"||State.Back!=0||State.Launched!=(caller==""?null:caller))throw new Exception("Invalid direct-search return behavior.");
 } else if(State.Back!=1||State.Hidden!=0||State.Event!=null||State.Launched!=null)throw new Exception("Store internal back must remain intact.");
}
Console.WriteLine("PASS: patched actual IL executes for direct/internal search and present/empty callers.");
public class FakeRouter {
 public static FakeRouter GetInstance()=>new();
 public int GetListCount()=>State.Count;
 public void Back()=>State.Back++;
}
public class FakeSearch {public void HideSearch()=>State.Hidden++;}
public static class State {
 public static FakeSearch Search=new();
 public static int Count,Back,Hidden;
 public static string Caller="";
 public static string? Event,Launched;
 public static string GetCallerID()=>Caller;
 public static void LaunchAppWithAUL(string id)=>Launched=id;
 public static void Trigger(string name,object? data)=>Event=name;
 public static void Reset(int count,string caller){Count=count;Caller=caller;Back=Hidden=0;Event=Launched=null;}
}
