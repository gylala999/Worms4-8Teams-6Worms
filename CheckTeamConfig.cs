using System;using System.IO;
internal static class CheckTeamConfig {
 static int checks;static void Need(bool v,string s){if(!v)throw new Exception(s);checks++;Console.WriteLine("PASS "+s);}
 static void Reject(Action action,string s){bool failed=false;try{action();}catch{failed=true;}Need(failed,s);}
 static int Main(string[] args){try{
  string template=File.ReadAllText(args[0]),output=args[1];Directory.CreateDirectory(output);
  var scenarios=new[]{TeamConfig.Default(),TeamConfig.Parse("0:4,0:4,0:4,0:4,0:4,0:4,0:4,0:4"),TeamConfig.Parse("2:1,2:2,2:3,2:4,2:5,2:6,2:1,2:2"),TeamConfig.Parse("0:1,2:6,0:2,2:5,0:3,2:4,0:6,2:1"),TeamConfig.Parse("0:1,0:1,0:1,0:1,0:1,0:1,0:1,0:1"),TeamConfig.Parse("2:6,2:6,2:6,2:6,2:6,2:6,2:6,2:6")};
  for(int s=0;s<scenarios.Length;s++){
   var c=scenarios[s];string path=Path.Combine(output,"scenario"+s+".txt");c.Save(path);var restored=TeamConfig.Load(path);
   Need(restored.Encode()==c.Encode() && restored.Total==c.Total,"saved configuration round trips scenario "+s);
   string rendered=c.Render(template);Need(rendered==restored.Render(template),"active configuration reproduces exactly the installed script for restoration "+s);
   Need(rendered.Substring(rendered.IndexOf('\n',rendered.IndexOf("Codex_TeamConfig = ")))==template.Substring(template.IndexOf('\n',template.IndexOf("Codex_TeamConfig = "))),"configuration preserves all script logic following its typed settings "+s);
   File.WriteAllBytes(Path.Combine(output,"scenario"+s+".lua"),c.RenderBytes(template));
  }
  Need(scenarios[0].Render(template)==template,"default settings preserve the checked template byte for byte");
  foreach(string bad in new[]{"","0:6","0:6,0:6,0:6,0:6,0:6,0:6,0:6","0:6,0:6,0:6,0:6,0:6,0:6,0:6,0:6,0:6","0:0,0:6,0:6,0:6,0:6,0:6,0:6,0:6","0:7,0:6,0:6,0:6,0:6,0:6,0:6,0:6","1:6,0:6,0:6,0:6,0:6,0:6,0:6,0:6","0:06,0:6,0:6,0:6,0:6,0:6,0:6,0:6","0:6;os.exit(),0:6,0:6,0:6,0:6,0:6,0:6,0:6"})Reject(()=>TeamConfig.Parse(bad),"malformed settings rejected: "+bad);
  Reject(()=>scenarios[0].Render(template.Replace(TeamConfig.Marker,"")),"missing template marker rejected");Reject(()=>scenarios[0].Render(template+TeamConfig.Marker),"duplicate template marker rejected");
  scenarios[0].Count[0]=100;Reject(()=>scenarios[0].Render(template),"out-of-range programmatic count rejected");
  Need(!File.Exists(Path.Combine(output,"missing.txt")) && TeamConfig.Load(Path.Combine(output,"missing.txt")).Total==48,"missing saved settings use valid defaults without writing files");
  Console.WriteLine("RESULT: "+checks+" configuration assertions passed.");return 0;
 }catch(Exception e){Console.Error.WriteLine(e);return 2;}}
}
