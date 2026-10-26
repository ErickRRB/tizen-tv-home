using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text.Json;
using Tizen.NUI;
using Tizen.NUI.BaseComponents;
using Tizen.Applications;

// Native APIs and the package-container approach are adapted from Quiet Tizen.
// No network client, package-registry writes, or consent changes.
public sealed class CleanHome : NUIApplication
{
 const string Root="/opt/usr/apps/com.samsung.tv.bixby-promotion/res/quiet-home/";
 const string Self="com.samsung.tv.bixby-promotion";
 readonly List<View> views=new List<View>();
 readonly List<List<Card>> rows=new List<List<Card>>();
 List<Entry> apps=new List<Entry>();
 List<Source> sources=new List<Source>();
 Config config;
 int row=1,col,appPage,lastSource=-1;
 bool libraryView;
 bool refreshing;
 int sceneBuilds,resumeRequests;
 double catalogUiMs,catalogWorkerMs,sourceReadMs;bool catalogCacheUsed;
 const string CatalogCache="/opt/usr/home/owner/apps_rw/com.samsung.tv.bixby-promotion/data/clean-home-catalog-cache.json";
 int nativeAppCount,uiThreadId,metadataThreadId;


 string[] windowHints=Array.Empty<string>();
 string windowEffect="not-set";
 int effectStarts,effectEnds;
 Tizen.NUI.Timer resumeRefreshTimer;
 const string PrefFile="/opt/usr/home/owner/apps_rw/com.samsung.tv.bixby-promotion/data/clean-home-layout.json";
 LayoutPreferences preferences=new LayoutPreferences();
 static readonly string[] DefaultHidden={Self,"com.samsung.tv.AICompanionUILauncher","com.samsung.tv.kmxdashboard","25emanual3.emanual","com.samsung.tv.SmartThingsApp","com.samsung.tv.SecurityCenter"};
 public sealed class LayoutPreferences { public List<string> Order{get;set;}=new List<string>(); public Dictionary<string,string> Names{get;set;}=new Dictionary<string,string>(); public List<string> Hidden{get;set;}=new List<string>(); }
 readonly List<View> overlay=new List<View>();
 readonly List<View> menuButtons=new List<View>();
 bool menuOpen,renaming,okHeld,longHandled,ignoreEnterUntilUp;
 int sourceOffset,sourceCursor,sourcesDirty;
 Tizen.NUI.Timer sourceEventsTimer;
 SourceEventCallback sourceEventCallback;
 bool sourceWatchRegistered;
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate void SourceEventCallback(int eventCode,IntPtr data);
 [DllImport("libsource-api.so")] static extern void sourceapi_register_source_api_cb(SourceEventCallback cb,IntPtr data);
 [DllImport("libsource-api.so")] static extern void sourceapi_unregister_source_api_cb(IntPtr data);
 [DllImport("libc.so.6")] static extern void free(IntPtr pointer);
 string selectedAppId,movingId;
 List<Entry> orderBeforeMove;
 int menuIndex;
 Tizen.NUI.Timer holdTimer;
 TextField renameField;

 [DllImport("libfeedback.so.0")] static extern int feedback_initialize();
 [DllImport("libfeedback.so.0")] static extern int feedback_deinitialize();
 [DllImport("libfeedback.so.0")] static extern int feedback_is_supported_pattern(int type,int pattern,out bool supported);
 [DllImport("libfeedback.so.0")] static extern int feedback_play_type(int type,int pattern);
 bool soundReady;int soundResult;double soundCallMs,keyUpMs,launchSendStartMs,launchSendEndMs;
 static double MonotonicMs()=>Stopwatch.GetTimestamp()*1000d/Stopwatch.Frequency;
 void InitializeFeedback(){try{soundResult=feedback_initialize();if(soundResult==0){bool supported;soundReady=feedback_is_supported_pattern(1,65,out supported)==0&&supported;}}catch{soundReady=false;}}
 void PlayFeedback(int pattern){if(!soundReady)return;double start=MonotonicMs();try{soundResult=feedback_play_type(1,pattern);}catch{soundReady=false;}soundCallMs=MonotonicMs()-start;}
 void PressApp(bool pressed){if(!IsAppCard())return;float scale=pressed?1.04f:1.12f;rows[row][col].View.Scale=new Vector3(scale,scale,1);}
 const string VolumeGuideKey="memory/volume-app/key_guide_status";
 int volumeGuideAttempted;string volumeGuideStatus="not-checked";int volumeGuideReadResult,volumeGuideWriteResult=int.MinValue,volumeGuideWorkerThreadId,volumeGuideCompletionThreadId;double volumeGuideCallMs;bool volumeGuideWriteAttempted;
 sealed class VolumeGuideResult {public string Status;public int ReadResult,WriteResult=int.MinValue,ThreadId;public bool WriteAttempted;public double CallMs;}
 static VolumeGuideResult UpdateVolumeGuide(){
  var result=new VolumeGuideResult{ThreadId=Environment.CurrentManagedThreadId};double start=MonotonicMs();
  try{
   bool alreadySeen;result.ReadResult=Tizen.TV.System.Vconf.GetBool(VolumeGuideKey,out alreadySeen);
   if(result.ReadResult!=0){result.Status="read-failed";return result;}
   if(alreadySeen){result.Status="already-seen";return result;}
   result.WriteAttempted=true;result.WriteResult=Tizen.TV.System.Vconf.SetBool(VolumeGuideKey,true);
   if(result.WriteResult!=0){result.Status="write-failed";return result;}
   bool confirmed;result.ReadResult=Tizen.TV.System.Vconf.GetBool(VolumeGuideKey,out confirmed);
   result.Status=result.ReadResult==0&&confirmed?"marked-seen":"verification-failed";
  }catch(Exception ex){result.Status="unavailable:"+ex.GetBaseException().GetType().Name;}
  finally{result.CallMs=MonotonicMs()-start;}
  return result;
 }
 async void RememberVolumeGuideOnce(){
  if(System.Threading.Interlocked.CompareExchange(ref volumeGuideAttempted,1,0)!=0)return;
  try{
   var result=await Task.Run(UpdateVolumeGuide);
   volumeGuideWriteAttempted=result.WriteAttempted;volumeGuideStatus=result.Status;volumeGuideReadResult=result.ReadResult;volumeGuideWriteResult=result.WriteResult;
   volumeGuideWorkerThreadId=result.ThreadId;volumeGuideCallMs=result.CallMs;volumeGuideCompletionThreadId=Environment.CurrentManagedThreadId;
  }catch(Exception ex){volumeGuideStatus="unavailable:"+ex.GetBaseException().GetType().Name;}
 }
 string refreshError="";
 TextLabel message;
 readonly Color ink=new Color(.12f,.14f,.2f,1), background=new Color(.025f,.025f,.03f,1);
 sealed class Card { public View View; public string Action; public bool Selected; }
 public sealed class Entry { public string Id{get;set;} public string Name{get;set;} public string Icon{get;set;} }
 public sealed class Config { public List<Entry> Apps{get;set;} public List<string> BaselineIds{get;set;} }
 public sealed class Source { public int sourceType{get;set;} public string editName{get;set;} public string detectedName{get;set;} public bool connectionState{get;set;} }
 [DllImport("libsource-api.so")] static extern IntPtr get_source_list();
 [DllImport("libsource-api.so")] static extern int connect_source(int source);
 [DllImport("libvconf.so.0")] static extern int vconf_get_int(string key,out int value);
 [DllImport("libcapi-appfw-app-control.so.0")] static extern int app_control_create(out IntPtr c);
 [DllImport("libcapi-appfw-app-control.so.0")] static extern int app_control_set_app_id(IntPtr c,string id);
 [DllImport("libcapi-appfw-app-control.so.0")] static extern int app_control_set_operation(IntPtr c,string operation);
 [DllImport("libcapi-appfw-app-control.so.0")] static extern int app_control_add_extra_data(IntPtr c,string key,string value);
 [DllImport("libcapi-appfw-app-control.so.0")] static extern int app_control_send_launch_request(IntPtr c,IntPtr callback,IntPtr data);
 [DllImport("libcapi-appfw-app-control.so.0")] static extern int app_control_destroy(IntPtr c);
 protected override void OnCreate()
 {
  base.OnCreate();uiThreadId=Environment.CurrentManagedThreadId;RememberVolumeGuideOnce();
  try{if(File.Exists(PrefFile))preferences=JsonSerializer.Deserialize<LayoutPreferences>(File.ReadAllText(PrefFile))??new LayoutPreferences();}catch{}
  config=JsonSerializer.Deserialize<Config>(File.ReadAllText(Root+"catalog.json"));
  apps=config.Apps.Where(a=>!IsHidden(a.Id)).ToList();
  try{if(File.Exists(CatalogCache)){var cached=JsonSerializer.Deserialize<List<Entry>>(File.ReadAllText(CatalogCache));if(cached!=null&&cached.Count>0){apps=OrderApps(cached);catalogCacheUsed=true;}}}catch{}

  try{FontClient.Instance.AddCustomFontDirectory(Root+"fonts");}catch{}
  Window.Default.Title="Inicio"; Window.Default.BackgroundColor=background;
  Window.Default.KeyEvent+=OnKey;
  try{var hints=new List<string>();uint count=Window.Default.GetSupportedAuxiliaryHintCount();for(uint i=0;i<Math.Min(count,256u);i++)hints.Add(Window.Default.GetSupportedAuxiliaryHint(i));windowHints=hints.ToArray();if(windowHints.Contains("wm.policy.win.effect.disable")){uint hint=Window.Default.AddAuxiliaryHint("wm.policy.win.effect.disable","1");windowEffect=hint==0?"disable-request-failed":"disable-requested:"+hint+":"+Window.Default.GetAuxiliaryHintValue(hint);}}catch(Exception e){windowEffect=e.Message;}
  Window.Default.TransitionEffect+=(sender,e)=>{if(e.State==Window.EffectState.Start)effectStarts++;if(e.State==Window.EffectState.End)effectEnds++;};
  RefreshSources(); Build(); InitializeFeedback(); if(!catalogCacheUsed)RefreshApps();
  sourceEventCallback=(code,data)=>System.Threading.Interlocked.Exchange(ref sourcesDirty,1);
  try{sourceapi_register_source_api_cb(sourceEventCallback,new IntPtr(0x514e));sourceWatchRegistered=true;}catch{}
  sourceEventsTimer=new Tizen.NUI.Timer(500);sourceEventsTimer.Tick+=(sender,args)=>{if(!menuOpen&&movingId==null&&!okHeld&&System.Threading.Interlocked.Exchange(ref sourcesDirty,0)!=0){string before=JsonSerializer.Serialize(sources);RefreshSources();if(before!=JsonSerializer.Serialize(sources))Build();}return true;};sourceEventsTimer.Start();
 }
 protected override void OnResume(){base.OnResume();RememberVolumeGuideOnce();sourceEventsTimer?.Start();if(config!=null){if(!menuOpen&&movingId==null)ScheduleResumeRefresh();}}
 protected override void OnAppControlReceived(AppControlReceivedEventArgs e){base.OnAppControlReceived(e);if(config!=null){string action;if(e.ReceivedAppControl.ExtraData.TryGet("clean_action",out action)&&action=="search"){Launch("com.samsung.tv.store","search");return;}if(!menuOpen&&movingId==null)ScheduleResumeRefresh();}}
 protected override void OnPause(){resumeRefreshTimer?.Stop();sourceEventsTimer?.Stop();StopHold();ignoreEnterUntilUp=false;base.OnPause();}
 protected override void OnTerminate(){if(soundReady)feedback_deinitialize();resumeRefreshTimer?.Stop();resumeRefreshTimer?.Dispose();sourceEventsTimer?.Stop();sourceEventsTimer?.Dispose();if(sourceWatchRegistered)sourceapi_unregister_source_api_cb(new IntPtr(0x514e));StopHold();base.OnTerminate();}
 void ScheduleResumeRefresh()
 {
  resumeRequests++;
  try{Window.Default.KeepRendering(.65f);}catch(Exception e){refreshError=e.Message;}
  if(resumeRefreshTimer==null){
   resumeRefreshTimer=new Tizen.NUI.Timer(1000);
   resumeRefreshTimer.Tick+=(sender,args)=>{
    if(!menuOpen&&movingId==null&&!okHeld){
     string before=JsonSerializer.Serialize(sources);RefreshSources();
     if(before!=JsonSerializer.Serialize(sources))Build();
     RefreshApps();
    }
    WriteState("resume-refresh");return false;
   };
  }
  resumeRefreshTimer.Stop();resumeRefreshTimer.Start();
 }
 void RefreshSources()
 {
  var sourceClock=Stopwatch.StartNew();
  try {
   IntPtr p=get_source_list();
   if(p==IntPtr.Zero)throw new InvalidOperationException("No se pudieron leer las entradas.");
   List<Source> found;try{found=JsonSerializer.Deserialize<List<Source>>(Marshal.PtrToStringAnsi(p));}finally{free(p);}
   int? focused=sources.Count>sourceCursor?sources[sourceCursor].sourceType:null;
   sources=found.Where(s=>s.sourceType==0||(s.sourceType>=13&&s.sourceType<=16)).GroupBy(s=>s.sourceType).Select(g=>g.First()).OrderBy(s=>s.sourceType==0?100:s.sourceType).ToList();
   sourceCursor=focused.HasValue?Math.Max(0,sources.FindIndex(s=>s.sourceType==focused.Value)):0;
   if(sources.Count>0)sourceOffset=Navigation.ShiftWindow(sources.Count,sourceOffset,sourceCursor,4,1);
   int current;if(vconf_get_int("memory/system/source_type",out current)==0&&sources.Any(s=>s.sourceType==current))lastSource=current;
  }catch(Exception e){refreshError=e.Message;}finally{sourceReadMs=sourceClock.Elapsed.TotalMilliseconds;}
 }
 sealed class CatalogSnapshot {public List<Entry> Apps;public int Count,ThreadId;public double WorkerMs;}
 List<Entry> OrderApps(IEnumerable<Entry> values)
 {
  string[] favorites={"Netflix","YouTube","Prime Video","Disney+","Crunchyroll","Apple TV"};
  return values.Where(a=>!IsHidden(a.Id)).OrderBy(a=>{int custom=preferences.Order.IndexOf(a.Id);if(custom>=0)return custom;int index=Array.FindIndex(favorites,n=>a.Name.Equals(n,StringComparison.OrdinalIgnoreCase));return 1000+(index<0?100:index);}).ToList();
 }
 Task<CatalogSnapshot> ReadCatalogSnapshot()
 {
  // Capture only plain configuration on the UI thread. Native handles remain on the worker.
  var seeds=config.Apps.Where(a=>!IsHidden(a.Id)).ToArray();
  var seedById=seeds.GroupBy(a=>a.Id).ToDictionary(g=>g.Key,g=>g.First());
  var baseline=new HashSet<string>(config.BaselineIds);
  var hidden=new HashSet<string>(DefaultHidden.Concat(preferences.Hidden));
  return Task.Run(async()=>{
   var clock=Stopwatch.StartNew();
   var installed=(await ApplicationManager.GetInstalledApplicationsAsync().ConfigureAwait(false)).ToList();
   int threadId=Environment.CurrentManagedThreadId;
   var matched=new Dictionary<string,Entry>();var extra=new List<Entry>();var seen=new HashSet<string>();
   try {
    foreach(var info in installed){
     string id=info.ApplicationId;if(!seen.Add(id)||hidden.Contains(id))continue;
     if(seedById.TryGetValue(id,out var seed))matched[id]=new Entry{Id=id,Name=seed.Name,Icon=string.IsNullOrEmpty(seed.Icon)?info.IconPath:seed.Icon};
     else if(!baseline.Contains(id)&&!info.IsNoDisplay){string label=info.Label;extra.Add(new Entry{Id=id,Name=string.IsNullOrWhiteSpace(label)?id:label,Icon=info.IconPath});}
    }
   }finally{foreach(var info in installed)info.Dispose();}
   var next=seeds.Where(a=>matched.ContainsKey(a.Id)).Select(a=>matched[a.Id]).Concat(extra.OrderBy(a=>a.Name)).ToList();
   if(next.Count>0)try{
    string data=JsonSerializer.Serialize(next);
    if(!File.Exists(CatalogCache)||File.ReadAllText(CatalogCache)!=data){Directory.CreateDirectory(System.IO.Path.GetDirectoryName(CatalogCache));File.WriteAllText(CatalogCache+".new",data);File.Move(CatalogCache+".new",CatalogCache,true);}
   }catch{} // A failed optional cache must not prevent app discovery.
   return new CatalogSnapshot{Apps=next,Count=installed.Count,ThreadId=threadId,WorkerMs=clock.Elapsed.TotalMilliseconds};
  });
 }
 async void RefreshApps()
 {
  if(refreshing||menuOpen||movingId!=null||okHeld)return;refreshing=true;
  try {
   var snapshot=await ReadCatalogSnapshot();
   var clock=Stopwatch.StartNew();nativeAppCount=snapshot.Count;metadataThreadId=snapshot.ThreadId;catalogWorkerMs=snapshot.WorkerMs;
   if(snapshot.Apps.Count==0)throw new InvalidOperationException("No se pudo actualizar la lista; se conserva la copia local.");
   if(menuOpen||movingId!=null||okHeld)return;
   var updated=OrderApps(snapshot.Apps);bool changed=JsonSerializer.Serialize(apps)!=JsonSerializer.Serialize(updated)||refreshError!="";
   apps=updated;refreshError="";if(changed)Build();catalogUiMs=clock.Elapsed.TotalMilliseconds;
  }catch(Exception e){refreshError=e.Message;WriteState("apps-refresh-failed");}
  finally{refreshing=false;}
 }
 TextLabel Label(string text,int x,int y,int w,int h,float size,Color color)
 {
  return new TextLabel{Text=text,Position2D=new Position2D(x,y),Size2D=new Size2D(w,h),PixelSize=size,TextColor=color,FontFamily="Quiet Inter Medium",Ellipsis=true,VerticalAlignment=VerticalAlignment.Center};
 }
 void Add(View view){views.Add(view);Window.Default.Add(view);}
 void Title(string text,int x,int y,int w,float size){Add(Label(text,x,y,w,48,size,ink));}
 void Button(List<Card> target,string title,string subtitle,string action,int x,int y,int w,int h,string icon=null)
 {
  var v=new View{Position2D=new Position2D(x,y),Size2D=new Size2D(w,h),PivotPoint=new Position(.5f,.5f,.5f),PositionUsesPivotPoint=false,BackgroundColor=new Color(1,1,1,.85f),CornerRadius=new Vector4(22,22,22,22),BorderlineWidth=4,BorderlineColor=new Color(0,0,0,0)};
  v.Name=title;v.AccessibilityName=title;
  if(!string.IsNullOrEmpty(icon))v.Add(new ImageView{ResourceUrl=Root+icon,Position2D=new Position2D((w-34)/2,(h-34)/2),Size2D=new Size2D(34,34)});
  else {var label=Label(title,12,0,w-24,h,25,ink);label.HorizontalAlignment=HorizontalAlignment.Center;v.Add(label);}
  Add(v);target.Add(new Card{View=v,Action=action});
 }
 void AppCard(List<Card> target,Entry app,int x,int y,int w,int h)
 {
  var v=new View{Position2D=new Position2D(x+(w-h)/2,y),Size2D=new Size2D(h,h),PivotPoint=new Position(.5f,.5f,.5f),PositionUsesPivotPoint=false,BackgroundColor=Color.Transparent,CornerRadius=new Vector4(18,18,18,18),BorderlineWidth=4,BorderlineColor=new Color(0,0,0,0)};
  v.Name=DisplayName(app);v.AccessibilityName=DisplayName(app);
  string icon=app.Id=="OGLLvqej7u.CrunchyrollWebApp"?Root+"app-crunchyroll-vector.svg":app.Id=="nbCEREFaoy.mercadoplay"?Root+"app-mercado-play-centered.png":app.Icon;
  if(!File.Exists(icon??""))icon=app.Icon;
  if(!string.IsNullOrEmpty(icon)&&File.Exists(icon))v.Add(new ImageView{ResourceUrl=icon,Position2D=new Position2D(0,0),Size2D=new Size2D(h,h),CornerRadius=new Vector4(16,16,16,16),FittingMode=FittingModeType.ShrinkToFit});
  else {var text=Label(DisplayName(app),8,0,h-16,h,25,ink);text.HorizontalAlignment=HorizontalAlignment.Center;v.Add(text);}
  Add(v);var name=Label(DisplayName(app),x,y+h+12,w,42,23,Color.White);name.HorizontalAlignment=HorizontalAlignment.Center;Add(name);
  target.Add(new Card{View=v,Action="app:"+app.Id});
 }
 void SourceCard(List<Card> target,Source source,int x,int y,int w,int h)
 {
  string name=source.sourceType==0?"TV":Uri.UnescapeDataString(!string.IsNullOrWhiteSpace(source.editName)?source.editName:source.detectedName??"");
  string port=source.sourceType==0?"Antena / cable":"HDMI "+(source.sourceType-12);
  if(string.IsNullOrWhiteSpace(name))name=port;
  string illustration=source.sourceType==0?"input-tv-fit.svg":name.Contains("Nintendo",StringComparison.OrdinalIgnoreCase)?"input-switch-hires.svg":name.Contains("PC",StringComparison.OrdinalIgnoreCase)?"input-pc-fit.svg":name.Contains("Google",StringComparison.OrdinalIgnoreCase)?"input-google-fit.svg":"input-hdmi-fit.svg";
  var v=new View{Position2D=new Position2D(x,y),Size2D=new Size2D(w,h),PivotPoint=new Position(.5f,.5f,.5f),PositionUsesPivotPoint=false,CornerRadius=new Vector4(24,24,24,24),BorderlineWidth=5,BorderlineColor=new Color(0,0,0,0)};
  v.Add(new ImageView{ResourceUrl=Root+illustration,Size2D=new Size2D(w,h),CornerRadius=new Vector4(24,24,24,24),FittingMode=FittingModeType.Fill});
  Add(v);Add(Label(name,x,y+h+14,w,46,30,Color.White));Add(Label(port+(source.connectionState?"":" · Sin conexión"),x,y+h+60,w,34,22,new Color(.9f,.91f,.94f,1)));
  target.Add(new Card{View=v,Action="source:"+source.sourceType});
 }
 void Build()
 {
  sceneBuilds++;
  foreach(var v in views){Window.Default.Remove(v);v.Dispose();} views.Clear();rows.Clear();
  Add(new ImageView{DesiredWidth=1920,DesiredHeight=1080,ResourceUrl=File.Exists(Root+"background.jpg")?Root+"background.jpg":Root+"background-default.svg",Size2D=new Size2D(1920,1080),FittingMode=FittingModeType.ScaleToFill});
  Add(new View{Size2D=new Size2D(1920,1080),BackgroundColor=new Color(0,0,0,.28f)});
  var nav=new List<Card>();rows.Add(nav);
  Button(nav,"Buscar apps",null,"search:",1596,42,72,68,"nav-search.svg");
  Button(nav,"Ajustes",null,"app:org.tizen.menu",1690,42,72,68,"nav-settings.svg");
  Button(nav,"Inicio Samsung",null,"app:com.samsung.tv.csfs",1784,42,72,68,"nav-home.svg");
  int pageSize=10;
  int pages=Math.Max(1,(apps.Count+pageSize-1)/pageSize);appPage=Math.Clamp(appPage,0,pages-1);
  if(!libraryView) {
   var inputs=new List<Card>();
   int visibleCount=Math.Min(4,sources.Count);int width=(1792-Math.Max(0,visibleCount-1)*24)/Math.Max(1,visibleCount);
   for(int i=0;i<visibleCount;i++)SourceCard(inputs,sources[(sourceOffset+i)%sources.Count],64+i*(width+24),164,width,290);
   if(row==1&&sources.Count>0)col=(sourceCursor-sourceOffset+sources.Count)%sources.Count;
   if(inputs.Count>0)rows.Add(inputs);
  }
  var visible=apps.Skip(appPage*pageSize).Take(pageSize).ToList();
  for(int r=0;r<1;r++) {
   var cards=new List<Card>();
   for(int c=0;c<10;c++) {
    int n=c;if(n>=visible.Count)break;
    AppCard(cards,visible[n],86+c*176,772,164,136);
   }
   if(cards.Count>0)rows.Add(cards);
  }
  message=Label(refreshError,64,1040,1792,30,18,new Color(.43f,.46f,.52f,1));Add(message);
  row=Math.Clamp(row,0,rows.Count-1);col=Math.Clamp(col,0,rows[row].Count-1);Focus();
 }
 void Focus(){
  if(!libraryView&&row==1&&rows.Count>1&&col<rows[1].Count&&rows[1][col].Action.StartsWith("source:"))sourceCursor=sources.FindIndex(x=>"source:"+x.sourceType==rows[1][col].Action);
  for(int r=0;r<rows.Count;r++)for(int c=0;c<rows[r].Count;c++){
   var card=rows[r][c];bool selected=r==row&&c==col;
   if(card.Selected==selected&&!selected)continue;card.Selected=selected;
   card.View.BorderlineColor=selected?new Color(.2f,.31f,.65f,1):new Color(0,0,0,0);
   float scale=selected?(card.Action.StartsWith("source:")?1.035f:1.12f):1f;card.View.Scale=new Vector3(scale,scale,1);
  }
 }
 bool IsHidden(string id)=>DefaultHidden.Contains(id)||preferences.Hidden.Contains(id);
 string DisplayName(Entry app)=>preferences.Names.TryGetValue(app.Id,out string name)&&!string.IsNullOrWhiteSpace(name)?name:app.Name;
 bool IsAppCard()=>rows.Count>row&&rows[row].Count>col&&rows[row][col].Action.StartsWith("app:")&&apps.Any(a=>"app:"+a.Id==rows[row][col].Action);
 void SaveLayout()
 {
  preferences.Order=apps.Select(a=>a.Id).ToList();
  Directory.CreateDirectory(System.IO.Path.GetDirectoryName(PrefFile));
  if(File.Exists(PrefFile))File.Copy(PrefFile,PrefFile+".before-"+DateTime.UtcNow.ToString("yyyyMMddHHmmssfff"));
  File.WriteAllText(PrefFile+".new",JsonSerializer.Serialize(preferences));File.Move(PrefFile+".new",PrefFile,true);
 }
 void StopHold(){holdTimer?.Stop();holdTimer?.Dispose();holdTimer=null;okHeld=false;}
 void OnKey(object sender,Window.KeyEventArgs e)
 {
  if(e.Key==null)return;string key=e.Key.KeyPressedName;
  bool enter=key=="Return"||key=="KP_Enter";
  if(enter&&ignoreEnterUntilUp){if(e.Key.State==Key.StateType.Up)ignoreEnterUntilUp=false;return;}
  if(e.Key.State==Key.StateType.Up){if(enter&&okHeld){keyUpMs=MonotonicMs();PressApp(false);bool launch=!longHandled;string action="app:"+selectedAppId;StopHold();if(launch){try{Activate(action);}catch(Exception ex){message.Text=ex.Message;WriteState("error");}}}return;}
  if(e.Key.State!=Key.StateType.Down)return;
  if(key=="Left"||key=="Right"||key=="Up"||key=="Down")PlayFeedback(65);
  try {
   if(menuOpen){HandleMenu(key);return;}
   if(movingId!=null){HandleMove(key);return;}
   if(enter&&IsAppCard()){
    if(okHeld)return;PressApp(true);PlayFeedback(69);okHeld=true;longHandled=false;selectedAppId=rows[row][col].Action.Substring(4);
    holdTimer=new Tizen.NUI.Timer(650);holdTimer.Tick+=(s,a)=>{longHandled=true;ignoreEnterUntilUp=true;OpenMenu();return false;};holdTimer.Start();return;
   }
   if(enter){PlayFeedback(69);Activate(rows[row][col].Action);return;}
   StopHold();
   if(!libraryView&&row==1&&sources.Count>0&&(key=="Right"||key=="Left")){
    int oldOffset=sourceOffset;int direction=key=="Right"?1:-1;sourceCursor=Navigation.CircularStep(sources.Count,sourceCursor,direction);sourceOffset=Navigation.ShiftWindow(sources.Count,sourceOffset,sourceCursor,4,direction);col=(sourceCursor-sourceOffset+sources.Count)%sources.Count;if(oldOffset==sourceOffset)Focus();else Build();return;
   }
   if((key=="Right"||key=="Left")&&IsAppCard()){
    int current=apps.FindIndex(a=>"app:"+a.Id==rows[row][col].Action);
    int next=Navigation.Step(apps.Count,current,key=="Right"?1:-1);FocusApp(next);return;
   }
   switch(key) {
    case "Left":col=Math.Max(0,col-1);break;
    case "Right":col=Math.Min(rows[row].Count-1,col+1);break;
    case "Up":row=Math.Max(0,row-1);col=Math.Min(col,rows[row].Count-1);break;
    case "Down":row=Math.Min(rows.Count-1,row+1);col=Math.Min(col,rows[row].Count-1);break;
    case "XF86Back":case "Back":case "Escape":case "XF86Exit":if(libraryView){libraryView=false;appPage=0;row=1;col=0;Build();}else if(lastSource>=0)SwitchSource(lastSource);else Launch("org.tizen.tv-viewer");return;
    case "XF86Home":row=1;col=0;Focus();ScheduleResumeRefresh();return;
   }
   Focus();
  }catch(Exception ex){message.Text=ex.Message;WriteState("error");}
 }
 void FocusApp(int absolute)
 {
  if(apps.Count==0)return;int size=10;int oldPage=appPage;appPage=absolute/size;int local=absolute%size;
  row=!libraryView&&sources.Count>0?2:1;col=local;if(oldPage==appPage&&movingId==null)Focus();else Build();
 }
 void AddOverlay(View view){overlay.Add(view);Window.Default.Add(view);}
 void CloseMenu()
 {
  if(renameField!=null){renameField.GetInputMethodContext()?.HideInputPanel();FocusManager.Instance.ClearFocus();renameField=null;}
  foreach(var v in overlay){Window.Default.Remove(v);v.Dispose();}overlay.Clear();menuButtons.Clear();menuOpen=false;renaming=false;
 }
 void OpenMenu()
 {
  StopHold();menuOpen=true;menuIndex=0;renaming=false;
  AddOverlay(new View{Size2D=new Size2D(1920,1080),BackgroundColor=new Color(0,0,0,.34f)});
  AddOverlay(new View{Position2D=new Position2D(535,265),Size2D=new Size2D(850,550),BackgroundColor=Color.White,CornerRadius=new Vector4(26,26,26,26)});
  AddOverlay(Label(DisplayName(apps.Single(a=>a.Id==selectedAppId)),580,290,760,70,34,ink));
  string[] labels={"Mover","Cambiar nombre","Ocultar del launcher","Cerrar"};
  for(int i=0;i<labels.Length;i++){
   var v=new View{Position2D=new Position2D(580,390+i*90),Size2D=new Size2D(760,74),CornerRadius=new Vector4(14,14,14,14)};v.Add(Label(labels[i],20,0,720,74,28,ink));AddOverlay(v);menuButtons.Add(v);
  }
  UpdateMenuFocus();WriteState("context-menu");
 }
 void UpdateMenuFocus(){for(int i=0;i<menuButtons.Count;i++)menuButtons[i].BackgroundColor=i==menuIndex?new Color(.84f,.89f,.98f,1):Color.White;}
 void HandleMenu(string key)
 {
  if(renaming){
   if(key=="Return"||key=="KP_Enter"){string value=renameField.Text.Trim();if(value.Length>0&&value.Length<=40){preferences.Names[selectedAppId]=value;SaveLayout();CloseMenu();Build();}}
   else if(key=="XF86Back"||key=="Back"||key=="Escape"){CloseMenu();Build();}return;
  }
  if(key=="Up")menuIndex=Math.Max(0,menuIndex-1);
  else if(key=="Down")menuIndex=Math.Min(menuButtons.Count-1,menuIndex+1);
  else if(key=="XF86Back"||key=="Back"||key=="Escape"){CloseMenu();Build();return;}
  else if(key=="Return"||key=="KP_Enter"){
   int choice=menuIndex;string id=selectedAppId;
   if(choice==0){CloseMenu();movingId=id;orderBeforeMove=new List<Entry>(apps);FocusApp(apps.FindIndex(a=>a.Id==id));message.Text="Flechas para mover · OK para guardar · Atrás para cancelar";return;}
   if(choice==1){string name=DisplayName(apps.Single(a=>a.Id==id));CloseMenu();menuOpen=true;renaming=true;
    AddOverlay(new View{Size2D=new Size2D(1920,1080),BackgroundColor=new Color(0,0,0,.4f)});
    AddOverlay(new View{Position2D=new Position2D(450,220),Size2D=new Size2D(1020,280),BackgroundColor=Color.White,CornerRadius=new Vector4(24,24,24,24)});
    AddOverlay(Label("Nombre en tu launcher",500,240,920,70,30,ink));
    renameField=new TextField{Text=name,Position2D=new Position2D(500,330),Size2D=new Size2D(920,95),PixelSize=32,TextColor=ink,BackgroundColor=new Color(.91f,.93f,.97f,1),Focusable=true};
    AddOverlay(renameField);FocusManager.Instance.SetCurrentFocusView(renameField);renameField.GetInputMethodContext()?.Activate();renameField.GetInputMethodContext()?.ShowInputPanel();return;
   }
   if(choice==2){preferences.Hidden.Add(id);apps.RemoveAll(a=>a.Id==id);SaveLayout();}
   CloseMenu();Build();return;
  }
  UpdateMenuFocus();
 }
 void HandleMove(string key)
 {
  int current=apps.FindIndex(a=>a.Id==movingId);
  if(key=="Left"||key=="Right"){
   int next=Navigation.Step(apps.Count,current,key=="Right"?1:-1);var entry=apps[current];apps.RemoveAt(current);apps.Insert(next,entry);FocusApp(next);message.Text="Flechas para mover · OK para guardar · Atrás para cancelar";
  }else if(key=="Return"||key=="KP_Enter"){SaveLayout();movingId=null;orderBeforeMove=null;Build();}
  else if(key=="XF86Back"||key=="Back"||key=="Escape"){apps=orderBeforeMove;movingId=null;orderBeforeMove=null;FocusApp(apps.FindIndex(a=>a.Id==selectedAppId));}
 }
 void Activate(string action)
 {
  if(action.StartsWith("source:")){SwitchSource(int.Parse(action.Substring(7)));return;}
  if(action.StartsWith("search:")){Launch("com.samsung.tv.store","search",action.Substring(7));return;}
  if(action.StartsWith("app:")){Launch(action.Substring(4));return;}
  if(action=="store-settings"){Launch("com.samsung.tv.store","settings");return;}
  if(action=="library"){libraryView=!libraryView;appPage=0;row=1;col=0;Build();return;}
  if(action=="previous")appPage=Math.Max(0,appPage-1);
  if(action=="next")appPage=Math.Min(Math.Max(0,(apps.Count-1)/(libraryView?12:6)),appPage+1);
  if(action=="refresh"){RefreshSources();RefreshApps();}
  Build();
 }
 void SwitchSource(int source)
 {
  int result=connect_source(source);WriteState("source:"+source+":"+result);
  if(result<0){message.Text="No se pudo cambiar la entrada. Revisá la conexión.";return;}
  lastSource=source;Launch("org.tizen.tv-viewer");
 }
 bool launchInProgress;int launchWorkerThreadId,launchCompletionThreadId;string launchBoostStatus="not-requested";double launchBoostMs;
 static Tizen.TV.System.Boost.Boost startupBoost;static bool boostUnavailable;
 sealed class LaunchDispatch {public int Result,ThreadId;public double StartMs,EndMs,BoostMs;public string BoostStatus;}
 static void RequestStartupBoost(string id,LaunchDispatch dispatch){
  if(boostUnavailable){dispatch.BoostStatus="unavailable";return;}
  double start=MonotonicMs();
  try{
   startupBoost??=new Tizen.TV.System.Boost.Boost();
   var info=new Tizen.TV.System.Boost.BoostCsfsInfo{
    parent=new Tizen.TV.System.Boost.BoostInfo{frameworkType=Tizen.TV.System.Boost.BoostTypeFramework.BOOST_CSFS},
    csfsEvent=Tizen.TV.System.Boost.BoostCsfsEvent.BOOST_CSFS_EVENT_USER_REQUEST_START,boostingTimeMs=0u};
   startupBoost.BoostRequestByAppId(id,Environment.ProcessId,out info);
   dispatch.BoostStatus="accepted";
  }catch(Exception ex){boostUnavailable=true;dispatch.BoostStatus="unavailable:"+ex.GetBaseException().GetType().Name;}
  finally{dispatch.BoostMs=MonotonicMs()-start;}
 }
 static LaunchDispatch SendLaunch(string id,string submenu){
  IntPtr control;int result=app_control_create(out control);if(result!=0)throw new InvalidOperationException("No se pudo preparar la apertura: "+result);
  try{
   result=app_control_set_app_id(control,id);
   if(result==0)result=app_control_set_operation(control,"http://tizen.org/appcontrol/operation/default");
   if(result==0&&submenu!=null)result=app_control_add_extra_data(control,"Sub_Menu",submenu);
   var dispatch=new LaunchDispatch{Result=result,ThreadId=Environment.CurrentManagedThreadId,StartMs=MonotonicMs()};
   if(result==0){RequestStartupBoost(id,dispatch);dispatch.StartMs=MonotonicMs();dispatch.Result=app_control_send_launch_request(control,IntPtr.Zero,IntPtr.Zero);}
   dispatch.EndMs=MonotonicMs();return dispatch;
  }finally{app_control_destroy(control);}
 }
 async void Launch(string id,string submenu=null,string keyword=null){
  if(launchInProgress)return;launchInProgress=true;
  try{
   Window.Default.KeepRendering(.65f);
   var dispatch=await Task.Run(()=>SendLaunch(id,submenu));
   launchBoostStatus=dispatch.BoostStatus;launchBoostMs=dispatch.BoostMs;launchWorkerThreadId=dispatch.ThreadId;launchCompletionThreadId=Environment.CurrentManagedThreadId;launchSendStartMs=dispatch.StartMs;launchSendEndMs=dispatch.EndMs;
   WriteState("launch:"+id+":"+submenu+":"+dispatch.Result);
   if(dispatch.Result!=0)throw new InvalidOperationException("No se pudo abrir la app: "+dispatch.Result);
  }catch(Exception ex){if(message!=null)message.Text=ex.Message;WriteState("launch-failed:"+id);}
  finally{launchInProgress=false;}
 }
 void WriteState(string activity){try{File.WriteAllText("/tmp/quiet-home-state.json",JsonSerializer.Serialize(new{activity,design="quiet-home-v27-public",volumeGuideWriteAttempted,volumeGuideStatus,volumeGuideReadResult,volumeGuideWriteResult,volumeGuideCallMs,volumeGuideWorkerThreadId,volumeGuideCompletionThreadId,launchBoostStatus,launchBoostMs,launchWorkerThreadId,launchCompletionThreadId,soundReady,soundResult,soundCallMs,keyUpMs,launchSendStartMs,launchSendEndMs,catalogCacheUsed,catalogWorkerMs,catalogUiMs,sourceReadMs,nativeAppCount,uiThreadId,metadataThreadId,sceneBuilds,resumeRequests,windowHints,windowEffect,effectStarts,effectEnds,libraryView,row,col,appPage,sourceOffset,sourceCursor,sourceWatchRegistered,menuOpen,movingId,apps=apps.Count,app_ids=apps.Select(a=>a.Id).ToArray(),sources=sources.Select(s=>s.sourceType).ToArray(),refreshError}));}catch{}}
 public static void Main(string[] args){using(var app=new CleanHome())app.Run(args);}
}
