Worms 4 Mayhem - Eight-Team Stress Test v33

This package contains the eight-team mod and launcher, not the game.
Use your own Worms 4 Mayhem installation. The launcher requires the exact
original executable and five original data files from the checked build
3013. It checks SHA256 hashes before applying the mod. Other versions,
Worms Ultimate Mayhem and already edited game data are not supported.

Extract this ZIP directly into the Worms 4 Mayhem game folder. Keep the
included CodexTeamTest folder next to W4-EightStressTest-v33.exe and the
game's WORMS 4 MAYHEM.EXE. The folder does not need to be on drive D:.
Run W4-EightStressTest-v33.exe. Missing original backups are created from
your own checked files before testing; existing backups are verified.
The original game executable is not modified on disk.

Choose Human or CPU and 1-6 worms for each of the eight teams.
CPU difficulty is fixed at Skill 2; each worm starts at 100 health.
Click Start configured match. In game choose Local Multiplayer >
Deathmatch, TWO menu teams, Standard rules and an empty Wormpot.
Keep the launcher open until the game closes and restoration finishes.

First stress test: set all eight teams to Human and six worms, then repeat
the crowded-worm Cluster Bomb test with the same rules, bonuses and looks.
Record the map and bonuses in tester-notes.txt. Check Mouse 2 inventory.
The earlier v31 alternating Human/CPU match completed with Team 5 winning;
that evidence is preserved separately from this new v33 stress-test build.
Known issue: surrender can leave teams controllable; it is still deferred.
Other configurations, maps and friend PCs need further gameplay testing.

IF THE GAME CRASHES
1. Keep the launcher open and wait until it reports original files restored.
2. Click Open test logs. Each run has a separate folder containing settings,
   result, game/graphics logs and native crash details when available.
3. Add the map, action just before the crash, eliminated teams and any other
   useful details to tester-notes.txt. Screenshots can go into that folder.
4. Click Make report ZIP, then manually send that ZIP to the mod developer.
5. You can click Start configured match again. Earlier reports stay saved.
   If restoration fails, use Restore original files after the game closes;
   another run checks the files again before applying the mod.

Reports: CodexTeamTest\Reports\eight-stress-v33\
The launcher does not upload or send anything automatically.
Report ZIPs contain logs/settings/notes, not the game or original backups.
Native crash details can include small diagnostic memory excerpts.
If the launcher was interrupted, reopen the same launcher and click
Restore original files with the game closed before continuing.

v33 keeps the v32 portable launcher, retry and per-run reports. Its gameplay
change promotes the original standard 550-mesh pool request to 4096 entries.
The new prefix preserves zero-size cleanup, custom requests and the native
in-use guard. The original allocation/release code is retained. Mesh capacity
and free count are now included in captured crash diagnostics.

This addresses the recorded crowded Cluster Bomb test: the game log reports
all 550 mesh instances used, then a null-render-object crash at 00541B07.
The private native fixture reproduces original exhaustion and verifies all
4096 slots, reference/resource balance, release/reuse and boundary canaries.
Live crowded-explosion testing is still required; the larger pool is finite.

Other reported issues remain unresolved in this build: some walking weapons
fail to detonate near Teams 5-8; UFO worm pickup needs investigation; and
idle worms sometimes remain suspended after terrain destruction until moved.
The old surrender/control issue remains deferred. General CPU attacks on all
eight teams are reported to work; walking-weapon detonation is a separate issue.

First v33 stress test: repeat the same crowded-worm Cluster Bomb test and
record the map, Human/CPU settings and any Wormpot damage/weapon bonuses.
If it crashes, keep the launcher open, wait for original-file restoration,
then add notes and click Make report ZIP before starting another test.
