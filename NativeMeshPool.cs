using System;using System.Collections.Generic;
internal static class NativeMeshPool {
 internal const int OriginalCapacity=550,Capacity=4096,Stride=0xD0;
 static byte[] H(string s){var b=new byte[s.Length/2];for(int i=0;i<b.Length;i++)b[i]=Convert.ToByte(s.Substring(i*2,2),16);return b;}
 internal static byte[] Build(uint cave,Func<uint,uint> bind){
  // Promote only a standard 550-WORD request. Custom and zero-size requests,
  // in-use rejection, vector construction/destruction and free-list code stay
  // in the original routine. Replay the stolen MOV before returning to it.
  byte[] b=H("66817c24042602750766c74424040010a190448500e900000000");
  Array.Copy(BitConverter.GetBytes(bind(0x854490)),0,b,17,4);
  Array.Copy(BitConverter.GetBytes(unchecked(bind(0x427827)-cave-(uint)b.Length)),0,b,b.Length-4,4);return b;
 }
 internal static void CheckDisk(byte[] disk){byte[] expected=H("a1904485002b059c448500556685c0668b6c2408740ab805400080e9e0000000663b2d904485000f84d1000000a18c4485005333db3bc35657741a8d48fc3919740e8b106a038bc8ff9284000000eb05e8fa99fdff8b0d98448500e8ef99fdff663beb750e891d8c448500891d98448500eb4c0fb7fd8bcf69c9d000000083c10433d2e87a98fdff3bc3741e68532c4200578d700468d0000000568938e879c2fdff89358c448500eb06891d8c4485008d0c3f33d2e84898fdffa39844850033f6663beb66892d9044850066892d9c448500762733ffa18c44850003c78b085650ff5134a198448500668934034681c7d00000004343663bf572db5f5e5b33c05dc20400");for(int i=0;i<expected.Length;i++)if(disk[0x27822+i]!=expected[i])throw new Exception("Mesh resize body differs from the checked build.");}
 internal static List<NativeWormCaches64.Patch> GetPatches(uint cave){
  byte[] stub=Build(cave,x=>x),hook=H("e900000000");Array.Copy(BitConverter.GetBytes(unchecked(cave-0x427827u)),0,hook,1,4);
  return new List<NativeWormCaches64.Patch>{new NativeWormCaches64.Patch(cave,new byte[stub.Length],stub),new NativeWormCaches64.Patch(0x427822,H("a190448500"),hook)};
 }
}
