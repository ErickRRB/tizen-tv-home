using Mono.Cecil;
using Mono.Cecil.Cil;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using System.Text.Json;
if(args.Length!=2)throw new ArgumentException("Expected original and new output path.");
byte[] original=File.ReadAllBytes(args[0]);
if(Convert.ToHexString(SHA256.HashData(original)).ToLowerInvariant()!="0c1813fa0f596797cbe5dc68cfea19a7fc45d1d08ea222b445c94648be4d6989")throw new InvalidOperationException("Unreviewed original.");
using var module=ModuleDefinition.ReadModule(args[0]);using var pe=new PEReader(new MemoryStream(original));
int Offset(int rva){var s=pe.PEHeaders.SectionHeaders.Single(s=>rva>=s.VirtualAddress&&rva<s.VirtualAddress+Math.Max(s.VirtualSize,s.SizeOfRawData));return s.PointerToRawData+rva-s.VirtualAddress;}
int CodeStart(MethodDefinition method){int o=Offset(method.RVA);int format=original[o]&3;return format==2?o+1:format==3?o+(((BitConverter.ToUInt16(original,o)>>12)&15)*4):throw new InvalidOperationException("Unknown header.");}
var types=All(module.Types).ToList();var target=types.Single(t=>t.Name=="SearchAllView").Methods.Single(m=>m.Name=="HideSearchHandler");
if(target.Body.ExceptionHandlers.Count!=0||target.Body.MaxStackSize<2)throw new InvalidOperationException("Unsupported body.");
var caller=types.Single(t=>t.Name=="CommonFunctions").Methods.Single(m=>m.Name=="GetCallerID");
var launch=types.Single(t=>t.Name=="WasWrapper").Methods.Single(m=>m.Name=="LaunchAppWithAUL");
var refs=types.SelectMany(t=>t.Methods).Where(m=>m.HasBody).SelectMany(m=>m.Body.Instructions).Select(i=>i.Operand).OfType<MethodReference>();
var empty=refs.First(m=>m.DeclaringType.FullName=="System.String"&&m.Name=="IsNullOrEmpty");
var trigger=refs.First(m=>m.DeclaringType.Name=="EventMediator"&&m.Name=="Trigger"&&m.Parameters.Count==2&&m.Parameters[1].ParameterType.FullName=="System.Object");
uint hideString=0;
foreach(var method in types.SelectMany(t=>t.Methods).Where(m=>m.HasBody)){
 var instruction=method.Body.Instructions.FirstOrDefault(i=>i.OpCode==OpCodes.Ldstr&&(string?)i.Operand=="EVENT_HIDE_APP");
 if(instruction!=null){hideString=BitConverter.ToUInt32(original,CodeStart(method)+instruction.Offset+1);break;}
}
if(hideString==0)throw new InvalidOperationException("No existing string token.");
var routeRefs=target.Body.Instructions.Select(i=>i.Operand).OfType<MethodReference>().ToList();
var getRouter=routeRefs.First(m=>m.Name=="GetInstance");var countRouter=routeRefs.First(m=>m.Name=="GetListCount");var backRouter=routeRefs.First(m=>m.Name=="Back");
var searchField=types.Single(t=>t.Name=="SearchAllView").Fields.Single(f=>f.Name=="commonSearch");var hideSearch=refs.First(m=>m.DeclaringType.Name=="CommonSearch"&&m.Name=="HideSearch"&&m.Parameters.Count==0);if(hideSearch.ReturnType.FullName!="System.Void")throw new InvalidOperationException("Unexpected return type");
var code=new List<byte>();void Token(byte opcode,uint token){code.Add(opcode);code.AddRange(BitConverter.GetBytes(token));}
Token(0x28,getRouter.MetadataToken.ToUInt32());Token(0x6f,countRouter.MetadataToken.ToUInt32());code.Add(0x17);code.Add(0x33);int backBranch=code.Count;code.Add(0);
Token(0x7e,searchField.MetadataToken.ToUInt32());Token(0x6f,hideSearch.MetadataToken.ToUInt32());
Token(0x72,hideString);code.Add(0x14);Token(0x28,trigger.MetadataToken.ToUInt32());
Token(0x28,caller.MetadataToken.ToUInt32());code.Add(0x25);Token(0x28,empty.MetadataToken.ToUInt32());
code.AddRange(new byte[]{0x2d,7});Token(0x28,launch.MetadataToken.ToUInt32());code.AddRange(new byte[]{0x2b,1,0x26});code.Add(0x2a);
int backStart=code.Count;Token(0x28,getRouter.MetadataToken.ToUInt32());Token(0x6f,backRouter.MetadataToken.ToUInt32());code.Add(0x2a);code[backBranch]=checked((byte)(backStart-backBranch-1));
int start=CodeStart(target),length=target.Body.CodeSize;if(code.Count>length)throw new InvalidOperationException("No room.");
byte[] patched=(byte[])original.Clone();Array.Clear(patched,start,length);code.CopyTo(patched,start);patched[start+length-1]=0x2a;
// Preserve the full PE metadata, MVID, tokens, tables and all other method bodies.
for(int i=0;i<original.Length;i++)if((i<start||i>=start+length)&&original[i]!=patched[i])throw new InvalidOperationException("Change outside reviewed method.");
Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(args[1]))!);if(File.Exists(args[1]))throw new IOException("Refuse overwrite.");File.WriteAllBytes(args[1],patched);
using var verify=ModuleDefinition.ReadModule(args[1]);var checkedMethod=All(verify.Types).Single(t=>t.Name=="SearchAllView").Methods.Single(m=>m.Name=="HideSearchHandler");
if(verify.Mvid!=module.Mvid||checkedMethod.Body.CodeSize!=length||checkedMethod.Body.Instructions[0].OpCode!=OpCodes.Call||checkedMethod.Body.Instructions.Last().OpCode!=OpCodes.Ret)throw new InvalidOperationException("Verification failed.");
File.WriteAllText(Path.Combine(Path.GetDirectoryName(Path.GetFullPath(args[1]))!,"manifest.json"),JsonSerializer.Serialize(new{method=target.FullName,offset=start,length,metadata_and_other_bytes_unchanged=true,patched_sha256=Convert.ToHexString(SHA256.HashData(patched)).ToLowerInvariant()},new JsonSerializerOptions{WriteIndented=true}));
Console.WriteLine("Verified: only original search-close IL bytes changed; PE metadata and all other bytes preserved.");
static IEnumerable<TypeDefinition> All(IEnumerable<TypeDefinition> ts){foreach(var t in ts){yield return t;foreach(var n in All(t.NestedTypes))yield return n;}}
