using System;using System.Collections.Generic;
internal static class NativeParticleCache64 {
internal const int CacheBase=0x188,Capacity=64,ObjectSize=0x288,CodeSize=96;
internal sealed class Routine {public uint Original;public int Offset,HookSize;public byte[] Expected,Template;public uint[][] Fixups;public Routine(uint a,int o,int h,byte[] e,byte[] t,uint[][] f){Original=a;Offset=o;HookSize=h;Expected=e;Template=t;Fixups=f;}}
static byte[] H(string s){var b=new byte[s.Length/2];for(int i=0;i<b.Length;i++)b[i]=Convert.ToByte(s.Substring(i*2,2),16);return b;}
internal static readonly Routine[] Routines=new Routine[]{
new Routine(0x560becu,0,7,H("33c0b9100000008dbe40010000f3ab"),H("33c0b9400000008dbe88010000f3abe900000000"),new uint[][]{new uint[]{16,1,0x560bfbu}}),
new Routine(0x561214u,32,11,H("0fb648308b948d40010000"),H("0fb6483083f9400f83000000008b948d88010000e900000000"),new uint[][]{new uint[]{9,1,0x5614c8u},new uint[]{21,1,0x56121fu}}),
new Routine(0x5618a9u,64,6,H("898e40010000898e44010000898e48010000898e4c010000898e50010000898e54010000898e58010000898e5c010000898e60010000898e64010000898e68010000898e6c010000898e70010000898e74010000898e78010000898e7c010000"),H("9c505157fc33c0b9400000008dbe88010000f3ab5f59589de900000000"),new uint[][]{new uint[]{25,1,0x561909u}}),
};
internal static readonly Routine[] Fixtures=new Routine[]{
new Routine(0x560c70u,0,0,H("a1e479820053568bf1f6407a0257741a8b0d78c68200683c0f790068d201000068f00e7900e8e6b3f7ff8bbe3401000033db3bfb74148bcfe8939008008bcfe8bd05eaff899e340100008b86800100003bc374068b0850ff5108899e800100008b86240100003bc374068b1050ff5208899e240100008b86280100003bc374068b0850ff5108899e280100008b862c0100003bc374068b1050ff5208899e2c0100008b86300100003bc374068b0850ff51085f899e300100008d4e205e5be94d4e0c00cc"),H("a1e479820053568bf1f6407a0257741a8b0d78c68200683c0f790068d201000068f00e7900e8e6b3f7ff8bbe3401000033db3bfb74148bcfe8939008008bcfe8bd05eaff899e340100008b86800100003bc374068b0850ff5108899e800100008b86240100003bc374068b1050ff5208899e240100008b86280100003bc374068b0850ff5108899e280100008b862c0100003bc374068b1050ff5208899e2c0100008b86300100003bc374068b0850ff51085f899e300100008d4e205e5be94d4e0c00cc"),new uint[][]{new uint[]{1,0,0x8279e4u},new uint[]{18,0,0x82c678u},new uint[]{23,0,0x790f3cu},new uint[]{33,0,0x790ef0u},new uint[]{38,1,0x4dc080u},new uint[]{57,1,0x5e9d40u},new uint[]{64,1,0x401271u},new uint[]{191,1,0x625b80u}}),
new Routine(0x561cfdu,0,0,H("8d8640010000ba100000008b083bcb74063bcf7302891883c0044a75ee"),H("8d8688010000ba400000008b083bcb74063bcf7302891883c0044a75ee"),new uint[][]{}),
};
internal static byte[] Build(Routine r,uint at,Func<uint,uint> bind){var b=(byte[])r.Template.Clone();foreach(var f in r.Fixups){uint t=bind==null?f[2]:bind(f[2]);Array.Copy(BitConverter.GetBytes(f[1]!=0?unchecked(t-at-f[0]-4):t),0,b,(int)f[0],4);}return b;}
internal static List<NativeWormCaches64.Patch> GetPatches(uint code){var p=new List<NativeWormCaches64.Patch>();
p.Add(new NativeWormCaches64.Patch(0x5615d8u,H("b988010000"),H("b988020000")));
p.Add(new NativeWormCaches64.Patch(0x561239u,H("89948d40010000"),H("89948d88010000")));
p.Add(new NativeWormCaches64.Patch(0x561cfdu,H("8d8640010000"),H("8d8688010000")));
p.Add(new NativeWormCaches64.Patch(0x561d03u,H("ba10000000"),H("ba40000000")));
foreach(var r in Routines){uint at=code+(uint)r.Offset;var b=Build(r,at,null);p.Add(new NativeWormCaches64.Patch(at,new byte[b.Length],b));var old=new byte[r.HookSize];Array.Copy(r.Expected,old,old.Length);var hook=new byte[old.Length];for(int i=0;i<hook.Length;i++)hook[i]=0x90;hook[0]=0xe9;Array.Copy(BitConverter.GetBytes(unchecked(at-r.Original-5)),0,hook,1,4);p.Add(new NativeWormCaches64.Patch(r.Original,old,hook));}return p;}
internal static void CheckDisk(byte[] disk){foreach(var r in Routines)for(int i=0;i<r.Expected.Length;i++)if(disk[r.Original-0x400000+i]!=r.Expected[i])throw new Exception("Particle cache routine guard");foreach(var r in Fixtures)for(int i=0;i<r.Expected.Length;i++)if(disk[r.Original-0x400000+i]!=r.Expected[i])throw new Exception("Particle cleanup/expiry guard");foreach(var p in GetPatches(0x200000))if(p.Address>=0x400000)for(int i=0;i<p.Expected.Length;i++)if(disk[p.Address-0x400000+i]!=p.Expected[i])throw new Exception("Particle cache scalar guard");}
}
