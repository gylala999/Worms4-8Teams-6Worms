using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
internal static class CheckArena64 {
 const uint Code=0x200000,Data=0x210000;
 sealed class Range {public uint Address;public int Size;public string Name;public Range(uint a,int s,string n){Address=a;Size=s;Name=n;}}
 static int Main(string[] args){try{
 var asm=Assembly.LoadFrom(args[0]);var cache=asm.GetType("NativeTeamCache",true);var ranges=new List<Range>();var native=new List<Range>();byte[] disk=File.ReadAllBytes(args[1]);
 foreach(object patch in (IEnumerable)cache.GetMethod("GetIntegratedPatches",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{Code,Data})){
  var t=patch.GetType();uint at=(uint)t.GetField("Address").GetValue(patch);byte[] expected=(byte[])t.GetField("Expected").GetValue(patch),replacement=(byte[])t.GetField("Replacement").GetValue(patch);
  if(expected.Length!=replacement.Length)throw new Exception("Patch size mismatch");
  if(at>=0x400000){for(int i=0;i<expected.Length;i++)if(disk[(int)(at-0x400000)+i]!=expected[i])throw new Exception("Original byte guard mismatch");native.Add(new Range(at,replacement.Length,"native hook"));}
  else ranges.Add(new Range(at,replacement.Length,"integrated patch"));
 }
 var bounds=(uint[][])cache.GetField("HudBounds",BindingFlags.NonPublic|BindingFlags.Static).GetValue(null);
 for(int i=0;i<bounds.Length;i++){uint at=Code+(uint)i*64;var b=(byte[])cache.GetMethod("HudGuardCode",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{bounds[i][0],bounds[i][1],at});ranges.Add(new Range(at,b.Length,"legacy HUD guard"));native.Add(new Range(bounds[i][0],7,"legacy HUD hook"));}
 foreach(var entry in new[]{Tuple.Create("CrateGuardCode",Code+512,new object[]{Code+512}),Tuple.Create("TopHealthGuardCode",Code+1664,new object[]{Code+1664,0x516b78u,0x516cdau})}){var b=(byte[])cache.GetMethod(entry.Item1,BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,entry.Item3);ranges.Add(new Range(entry.Item2,b.Length,entry.Item1));}
 native.Add(new Range(0x59e2a0,10,"crate hook"));native.Add(new Range(0x516b71,7,"legacy top HUD hook"));
 for(int i=0;i<4;i++)ranges.Add(new Range(Data+(uint)i*64,32,"team query cache"));
 for(int i=0;i<4;i++)ranges.Add(new Range(Data+2048+(uint)i*256,256,"worm query cache"));ranges.Add(new Range(Data+3072,512,"64-slot pair table"));
 ranges.Add(new Range(Data+3584,256,"CPU candidate pointers"));ranges.Add(new Range(Data+3840,64,"CPU pending flags"));
 var names=(byte[])asm.GetType("InventoryTeamDelays").GetMethod("NameData",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{Data+640});ranges.Add(new Range(Data+640,names.Length,"delay names"));
 foreach(var set in new[]{ranges,native}){set.Sort((a,b)=>a.Address.CompareTo(b.Address));uint end=0;foreach(var r in set){if(r.Address<end)throw new Exception("Overlapping patches at "+r.Address.ToString("X")+": "+r.Name);end=r.Address+(uint)r.Size;}}
 uint codeEnd=Code,dataEnd=Data;
 foreach(var r in ranges){uint end=r.Address+(uint)r.Size;if(r.Address>=Code && end<=Code+49152)codeEnd=Math.Max(codeEnd,end);else if(r.Address>=Data && end<=Data+4096)dataEnd=Math.Max(dataEnd,end);else throw new Exception("Arena overflow: "+r.Name);}
 Console.WriteLine("PASS "+ranges.Count+" allocated ranges do not overlap; code "+(codeEnd-Code)+"/49152, data "+(dataEnd-Data)+"/4096");
 Console.WriteLine("PASS "+native.Count+" original hook ranges do not overlap and match guarded disk bytes");return 0;
 }catch(Exception e){Console.Error.WriteLine(e);return 2;}}
}
