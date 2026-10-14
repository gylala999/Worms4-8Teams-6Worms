using System;
using System.Collections.Generic;
internal static class NativeCpuTables64 {
 internal const int Entries=3584, Flags=3840, Count=64;
 static byte[] H(string s){var b=new byte[s.Length/2];for(int i=0;i<b.Length;i++)b[i]=Convert.ToByte(s.Substring(i*2,2),16);return b;}
 static NativeWormCaches64.Patch P(uint at,string expected,int operand,uint data,int relative){var old=H(expected);var b=(byte[])old.Clone();Array.Copy(BitConverter.GetBytes(data+(uint)relative),0,b,operand,4);return new NativeWormCaches64.Patch(at,old,b);}
 internal static List<NativeWormCaches64.Patch> GetPatches(uint data){return new List<NativeWormCaches64.Patch>{
P(0x61E736u,"c68628b2830001",2,data,3840),
P(0x61E73Fu,"889e28b28300",2,data,3840),
P(0x61E745u,"891cb5e8b18300",3,data,3584),
P(0x621C8Du,"889828b28300",2,data,3840),
P(0x621C93u,"890c85e8b18300",3,data,3584),
P(0x621CB0u,"389e28b28300",2,data,3840),
P(0x621CCEu,"889e28b28300",2,data,3840),
P(0x621CF1u,"baecb18300",1,data,3588),
P(0x621F0Au,"bee8b18300",1,data,3584),
P(0x621F22u,"81fe28b28300",2,data,3840)
 };}
}
