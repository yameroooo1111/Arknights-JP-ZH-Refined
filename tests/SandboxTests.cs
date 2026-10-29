using System;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;
using System.Text.Json;
class SandboxTests {
 static int count;
 static void Eq(object? a,object? b,string label){if(!Equals(a,b))throw new Exception(label+": "+a+" != "+b);count++;}
 static void Main(string[] args){
 string root=args[0];
 string[] dirs={Path.Combine(root,"repair-project/runtime/jp-zh-offline-final/BepInEx/core"),Path.Combine(root,"repair-project/runtime/jp-zh-offline-final/BepInEx/plugins/XUnity.AutoTranslator"),Path.Combine(root,"game-interop")};
 AssemblyLoadContext.Default.Resolving+=(c,n)=>{foreach(var d in dirs){var p=Path.Combine(d,n.Name+".dll");if(File.Exists(p))return c.LoadFromAssemblyPath(p);}return null;};
 var a=Assembly.LoadFrom(Path.Combine(root,"story-context-build/ArknightsLocalization.RichTextFix.dll"));var flags=BindingFlags.Static|BindingFlags.NonPublic;
 a.GetType("ArknightsLocalization.RichTextFix.ContextTranslations")!.GetMethod("Load",flags)!.Invoke(null,null);
 var lookup=a.GetType("ArknightsLocalization.RichTextFix.CatalogDisplayTranslations")!.GetMethod("Lookup",flags)!;
 using var data=JsonDocument.Parse(File.ReadAllText(Path.Combine(root,"sandbox-runtime-cases.json")));
 foreach(var p in data.RootElement.EnumerateObject()){
  string source=p.Name,target=p.Value.GetString()!;
  foreach(string suffix in new[]{""," (0/1)","\n(17/100)","（1/1）"}) Eq(lookup.Invoke(null,new object[]{"sandbox-display",source+suffix}),target+suffix,"progress suffix");
  Eq(lookup.Invoke(null,new object[]{"sandbox-display","<color=#ffffff>"+source+"</color> (0/1)"}),target+" (0/1)","rich wrapper");
  Eq(lookup.Invoke(null,new object[]{"identity",source}),null,"identity protected");
  Eq(lookup.Invoke(null,new object[]{"sandbox-display",source+" (Player/1)"}),null,"numeric progress only");
  Eq(lookup.Invoke(null,new object[]{"sandbox-display",source+"未知の追加条件"}),null,"no substring replacement");
 }
 var caption=a.GetType("ArknightsLocalization.RichTextFix.SandboxBaseLabels")!.GetMethod("CaptionIndex",flags)!;
 for(int i=1;i<=4;i++)Eq(caption.Invoke(null,new object[]{"basement_detail_state/panel_left/root_build ("+i+")/title_"+i+"/ime_title"}),i-1,"exact prefab path");
 Eq(caption.Invoke(null,new object[]{"other_page/panel_left/root_build (1)/title_1/ime_title"}),-1,"other pages excluded");
 var refresh=(MethodInfo)a.GetType("ArknightsLocalization.RichTextFix.SandboxBaseLabels")!.GetMethod("RefreshTarget",flags)!.Invoke(null,null)!;
 Eq(refresh.GetParameters().Length,1,"refresh overload is unambiguous");
 Eq(refresh.GetParameters()[0].ParameterType.Name,"SandboxV3BasementDetailViewProperty","native refresh property type");
 Eq(lookup.Invoke(null,new object[]{"sandbox-display","未知の条件 (0/1)"}),null,"unknown condition preserved");
 Console.WriteLine("Passed "+count+" sandbox compiled-plugin assertions. Visual confirmation pending.");
 }}