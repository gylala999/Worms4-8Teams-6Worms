-- Separate local eight-team/six-worm test v31; requires its matching launcher.

-- standard VS game

function Initialise()
   StartedSuddenDeath = false
   
   SetupScheme()

   Codex_SetupEightTeams()
   SendMessage("WormManager.Reinitialise")
   Codex_LogNativeCounts()

   lib_SetupMinesAndOildrums() -- do this after worms are set up to get correct worm collision


   local scheme = QueryContainer("GM.SchemeData")
   if scheme.RoundTime == 0 then
      StartSuddenDeath() -- do this after worms are set up to readjust health
      local GameOver = GetData("FCS.GameOver")
      -- do no further processing if sudden death drew the game
      if GameOver ~= 0 then
         return 
      end
   end
   
   if scheme.MineFactoryOn == true then
      SendMessage("GameLogic.CreateRandMineFactory")
   end
   
   if scheme.TelepadsOn == true then
       SendMessage("GameLogic.PlaceTelepads")       
   end
   
   SetData("Camera.StartOfTurnCamera","Default")
   
   SetWormpotModes()
   

   WaitingForStartFirstTurn = false
   StartFirstTurn()
--   Activity = GetData("ObjectCount.Active")
--   if Activity == 0 then
--		WaitingForStartFirstTurn = false
--        StartFirstTurn()
--	else
--		WaitingForStartFirstTurn = true
--	end
end


function SetSpecialWeapon(TeamIndex, WeaponName)
   SetData("SpecialWeapon.TeamIndex", WormIndex)
   SetData("SpecialWeapon.Weapon", WeaponName)
   SendMessage("GameLogic.SetSpecialWeapon")

   -- TODO
   -- uses GameLogicService to call inventory.SetWeaponCount(0, -1)
end



function DoOncePerTurnFunctions()
   -- skip all this if the end of the deathmatch
   SendMessage("WormManager.GetActiveAlliances")
   AllianceCount = GetData("AllianceCount")
   if AllianceCount >1 then
      CheckSuddenDeath()
      SendMessage("GameLogic.DropRandomCrate")
      SendMessage("GameLogic.StartMineFactory")
      DoWormpotOncePerTurnFunctions()
   end
end



function SetupScheme()

   local scheme = QueryContainer("GM.SchemeData")
   
   if scheme.FallDamage == 0 then
--       -- Don't override the wormpot setting
--       local Wormpot = QueryContainer("WormPot")
--       if Wormpot.FallingHurtsMore == false then
--           SetData("Worm.FallDamageRatio", 0)
--      end

		SendMessage("GameLogic.SetNoFallDamage")
   end
   
   SetData("HUD.Clock.DisplayRoundTime", scheme.DisplayTime)
   SetData("Crate.HealthInCrates", scheme.HealthInCrates)
   SetData("DefaultRetreatTime", scheme.LandTime)
   SetData("Land.Indestructable", GetData("FE.Land.Ind"))
   SetData("Wind.Cap", scheme.WindMaxStrength)

   SetupInventoriesAndDelays()
   SetupTeleportIn()
 
   SetData("HotSeatTime", scheme.HotSeat)
   SetData("TurnTime", scheme.TurnTime)
   SetData("RoundTime", scheme.RoundTime)


end

function SetupInventoriesAndDelays()
 
   local scheme = QueryContainer("GM.SchemeData")

   lib_SetupDefaultInventoryAndDelays()
   local index
   for index = 0,3 do
       lib_SetupTeamWeaponDelays(index, "Inventory.WeaponDelays.Default")
   end
   CopyContainer("Inventory.WeaponDelays.Default", "Inventory4.WeaponDelays")
   CopyContainer("Inventory.WeaponDelays.Default", "Inventory5.WeaponDelays")
   CopyContainer("Inventory.WeaponDelays.Default", "Inventory6.WeaponDelays")
   CopyContainer("Inventory.WeaponDelays.Default", "Inventory7.WeaponDelays")

   -- If this is the first round then no matter what the stockpiling options are
   -- of if "Stockpiling Off" then give every Alliance a copy of the Scheme inventory
   local RoundsCompleted = GetData("GameOver.RoundNumber")
   if RoundsCompleted == 0 or scheme.Stockpiling == 0 then
      CopyContainer("Inventory.Alliance.Default", "Inventory.Alliance00")
      CopyContainer("Inventory.Alliance.Default", "Inventory.Alliance01")
      CopyContainer("Inventory.Alliance.Default", "Inventory.Alliance02")
      CopyContainer("Inventory.Alliance.Default", "Inventory.Alliance03")
   -- "Stockpiling" this option gives the alliance the total of the default and the stockpile
   elseif scheme.Stockpiling == 1 then
      for i = 0,3 do
         InventoryName = lib_GetAllianceInventoryName(i)
         StockpileName = lib_GetStockpileInventoryName(i)
         CopyContainer("Inventory.Alliance.Default", InventoryName)
         SetData("GameLogic.AddInventory.Arg0", InventoryName)
         SetData("GameLogic.AddInventory.Arg1", StockpileName)
         SendMessage("GameLogic.AddInventory")
      end
   -- "Stockpiling Anti" (Stock continues to decrement throught all turns)
   elseif scheme.Stockpiling == 2 then 
      CopyContainer("Inventory.Stockpile00", "Inventory.Alliance00")
      CopyContainer("Inventory.Stockpile01", "Inventory.Alliance01")
      CopyContainer("Inventory.Stockpile02", "Inventory.Alliance02")
      CopyContainer("Inventory.Stockpile03", "Inventory.Alliance03")
   end

   -- Special weapons
   -- if option is enabled then the Alliance inventories are supplemented with
   -- the combined special weapons of all the teams
   GM = QueryContainer("GM.GameInitData")
   if scheme.Special == 1 then
       -- only add in the specials when we add the full inventories
       if RoundsCompleted == 0 or scheme.Stockpiling == 0 or scheme.Stockpiling == 1 then
          if GM.NumberOfTeams>=1 then 
             IncrementAlliedInventory(GM.T1_AlliedGroup, GM.T1_SWeapon)
          end
          if GM.NumberOfTeams>=2 then 
             IncrementAlliedInventory(GM.T2_AlliedGroup, GM.T2_SWeapon)
          end
          if GM.NumberOfTeams>=3 then 
             IncrementAlliedInventory(GM.T3_AlliedGroup, GM.T3_SWeapon)
          end
          if GM.NumberOfTeams>=4 then 
             IncrementAlliedInventory(GM.T4_AlliedGroup, GM.T4_SWeapon)
          end
       end
   end
   Codex_SetupTeamInventories()
end


function IncrementAlliedInventory(AlliedGroup, Weapon)
   local InventoryName = lib_GetAllianceInventoryName(AlliedGroup)
   SetData("GameLogic.AddInventory.Arg0", InventoryName)
   SetData("GameLogic.AddInventory.Arg1", Weapon)
   SendMessage("GameLogic.IncrementInventory")
end


function Stockpile()
   -- record the remaining stock for future rounds
   CopyContainer("Inventory.Alliance00", "Inventory.Stockpile00")
   CopyContainer("Inventory.Alliance01", "Inventory.Stockpile01")
   CopyContainer("Inventory.Alliance02", "Inventory.Stockpile02")
   CopyContainer("Inventory.Alliance03", "Inventory.Stockpile03")
end



function RoundOver()
   Stockpile()
end

function TurnStarted()
   Codex_RecordTurn()
   local scheme = QueryContainer("GM.SchemeData")
   if scheme.WormSelect == 1 then
      SendMessage("WormSelect.OptionSelected")
   end
   TeleportIn()
end



function SetupTeleportIn()
   -- Must do this BEFORE call to ReinitialiseWorms
   local scheme = QueryContainer("GM.SchemeData")
   local WormIndex = 0
   local DataId = lib_GetWormContainerName(WormIndex)
   while DataId ~= "" do
      
      lock, worm = EditContainer(DataId)
      if scheme.TeleportIn == 1 then
         worm.TeleportIn = true
      else
         worm.TeleportIn = false
      end
      CloseContainer(lock)
      
      WormIndex = WormIndex + 1
      DataId = lib_GetWormContainerName(WormIndex)
   end
end


function TeleportIn()
   local CurrentWorm = GetData("ActiveWormIndex")
   if CurrentWorm ~= -1 then
      local DataId = lib_GetWormContainerName(CurrentWorm)
      local worm = QueryContainer(DataId)
      if worm.TeleportIn == true then
         SendMessage("WormManager.TeleportIn")
      end
   end
end




function Timer_GameTimedOut()
   -- game over dealth with in CheckSuddenDeath()
end


function CheckSuddenDeath()

   -- dont start or continue sudden death if a win is pending
   -- ie. if a team already won dont risk killing it due to sudden death
   SendMessage("WormManager.GetActiveAlliances")
   local AllianceCount = GetData("AllianceCount")

   if AllianceCount>1 then
      local RoundTimeRemaining = GetData("RoundTimeRemaining")
   
      if RoundTimeRemaining == 0 and StartedSuddenDeath==false then
         StartSuddenDeath()
      end

      -- Tell game water is about to rise, so as not to award kills
      SendMessage( "GameLogic.AboutToWaterRise" )
      
      -- Raise the water level
      SetData("Water.Level", GetData("Water.Level") + GetData("Water.RiseSpeed.Current") )
   end
end


function StartSuddenDeath()
   -- called at start if No game timer

   -- Set Sudden Death Options
   -- (0 = One health, 1 = Water rise, 2 = GameOver)

   StartedSuddenDeath = true

   local scheme = QueryContainer("GM.SchemeData")
   if scheme.SuddenDeath == 0 then
      SendMessage("Comment.SuddenDeath")
      lib_SetAllWormsEnergy(1)
   -- unecessary (we can switch it on and off by the later level rise button)
   elseif scheme.SuddenDeath == 1 then
      SendMessage("Comment.SuddenDeath")
   elseif scheme.SuddenDeath == 2 then
      RoundOver()
      SendMessage("GameLogic.Draw")
   end
   
   -- Set Water Rise Speed
   -- (0 = No Rise, 1 = Slow rise, 2 = Medium rise, 3 = fast rise)
   if scheme.WaterSpeed == 0 then
      SetData("Water.RiseSpeed.Current", 0)
   elseif scheme.WaterSpeed == 1 then
      SetData("Water.RiseSpeed.Current", GetData("Water.RiseSpeed.Slow") )
   elseif scheme.WaterSpeed == 2 then
      SetData("Water.RiseSpeed.Current", GetData("Water.RiseSpeed.Medium") )
   elseif scheme.WaterSpeed == 3 then
      SetData("Water.RiseSpeed.Current", GetData("Water.RiseSpeed.Fast") )
   end


end



-- If there was some activity its finished now
function GameLogic_NoActivity()

   if WaitUntilNoActivity == true then
      SendMessage("Timer.StartPostActivity")
      WaitUntilNoActivity = false
   end
   
   if WaitingForStartFirstTurn == true then
      StartFirstTurn()
      WaitingForStartFirstTurn = false
   end
   
end


function Worm_Died()
   -- if there is only one team left abort the turn now rather than wait on
   SendMessage("WormManager.GetActiveAlliances")
   AllianceCount = GetData("AllianceCount")
   if AllianceCount < 2 then
      SendMessage("Weapon.Delete")
      SendMessage("Utility.Delete")
      SendMessage("Timer.EndTurn") 
      SendMessage("Weapon.DisableWeaponChange")
      EndTurn()
   end
end

-- CODEXNATIVE8: configurable local deathmatch; eight independent teams, 1-6 worms each.
Codex_TeamConfig = {{0,6},{2,6},{0,6},{0,6},{0,6},{0,6},{0,6},{0,6}}
-- Seven human teams and Team 2 CPU, each with an independent alliance.
-- GM.GameInitData has only four frontend team fields: do not invent T5 fields.
-- Native capacity is extended by the matching v31 launcher.
function Codex_LogLine(message)
   if type(log) == "function" then
      log("CODEXNATIVE8 " .. message)
   end
end

function Codex_SetupEightTeams()
   local wormIds = {"Worm.Data00", "Worm.Data01", "Worm.Data02", "Worm.Data03",
                    "Worm.Data04", "Worm.Data05", "Worm.Data06", "Worm.Data07",
                    "Worm.Data08", "Worm.Data09", "Worm.Data10", "Worm.Data11",
                    "Worm.Data12", "Worm.Data13", "Worm.Data14", "Worm.Data15",
                    "Worm.Data16", "Worm.Data17", "Worm.Data18", "Worm.Data19",
                    "Worm.Data20", "Worm.Data21", "Worm.Data22", "Worm.Data23",
                    "Worm.Data24", "Worm.Data25", "Worm.Data26", "Worm.Data27",
                    "Worm.Data28", "Worm.Data29", "Worm.Data30", "Worm.Data31",
                    "Worm.Data32", "Worm.Data33", "Worm.Data34", "Worm.Data35",
                    "Worm.Data36", "Worm.Data37", "Worm.Data38", "Worm.Data39",
                    "Worm.Data40", "Worm.Data41", "Worm.Data42", "Worm.Data43",
                    "Worm.Data44", "Worm.Data45", "Worm.Data46", "Worm.Data47",
                    "Worm.Data48", "Worm.Data49", "Worm.Data50", "Worm.Data51",
                    "Worm.Data52", "Worm.Data53", "Worm.Data54", "Worm.Data55",
                    "Worm.Data56", "Worm.Data57", "Worm.Data58", "Worm.Data59",
                    "Worm.Data60", "Worm.Data61", "Worm.Data62", "Worm.Data63"}
   lib_GetWormContainerName = function(id)
      if type(id) ~= "number" then return "" end
      return wormIds[id+1] or ""
   end
   local teamIds = {"Team.Data00", "Team.Data01", "Team.Data02",
                    "Team.Data03", "Team.Data04", "Team.Data05", "Team.Data06", "Team.Data07", ""}
   lib_GetTeamContainerName = function(id)
      return teamIds[id+1] or ""
   end
   local gm = QueryContainer("GM.GameInitData")
   local index
   -- Clear every supported slot before assigning the configured worms; 64 supported resource slots.
   for index = 0,63 do
      local lock, worm = EditContainer(lib_GetWormContainerName(index))
      worm.Active = false
      worm.PlayedInGame = false
      CloseContainer(lock)
   end
   for index = 0,7 do
      local lock, team = EditContainer(teamIds[index+1])
      team.Active = false
      team.Surrendered = false
      team.TeamMustSurvive = false
      CloseContainer(lock)
   end
   WormIndex = 0
   TeamIndex = 0
   local alliances = {0,1,2,3,4,5,6,7}
   for index = 1,8 do
      local skill = Codex_TeamConfig[index][1]
      local count = Codex_TeamConfig[index][2]
      local teamName = "TEST Team " .. index
      local names = {"TEST" .. index .. "-1", "TEST" .. index .. "-2", "TEST" .. index .. "-3",
                     "TEST" .. index .. "-4", "TEST" .. index .. "-5", "TEST" .. index .. "-6"}
      if skill > 0 then
         teamName = "CPU Team " .. index
         for w = 1,6 do names[w] = "CPU" .. index .. "-" .. w end
         if index == 2 then
            names = {"CPU01", "CPU02", "CPU03", "CPU04", "CPU05", "CPU06"}
         end
      end
      lib_SetupMultiplayerTeam(teamName, count,
         names[1], names[2], names[3], names[4], names[5], names[6],
         skill, gm.T1_Grave, gm.T1_Flag, gm.T1_Speech, true,
         alliances[index], 0, gm.T1_HatAttachment,
         gm.T1_GlassesAttachment, gm.T1_TashAttachment, gm.T1_GlovesAttachment)
   end
   Codex_LogLine("requested teams=8 worms=" .. tostring(WormIndex) .. "; independent alliances 0 through 7")
end

function Codex_LogNativeCounts()
   Codex_LogLine("after reinitialise WormCount=" .. tostring(GetData("WormCount")) ..
      " Game.NumTeams=" .. tostring(GetData("Game.NumTeams")))
end

function Codex_RecordTurn()
   local index = GetData("ActiveWormIndex")
   if index >= 0 and index < 64 then
      local worm = QueryContainer(lib_GetWormContainerName(index))
      Codex_LogLine("turn worm=" .. tostring(index) .. " team=" ..
         tostring(worm.TeamIndex) .. " name=" .. worm.Name)
   end
end

-- Separate scheme ammo for eight teams; every alliance inventory is empty.
-- Stockpiling remains outside this diagnostic test.
function Codex_SetupTeamInventories()
   local alliances = {"Inventory.Alliance00", "Inventory.Alliance01",
                      "Inventory.Alliance02", "Inventory.Alliance03", "Inventory.Alliance04", "Inventory.Alliance05", "Inventory.Alliance06", "Inventory.Alliance07"}
   local teams = {"Inventory.Team00", "Inventory.Team01", "Inventory.Team02",
                  "Inventory.Team03", "Inventory.Team04", "Inventory.Team05", "Inventory.Team06", "Inventory.Team07"}
   local source = {1,2,3,4,1,2,3,4}
   local index
   for index = 1,8 do
      CopyContainer(alliances[source[index]], teams[index])
   end
   for index = 1,8 do
      CopyContainer("Inventory.Team.Default", alliances[index])
   end
end
