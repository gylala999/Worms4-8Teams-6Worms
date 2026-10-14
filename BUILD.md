# Build the v33 source

Requirements: Windows, Windows PowerShell 5.1 or later, and the .NET Framework 4.x C# compiler. The script checks the normal Framework64 and Framework compiler paths under the Windows directory. No downloaded packages are needed.

From the repository folder:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\build.ps1
```

This compiles the launcher as **x86**, creates `dist\W4-EightStressTest-v33.exe`, copies the five payloads into `dist\CodexTeamTest`, and creates `artifacts\W4-EightStressTest-v33-source-build.zip`. It does not launch the game or install into your game folder.

For the isolated checks, supply your own compatible build 3013 installation with the game closed:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\build.ps1 -RunChecks -GameFolder 'C:\Games\Worms 4 Mayhem'
```

The checks verify the original executable hash, read the original game assets or verified backups, and create a separate fixture under `artifacts`. They copy into that fixture; they do not change the supplied game installation or launch its game process. The mesh fixture runs native x86 code in its own process with mocked external boundaries. Test logs and generated fixture files stay local and are ignored by `.gitignore`.

The production source and five payloads are copied unchanged from the tested v33 build. `CheckFriendLauncher.cs` has a packaging adaptation so its fixture can read a vanilla installation or verified backup instead of depending on existing author-side backups. The build script replaces the fixture profile's root only in generated local test source.

The preserved download ZIP is included separately. Compilation timestamps and archive metadata can cause source-build hashes to differ; its file hashes are not expected to match a freshly rebuilt package byte for byte. `SHA256SUMS.txt` records the published repository files except itself.

The included native patch modules guard against the exact executable bytes. Changes to a different game version require analysis and new guards, not removal of the existing checks.
