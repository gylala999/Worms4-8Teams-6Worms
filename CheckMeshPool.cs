using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

// The production mesh resize clone and original allocation/release APIs run
// only in this private x86 process. Heap, object construction/destruction and
// image initialization are mocked; pool indexing and bookkeeping are native.
internal static class CheckMeshPool {
 [DllImport("kernel32.dll")] static extern IntPtr VirtualAlloc(IntPtr p,UIntPtr n,uint t,uint flags);
 [DllImport("kernel32.dll")] static extern bool VirtualProtect(IntPtr p,UIntPtr n,uint flags,out uint old);
 [DllImport("kernel32.dll")] static extern bool VirtualFree(IntPtr p,UIntPtr n,uint t);
 [UnmanagedFunctionPointer(CallingConvention.StdCall)] delegate int Resize(int n);
 [UnmanagedFunctionPointer(CallingConvention.StdCall)] delegate int Create(IntPtr resource,IntPtr output,int section,int flags);
 [UnmanagedFunctionPointer(CallingConvention.StdCall)] delegate int Release(IntPtr resource,IntPtr mesh);
 [UnmanagedFunctionPointer(CallingConvention.StdCall)] delegate int ObjectCall(IntPtr self);
 [UnmanagedFunctionPointer(CallingConvention.StdCall)] delegate IntPtr Alloc(int size,int flags);
 [UnmanagedFunctionPointer(CallingConvention.StdCall)] delegate int VectorCtor(IntPtr first,int stride,int count,IntPtr constructor);
 [UnmanagedFunctionPointer(CallingConvention.StdCall)] delegate int Delete(IntPtr self,int flags);
 [UnmanagedFunctionPointer(CallingConvention.StdCall)] delegate IntPtr Free(IntPtr p);
 static IntPtr code,data;static int next=0x1000,checks,constructed,destroyed,freed;
 static readonly List<Delegate> keep=new List<Delegate>();
 static readonly Dictionary<uint,uint> bind=new Dictionary<uint,uint>();
 static readonly Dictionary<uint,int> blocks=new Dictionary<uint,int>();
 static readonly List<IntPtr> rawBlocks=new List<IntPtr>();
 static uint P(IntPtr p){return unchecked((uint)p.ToInt32());}static IntPtr A(uint p){return new IntPtr(unchecked((int)p));}
 static uint Read(int at){return unchecked((uint)Marshal.ReadInt32(data,at));}
 static void Write(int at,uint n){Marshal.WriteInt32(data,at,unchecked((int)n));}
 static int Word(int at){return (ushort)Marshal.ReadInt16(data,at);}
 static void Need(bool ok,string text){if(!ok)throw new Exception(text);checks++;Console.WriteLine("PASS "+text);}
 static byte[] H(string s){s=s.Replace(" ","");var b=new byte[s.Length/2];for(int i=0;i<b.Length;i++)b[i]=Convert.ToByte(s.Substring(i*2,2),16);return b;}
 static uint Add(byte[] b){uint p=P(code)+(uint)next;Marshal.Copy(b,0,A(p),b.Length);next=(next+b.Length+15)&~15;return p;}
 static uint Direct(Delegate d){keep.Add(d);return P(Marshal.GetFunctionPointerForDelegate(d));}
 static uint Bridge(Delegate d,bool fast){uint at=P(code)+(uint)next;var b=new List<byte>(H(fast?"52 51":"51"));b.Add(0xe8);b.AddRange(BitConverter.GetBytes(unchecked(Direct(d)-(at+(uint)b.Count+4))));b.Add(0xc3);return Add(b.ToArray());}
 static uint Resolve(uint original){uint p;if(!bind.TryGetValue(original,out p))throw new Exception("Unbound fixture address "+original.ToString("X"));return p;}
 static T Fn<T>(uint p){return (T)(object)Marshal.GetDelegateForFunctionPointer(A(p),typeof(T));}
 static int Main(string[] args){
  code=VirtualAlloc(IntPtr.Zero,(UIntPtr)65536,0x3000,4);data=Marshal.AllocHGlobal(0x6000);
  try{
   if(code==IntPtr.Zero)throw new Exception("Private code allocation");Marshal.Copy(new byte[0x6000],0,data,0x6000);
   byte[] disk=File.ReadAllBytes(args[0]);NativeMeshPool.CheckDisk(disk);
   uint d=P(data),vt=d+0x100,resource=d+0x300,resourceVt=d+0x400,output=d+0x500;
   bind.Add(0x85448c,d);bind.Add(0x854490,d+4);bind.Add(0x854498,d+8);bind.Add(0x85449c,d+12);
   bind.Add(0x851f58,d+0x1000);bind.Add(0x84c4a0,d+0x10);
   uint log=Add(H("8b c1 c3"));foreach(uint a in new uint[]{0x4045b5,0x40446f})bind.Add(a,log);
   foreach(uint a in new uint[]{0x7e0cf8,0x7e064c,0x7d463c})bind.Add(a,d+0x550);
   uint zero=Add(H("33 c0 c3"));bind.Add(0x422c53,zero);
   var descriptor=H("b8 00 00 00 00 c2 04 00");Array.Copy(BitConverter.GetBytes(d+0x580),0,descriptor,1,4);
   bind.Add(0x4268fc,Add(descriptor));bind.Add(0x426910,Add(descriptor));bind.Add(0x4254fb,Add(H("33 c0 c2 04 00")));
   Alloc allocator=(size,flags)=>{if(flags!=0 || size<=0)throw new Exception("Invalid allocation");IntPtr raw=Marshal.AllocHGlobal(size+8);rawBlocks.Add(raw);Marshal.Copy(new byte[size+8],0,raw,size+8);Marshal.WriteInt32(raw,0,0x12345678);Marshal.WriteInt32(raw,size+4,0x76543210);uint p=P(raw)+4;blocks.Add(p,size);return A(p);};
   Free free=p=>{if(p==IntPtr.Zero)return p;int size;if(!blocks.TryGetValue(P(p),out size))throw new Exception("Unknown block freed");Need(Marshal.ReadInt32(A(P(p)-4))==0x12345678 && Marshal.ReadInt32(p,size)==0x76543210,"mesh block boundaries survive cleanup");blocks.Remove(P(p));freed++;return IntPtr.Zero;};
   Delete destroy=(self,flags)=>{if(flags!=3)throw new Exception("Vector delete flags");int count=Marshal.ReadInt32(self,-4);destroyed+=count;free(A(P(self)-4));return 0;};
   VectorCtor constructor=(first,stride,count,callback)=>{if(stride!=0xd0 || callback!=A(zero) || (count!=550 && count!=4096 && count!=16))throw new Exception("Mesh constructor dimensions");for(int i=0;i<count;i++)Marshal.WriteInt32(first,i*stride,unchecked((int)vt));constructed+=count;return 0;};
   bind.Add(0x401124,Bridge(allocator,true));bind.Add(0x401271,Bridge(free,false));bind.Add(0x403b3d,Direct(constructor));
   Marshal.WriteInt32(A(vt),0x84,unchecked((int)BridgeDelete(destroy)));
   // Render/image setup is external to the pool; retain real native indices,
   // references and resource accounting around this one mocked boundary.
   Create initialize=(self,res,section,flags)=>{uint p=P(self),first=Read(0);int capacity=Word(4);if(p<first || (p-first)%0xd0!=0 || (p-first)/0xd0>=capacity || res!=A(resource) || section!=3)throw new Exception("Mesh initializer target");Marshal.WriteInt32(self,0x78,unchecked((int)resource));Marshal.WriteByte(self,4,(byte)section);return 0;};
   Marshal.WriteInt32(A(vt),0x28,unchecked((int)Direct(initialize)));
   uint[] apis={0x427822,0x427f86,0x42804e,0x428368,0x43144e,0x431460,0x43248c,0x41b5a9,0x41b5b3,0x431346,0x431369,0x43139b,0x42f382,0x42f39e};
   for(int i=0;i<apis.Length;i++)bind.Add(apis[i],P(code)+0x3000u+(uint)i*0x300);
   for(int i=0;i<apis.Length;i++){uint at=Resolve(apis[i]);byte[] b=MeshFixtureData.Build(apis[i],at,Resolve,disk);if(b.Length>0x300)throw new Exception("Fixture overlap");Marshal.Copy(b,0,A(at),b.Length);}
   foreach(var pair in new[]{Tuple.Create(4,0x431346u),Tuple.Create(8,0x431369u),Tuple.Create(0x1c,0x43139bu),Tuple.Create(0x30,0x43248cu),Tuple.Create(0x34,0x43144eu),Tuple.Create(0x38,0x431460u),Tuple.Create(0x3c,0x41b5a9u),Tuple.Create(0x40,0x41b5b3u)})Marshal.WriteInt32(A(vt),pair.Item1,unchecked((int)Resolve(pair.Item2)));
   Marshal.WriteInt32(A(vt),0x88,unchecked((int)Add(H("33 c0 c2 04 00"))));
   Write(0x300,resourceVt);Write(0x334,0);Marshal.WriteInt32(A(resourceVt),8,unchecked((int)Add(H("33 c0 c2 04 00"))));
   Marshal.WriteInt32(A(resourceVt),0x40,unchecked((int)Add(H("8b 44 24 04 83 c0 34 c2 04 00"))));
   bind.Add(0x427827,Resolve(0x427822)+5);uint expanded=P(code)+0x8000;byte[] modified=NativeMeshPool.Build(expanded,Resolve);Marshal.Copy(modified,0,A(expanded),modified.Length);
   uint old;if(!VirtualProtect(code,(UIntPtr)65536,0x20,out old))throw new Exception("Fixture execute protection");
   var originalResize=Fn<Resize>(Resolve(0x427822));var resize=Fn<Resize>(expanded);var create=Fn<Create>(Resolve(0x427f86));var release=Fn<Release>(Resolve(0x42804e));var releaseRef=Fn<ObjectCall>(Resolve(0x431369));
   Need(originalResize(550)==0 && Word(4)==550,"original native mesh pool has exactly 550 slots");
   var live=new List<uint>();Fill(550,create,resource,output,live);
   Marshal.WriteInt32(A(output),0);Need(create(A(resource),A(output),3,0)==unchecked((int)0x80004005) && Marshal.ReadInt32(A(output))==0,"original mesh exhaustion reproduces the inventory's null result");
   Clear(live,release,releaseRef,resource);Need(originalResize(0)==0 && blocks.Count==0,"original 550-slot pool is fully released and cleaned up");
   Need(resize(unchecked((int)0xabcd0226))==0 && Word(4)==4096 && Word(12)==4096,"550 WORD request creates 4096 mesh slots even with upper argument bits set");
   for(int i=0;i<4096;i++)Need((ushort)Marshal.ReadInt16(A(Read(8)),i*2)==i && (ushort)Marshal.ReadInt16(A(Read(0)),i*0xd0+8)==i,"mesh free list and native index agree at slot "+i);
   Fill(4096,create,resource,output,live);Need(live.Count==4096 && Word(12)==0 && Read(0x1000)==4096 && Read(0x31c)==4096,"actual allocation crosses slot 550 through all 4096 distinct meshs with balanced resource counts");
   uint objects=Read(0),list=Read(8);int ctorCount=constructed;
   Need(resize(0)==unchecked((int)0x80004005) && resize(550)==unchecked((int)0x80004005) && Read(0)==objects && Read(8)==list && constructed==ctorCount,"in-use guard prevents cleanup and reallocation of live meshs");
   Marshal.WriteInt32(A(output),0);Need(create(A(resource),A(output),3,0)==unchecked((int)0x80004005) && Marshal.ReadInt32(A(output))==0,"expanded full-pool failure leaves the output empty without corrupting storage");
   Clear(live,release,releaseRef,resource);Need(Word(12)==4096 && Read(0x1000)==0 && Read(0x31c)==0,"native release returns every mesh slot and balances resource bookkeeping");
   Fill(4096,create,resource,output,live);Clear(live,release,releaseRef,resource);Need(Word(12)==4096,"all expanded mesh slots can be reused after release");
   Need(resize(0)==0 && Read(0)==0 && Read(8)==0 && Word(4)==0 && Word(12)==0 && blocks.Count==0 && constructed==destroyed,"zero-size cleanup destroys every constructed mesh and releases both allocations");
   Need(resize(16)==0 && Word(4)==16 && Word(12)==16,"custom mesh capacities retain original behavior");Need(resize(0)==0 && blocks.Count==0 && constructed==destroyed && freed==6,"custom-capacity cleanup remains balanced");
   Console.WriteLine("RESULT: "+checks+" mesh-pool assertions passed; live crowded cluster-bomb test pending.");GC.KeepAlive(keep);return 0;
  }catch(Exception e){Console.Error.WriteLine(e);return 2;}
  finally{foreach(IntPtr p in rawBlocks)Marshal.FreeHGlobal(p);Marshal.FreeHGlobal(data);if(code!=IntPtr.Zero)VirtualFree(code,UIntPtr.Zero,0x8000);}
 }
 static uint BridgeDelete(Delete callback){uint at=P(code)+(uint)next;var b=new List<byte>(H("ff 74 24 04 51 e8"));b.AddRange(BitConverter.GetBytes(unchecked(Direct(callback)-(at+(uint)b.Count+4))));b.AddRange(H("c2 04 00"));return Add(b.ToArray());}
 static void Fill(int n,Create create,uint resource,uint output,List<uint> live){var unique=new HashSet<uint>();for(int i=0;i<n;i++){Marshal.WriteInt32(A(output),0);int result=create(A(resource),A(output),3,0);uint p=unchecked((uint)Marshal.ReadInt32(A(output))),first=Read(0);Need(result==0 && p>=first && (p-first)%0xd0==0 && (p-first)/0xd0<Word(4) && unique.Add(p),"native mesh allocation returns a distinct valid slot "+i);live.Add(p);}}
 static void Clear(List<uint> live,Release release,ObjectCall releaseRef,uint resource){foreach(uint p in live){if(release(A(resource),A(p))!=0 || releaseRef(A(p))!=0 || Marshal.ReadInt32(A(p),0x78)!=0)throw new Exception("Mesh release/reference cleanup");}live.Clear();}
}
