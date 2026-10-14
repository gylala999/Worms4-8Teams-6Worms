using System;
internal static class Profile {
 public static readonly string Root = System.IO.Path.GetFullPath(AppDomain.CurrentDomain.BaseDirectory);
 public const string ExeHash = "A5C51FDCA84AD42FD11A15194AE812F1C09E075C42731C36391D0E8756A53E0C";
 public static readonly string[] Rel = { @"data/scripts/stdvs.lua",@"data/Tweak/Local.xml",@"data/Tweak/Persist.xml",@"data/Tweak/HudTwk.xml",@"data/Tweak/AITwk.xml" };
 public static readonly string[] Original = { @"stdvs.original.lua",@"Local.original.xml",@"Persist.original.xml",@"HudTwk.original.xml",@"AITwk.original.xml" };
 public static readonly string[] Modified = { @"stdvs.eight-config-v31.lua",@"Local.eight-config-v31.xml",@"Persist.eight-config-v31.xml",@"HudTwk.eight-config-v31.xml",@"AITwk.eight-config-v31.xml" };
 public static readonly string[] OriginalHash = { @"AA67EE6E8AB3C39F9EB8D495549AAF42FC26F74085FD4E6E8DBE88EE93DC6595",@"DDE904C8B3BC711F828C3028C0471F3FC2AFD0495BA4F32B829320720EE07B40",@"19A7125DED743F69DED751C525DA96E47935BEFE3BDEA80F13B6A171F7F8799C",@"17BD7E280A99BE8CFBFC18B0D3547DCF1897FB846F836DFCEA901DF5FA12795D",@"89EEA5773C29E6B65FAE523055275EBCFE63E19081508D7551F222ED93E60FCB" };
 public static readonly string[] ModifiedHash = { @"175A4B239C91BC1D15436FC55EA54F260DDA15F7F1F09F40A05D4BB4CB631D8E",@"1100C0EFA811EEE5F440E8399F905707E8700FD669E1CD6863A571E3D9B66212",@"C53E6BDEA92A0959ED5BE2DAA8B2A37ADE771AD4A955243F85C84F6AC2110831",@"4836EB1126223C1A8BC77ACD38679D6E5F8E0CE86EB3E663E395502ACA5FD40D",@"3EB71D5628C696B65D5953F091229CDE153CCCD585C950F62A5C7F4C477D120A" };
 public const string Id = "eight-stress-v33";
 public const string Title = "Worms 4 - Eight-Team Stress Test v33";
 public const string Heading = "Eight-team stress test - v33";
 public const string Instructions = "Choose Human or CPU and 1-6 worms for each team.\r\nCPU difficulty is fixed at Skill 2. Each worm starts at 100 health.\r\n\r\nIn game: Local Multiplayer > Deathmatch, choose TWO menu teams,\r\nStandard rules and empty Wormpot, then start.\r\nKeep this launcher open until the game closes.";
 public const string ButtonText = "Start configured match";
 public const string RunningText = "Configured eight-team test running.";
 public const bool Enabled = true;
 public const bool NativeCachePatch = true;
}
