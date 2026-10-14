using System;using System.IO;using System.IO.Compression;
internal static class CheckFriendLauncher {
 static int checks;
 static void Need(bool value,string text){if(!value)throw new Exception(text);checks++;Console.WriteLine("PASS "+text);}
 static void Reject(Action action,string text){bool failed=false;try{action();}catch{failed=true;}Need(failed,text);}
 static void Original(){for(int i=0;i<Profile.Rel.Length;i++)Need(TestFiles.Hash(Path.Combine(Profile.Root,Profile.Rel[i]))==Profile.OriginalHash[i],"original restored "+Profile.Rel[i]);Need(!File.Exists(Path.Combine(TestFiles.Lab,"test-active.txt")) && !File.Exists(Path.Combine(TestFiles.Lab,Profile.Id+"-active-config.txt")),"active markers removed");}
 static void Apply(TeamConfig c){
  c.Save(Path.Combine(TestFiles.Lab,Profile.Id+"-active-config.txt"));
  File.WriteAllText(Path.Combine(TestFiles.Lab,"test-active.txt"),"private fixture");
  for(int i=0;i<Profile.Rel.Length;i++){
   string target=Path.Combine(Profile.Root,Profile.Rel[i]);
   if(i==0)File.WriteAllBytes(target,c.RenderBytes(File.ReadAllText(Path.Combine(TestFiles.Lab,Profile.Modified[i]))));
   else File.Copy(Path.Combine(TestFiles.Lab,Profile.Modified[i]),target,true);
  }
 }
 static int Main(string[] args){try{
  if(!Profile.Root.EndsWith("portable-restore-fixture"))throw new Exception("Unsafe fixture path");
  string game=args[0],source=args[1];Directory.CreateDirectory(TestFiles.Lab);
  File.Copy(Path.Combine(game,"WORMS 4 MAYHEM.EXE"),TestFiles.Exe,false);
  for(int i=0;i<Profile.Rel.Length;i++){
   string target=Path.Combine(Profile.Root,Profile.Rel[i]);Directory.CreateDirectory(Path.GetDirectoryName(target));
   // Read checked vanilla files, or a checked backup from an earlier launcher run.
   string original=Path.Combine(game,Profile.Rel[i]);
   if(!File.Exists(original) || TestFiles.Hash(original)!=Profile.OriginalHash[i])original=Path.Combine(game,"CodexTeamTest","Backups",Profile.Original[i]);
   if(TestFiles.Hash(original)!=Profile.OriginalHash[i])throw new Exception("Fixture original hash mismatch: "+Profile.Rel[i]);
   File.Copy(original,target,false);
   File.Copy(Path.Combine(source,Profile.Modified[i]),Path.Combine(TestFiles.Lab,Profile.Modified[i]),false);
  }
  // Incompatible first use must reject before creating original backups.
  string payload=Path.Combine(TestFiles.Lab,Profile.Modified[4]);byte[] checkedPayload=File.ReadAllBytes(payload);
  File.AppendAllText(payload,"bad");Reject(()=>TestFiles.Check(),"bad payload rejects first use");
  Need(!Directory.Exists(Path.Combine(TestFiles.Lab,"Backups")),"bad first use creates no backups");
  File.WriteAllBytes(payload,checkedPayload);
  TestFiles.Check();Original();
  for(int i=0;i<Profile.Rel.Length;i++)Need(TestFiles.Hash(Path.Combine(TestFiles.Lab,"Backups",Profile.Original[i]))==Profile.OriginalHash[i],"first use creates checked local backup "+i);
  var config=TeamConfig.Parse("0:1,2:6,0:2,2:5,0:3,2:4,0:6,2:1");
  File.WriteAllText(Path.Combine(Profile.Root,"XOM.log"),"BEFORE-FIRST-RUN");
  bool called=false;string crashDir=null;
  Reject(()=>TestFiles.RunWithProcess(config,s=>{},()=>{
   called=true;TestFiles.Check();crashDir=RunEvidence.ActivePath;
   Need(TeamConfig.Load(Path.Combine(crashDir,"team-settings.txt")).Encode()==config.Encode(),"crash report records exact team settings");
   Need(TestFiles.Hash(Path.Combine(Profile.Root,Profile.Rel[0]))==HashBytes(config.RenderBytes(File.ReadAllText(Path.Combine(TestFiles.Lab,Profile.Modified[0])))),"callback sees configured active Lua");
   File.WriteAllText(RunEvidence.CrashPath,"SIMULATED NATIVE CRASH CONTEXT");
   File.WriteAllText(Path.Combine(Profile.Root,"XOM.log"),"CRASH-RUN-XOM");File.WriteAllText(Path.Combine(Profile.Root,"gliLog.txt"),"CRASH-RUN-GLI");
   return unchecked((int)0xC0000005u);
  }),"game crash is reported after restoration");
  Need(called,"crashing test reached process callback");Original();
  Need(File.ReadAllText(Path.Combine(crashDir,"before-XOM.log"))=="BEFORE-FIRST-RUN","before log archived separately");
  Need(File.ReadAllText(Path.Combine(crashDir,"XOM.log"))=="CRASH-RUN-XOM","crash game log saved");
  Need(File.ReadAllText(Path.Combine(crashDir,"gliLog.txt"))=="CRASH-RUN-GLI","graphics log saved");
  Need(File.ReadAllText(Path.Combine(crashDir,"result.txt")).Contains("0xC0000005") && File.ReadAllText(Path.Combine(crashDir,"result.txt")).Contains("restored: True"),"crash exit and verified restoration recorded");
  Need(File.Exists(Path.Combine(crashDir,"launcher-error.txt")),"launcher error archived");
  Need(!File.Exists(Path.Combine(TestFiles.Lab,Profile.Id+"-failed.txt")),"crash does not disable next test");
  string crashZip=RunEvidence.ExportLatest();
  using(var zip=ZipFile.OpenRead(crashZip)){
   Need(zip.GetEntry(Profile.Id+"-crash.txt")!=null && zip.GetEntry("team-settings.txt")!=null && zip.GetEntry("result.txt")!=null,"report ZIP contains crash, configuration and restoration evidence");
   Need(zip.GetEntry("WORMS 4 MAYHEM.EXE")==null && zip.GetEntry("stdvs.original.lua")==null,"report ZIP excludes game executable and original assets");
  }
  string successDir=null;TestFiles.RunWithProcess(TeamConfig.Default(),s=>{},()=>{
   successDir=RunEvidence.ActivePath;Need(successDir!=crashDir,"retry gets a distinct report folder");
   File.WriteAllText(Path.Combine(Profile.Root,"XOM.log"),"SUCCESS-RUN-XOM");return 0;
  });Original();
  Need(RunEvidence.ActivePath==null && RunEvidence.LatestPath==successDir,"report state cleared and latest run retained");
  Need(File.ReadAllText(Path.Combine(crashDir,Profile.Id+"-crash.txt"))=="SIMULATED NATIVE CRASH CONTEXT","retry preserves earlier native crash details");
  Need(File.ReadAllText(Path.Combine(crashDir,"XOM.log"))=="CRASH-RUN-XOM" && File.Exists(crashZip),"retry preserves earlier logs and ZIP");
  Need(File.ReadAllText(Path.Combine(successDir,"result.txt")).Contains("0x00000000"),"normal exit saved separately");
  Reject(()=>TestFiles.RunWithProcess(config,s=>{},()=>{throw new Exception("SIMULATED START FAILURE");}),"launch failure is reported");Original();
  Need(File.ReadAllText(Path.Combine(RunEvidence.LatestPath,"launcher-error.txt")).Contains("SIMULATED START FAILURE"),"launch failure gets its own report");
  TestFiles.RunWithProcess(config,s=>{},()=>{RunEvidence.Snapshot(Path.Combine(Profile.Root,"XOM.log"),"XOM.log");return 0;});Original();
  Need(File.ReadAllText(Path.Combine(RunEvidence.LatestPath,"result.txt")).Contains("Report warning:"),"log collection failure recorded while originals still restore");
  Apply(config);TeamConfig.Default().Save(TestFiles.SettingsPath);TestFiles.Restore();Original();
  Need(TeamConfig.Load(TestFiles.SettingsPath).Total==48,"interrupted recovery uses applied config and preserves UI settings");
  Apply(config);string active=Path.Combine(Profile.Root,Profile.Rel[0]);File.AppendAllText(active,"-- outside edit");string unknown=TestFiles.Hash(active);
  Reject(()=>TestFiles.Restore(),"unknown active edit rejects restoration");
  Need(TestFiles.Hash(active)==unknown && TestFiles.Hash(Path.Combine(Profile.Root,Profile.Rel[1]))==Profile.ModifiedHash[1],"unknown edit rejection precedes all original-file overwrites");
  File.WriteAllBytes(active,config.RenderBytes(File.ReadAllText(Path.Combine(TestFiles.Lab,Profile.Modified[0]))));TestFiles.Restore();Original();
  string pointer=Path.Combine(RunEvidence.ReportsRoot,"last-run.txt"),saved=File.ReadAllText(pointer);File.WriteAllText(pointer,"..\\outside");
  Reject(()=>{var unused=RunEvidence.LatestPath;},"report pointer cannot escape reports folder");File.WriteAllText(pointer,saved);
  Console.WriteLine("RESULT: "+checks+" production friend-launcher assertions passed; no game process launched.");return 0;
 }catch(Exception e){Console.Error.WriteLine(e);return 2;}}
 static string HashBytes(byte[] bytes){using(var sha=System.Security.Cryptography.SHA256.Create())return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-","");}
}
