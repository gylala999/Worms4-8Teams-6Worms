using System;
using System.Collections.Generic;
using System.IO;

// Four 64-entry resource caches for the separate v31 local launcher.
// Includes initializers, reset and reverse-order cleanup.
internal static class NativeWormCaches64 {
 internal const int Count=64, DataSize=1024, CodeSize=4096;
 internal sealed class Patch {
  public uint Address; public byte[] Expected,Replacement;
  public Patch(uint a,byte[] e,byte[] r){Address=a;Expected=e;Replacement=r;}
 }
 static readonly uint[] Bases={0x829258,0x829298,0x8292f8,0x829358};
 static readonly uint[][] References={
  new uint[]{0x4c8978,0x4c8990,0x4c8f33,0x4c8f43,0x4c96b0,0x4c98c3,0x4c9a06,0x4c9aa0},
  new uint[]{0x4c8988,0x4c89a6},
  new uint[]{0x4c899e,0x4c89bc,0x4c9023,0x4c9033,0x4c96ee,0x4c9966,0x4c9b3e},
  new uint[]{0x4c89b4,0x4c89ca,0x4c9073,0x4c9083,0x4c972c}
 };
 static readonly uint[] Initializers={0x747e70,0x747ed0,0x747f50,0x747fd0};
 static readonly uint[] Destructors={0x767ad0,0x767b00,0x767b90,0x767c20};
 static byte[] U(uint n){return BitConverter.GetBytes(n);}
 static byte[] Join(params byte[][] values){var b=new List<byte>();foreach(var v in values)b.AddRange(v);return b.ToArray();}
 static byte[] H(string text){text=text.Replace(" ","");var b=new byte[text.Length/2];for(int i=0;i<b.Length;i++)b[i]=Convert.ToByte(text.Substring(i*2,2),16);return b;}
 static byte[] Relative(byte op,uint from,uint to){return Join(new byte[]{op},U(unchecked(to-from-5)));}
 internal static byte[] ZeroCode(uint at,uint table,uint destructor,uint registerDestructor){
  // Save callee-saved EDI; clear every entry, including the upper 16. Preserve
  // the original CRT destructor registration and its caller-cleaned argument.
  var b=new List<byte>(H("57 33 c0 fc bf"));b.AddRange(U(table));b.Add(0xb9);b.AddRange(U(Count));
  b.AddRange(H("f3 ab 5f 68"));b.AddRange(U(destructor));
  b.AddRange(Relative(0xe8,at+(uint)b.Count,registerDestructor));b.AddRange(H("59 c3"));return b.ToArray();
 }
 internal static byte[] ResetTail(uint at,uint loop,uint after){
  // 0x80 does NOT fit a positive sign-extended imm8. Use CMP r32,imm32,
  // otherwise the original byte-size instruction would compare with -128.
  var b=new List<byte>(H("83 c6 04 81 fe 00 01 00 00 0f 82"));
  b.AddRange(U(unchecked(loop-(at+15))));b.AddRange(Relative(0xe9,at+(uint)b.Count,after));return b.ToArray();
 }
 internal static List<Patch> GetPatches(uint data,uint code){
  var patches=new List<Patch>();
  for(int table=0;table<4;table++){
   uint moved=data+(uint)table*256;
   foreach(uint site in References[table])patches.Add(new Patch(site,U(Bases[table]),U(moved)));
   // These addresses are one-past THIS table. In particular, leave the
   // TeamPersist and AllianceInventory destructors alone, although their old
   // one-past values happen to equal worm-table base addresses.
   patches.Add(new Patch(Destructors[table]+3,U(Bases[table]+64),U(moved+256)));
   patches.Add(new Patch(Destructors[table]+8,U(16),U(Count)));
   uint cave=code+(uint)table*64;byte[] routine=ZeroCode(cave,moved,Destructors[table],0x4a8c16);
   patches.Add(new Patch(cave,new byte[routine.Length],routine));
   byte[] expected=Join(H("33 c0 a3"),U(Bases[table]));
   patches.Add(new Patch(Initializers[table],expected,Join(Relative(0xe9,Initializers[table],cave),H("90 90"))));
  }
  patches.Add(new Patch(0x4c973b,H("83 fe 10"),H("83 fe 40")));
  byte[] tail=ResetTail(code+256,0x4c8976,0x4c89d6);
  patches.Add(new Patch(code+256,new byte[tail.Length],tail));
  patches.Add(new Patch(0x4c89ce,H("83 c6 04 83 fe 40 72 a0"),Join(Relative(0xe9,0x4c89ce,code+256),H("90 90 90"))));
  return patches;
 }
 internal static void CheckDisk(byte[] disk){
  foreach(var p in GetPatches(0x10000000,0x10010000)){
   if(p.Address>=0x10000000)continue;
   int o=checked((int)(p.Address-0x400000));
   for(int i=0;i<p.Expected.Length;i++)if(disk[o+i]!=p.Expected[i])throw new Exception("Capacity patch byte mismatch at "+p.Address.ToString("X8"));
  }
 }
}
