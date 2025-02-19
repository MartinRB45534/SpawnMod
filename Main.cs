using BepInEx.Logging;
using HarmonyLib;
using Polytopia.Data;
using PolytopiaBackendBase.Game;
using Il2CppSystem.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SpawnMod;
public static class Main
{
    private static ManualLogSource? modLogger;
    public static void Load(ManualLogSource logger)
    {
        Harmony.CreateAndPatchAll(typeof(Main));
        modLogger = logger;
    }
    [HarmonyPrefix]
    [HarmonyPatch(typeof(BuildAction), nameof(BuildAction.Execute))]
    public static bool BuildAction_Execute_Prefix(BuildAction __instance)
    {
        if (__instance.Type == EnumCache<ImprovementData.Type>.GetType("secession"))
        {
            modLogger?.LogInfo("Spawning player");
            SpawnPlayer(__instance.Coordinates);
        }
        return true;
    }

    private static void SpawnPlayer(WorldCoordinates coordinates)
    {
        WorldCoordinates cityCoordinates = GameManager.GameState.Map.GetTile(coordinates).rulingCityCoordinates;
        int Id = GameManager.GameState.PlayerStates[^2].Id + 1;
        if(GameManager.GameState.TryGetPlayer(GameManager.GameState.CurrentPlayer, out PlayerState currentPlayerState))
        {
            PlayerState playerState = new()
            {
                Id = (byte)Id,
                UserName = "Player " + Id,
                AccountId = new Il2CppSystem.Nullable<Il2CppSystem.Guid>(Il2CppSystem.Guid.NewGuid()),
                AutoPlay = false,
                startTile = cityCoordinates,
                hasChosenTribe = currentPlayerState.tribe > TribeData.Type.None,
                tribe = currentPlayerState.tribe,
                skinType = currentPlayerState.skinType == SkinType.Default ? GameManager.GameState.Settings.GetSelectedSkin(GameManager.LocalPlayer.tribe) : GameManager.LocalPlayer.skinType,
                handicap = 0,
                currency = 0,
                score = 0,
                cities = 0,
                kills = 0,
                casualities = 0,
                wipeOuts = 0,
                unlockedTechCache = currentPlayerState.unlockedTechCache,
                availableTech = currentPlayerState.availableTech,
            };
            GameManager.GameState.PlayerStates.Add(playerState);

            PlayerData playerData = new PlayerData
            {
                type = PlayerData.Type.Player,
                state = PlayerData.State.None,
                knownTribe = false,
                tribe = playerState.tribe,
                tribeMix = playerState.tribeMix,
                skinType = playerState.skinType,
                profile = HotseatProfilesState.CreateRandomPlayerProfileState(GameManager.Client.playerData.Length, GameManager.GameState.Version, GameManager.GameState.Seed),
                defaultName = "test"
            };
            List<PlayerData> playerDatasList = GameManager.Client.playerData.ToList();
            playerDatasList.Add(playerData);
            GameManager.Client.playerData = playerDatasList.ToArray();

            List<ushort> lastSeenCommandsList = GameManager.Client.lastSeenCommands.ToList();
            lastSeenCommandsList.Add(0);
            GameManager.Client.lastSeenCommands = lastSeenCommandsList.ToArray();

            foreach (TileData tile in GameManager.GameState.Map.tiles)
            {
                if (tile.rulingCityCoordinates == cityCoordinates)
                {
                    tile.owner = playerState.Id;
                }
            }
        }
        else
        {
            modLogger?.LogWarning("Current player not found, aborting.");
        }
    }
}