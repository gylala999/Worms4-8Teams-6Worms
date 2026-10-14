using System;
using System.IO;
using System.Text;
using System.IO.Compression;

internal static class RunEvidence {
 internal static string ActivePath;
 internal static string ReportsRoot { get { return Path.Combine(TestFiles.Lab,"Reports",Profile.Id); } }
 internal static string CrashPath { get { return Path.Combine(ActivePath ?? TestFiles.Lab,Profile.Id+"-crash.txt"); } }
 internal static string LatestPath {
  get {
   if(ActivePath!=null)return ActivePath;
   string pointer=Path.Combine(ReportsRoot,"last-run.txt");
   if(!File.Exists(pointer))return null;
   string name=File.ReadAllText(pointer).Trim();
   if(name.Length==0 || name!=Path.GetFileName(name) || name=="." || name=="..")throw new Exception("Invalid test report folder.");
   string path=Path.Combine(ReportsRoot,name);
   return Directory.Exists(path)?path:null;
  }
 }
 internal static void Begin(TeamConfig config) {
  if(ActivePath!=null)throw new Exception("A test report is already active.");
  Directory.CreateDirectory(ReportsRoot);
  string name=DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff")+"-"+Guid.NewGuid().ToString("N").Substring(0,8);
  ActivePath=Path.Combine(ReportsRoot,name);Directory.CreateDirectory(ActivePath);
  File.WriteAllText(Path.Combine(ReportsRoot,"last-run.txt"),name,new UTF8Encoding(false));
  config.Save(Path.Combine(ActivePath,"team-settings.txt"));
  var text=new StringBuilder();
  text.AppendLine("Worms 4 eight-team stress test v33");
  text.AppendLine("Confirmed v31 gameplay/payloads plus the guarded 550-to-4096 mesh-pool request correction.");
  text.AppendLine("Started UTC: "+DateTime.UtcNow.ToString("o"));
  text.AppendLine("Teams: "+config.Encode());text.AppendLine("Total worms: "+config.Total);
  text.AppendLine("CPU difficulty: Skill 2; initial health: 100 per worm");
  text.AppendLine("Game executable SHA256: "+TestFiles.Hash(TestFiles.Exe));
  text.AppendLine("Launcher SHA256: "+TestFiles.Hash(System.Reflection.Assembly.GetExecutingAssembly().Location));
  for(int i=0;i<Profile.Modified.Length;i++)text.AppendLine(Profile.Modified[i]+" SHA256: "+TestFiles.Hash(Path.Combine(TestFiles.Lab,Profile.Modified[i])));
  File.WriteAllText(Path.Combine(ActivePath,"test-info.txt"),text.ToString(),new UTF8Encoding(false));
  File.WriteAllText(Path.Combine(ActivePath,"tester-notes.txt"),"Please add the map, rules/Wormpot, what happened just before a crash, eliminated teams, and whether another test reproduced it.\r\n",new UTF8Encoding(false));
  Snapshot(Path.Combine(Profile.Root,"XOM.log"),"before-XOM.log");
 }
 internal static void Log(string message) {
  if(ActivePath!=null)File.AppendAllText(Path.Combine(ActivePath,"session.log"),message+Environment.NewLine,new UTF8Encoding(false));
 }
 internal static void Snapshot(string source,string name) {
  if(ActivePath==null || !File.Exists(source))return;
  using(var input=new FileStream(source,FileMode.Open,FileAccess.Read,FileShare.ReadWrite|FileShare.Delete))
  using(var output=new FileStream(Path.Combine(ActivePath,name),FileMode.CreateNew,FileAccess.Write,FileShare.Read))input.CopyTo(output);
 }
 internal static void CaptureGameLogs() {
  Snapshot(Path.Combine(Profile.Root,"XOM.log"),"XOM.log");
  Snapshot(Path.Combine(Profile.Root,"gliLog.txt"),"gliLog.txt");
 }
 internal static void Finish(int? exitCode,bool restored,Exception error,string warning) {
  if(ActivePath==null)return;
  try {
   var result=new StringBuilder();result.AppendLine("Finished UTC: "+DateTime.UtcNow.ToString("o"));
   result.AppendLine("Exit code: "+(exitCode.HasValue?"0x"+unchecked((uint)exitCode.Value).ToString("X8"):"not available"));
   result.AppendLine("Original files verified/restored: "+restored);
   result.AppendLine("Result: "+(error==null && exitCode==0?"normal game exit; gameplay result requires tester confirmation":"failed or interrupted test"));
   if(warning!=null)result.AppendLine("Report warning: "+warning);
   File.WriteAllText(Path.Combine(ActivePath,"result.txt"),result.ToString(),new UTF8Encoding(false));
   if(error!=null)File.WriteAllText(Path.Combine(ActivePath,"launcher-error.txt"),error.ToString(),new UTF8Encoding(false));
  } finally {ActivePath=null;}
 }
 internal static string ExportLatest() {
  if(ActivePath!=null)throw new Exception("Close the game and wait for restoration before exporting a report.");
  string source=LatestPath;if(source==null)throw new Exception("No test report has been saved yet.");
  string zip=source+"-"+Guid.NewGuid().ToString("N").Substring(0,8)+".zip";
  ZipFile.CreateFromDirectory(source,zip,CompressionLevel.Optimal,false);
  return zip;
 }
}
