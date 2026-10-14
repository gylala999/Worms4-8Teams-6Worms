# Testing and development history

This is a summary of local evidence and tester reports through 14 October 2026. Private crash dumps, full game logs, original backups and save data are not included.

## From repeated crashes to playable matches

| Stage | Observation and change |
| --- | --- |
| Early experiments / v18 | Eight teams with one worm were possible, but some match-ending paths still assumed fewer teams. |
| v25 | Corrected rival-team and victory handling; an eight-team, one-worm match reached the expected final winner. |
| v26 | A Lua string-library assumption caused an empty arena. |
| v27 | Eight teams × four worms loaded; selection, damage, deaths and final Team 7 versus Team 8 victory were tested. Surrender remained problematic. |
| v28 | Eight teams × six worms hit loading resource limits. |
| v29 | Expanded loading pools; a later Mouse 2 inventory test exhausted the original bitmap pool and crashed. |
| v30 | Expanded the bitmap pool to 2048. Inventory and CPU turns worked, but an elimination crash exposed an effect cache still sized for sixteen worms. |
| v31 | Expanded that cache to 64 and added the eight-row Human/CPU and 1–6 worm configuration. An alternating Human/CPU, 48-worm match completed, with Team 5 the sole winner. |
| v32 | Added portable friend packaging, separate reports for every run, restoration checks, report ZIPs and retry after a crash. Crowded Cluster Bomb testing exhausted the standard 550-mesh pool. |
| v33 | Expanded the standard mesh request to 4096 and added mesh capacity/free counts to crash diagnostics. A later live test exposed a separate null resource lookup crash. |

Versions describe experiments and fixes; they do not imply that every earlier crash is covered or that all modes have been tested. Earlier private builds and evidence are preserved separately.

## v33 isolated checks

The original local v33 build passed **12,963 assertions/checks**:

| Check | Count | Scope |
| --- | ---: | --- |
| CheckMeshPool | 12,856 | Native pool allocation/release, exhaustion at 550 and 4096, reuse, bookkeeping, reference balance, cleanup, custom requests, in-use guard and boundary canaries. Heap and construction/render boundaries are mocked. |
| CheckArena64 | 2 | All integrated patch ranges fit their arenas, do not overlap, and original hook bytes match. |
| CheckFriendLauncher | 73 | Simulated crash/normal exit/start failure, verified restoration, separate reports, ZIP contents, retry and interrupted recovery. No game process is launched by this fixture. |
| CheckTeamConfig | 32 | Six configurations round-trip and render correctly; malformed input and invalid template markers are rejected. |

The public fixture reads checked original game files or checked backups supplied by the tester. It does not require the author's installation path. These checks exercise isolated behavior; they cannot certify live game stability.

## Latest captured v33 live stress session

- Eight Human teams, six worms each: **48 worms**.
- Map recorded as `Multi_TraitorousWaters.xan`.
- Session ran for approximately **23 minutes**, with four level initializations and three completed round-stat imports.
- The tester reported successful crowded Cluster Bomb knockouts/drowning and later a crash after a Bazooka destroyed a wall and multiple worms reacted.
- Exception: `0xC0000005`, fault address **`0x0041E0D1`**.
- Captured free counts: mesh **3566 / 4096**, bitmap **1260 / 2048**, sprite **755 / 1024**.
- The immediate fault dereferenced a null resource pointer. The calling path performed a weapon/utility resource-name lookup. The exact missing name/enum and cause of lookup failure have not been established.
- The final weapon log mentioned `kWeaponBazooka` and a `StoreShotsFiredWormIndex 18` warning. The warning is not proven to cause the crash.
- The launcher restored and verified the five original game data files after this failure.

This shows that the latest crash was not mesh-pool exhaustion. It does not prove that the engine is simply too weak or that adding more slots would solve it.

## Useful next gameplay tests

1. A baseline 48-worm match with Standard rules and no Wormpot bonuses, including Mouse 2 inventory and completion to the results screen.
2. One Human plus seven CPU teams, then alternating Human/CPU.
3. Eliminate whole teams, including Teams 5–8, and check turn skipping and victory.
4. Crowded Cluster Bomb damage, then Bazooka terrain destruction, recording the map and round number.
5. Compare walking-weapon contact near Teams 1–4 and 5–8.
6. Observe idle worms after their supporting terrain is destroyed; record whether small movement causes falling.

Change one variable at a time and preserve the report from every failure. See BUG_REPORT.md.
