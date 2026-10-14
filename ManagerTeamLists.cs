using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

// Build 3013 only. Keep the four original alliance queues in place, with a
// separate fifth queue at 258. Move team worm queues to new manager space.
internal static class ManagerTeamLists {
 internal sealed class Patch {
  public uint Address; public byte[] Expected, Replacement;
  public Patch(uint address,byte[] expected,byte[] replacement){Address=address;Expected=expected;Replacement=replacement;}
 }
 internal sealed class Asm {
  readonly uint origin;
  readonly MemoryStream code=new MemoryStream();
  readonly Dictionary<string,int> labels=new Dictionary<string,int>();
  readonly List<Tuple<int,string>> fixups=new List<Tuple<int,string>>();
  public Asm(uint address){origin=address;}
  public void Emit(params byte[] bytes){code.Write(bytes,0,bytes.Length);}
  public void Mark(string label){labels.Add(label,(int)code.Position);}
  public void Branch(byte condition,string label){Emit(0x0f,condition);fixups.Add(Tuple.Create((int)code.Position,label));Emit(0,0,0,0);}
  public void Jump(string label){Emit(0xe9);fixups.Add(Tuple.Create((int)code.Position,label));Emit(0,0,0,0);}
  public void Relative(byte instruction,uint target){Emit(instruction);Emit(BitConverter.GetBytes(unchecked(target-(origin+(uint)code.Position+4))));}
  public byte[] Finish(){byte[] bytes=code.ToArray();foreach(var fixup in fixups)Array.Copy(BitConverter.GetBytes(labels[fixup.Item2]-(fixup.Item1+4)),0,bytes,fixup.Item1,4);return bytes;}
 }
 static byte[] U32(uint number){return BitConverter.GetBytes(number);}
 static byte[] Redirect(byte opcode,uint site,uint target,int length){var a=new Asm(site);a.Relative(opcode,target);while(a.Finish().Length<length)a.Emit(0x90);return a.Finish();}

 internal static byte[] ConstructorCode(uint cave,uint embeddedConstructor) {
  var a=new Asm(cave);
  a.Relative(0xe8,embeddedConstructor);
  a.Emit(0x9c,0x50,0x51,0x57,0xfc,0x33,0xc0); // preserve return, registers and flags
  a.Emit(0x8d,0xbe,0xec,0x01,0,0,0xb9,0x18,0,0,0,0xf3,0xab); // zero 8 vector triples at manager+1EC
  a.Emit(0x8d,0xbe,0x58,0x02,0,0,0xb9,12,0,0,0,0xf3,0xab); // four extra alliance vectors, separate from original CC member
  a.Emit(0x5f,0x59,0x58,0x9d,0xc3);
  return a.Finish();
 }
 internal static byte[] ClearCode(uint cave,uint resume) {
  var a=new Asm(cave);
  a.Emit(0x8d,0x86,0xf0,0x01,0,0,0xc7,0x44,0x24,0x10,0x08,0,0,0);
  a.Relative(0xe9,resume);return a.Finish();
 }
 internal static byte[] EndPointerCode(uint cave,uint resume) {
  var a=new Asm(cave);
  a.Emit(0x8b,0x84,0x85,0xf0,0x01,0,0,0x3b,0xf8);
  a.Relative(0xe9,resume);return a.Finish();
 }
 internal static byte[] AppendAllianceCode(uint cave,uint alive,uint teamGetter,uint resize,uint resume) {
  var a=new Asm(cave);
  a.Emit(0x9c,0x60,0x83,0xec,4);
  for(uint i=0;i<4;i++) { a.Emit(0x8b,0x85);a.Emit(U32(0x258+i*12));a.Emit(0x89,0x85);a.Emit(U32(0x25c+i*12)); }
  a.Emit(0xc7,0x04,0x24,4,0,0,0);
  a.Mark("team");a.Emit(0xff,0x34,0x24,0x8b,0xcd);
  a.Relative(0xe8,alive);
  a.Emit(0x84,0xc0);a.Branch(0x84,"nextTeam");
  a.Emit(0x8b,0x0c,0x24);a.Relative(0xe8,teamGetter);
  a.Emit(0x0f,0xb6,0x40,0x45,0x83,0xf8,8);a.Branch(0x83,"nextTeam");
  a.Emit(0x83,0xf8,4);a.Branch(0x83,"extra");
  a.Emit(0x6b,0xc0,0x0c,0x8d,0xb4,0x05,0x9c,0,0,0);a.Jump("queue");
  a.Mark("extra");a.Emit(0x83,0xe8,4,0x6b,0xc0,12,0x8d,0xb4,0x05,0x58,0x02,0,0);
  a.Mark("queue");a.Emit(0x8b,0x3e,0x8b,0x5e,0x04);
  a.Mark("search");a.Emit(0x3b,0xfb);a.Branch(0x84,"append");
  a.Emit(0x8b,0x04,0x24,0x39,0x07);a.Branch(0x84,"nextTeam");
  a.Emit(0x83,0xc7,0x04);a.Jump("search");
  a.Mark("append");a.Emit(0x3b,0x5e,0x08);a.Branch(0x85,"capacity");
  a.Emit(0x6a,0x01,0x8b,0xce);a.Relative(0xe8,resize);
  a.Mark("capacity");a.Emit(0x8b,0x46,4,0x8b,0x14,0x24,0x89,0x10,0x83,0x46,4,4);
  a.Mark("nextTeam");a.Emit(0xff,0x04,0x24,0x83,0x3c,0x24,8);a.Branch(0x82,"team");
  a.Emit(0x83,0xc4,4,0x61,0x9d,0x8b,0x44,0x24,0x0c,0x85,0xc0);
  a.Relative(0xe9,resume);return a.Finish();
 }
 internal static List<Patch> GetPatches(uint codePage) {
  var patches=new List<Patch>();
  // 1E0 bytes was the complete original object, including its embedded member
  // at 1A4. Team queues span 1EC..24C. Fifth alliance spans 258..264; allocate270.
  patches.Add(new Patch(0x536f39,U32(0x1e0),U32(0x290)));
  foreach(uint site in new uint[]{0x5366d9,0x537392,0x53795c})
   patches.Add(new Patch(site,new byte[]{0x1b},new byte[]{0x7b}));
  foreach(uint site in new uint[]{0x537751,0x537d4c,0x537d7b})
   patches.Add(new Patch(site,new byte[]{9},new byte[]{0x29}));
  uint ctor=codePage+1024,clear=codePage+1152,end=codePage+1280,append=codePage+6000;
  Action<uint,byte[]> routine=(address,bytes)=>patches.Add(new Patch(address,new byte[bytes.Length],bytes));
  routine(ctor,ConstructorCode(ctor,0x4ebdc0));
  routine(clear,ClearCode(clear,0x537c77));
  routine(end,EndPointerCode(end,0x5366eb));
  routine(append,AppendAllianceCode(append,0x5365d0,0x4c8f80,0x586030,0x53767e));
  patches.Add(new Patch(0x535737,new byte[]{0xe8,0x84,0x66,0xfb,0xff},Redirect(0xe8,0x535737,ctor,5)));
  patches.Add(new Patch(0x537c6c,new byte[]{0x8d,0x46,0x70,0xc7,0x44,0x24,0x10,4,0,0,0},Redirect(0xe9,0x537c6c,clear,11)));
  patches.Add(new Patch(0x5366e5,new byte[]{0x8b,0x44,0x85,0x70,0x3b,0xf8},Redirect(0xe9,0x5366e5,end,6)));
  patches.Add(new Patch(0x537678,new byte[]{0x8b,0x44,0x24,0x0c,0x85,0xc0},Redirect(0xe9,0x537678,append,6)));
  return patches;
 }
 internal static string Report() {
  var report=new StringBuilder();
  foreach(var patch in GetPatches(0x201000))
   report.AppendLine(patch.Address.ToString("X8")+" "+BitConverter.ToString(patch.Expected)+" => "+BitConverter.ToString(patch.Replacement));
  return report.ToString();
 }
}
