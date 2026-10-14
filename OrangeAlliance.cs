using System;
using System.Collections.Generic;

// Build 3013 diagnostic: eight distinct sides, one team on each side.
// The original CC member is not a fifth vector. Store four extra vectors at 258 through 27C.
internal static class OrangeAlliance {
 internal static readonly uint[] CacheRefs={0x4c8a32,0x4c8a42,0x4c9113,0x4c9123,0x4c980d,0x4c99b9,0x4c9b91};
 static byte[] U32(uint number){return BitConverter.GetBytes(number);}
 static byte[] Redirect(uint site,uint target,int length){var a=new ManagerTeamLists.Asm(site);a.Relative(0xe9,target);while(a.Finish().Length<length)a.Emit(0x90);return a.Finish();}
 internal static byte[] CountCode(uint cave,uint resume) {
  var a=new ManagerTeamLists.Asm(cave);
  for(uint i=3;i<8;i++) {
   uint offset=i==3?0xc0:0x258+(i-4)*12;
   a.Emit(0x8b,0x90);a.Emit(U32(offset));a.Emit(0x3b,0x90);a.Emit(U32(offset+4));
   a.Branch(0x84,"next"+i);a.Emit(0x47);a.Mark("next"+i);
  }
  a.Relative(0xe9,resume);return a.Finish();
 }
 internal static byte[] RemoveCode(uint cave,uint normal,uint tail) {
  var a=new ManagerTeamLists.Asm(cave);
  a.Emit(0x0f,0xb6,0x44,0x24,8,0x83,0xf8,4);a.Branch(0x82,"normal");
  a.Emit(0x83,0xe8,4,0x6b,0xc0,12,0x8d,0x94,0x01,0x58,0x02,0,0,0x8b,0x4a,4);a.Relative(0xe9,tail);
  a.Mark("normal");a.Relative(0xe9,normal);return a.Finish();
 }
 internal static byte[] DeathCode(uint cave,uint normal,uint tail) {
  var a=new ManagerTeamLists.Asm(cave);
  a.Emit(0x83,0xf8,4);a.Branch(0x82,"normal");
  a.Emit(0x83,0xe8,4,0x6b,0xc0,12,0x8d,0x94,0x05,0x58,0x02,0,0,0x8b,0x3a,0x8b,0x6a,4);a.Relative(0xe9,tail);
  a.Mark("normal");a.Emit(0x8d,0x54,0x40,0x27,0x8b,0x7c,0x95,0);a.Relative(0xe9,normal);return a.Finish();
 }
 internal static byte[] DestructorCode(uint cave,uint free,uint resume) {
  var a=new ManagerTeamLists.Asm(cave);
  for(uint i=0;i<4;i++) {
   a.Emit(0x8b,0x8e);a.Emit(U32(0x258+i*12));a.Emit(0x85,0xc9);a.Branch(0x84,"empty"+i);a.Relative(0xe8,free);a.Mark("empty"+i);
  }
  a.Emit(0x8d,0xbe,0x4c,0x02,0,0,0xbb,8,0,0,0);a.Relative(0xe9,resume);return a.Finish();
 }
 internal static byte[] SelectorCode(uint cave,uint wormGetter) {
  var a=new ManagerTeamLists.Asm(cave);
  a.Emit(0x53,0x56,0x57,0x55,0x8b,0xe9,0xbf,7,0,0,0,0x8b,0x4c,0x24,0x14,0x85,0xc9);a.Branch(0x88,"advance");
  a.Relative(0xe8,wormGetter);a.Emit(0x0f,0xb6,0xb8,0x2b,0x01,0,0,0x83,0xff,8);a.Branch(0x83,"first");
  a.Emit(0x80,0x7c,0x24,0x18,0);a.Branch(0x85,"searchStart");a.Jump("advance");
  a.Mark("first");a.Emit(0xbf,7,0,0,0);
  a.Mark("advance");a.Emit(0x47,0x83,0xff,8);a.Branch(0x82,"searchStart");a.Emit(0x33,0xff);
  a.Mark("searchStart");a.Emit(0xbb,8,0,0,0);
  a.Mark("search");a.Emit(0x6b,0xf7,0x0c,0x8d,0xb4,0x35,0xec,0x01,0,0,0x8b,0x16,0x8b,0x4e,4,0x3b,0xd1);a.Branch(0x85,"select");
  a.Emit(0x47,0x83,0xff,8);a.Branch(0x82,"next");a.Emit(0x33,0xff);
  a.Mark("next");a.Emit(0x4b);a.Branch(0x85,"search");a.Emit(0xb8,0xff,0xff,0xff,0xff);a.Jump("done");
  a.Mark("select");a.Emit(0x8b,0x02,0x83,0xe9,4);
  a.Mark("rotate");a.Emit(0x3b,0xd1);a.Branch(0x84,"last");a.Emit(0x8b,0x5a,4,0x89,0x1a,0x83,0xc2,4);a.Jump("rotate");
  a.Mark("last");a.Emit(0x89,0x02);
  a.Mark("done");a.Emit(0x5d,0x5f,0x5e,0x5b,0xc2,8,0);return a.Finish();
 }
 internal static byte[] ColourCode(uint cave,uint normal,uint surrendered) {
  var a=new ManagerTeamLists.Asm(cave);
  a.Emit(0x0f,0xb6,0x48,0x45,0x83,0xf9,4);a.Branch(0x82,"normal");
  a.Emit(0x83,0xf9,8);a.Branch(0x83,"normal");
  a.Emit(0x80,0x78,0x3f,0);a.Branch(0x85,"surrendered");
  uint[] colours={0xff329aff,0xff4177b4,0xffe866b3,0xffc875ff};
  for(byte i=4;i<8;i++) {
   a.Emit(0x83,0xf9,i);a.Branch(0x85,"color"+(i+1));a.Emit(0xc7,0x06);a.Emit(U32(colours[i-4]));a.Jump("colorDone");a.Mark("color"+(i+1));
  }
  a.Mark("colorDone");a.Emit(0x8b,0x44,0x24,8,0xc7,0x00,0xff,0xff,0xff,0xff,0x5e,0xc2,4,0);
  a.Mark("surrendered");a.Relative(0xe9,surrendered);
  a.Mark("normal");a.Emit(0x83,0xf9,3);a.Relative(0xe9,normal);return a.Finish();
 }
 internal static List<ManagerTeamLists.Patch> GetPatches(uint codePage,uint data) {
  var patches=new List<ManagerTeamLists.Patch>();
  Action<uint,byte[],byte[]> patch=(address,expected,replacement)=>patches.Add(new ManagerTeamLists.Patch(address,expected,replacement));
  Action<uint,byte[]> routine=(address,bytes)=>patch(address,new byte[bytes.Length],bytes);
  uint cache=data+192,palette=data+512;
  // The original TeamTakesTurn guard rejects indices 5..7 before examining
  // their live worms. AppendAllianceCode calls this actual native predicate;
  // without this correction those teams never enter the victory queues.
  // Keep the sixteen-worm scan unchanged for this eight-by-one profile.
  patch(0x5365d9,new byte[]{0x83,0xff,4},new byte[]{0x83,0xff,7});
  foreach(uint site in CacheRefs)patch(site,U32(0x829348),U32(cache));
  patch(0x4c984c,new byte[]{0x83,0xfe,4},new byte[]{0x83,0xfe,8});
  patch(0x4c8a49,new byte[]{0x83,0xfe,0x10},new byte[]{0x83,0xfe,0x20});
  patch(0x767bf3,U32(0x829358),U32(cache+32));patch(0x767bf8,U32(4),U32(8));
  routine(palette,new byte[]{0xff,0x4e,0x3c,0xff,0x6c,0x7f,0xff,0xff,0x3b,0xf4,0x7b,0xff,0xff,0xda,0x4d,0xff,0xff,0x9a,0x32,0xff,0xb4,0x77,0x41,0xff,0xb3,0x66,0xe8,0xff,0xff,0x75,0xc8,0xff});
  foreach(uint site in new uint[]{0x4c9396,0x4c9a29})patch(site,U32(0x80dfcc),U32(palette));
  uint count=codePage+4096,remove=codePage+4224,death=codePage+4352,dtor=codePage+4480,selector=codePage+4608,colour=codePage+5120;
  routine(count,CountCode(count,0x5367cb));routine(remove,RemoveCode(remove,0x536655,0x536666));
  routine(death,DeathCode(death,0x536732,0x536740));routine(dtor,DestructorCode(dtor,0x401271,0x5359dd));
  routine(selector,SelectorCode(selector,0x4c8f30));routine(colour,ColourCode(colour,0x4c93b6,0x4c93e6));
  patch(0x5367bc,new byte[]{0x8b,0x90,0xc0,0,0,0},Redirect(0x5367bc,count,6));
  patch(0x536650,new byte[]{0x0f,0xb6,0x44,0x24,8},Redirect(0x536650,remove,5));
  patch(0x53672a,new byte[]{0x8d,0x54,0x40,0x27,0x8b,0x7c,0x95,0},Redirect(0x53672a,death,8));
  patch(0x5359d2,new byte[]{0x8d,0xbe,0x9c,0,0,0,0xbb,4,0,0,0},Redirect(0x5359d2,dtor,11));
  patch(0x537690,new byte[]{0x53,0x55,0x56,0x8b,0x74,0x24,0x10},Redirect(0x537690,selector,7));
  patch(0x4c93af,new byte[]{0x0f,0xb6,0x48,0x45,0x83,0xf9,3},Redirect(0x4c93af,colour,7));
  patch(0x536826,new byte[]{0x83,0xfe,4},new byte[]{0x83,0xfe,8});
  return patches;
 }
}
