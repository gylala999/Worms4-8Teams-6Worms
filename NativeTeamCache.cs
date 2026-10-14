using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

// Narrow, build-specific game mod. Creates only its own child process,
// expands team/alliance caches to 8 and worm/actor storage to 64, with
// separate acting-source storage and expanded sprite/bitmap pools.
// Does not modify the executable on disk or any licensing code.
internal static class NativeTeamCache {
 static readonly uint[] TeamRefs={0x4c89e2,0x4c89fa,0x4c8f83,0x4c8f93,0x4c9386,0x4c93a3,0x4c9776,0x4c98d8,0x4c9a19,0x4c9ab5};
 static readonly uint[] PersistRefs={0x4c8a08,0x4c8a1e,0x4c8fd3,0x4c8fe3,0x4c97f2};
 static readonly uint[] InventoryRefs={0x4c89f2,0x4c8a10,0x4c90c3,0x4c90d3,0x4c97b4,0x4c9989,0x4c9b61};
 const uint TeamBase=0x8292d8, PersistBase=0x8292e8, InventoryBase=0x829338;
 static readonly uint[][] HudBounds={new uint[]{0x54512a,0x54516c},new uint[]{0x5452b5,0x5452fd},new uint[]{0x5454b9,0x545501}};

 [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Unicode)] struct STARTUPINFO {
  public int cb; public string reserved,desktop,title; public uint x,y,xSize,ySize,xChars,yChars,fill,flags;
  public ushort showWindow,reserved2Size; public IntPtr reserved2,stdInput,stdOutput,stdError;
 }
 [StructLayout(LayoutKind.Sequential)] struct PROCESS_INFORMATION {
  public IntPtr process,thread; public uint processId,threadId;
 }
 [StructLayout(LayoutKind.Explicit,Size=96)] struct DEBUG_EVENT {
  [FieldOffset(0)] public uint code;
  [FieldOffset(4)] public uint processId;
  [FieldOffset(8)] public uint threadId;
  [FieldOffset(12)] public uint data0;
  [FieldOffset(16)] public uint data1;
  [FieldOffset(20)] public uint data2;
  [FieldOffset(24)] public uint exceptionAddress;
  [FieldOffset(92)] public uint firstChance;
 }
 [DllImport("kernel32.dll",CharSet=CharSet.Unicode,SetLastError=true)]
 static extern bool CreateProcessW(string app,StringBuilder command,IntPtr processAttr,IntPtr threadAttr,bool inherit,uint flags,IntPtr environment,string directory,ref STARTUPINFO startup,out PROCESS_INFORMATION info);
 [DllImport("kernel32.dll",SetLastError=true)] static extern IntPtr VirtualAllocEx(IntPtr process,IntPtr address,UIntPtr size,uint type,uint protection);
 [DllImport("kernel32.dll",SetLastError=true)] static extern bool VirtualProtectEx(IntPtr process,IntPtr address,UIntPtr size,uint protection,out uint oldProtection);
 [DllImport("kernel32.dll",SetLastError=true)] static extern bool ReadProcessMemory(IntPtr process,IntPtr address,byte[] data,UIntPtr size,out UIntPtr read);
 [DllImport("kernel32.dll",SetLastError=true)] static extern bool WriteProcessMemory(IntPtr process,IntPtr address,byte[] data,UIntPtr size,out UIntPtr written);
 [DllImport("kernel32.dll",SetLastError=true)] static extern bool FlushInstructionCache(IntPtr process,IntPtr address,UIntPtr size);
 [DllImport("kernel32.dll",SetLastError=true)] static extern uint ResumeThread(IntPtr thread);
 [DllImport("kernel32.dll",SetLastError=true)] static extern bool TerminateProcess(IntPtr process,uint code);
 [DllImport("kernel32.dll")] static extern uint WaitForSingleObject(IntPtr handle,uint milliseconds);
 [DllImport("kernel32.dll")] static extern bool CloseHandle(IntPtr handle);
 [DllImport("kernel32.dll",SetLastError=true)] static extern bool GetExitCodeProcess(IntPtr process,out uint code);
 [DllImport("kernel32.dll",SetLastError=true)] static extern bool WaitForDebugEvent(out DEBUG_EVENT value,uint milliseconds);
 [DllImport("kernel32.dll",SetLastError=true)] static extern bool ContinueDebugEvent(uint processId,uint threadId,uint status);
 [DllImport("kernel32.dll",SetLastError=true)] static extern IntPtr OpenThread(uint access,bool inherit,uint threadId);
 [DllImport("kernel32.dll",SetLastError=true)] static extern bool GetThreadContext(IntPtr thread,IntPtr context);
 [DllImport("kernel32.dll",SetLastError=true)] static extern bool DebugActiveProcessStop(uint processId);

 static IntPtr Address(uint address) { return new IntPtr(unchecked((int)address)); }
 static Exception Win32(string action) { return new Win32Exception(Marshal.GetLastWin32Error(),action); }
 static byte[] Read(IntPtr process,uint address,int count) {
  var bytes=new byte[count]; UIntPtr read;
  if(!ReadProcessMemory(process,Address(address),bytes,(UIntPtr)(uint)count,out read) || read.ToUInt64()!=(ulong)count) throw Win32("Read game patch location");
  return bytes;
 }
 static bool Equal(byte[] left,byte[] right) {
  if(left.Length!=right.Length) return false;
  for(int i=0;i<left.Length;i++) if(left[i]!=right[i]) return false;
  return true;
 }
 static void WriteGuarded(IntPtr process,uint address,byte[] expected,byte[] replacement) {
  if(!Equal(Read(process,address,expected.Length),expected)) throw new Exception("Unexpected game memory at 0x"+address.ToString("X8")+"; stopped before running the game.");
  uint oldProtection;
  if(!VirtualProtectEx(process,Address(address),(UIntPtr)(uint)replacement.Length,0x40,out oldProtection)) throw Win32("Unlock game patch location");
  try {
   UIntPtr written;
   if(!WriteProcessMemory(process,Address(address),replacement,(UIntPtr)(uint)replacement.Length,out written) || written.ToUInt64()!=(ulong)replacement.Length) throw Win32("Write game patch location");
  } finally {
   uint ignored;
   if(!VirtualProtectEx(process,Address(address),(UIntPtr)(uint)replacement.Length,oldProtection,out ignored)) throw Win32("Restore game memory protection");
  }
  if(!Equal(Read(process,address,replacement.Length),replacement)) throw new Exception("Game memory patch verification failed.");
 }
 static void ChangePointer(IntPtr process,uint address,uint expected,uint value) {
  WriteGuarded(process,address,BitConverter.GetBytes(expected),BitConverter.GetBytes(value));
 }
 public static void CheckDisk(string exe) {
  NativeMeshPool.CheckDisk(File.ReadAllBytes(exe));
  byte[] bytes=File.ReadAllBytes(exe);
  Action<uint,byte[]> guard=(address,expected)=>{
   int offset=checked((int)(address-0x400000));
   // All selected code lives in the first section, whose raw offset equals RVA.
   if(offset<0 || offset+expected.Length>bytes.Length) throw new Exception("Invalid patch location.");
   for(int i=0;i<expected.Length;i++) if(bytes[offset+i]!=expected[i]) throw new Exception("Native patch does not match this executable: 0x"+address.ToString("X8"));
  };
  foreach(uint address in TeamRefs) guard(address,BitConverter.GetBytes(TeamBase));
  foreach(uint address in PersistRefs) guard(address,BitConverter.GetBytes(PersistBase));
  foreach(uint address in InventoryRefs) guard(address,BitConverter.GetBytes(InventoryBase));
  guard(0x4c9801,new byte[]{0x83,0xfe,0x04});
  guard(0x4c8a25,new byte[]{0x83,0xfe,0x10});
  guard(0x767b33,BitConverter.GetBytes(PersistBase)); // one-past Team.Data, not Persist base
  guard(0x767b63,BitConverter.GetBytes(0x8292f8u)); // one-past TeamPersist.Data
  guard(0x767bc3,BitConverter.GetBytes(0x829348u)); // one-past Inventory.Team
  foreach(uint address in new uint[]{0x767b38,0x767b68,0x767bc8}) guard(address,BitConverter.GetBytes(4));
  foreach(var hook in HudBounds) guard(hook[0],new byte[]{0x0f,0xb6,0xb0,0x2b,0x01,0x00,0x00});
  guard(0x59e2a0,new byte[]{0x51,0x56,0xc7,0x44,0x24,0x04,0,0,0,0});
  guard(0x516b71,new byte[]{0x0f,0xb6,0xbd,0x2b,0x01,0,0});
  foreach(var patch in ManagerTeamLists.GetPatches(0x200000))
   if(patch.Address>=0x400000 && patch.Address<0x780000) guard(patch.Address,patch.Expected);
  foreach(var patch in InventoryTeamDelays.GetPatches(0x200000,0x210000))
   if(patch.Address>=0x400000 && patch.Address<0x780000) guard(patch.Address,patch.Expected);
  foreach(var patch in OrangeAlliance.GetPatches(0x200000,0x210000))
   if(patch.Address>=0x400000 && patch.Address<0x780000) guard(patch.Address,patch.Expected);
  foreach(var patch in HudFive.GetPatches(0x200000,0x210000))
   if(patch.Address>=0x400000 && patch.Address<0x780000) guard(patch.Address,patch.Expected);
  NativeWormCaches64.CheckDisk(bytes);NativeActorStorage64.CheckDisk(bytes);NativeLoadingPools.CheckDisk(bytes);NativeParticleCache64.CheckDisk(bytes);
  foreach(var patch in GetIntegratedPatches(0x200000,0x210000))if(patch.Address>=0x400000 && patch.Address<0x780000)guard(patch.Address,patch.Expected);
 }
 // Unified patch list. Whole-function clones supersede the old manager
 // allocation and in-body constructor/queue hooks at the listed locations.
 internal static List<ManagerTeamLists.Patch> GetIntegratedPatches(uint code,uint data){
  var result=new List<ManagerTeamLists.Patch>();
  var replaced=new HashSet<uint>{0x536f39,0x535737,0x537c6c,0x537392,0x537d4c,0x537d7b};
  foreach(var p in ManagerTeamLists.GetPatches(code))if(!replaced.Contains(p.Address))result.Add(p);
  result.AddRange(InventoryTeamDelays.GetPatches(code,data+640));
  result.AddRange(OrangeAlliance.GetPatches(code,data));
  result.AddRange(HudFive.GetPatches(code,data));
  foreach(var p in NativeWormCaches64.GetPatches(data+2048,code+24576))result.Add(new ManagerTeamLists.Patch(p.Address,p.Expected,p.Replacement));
  foreach(var p in NativeActorStorage64.GetPatches(code+28672,data+3072))result.Add(new ManagerTeamLists.Patch(p.Address,p.Expected,p.Replacement));
  foreach(var p in NativeCpuTables64.GetPatches(data))result.Add(new ManagerTeamLists.Patch(p.Address,p.Expected,p.Replacement));
  foreach(var p in NativeScans64.GetPatches())result.Add(new ManagerTeamLists.Patch(p.Address,p.Expected,p.Replacement));
  foreach(var p in NativeLoadingPools.GetPatches(code+36864,0))result.Add(new ManagerTeamLists.Patch(p.Address,p.Expected,p.Replacement));
  foreach(var p in NativeParticleCache64.GetPatches(code+45056))result.Add(new ManagerTeamLists.Patch(p.Address,p.Expected,p.Replacement));
  foreach(var p in NativeMeshPool.GetPatches(code+46080))result.Add(new ManagerTeamLists.Patch(p.Address,p.Expected,p.Replacement));
  return result;
 }
 static void AddRelativeJump(MemoryStream stream,uint baseAddress,uint target) {
  stream.WriteByte(0xe9);
  byte[] displacement=BitConverter.GetBytes(unchecked(target-(baseAddress+(uint)stream.Position+4)));
  stream.Write(displacement,0,4);
 }
 static byte[] HudGuardCode(uint hook,uint skip,uint cave) {
  byte[] original={0x0f,0xb6,0xb0,0x2b,0x01,0x00,0x00};
  byte[] code;
  using(var stream=new MemoryStream()) {
   // Original load, save flags, compare index. The old next instruction tests
   // flags set before the load; restore them on both paths.
   stream.Write(original,0,original.Length);
   byte[] guard={0x9c,0x83,0xfe,0x04,0x72,0x06,0x9d};
   stream.Write(guard,0,guard.Length);
   AddRelativeJump(stream,cave,skip);
   stream.WriteByte(0x9d);
   AddRelativeJump(stream,cave,hook+7);
   code=stream.ToArray();
  }
  return code;
 }
 static byte[] CrateGuardCode(uint cave) {
  using(var stream=new MemoryStream()) {
   var labels=new Dictionary<string,int>();
   var fixups=new List<Tuple<int,string>>();
   Action<byte[]> emit=bytes=>stream.Write(bytes,0,bytes.Length);
   Action<byte,string> branch=(condition,label)=>{emit(new byte[]{0x0f,condition});fixups.Add(Tuple.Create((int)stream.Position,label));emit(new byte[4]);};
   // Eight local team-alive flags. Preserve callee-saved registers. The
   // original check used four flags and let team index 4 overwrite its return.
   emit(new byte[]{0x56,0x57,0x83,0xec,0x08,0x33,0xc0,0x89,0x04,0x24,0x89,0x44,0x24,0x04,0x33,0xf6});
   labels["worm"]=(int)stream.Position;
   emit(new byte[]{0x8b,0xce,0xe8});
   byte[] call=BitConverter.GetBytes(unchecked(0x4c8f30u-(cave+(uint)stream.Position+4)));
   emit(call);
   emit(new byte[]{0x80,0xb8,0x2f,0x01,0,0,0});branch(0x84,"nextWorm");
   emit(new byte[]{0x0f,0xb7,0x90,0x1e,0x01,0,0,0x2b,0x90,0x14,0x01,0,0,0x85,0xd2});branch(0x8e,"nextWorm");
   emit(new byte[]{0x0f,0xb6,0x90,0x2b,0x01,0,0,0x83,0xfa,0x08});branch(0x83,"nextWorm");
   emit(new byte[]{0xc6,0x04,0x14,0x01});
   labels["nextWorm"]=(int)stream.Position;
   emit(new byte[]{0x46,0x83,0xfe,0x40});branch(0x8c,"worm");
   emit(new byte[]{0x33,0xc0,0x33,0xff});
   labels["count"]=(int)stream.Position;
   emit(new byte[]{0x80,0x3c,0x3c,0});branch(0x84,"nextFlag");emit(new byte[]{0x40});
   labels["nextFlag"]=(int)stream.Position;
   emit(new byte[]{0x47,0x83,0xff,0x08});branch(0x8c,"count");
   emit(new byte[]{0x83,0xf8,0x01,0x0f,0x9f,0xc0,0x0f,0xb6,0xc0,0x83,0xc4,0x08,0x5f,0x5e,0xc3});
   byte[] code=stream.ToArray();
   foreach(var fixup in fixups) Array.Copy(BitConverter.GetBytes(labels[fixup.Item2]-(fixup.Item1+4)),0,code,fixup.Item1,4);
   return code;
  }
 }
 internal static byte[] TopHealthGuardCode(uint cave,uint resume,uint skip) {
  var a=new ManagerTeamLists.Asm(cave);
  a.Emit(0x0f,0xb6,0xbd,0x2b,0x01,0,0,0x9c,0x83,0xff,0x04);
  a.Branch(0x82,"normal");
  // The original upper health panel owns only four widgets, query handles,
  // totals and presence flags. Never index them with a fifth team. Continue
  // updating the selected worm's health number separately from those widgets.
  a.Emit(0x80,0xbd,0x2f,0x01,0,0,0);a.Branch(0x84,"skip");
  a.Emit(0x8b,0x44,0x24,0x14,0x3b,0x44,0x24,0x1c);a.Branch(0x85,"skip"); // offsets include PUSHFD
  a.Emit(0xf6,0x85,0x0d,0x01,0,0,0x08);a.Branch(0x84,"health");
  a.Emit(0xc7,0x86,0x00,0x01,0,0,0,0,0x80,0xbf);a.Jump("skip");
  a.Mark("health");
  a.Emit(0x0f,0xb7,0x85,0x1e,0x01,0,0,0x50,0xdb,0x04,0x24,0xd9,0x9e,0x00,0x01,0,0,0x83,0xc4,0x04);
  a.Mark("skip");a.Emit(0x9d);a.Relative(0xe9,skip);
  a.Mark("normal");a.Emit(0x9d);a.Relative(0xe9,resume);
  return a.Finish();
 }
 public static string GuardCodeReport() {
  var report=new StringBuilder();
  for(int i=0;i<HudBounds.Length;i++) {
   uint cave=0x200000u+(uint)i*64;
   report.AppendLine(cave.ToString("X8")+" "+HudBounds[i][0].ToString("X8")+" "+HudBounds[i][1].ToString("X8")+" "+BitConverter.ToString(HudGuardCode(HudBounds[i][0],HudBounds[i][1],cave)));
  }
  report.AppendLine("CRATE 00201000 "+BitConverter.ToString(CrateGuardCode(0x201000)));
  report.AppendLine("TOPHEALTH 00202000 "+BitConverter.ToString(TopHealthGuardCode(0x202000,0x516b78,0x516cda)));
  return report.ToString();
 }
 static void GuardHudTeamIndex(IntPtr process,uint hook,uint skip,uint cave) {
  byte[] original={0x0f,0xb6,0xb0,0x2b,0x01,0x00,0x00};
  byte[] code=HudGuardCode(hook,skip,cave);
  WriteGuarded(process,cave,new byte[code.Length],code);
  byte[] redirect;
  using(var stream=new MemoryStream()) {
   AddRelativeJump(stream,hook,cave);stream.WriteByte(0x90);stream.WriteByte(0x90);redirect=stream.ToArray();
  }
  WriteGuarded(process,hook,original,redirect);
 }
 static void CaptureFailure(IntPtr process,DEBUG_EVENT value,uint team,uint persist,uint inventory) {
  var report=new StringBuilder();
  report.AppendLine("Own child process " + value.processId + "; exception 0x" + value.data0.ToString("X8") + " at 0x" + value.exceptionAddress.ToString("X8") + "; second chance.");
  uint ecx=0,esp=0,eax=0,esi=0;
  IntPtr thread=OpenThread(8,false,value.threadId);
  if(thread!=IntPtr.Zero) {
   IntPtr context=Marshal.AllocHGlobal(716);
   try {
    Marshal.Copy(new byte[716],0,context,716);Marshal.WriteInt32(context,0,0x10007);
    if(GetThreadContext(thread,context)) {
     foreach(var register in new[]{new[]{"EAX","176"},new[]{"EBX","164"},new[]{"ECX","172"},new[]{"EDX","168"},new[]{"ESI","160"},new[]{"EDI","156"},new[]{"EBP","180"},new[]{"ESP","196"},new[]{"EIP","184"}}) {
      uint number=unchecked((uint)Marshal.ReadInt32(context,Int32.Parse(register[1])));
      report.AppendLine(register[0]+"=0x"+number.ToString("X8"));
      if(register[0]=="ECX") ecx=number;
      if(register[0]=="ESP") esp=number;
      if(register[0]=="EAX") eax=number;
      if(register[0]=="ESI") esi=number;
     }
    } else report.AppendLine("GetThreadContext error " + Marshal.GetLastWin32Error());
   } finally {Marshal.FreeHGlobal(context);CloseHandle(thread);}
  } else report.AppendLine("OpenThread error " + Marshal.GetLastWin32Error());
  foreach(var buffer in new[]{new[]{team,8u},new[]{persist,8u},new[]{inventory,8u}}) {
   try {
    byte[] bytes=Read(process,buffer[0],checked((int)(buffer[1]*4)));
    report.Append("Table 0x"+buffer[0].ToString("X8")+":");
    for(int i=0;i<8;i++) report.Append(" "+BitConverter.ToUInt32(bytes,i*4).ToString("X8"));
    report.AppendLine();
   } catch(Exception x) {report.AppendLine(x.Message);}
  }
  report.AppendLine("ECX-indexed Team table address: 0x"+unchecked(team+ecx*4).ToString("X8"));
  foreach(var region in new[]{new[]{esp,128u},new[]{eax,64u},new[]{esi,64u}}) {
   if(region[0]==0) continue;
   try {report.AppendLine("Memory 0x"+region[0].ToString("X8")+": "+BitConverter.ToString(Read(process,region[0],checked((int)region[1]))));}
   catch(Exception x){report.AppendLine(x.Message);}
  }
  try {report.AppendLine("Fault code bytes: "+BitConverter.ToString(Read(process,value.exceptionAddress,16)));}catch(Exception x){report.AppendLine(x.Message);}
  foreach(var pool in new[]{new[]{"Bitmap","008519E0","008519D4"},new[]{"Sprite","008544F0","008544FC"},new[]{"Mesh","00854490","0085449C"}}){
   try{uint capacity=UInt32.Parse(pool[1],System.Globalization.NumberStyles.HexNumber),free=UInt32.Parse(pool[2],System.Globalization.NumberStyles.HexNumber);report.AppendLine(pool[0]+" pool: capacity="+BitConverter.ToUInt16(Read(process,capacity,2),0)+" free="+BitConverter.ToUInt16(Read(process,free,2),0));}
   catch(Exception x){report.AppendLine(x.Message);}
  }
  string path=RunEvidence.CrashPath;
  File.AppendAllText(path,report.ToString());
  TestFiles.Log("Captured native crash context in "+Path.GetFileName(path));
 }
 static int WaitForChild(IntPtr process,IntPtr mainThread,uint processId,uint team,uint persist,uint inventory) {
  while(true) {
   DEBUG_EVENT value;
   if(!WaitForDebugEvent(out value,1000)) {
    int error=Marshal.GetLastWin32Error();
    if(error==121) continue;
    throw new Win32Exception(error,"Wait for own test game debug event");
   }
   uint status=0x10002;
   if(value.code==1 && value.data0!=0x80000003) {
    status=0x80010001;
    if(value.firstChance==0) CaptureFailure(process,value,team,persist,inventory);
   }
   // Debug events supply additional file/thread/process handles owned by us.
   if(value.code==3) {
    if(value.data0!=0) CloseHandle(Address(value.data0));
    if(value.data1!=0 && Address(value.data1)!=process) CloseHandle(Address(value.data1));
    if(value.data2!=0 && Address(value.data2)!=mainThread) CloseHandle(Address(value.data2));
   } else if((value.code==2 || value.code==6) && value.data0!=0) CloseHandle(Address(value.data0));
   if(!ContinueDebugEvent(value.processId,value.threadId,status)) throw Win32("Continue own game debug event");
   if(value.code==5 && value.processId==processId) {
    if(WaitForSingleObject(process,3000)!=0) throw new Exception("Game exit did not complete.");
    uint exit;
    if(!GetExitCodeProcess(process,out exit)) throw Win32("Read own test game exit code");
    return unchecked((int)exit);
   }
  }
 }
 public static int RunBlocking(string exe,string directory) {
  CheckDisk(exe);
  var startup=new STARTUPINFO();startup.cb=Marshal.SizeOf(typeof(STARTUPINFO));
  PROCESS_INFORMATION info;
  if(!CreateProcessW(exe,new StringBuilder("\""+exe+"\""),IntPtr.Zero,IntPtr.Zero,false,6,IntPtr.Zero,directory,ref startup,out info)) throw Win32("Create suspended Worms 4 test process");
  try {
   IntPtr allocation=VirtualAllocEx(info.process,IntPtr.Zero,(UIntPtr)53248u,0x3000,4);
   if(allocation==IntPtr.Zero) throw Win32("Allocate extended team caches");
   uint team=unchecked((uint)allocation.ToInt32()),persist=team+64,inventory=team+128;
   // Three separate zero-initialized eight-slot allocations inside one page.
   // All eight slots are initialized and used by this profile.
   foreach(uint address in TeamRefs) ChangePointer(info.process,address,TeamBase,team);
   foreach(uint address in PersistRefs) ChangePointer(info.process,address,PersistBase,persist);
   foreach(uint address in InventoryRefs) ChangePointer(info.process,address,InventoryBase,inventory);
   WriteGuarded(info.process,0x4c9801,new byte[]{0x83,0xfe,0x04},new byte[]{0x83,0xfe,0x08});
   WriteGuarded(info.process,0x4c8a25,new byte[]{0x83,0xfe,0x10},new byte[]{0x83,0xfe,0x20});
   // Fix the reverse-order static cleanup functions too. Other tables' one-
   // past-end addresses that equal these old bases are deliberately untouched.
   ChangePointer(info.process,0x767b33,PersistBase,team+32);
   ChangePointer(info.process,0x767b63,0x8292f8,persist+32);
   ChangePointer(info.process,0x767bc3,0x829348,inventory+32);
   foreach(uint address in new uint[]{0x767b38,0x767b68,0x767bc8}) ChangePointer(info.process,address,4,8);
   for(int i=0;i<HudBounds.Length;i++) GuardHudTeamIndex(info.process,HudBounds[i][0],HudBounds[i][1],team+4096+(uint)i*64);
   uint crateCave=team+4096+512;
   byte[] crateCode=CrateGuardCode(crateCave);
   WriteGuarded(info.process,crateCave,new byte[crateCode.Length],crateCode);
   using(var redirect=new MemoryStream()) {
    AddRelativeJump(redirect,0x59e2a0,crateCave);
    while(redirect.Length<10) redirect.WriteByte(0x90);
    WriteGuarded(info.process,0x59e2a0,new byte[]{0x51,0x56,0xc7,0x44,0x24,0x04,0,0,0,0},redirect.ToArray());
   }

   byte[] delayNames=InventoryTeamDelays.NameData(team+640);
   WriteGuarded(info.process,team+640,new byte[delayNames.Length],delayNames);


   uint topHealthCave=team+4096+1664;
   byte[] topHealthCode=TopHealthGuardCode(topHealthCave,0x516b78,0x516cda);
   WriteGuarded(info.process,topHealthCave,new byte[topHealthCode.Length],topHealthCode);
   using(var redirect=new MemoryStream()) {
    AddRelativeJump(redirect,0x516b71,topHealthCave);
    while(redirect.Length<7)redirect.WriteByte(0x90);
    WriteGuarded(info.process,0x516b71,new byte[]{0x0f,0xb6,0xbd,0x2b,0x01,0,0},redirect.ToArray());
   }

   foreach(var patch in GetIntegratedPatches(team+4096,team))
    WriteGuarded(info.process,patch.Address,patch.Expected,patch.Replacement);
   uint oldCodeProtection;
   if(!VirtualProtectEx(info.process,Address(team+4096),(UIntPtr)49152u,0x20,out oldCodeProtection)) throw Win32("Make test routines executable");
   if(!FlushInstructionCache(info.process,IntPtr.Zero,UIntPtr.Zero)) throw Win32("Refresh game instructions");
   TestFiles.Log("Native caches relocated: Team="+team.ToString("X8")+" Persist="+persist.ToString("X8")+" Inventory="+inventory.ToString("X8")+"; eight teams, eight distinct alliances, 48 active worms in 64 resource/actor slots; current Human/CPU configuration is recorded in team-settings.txt; eight-row bottom HUD and eight upper health widgets; separate manager queues, eight name colors, cyclic team turns and eight-entry weapon delay query cache. Extra team health fills preserve gray empty/frame vertices; round award names use actual alliance colors.");
   TestFiles.Log("v33 mesh request 550 -> 4096; retained v31 pool guards: normal sprite request 250 -> 1024; normal bitmap request 700 -> 2048; separate 253 usable acting sources.");
   if(ResumeThread(info.thread)==UInt32.MaxValue) throw Win32("Resume test game");
   return WaitForChild(info.process,info.thread,info.processId,team,persist,inventory);
  } catch {
   TerminateProcess(info.process,0xc0de0001);
   DebugActiveProcessStop(info.processId);
   WaitForSingleObject(info.process,3000);
   throw;
  } finally { CloseHandle(info.thread);CloseHandle(info.process); }
 }
}
