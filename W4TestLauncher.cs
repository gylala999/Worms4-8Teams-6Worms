using System;
using System.IO;
using System.Text;
using System.Diagnostics;
using System.Drawing;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

internal static class TestFiles {
 public static string Lab { get { return Path.Combine(Profile.Root, "CodexTeamTest"); } }
 public static string Exe { get { return Path.Combine(Profile.Root, "WORMS 4 MAYHEM.EXE"); } }
 public static string Hash(string path) {
  using (var s = File.OpenRead(path)) using (var sha = SHA256.Create())
   return BitConverter.ToString(sha.ComputeHash(s)).Replace("-", "");
 }
 static string HashBytes(byte[] bytes){using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-","");}
 internal static string SettingsPath {get{return Path.Combine(Lab,Profile.Id+"-settings.txt");}}
 static string ActiveConfigPath {get{return Path.Combine(Lab,Profile.Id+"-active-config.txt");}}
 static byte[] ConfiguredScript(TeamConfig config){
  string template=Path.Combine(Lab,Profile.Modified[0]);
  if(Hash(template)!=Profile.ModifiedHash[0])throw new Exception("Configuration template hash mismatch.");
  return config.RenderBytes(File.ReadAllText(template));
 }
 static string AppliedHash(int i){return i==0 && File.Exists(ActiveConfigPath)?HashBytes(ConfiguredScript(TeamConfig.Parse(File.ReadAllText(ActiveConfigPath)))):Profile.ModifiedHash[i];}
 public static bool GameRunning() {
  foreach (var p in Process.GetProcesses()) {
   using (p) { try { if (p.ProcessName.Equals("WORMS 4 MAYHEM", StringComparison.OrdinalIgnoreCase)) return true; } catch (InvalidOperationException) {} }
  }
  return false;
 }
 public static void EnsureBackups() {
  if(!File.Exists(Exe))throw new Exception("Extract the friend-test ZIP into your Worms 4 Mayhem game folder first. WORMS 4 MAYHEM.EXE was not found beside this launcher.");
  if (Hash(Exe) != Profile.ExeHash) throw new Exception("Use the original checked Worms 4 Mayhem build 3013. This game executable is incompatible.");
  // Validate every source before creating any missing backup.
  for(int i=0;i<Profile.Rel.Length;i++) {
   if(Hash(Path.Combine(Lab,Profile.Modified[i]))!=Profile.ModifiedHash[i])throw new Exception("Friend-test payload mismatch: "+Profile.Modified[i]);
   string backup=Path.Combine(Lab,"Backups",Profile.Original[i]);
   if(File.Exists(backup)) {if(Hash(backup)!=Profile.OriginalHash[i])throw new Exception("Original backup mismatch: "+Profile.Original[i]);}
   else if(Hash(Path.Combine(Profile.Root,Profile.Rel[i]))!=Profile.OriginalHash[i])throw new Exception("A missing backup can only be created from original game data: "+Profile.Rel[i]);
  }
  Directory.CreateDirectory(Path.Combine(Lab,"Backups"));
  for(int i=0;i<Profile.Rel.Length;i++) {
   string backup=Path.Combine(Lab,"Backups",Profile.Original[i]);
   if(!File.Exists(backup))File.Copy(Path.Combine(Profile.Root,Profile.Rel[i]),backup,false);
   if(Hash(backup)!=Profile.OriginalHash[i])throw new Exception("Original backup verification failed: "+Profile.Original[i]);
  }
 }
 public static void Check() {
  EnsureBackups();
  if (Hash(Exe) != Profile.ExeHash) throw new Exception("Game executable differs from the checked build 3013.");
  if (Profile.NativeCachePatch) NativeTeamCache.CheckDisk(Exe);
  for (int i=0;i<Profile.Rel.Length;i++) {
   if (Hash(Path.Combine(Lab,"Backups",Profile.Original[i])) != Profile.OriginalHash[i]) throw new Exception("Original backup mismatch.");
   if (Hash(Path.Combine(Lab,Profile.Modified[i])) != Profile.ModifiedHash[i]) throw new Exception("Experimental profile mismatch.");
   string hash = Hash(Path.Combine(Profile.Root,Profile.Rel[i]));
   if (hash != Profile.OriginalHash[i] && hash != Profile.ModifiedHash[i] && hash != AppliedHash(i)) throw new Exception("Game data changed outside this test: " + Profile.Rel[i]);
  }
 }
 public static void Log(string message) {
  Directory.CreateDirectory(Lab);
  string line=DateTime.Now.ToString("s") + " " + message;
  File.AppendAllText(Path.Combine(Lab,"session.log"),line+Environment.NewLine);
  RunEvidence.Log(line);
 }
 public static void Restore() {
  if (GameRunning()) throw new Exception("Close Worms 4 before restoring its data files.");
  Check();
  for (int i=0;i<Profile.Rel.Length;i++) {
   string path=Path.Combine(Profile.Root,Profile.Rel[i]);
   if (Hash(path) != Profile.OriginalHash[i]) File.Copy(Path.Combine(Lab,"Backups",Profile.Original[i]),path,true);
   if (Hash(path) != Profile.OriginalHash[i]) throw new Exception("Restore verification failed: " + Profile.Rel[i]);
  }
  string marker=Path.Combine(Lab,"test-active.txt");
  if (File.Exists(marker)) File.Delete(marker);
  if (File.Exists(ActiveConfigPath)) File.Delete(ActiveConfigPath);
  Log("Original test data verified/restored.");
 }
 public static void Run(TeamConfig config,Action<string> report) {
  RunWithProcess(config,report,()=>{
   if(Profile.NativeCachePatch)return NativeTeamCache.RunBlocking(Exe,Profile.Root);
   using(var game=Process.Start(new ProcessStartInfo(Exe){WorkingDirectory=Profile.Root,UseShellExecute=false})) {
    if(game==null)throw new Exception("Game did not start.");game.WaitForExit();return game.ExitCode;
   }
  });
 }
 internal static void RunWithProcess(TeamConfig config,Action<string> report,Func<int> runProcess) {
  if(!Profile.Enabled)throw new Exception("This profile is disabled.");
  if(GameRunning())throw new Exception("Close the current Worms 4 game before starting this profile.");
  Check();Restore();
  byte[] script=ConfiguredScript(config);
  int? exitCode=null;bool restored=false;Exception failure=null;string warning=null;
  try {
   RunEvidence.Begin(config);
   config.Save(ActiveConfigPath);
   File.WriteAllText(Path.Combine(Lab,"test-active.txt"),"v33 eight teams; settings="+config.Encode()+"; total="+config.Total+". Close game, then use Restore if the launcher is interrupted.");
   for(int i=0;i<Profile.Rel.Length;i++) {
    if(i==0)File.WriteAllBytes(Path.Combine(Profile.Root,Profile.Rel[i]),script);
    else File.Copy(Path.Combine(Lab,Profile.Modified[i]),Path.Combine(Profile.Root,Profile.Rel[i]),true);
    if(Hash(Path.Combine(Profile.Root,Profile.Rel[i]))!=AppliedHash(i))throw new Exception("Profile verification failed.");
   }
   Log("Applied profile "+Profile.Id+"; teams="+config.Encode()+"; total="+config.Total);
   report(Profile.RunningText+"\r\n"+config.Total+" worms. Keep this launcher open; logs are saved for this run.");
   exitCode=runProcess();
   while(GameRunning())Thread.Sleep(500);
   Log("Game process ended. Exit code "+exitCode);
   if(exitCode!=0)failure=new Exception("Game crashed: 0x"+unchecked((uint)exitCode.Value).ToString("X8"));
  } catch(Exception x) {failure=x;}
  finally {
   // Log collection failures must never prevent original-data restoration.
   try{RunEvidence.CaptureGameLogs();}catch(Exception x){warning=x.Message;}
   try{Restore();restored=true;}catch(Exception x){failure=new Exception("Original-file restoration needs attention: "+x.Message,failure ?? x);}
   try{RunEvidence.Finish(exitCode,restored,failure,warning);}catch(Exception x){warning="Could not finish the test report: "+x.Message;}
  }
  string saved=RunEvidence.LatestPath;
  if(failure!=null)throw new Exception(failure.Message+"\r\n"+(restored?"Original files verified/restored. You can start another test.":"Use Restore original files after the game is closed.")+"\r\nLogs: "+saved+(warning==null?"":"\r\n"+warning),failure);
  report("Game closed. Original files verified/restored. You can start another test.\r\nLogs: "+saved+(warning==null?"":"\r\n"+warning));
 }

}

internal sealed class Launcher : Form {
 readonly Label state = new Label();
 readonly Button run = new Button();
 readonly Button restore = new Button();
 readonly Button logs = new Button();
 readonly Button export = new Button();
 readonly ComboBox[] modes=new ComboBox[8];
 readonly NumericUpDown[] counts=new NumericUpDown[8];
 readonly Label total=new Label();
 bool busy;
 public Launcher() {
  Text=Profile.Title;
  StartPosition=FormStartPosition.CenterScreen; FormBorderStyle=FormBorderStyle.FixedDialog; MaximizeBox=false;
  Font=new Font("Segoe UI",10); BackColor=Color.FromArgb(242,245,248);
  var title=new Label {Text=Profile.Heading,Font=new Font("Segoe UI",17,FontStyle.Bold),Location=new Point(22,18),Size=new Size(585,34)};
  int instructionHeight=TextRenderer.MeasureText(Profile.Instructions,Font,new Size(584,1000),TextFormatFlags.WordBreak|TextFormatFlags.TextBoxControl).Height+8;
  int rowY=65+instructionHeight+34,buttonY=rowY+8*34+75,stateY=buttonY+103;
  ClientSize=new Size(630,stateY+110);
  var instructions=new Label {Text=Profile.Instructions,Location=new Point(23,65),Size=new Size(584,instructionHeight)};
  Controls.Add(new Label{Text="Team",Location=new Point(23,rowY-25),Size=new Size(140,24)});
  Controls.Add(new Label{Text="Control",Location=new Point(185,rowY-25),Size=new Size(150,24)});
  Controls.Add(new Label{Text="Worms",Location=new Point(375,rowY-25),Size=new Size(90,24)});
  var settings=TeamConfig.Default();string settingsError=null;
  try{settings=TeamConfig.Load(TestFiles.SettingsPath);}catch(Exception x){settingsError=x.Message;}
  var colors=new[]{Color.Firebrick,Color.RoyalBlue,Color.ForestGreen,Color.DarkGoldenrod,Color.DarkOrange,Color.SaddleBrown,Color.DarkViolet,Color.DeepPink};
  for(int i=0;i<8;i++){
   int y=rowY+i*34;
   var label=new Label{Text="Team "+(i+1),ForeColor=colors[i],Font=new Font(Font,FontStyle.Bold),Location=new Point(23,y+3),Size=new Size(140,28)};
   modes[i]=new ComboBox{DropDownStyle=ComboBoxStyle.DropDownList,Location=new Point(185,y),Size=new Size(150,28)};
   modes[i].Items.AddRange(new object[]{"Human","CPU"});modes[i].SelectedIndex=settings.Skill[i]>0?1:0;
   counts[i]=new NumericUpDown{Minimum=1,Maximum=6,Value=settings.Count[i],Location=new Point(375,y),Size=new Size(80,28)};
   counts[i].ValueChanged+=(s,e)=>UpdateTotal();
   Controls.AddRange(new Control[]{label,modes[i],counts[i]});
  }
  total.Location=new Point(23,rowY+8*34+7);total.Size=new Size(584,25);total.Font=new Font(Font,FontStyle.Bold);Controls.Add(total);UpdateTotal();
  Controls.Add(new Label{Text="Known issue: surrender can leave teams controllable.",Location=new Point(23,rowY+8*34+35),Size=new Size(584,24)});
  run.Text=Profile.ButtonText;run.Location=new Point(23,buttonY);run.Size=new Size(220,42);run.Click+=(s,e)=>StartTest();
  run.Enabled=Profile.Enabled;
  restore.Text="Restore original files";restore.Location=new Point(260,buttonY);restore.Size=new Size(240,42);restore.Click+=(s,e)=>{try{TestFiles.Restore();state.Text="Original files verified/restored.";}catch(Exception x){state.Text=x.Message;}};
  logs.Text="Open test logs";logs.Location=new Point(23,buttonY+49);logs.Size=new Size(220,36);
  logs.Click+=(s,e)=>{try{string folder=RunEvidence.LatestPath ?? RunEvidence.ReportsRoot;Directory.CreateDirectory(folder);Process.Start(new ProcessStartInfo(folder){UseShellExecute=true});}catch(Exception x){state.Text=x.Message;}};
  export.Text="Make report ZIP";export.Location=new Point(260,buttonY+49);export.Size=new Size(240,36);
  export.Click+=(s,e)=>{try{string zip=RunEvidence.ExportLatest();state.Text="Report ZIP saved. You can send this file to the mod developer:\r\n"+zip;}catch(Exception x){state.Text=x.Message;}};
  state.Location=new Point(23,stateY);state.Size=new Size(584,100);state.Text="Stress test: extended mesh pool, per-run crash logs and retry after restoration.\r\nAfter a crash, wait for restoration, then start another test. Nothing is sent automatically.";
  if(settingsError!=null)state.Text=settingsError+" Default settings shown.";
  Controls.AddRange(new Control[]{title,instructions,run,restore,logs,export,state});
  FormClosing+=(s,e)=>{if(busy){e.Cancel=true;state.Text="Close the game first; the launcher must stay open to restore its files.";}};
 }
 void UpdateTotal(){int n=0;foreach(var c in counts)if(c!=null)n+=(int)c.Value;total.Text="Eight teams / "+n+" worms total / 100 health per worm";}
 void Report(string message) {if(!IsDisposed) BeginInvoke((Action)(()=>state.Text=message));}
 void StartTest() {
  var config=new TeamConfig();for(int i=0;i<8;i++){config.Skill[i]=modes[i].SelectedIndex==1?2:0;config.Count[i]=(int)counts[i].Value;}
  try{Directory.CreateDirectory(TestFiles.Lab);config.Save(TestFiles.SettingsPath);}catch(Exception x){state.Text=x.Message;return;}
  busy=true;run.Enabled=false;restore.Enabled=false;logs.Enabled=false;export.Enabled=false;
  for(int i=0;i<8;i++){modes[i].Enabled=false;counts[i].Enabled=false;}
  Task.Factory.StartNew(()=>{
   try { TestFiles.Run(config,Report); }
   catch(Exception x) {try{TestFiles.Log("ERROR "+x.Message);}catch{}Report(x.Message);}
   finally {BeginInvoke((Action)(()=>{busy=false;run.Enabled=Profile.Enabled;restore.Enabled=true;logs.Enabled=true;export.Enabled=true;for(int i=0;i<8;i++){modes[i].Enabled=true;counts[i].Enabled=true;}}));}
  });
 }
}

internal static class Program {
 [STAThread] static int Main(string[] args) {
  if(args.Length>0 && args[0]=="--inspect-team-queues") {File.WriteAllText(args[1],ManagerTeamLists.Report());return 0;}
  if(args.Length>0 && args[0]=="--inspect-hud-guards") {File.WriteAllText(args[1],NativeTeamCache.GuardCodeReport());return 0;}
  if(args.Length>0 && args[0]=="--check-native-code") {
   try {if(TestFiles.Hash(TestFiles.Exe)!=Profile.ExeHash) throw new Exception("Game executable hash mismatch.");NativeTeamCache.CheckDisk(TestFiles.Exe);Console.WriteLine("Native code locations verified.");return 0;}
   catch(Exception x){Console.Error.WriteLine(x.Message);return 2;}
  }
  bool created;
  using(var mutex=new Mutex(true,@"Local\CodexW4FiveTeamTest",out created)) {
   if(!created) return 4;
   if(args.Length>0) {
    try {if(args[0]=="--restore") TestFiles.Restore(); else if(args[0]=="--check") TestFiles.Check(); else if(args[0]=="--export-report") Console.WriteLine(RunEvidence.ExportLatest()); else return 3; Console.WriteLine("Verified."); return 0;}
    catch(Exception x){Console.Error.WriteLine(x.Message);return 2;}
   }
   Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
   Application.Run(new Launcher()); return 0;
  }
 }
}
