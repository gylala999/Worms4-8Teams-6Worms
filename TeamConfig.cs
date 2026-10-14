using System;using System.IO;using System.Text;
internal sealed class TeamConfig {
 internal readonly int[] Skill=new int[8],Count=new int[8];
 internal const string Marker="Codex_TeamConfig = {{0,6},{2,6},{0,6},{0,6},{0,6},{0,6},{0,6},{0,6}}";
 internal static TeamConfig Default(){return Parse("0:6,2:6,0:6,0:6,0:6,0:6,0:6,0:6");}
 internal static TeamConfig Parse(string text){
  if(text==null)throw new Exception("Missing team settings.");var rows=text.Trim().Split(',');
  if(rows.Length!=8)throw new Exception("Settings must contain eight teams.");var config=new TeamConfig();
  for(int i=0;i<8;i++){var pair=rows[i].Split(':');
   if(pair.Length!=2 || (pair[0]!="0" && pair[0]!="2") || pair[1].Length!=1 || pair[1][0]<'1' || pair[1][0]>'6')throw new Exception("Each team needs Human/CPU and 1-6 worms.");
   config.Skill[i]=pair[0]=="2"?2:0;config.Count[i]=pair[1][0]-'0';}
  return config;
 }
 internal int Total {get{int n=0;foreach(int v in Count)n+=v;return n;}}
 internal string Encode(){var rows=new string[8];for(int i=0;i<8;i++)rows[i]=Skill[i]+":"+Count[i];string text=string.Join(",",rows);Parse(text);return text;}
 internal string Render(string template){
  Encode();int first=template.IndexOf(Marker,StringComparison.Ordinal);
  if(first<0 || template.IndexOf(Marker,first+Marker.Length,StringComparison.Ordinal)>=0)throw new Exception("Team configuration template mismatch.");
  var rows=new string[8];for(int i=0;i<8;i++)rows[i]="{"+Skill[i]+","+Count[i]+"}";
  return template.Substring(0,first)+"Codex_TeamConfig = {"+string.Join(",",rows)+"}"+template.Substring(first+Marker.Length);
 }
 internal byte[] RenderBytes(string template){return new UTF8Encoding(false,true).GetBytes(Render(template));}
 internal static TeamConfig Load(string path){return File.Exists(path)?Parse(File.ReadAllText(path)):Default();}
 internal void Save(string path){File.WriteAllText(path,Encode(),new UTF8Encoding(false));}
}
