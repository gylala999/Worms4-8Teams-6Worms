using System;
using System.Collections.Generic;
using System.Text;

// Build 3013's weapon panel owns four cached WeaponDelays query handles.
// Move them to an allocated eight-entry tail, including all eight resources.
internal static class InventoryTeamDelays {
 static byte[] Hex(string value) {string[] parts=value.Split('-');var bytes=new byte[parts.Length];for(int i=0;i<parts.Length;i++)bytes[i]=Convert.ToByte(parts[i],16);return bytes;}
 static byte[] Redirect(uint site,uint target,int length) {var a=new ManagerTeamLists.Asm(site);a.Relative(0xe9,target);while(a.Finish().Length<length)a.Emit(0x90);return a.Finish();}
 internal static byte[] ConstructorCode(uint cave,uint original) {
  var a=new ManagerTeamLists.Asm(cave);a.Relative(0xe8,original);
  a.Emit(0x9c,0x50,0x51,0x57,0xfc,0x8d,0xb8,0x70,0x05,0,0,0x33,0xc0,0xb9,0x08,0,0,0,0xf3,0xab,0x5f,0x59,0x58,0x9d,0xc3);
  return a.Finish();
 }
 internal static byte[] InitializeCode(uint cave,uint names,uint query,uint resume) {
  var a=new ManagerTeamLists.Asm(cave);
  a.Emit(0x9c,0x60,0x33,0xdb); // object remains in ESI
  a.Mark("next");a.Emit(0xff,0x34,0x9d);a.Emit(BitConverter.GetBytes(names));
  a.Emit(0x8b,0xcc,0x8d,0x94,0x9e,0x70,0x05,0,0);a.Relative(0xe8,query);
  a.Emit(0x83,0xc4,0x04,0x43,0x83,0xfb,0x08);a.Branch(0x82,"next");
  a.Emit(0x61,0x9d);a.Relative(0xe9,resume);return a.Finish();
 }
 internal static byte[] DestructorCode(uint cave,uint resume) {
  var a=new ManagerTeamLists.Asm(cave);a.Emit(0x8d,0xbe,0x90,0x05,0,0,0xbb,0x08,0,0,0);a.Relative(0xe9,resume);return a.Finish();
 }
 internal static byte[] LoadCode(uint cave,bool eaxIndex,uint resume) {
  var a=new ManagerTeamLists.Asm(cave);
  if(eaxIndex)a.Emit(0x0f,0xb6,0x87,0x2b,0x01,0,0,0x8b,0x8c,0x86,0x70,0x05,0,0);
  else a.Emit(0x0f,0xb6,0x8b,0x2b,0x01,0,0,0x8b,0x8c,0x8e,0x70,0x05,0,0);
  a.Relative(0xe9,resume);return a.Finish();
 }
 internal static byte[] NameData(uint data) {
  var bytes=new byte[320];
  for(int i=0;i<8;i++){
   Array.Copy(BitConverter.GetBytes(data+64+(uint)i*32),0,bytes,i*4,4);
   byte[] name=Encoding.ASCII.GetBytes("Inventory"+i+".WeaponDelays\0");Array.Copy(name,0,bytes,64+i*32,name.Length);
  }
  return bytes;
 }
 internal static byte[] GirderDelayCode(uint cave,uint names,uint originalSwitch,uint queryPath,uint cleanup) {
  var a=new ManagerTeamLists.Asm(cave);
  a.Emit(0x83,0xf8,0x03);a.Branch(0x87,"extra");
  a.Relative(0xe9,originalSwitch);
  a.Mark("extra");a.Emit(0x83,0xf8,0x08);a.Branch(0x83,"invalid");
  a.Emit(0x8b,0x04,0x85);a.Emit(BitConverter.GetBytes(names));
  a.Emit(0x89,0x44,0x24,0x04,0x8d,0x4c,0x24,0x04);a.Relative(0xe9,queryPath);
  // Only eight delay resources are initialized in this profile. Preserve the
  // original cleanup frame for unsupported indices without a null query.
  a.Mark("invalid");a.Emit(0x56,0x57);a.Relative(0xe9,cleanup);
  return a.Finish();
 }
 internal static byte[] WeaponCrateTeamCode(uint cave,uint wormGetter,uint resume) {
  var a=new ManagerTeamLists.Asm(cave);
  // EBP is the collecting worm index. Recover its actual team rather than
  // reusing TeamData.AlliedGroup; preserve the original volatile registers.
  a.Emit(0x50,0x51,0x52,0x8b,0xcd);a.Relative(0xe8,wormGetter);
  a.Emit(0x0f,0xb6,0xb8,0x2b,0x01,0,0,0x5a,0x59,0x58,0x85,0xff);
  a.Relative(0xe9,resume);return a.Finish();
 }
 internal static List<ManagerTeamLists.Patch> GetPatches(uint page,uint names) {
  var patches=new List<ManagerTeamLists.Patch>();
  Action<uint,byte[]> routine=(address,code)=>patches.Add(new ManagerTeamLists.Patch(address,new byte[code.Length],code));
  uint ctor=page+2048,init=page+2112,dtor=page+2240,loadEax=page+2304,loadEcx=page+2368;
  routine(ctor,ConstructorCode(ctor,0x4e87f0));routine(init,InitializeCode(init,names,0x4c8ac0,0x4e96c1));
  routine(dtor,DestructorCode(dtor,0x4e8c2e));routine(loadEax,LoadCode(loadEax,true,0x4ea7f5));routine(loadEcx,LoadCode(loadEcx,false,0x4eae6e));
  patches.Add(new ManagerTeamLists.Patch(0x4e95e9,BitConverter.GetBytes(0x570),BitConverter.GetBytes(0x590)));
  var call=new ManagerTeamLists.Asm(0x4e95f8);call.Relative(0xe8,ctor);
  patches.Add(new ManagerTeamLists.Patch(0x4e95f8,Hex("E8-F3-F1-FF-FF"),call.Finish()));
  patches.Add(new ManagerTeamLists.Patch(0x4e9671,Hex("8D-56-28-8D-4C-24-14-C7-44-24-14-F4-36-78-00-E8-3B-F4-FD-FF-8D-56-2C-8D-4C-24-14-C7-44-24-14-DC-36-78-00-E8-27-F4-FD-FF-8D-56-30-8D-4C-24-14-C7-44-24-14-C4-36-78-00-E8-13-F4-FD-FF-8D-56-34-8D-4C-24-14-C7-44-24-14-AC-36-78-00-E8-FF-F3-FD-FF"),Redirect(0x4e9671,init,80)));
  patches.Add(new ManagerTeamLists.Patch(0x4e8c26,Hex("8D-7E-38-BB-04-00-00-00"),Redirect(0x4e8c26,dtor,8)));
  patches.Add(new ManagerTeamLists.Patch(0x4ea7ea,Hex("0F-B6-87-2B-01-00-00-8B-4C-86-28"),Redirect(0x4ea7ea,loadEax,11)));
  patches.Add(new ManagerTeamLists.Patch(0x4eae63,Hex("0F-B6-8B-2B-01-00-00-8B-4C-8E-28"),Redirect(0x4eae63,loadEcx,11)));
  // Custom weapon names are stored in four frontend team configurations.
  // Preserve the generic localized name for a team with no frontend entry,
  // then continue through the original string cleanup and return path.
  patches.Add(new ManagerTeamLists.Patch(0x4e955a,Hex("77-3E"),Hex("77-43")));
  uint girder=page+2432;
  routine(girder,GirderDelayCode(girder,names,0x547d14,0x547d51,0x547d99));
  patches.Add(new ManagerTeamLists.Patch(0x547d0f,Hex("83-F8-03-77-45"),Redirect(0x547d0f,girder,5)));
  // Utility and weapon crates must add to the collecting team's inventory.
  // The utility's explicit AddToWormInventory path remains unchanged.
  patches.Add(new ManagerTeamLists.Patch(0x599125,Hex("0F-B6-48-45"),Hex("8B-4C-24-14")));
  var utilityCall=new ManagerTeamLists.Asm(0x59912d);utilityCall.Relative(0xe8,0x4c90d0);
  patches.Add(new ManagerTeamLists.Patch(0x59912d,Hex("E8-EE-FF-F2-FF"),utilityCall.Finish()));
  uint weaponCrate=page+2560;
  routine(weaponCrate,WeaponCrateTeamCode(weaponCrate,0x4c8f30,0x59128e));
  patches.Add(new ManagerTeamLists.Patch(0x591288,Hex("0F-B6-7F-45-85-FF"),Redirect(0x591288,weaponCrate,6)));
  var weaponRead=new ManagerTeamLists.Asm(0x591292);weaponRead.Relative(0xe8,0x4c90c0);
  patches.Add(new ManagerTeamLists.Patch(0x591292,Hex("E8-79-7E-F3-FF"),weaponRead.Finish()));
  var weaponWrite=new ManagerTeamLists.Asm(0x5912d6);weaponWrite.Relative(0xe8,0x4c90d0);
  patches.Add(new ManagerTeamLists.Patch(0x5912d6,Hex("E8-45-7E-F3-FF"),weaponWrite.Finish()));
  return patches;
 }
}
