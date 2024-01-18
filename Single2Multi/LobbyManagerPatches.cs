using HarmonyLib;

namespace Single2Multi;

[HarmonyPatch(typeof(LobbyManager))]
public static class LobbyManagerPatches
{
    private static readonly HashSet<string> PrivateRooms =
    [
        "map_coriolis",
        "map_stovetophq",
        "map_ring 1",
        "map_Miami",
        "map_abstract",
        "map_ring",
        "map_seattle",
    ];

    [HarmonyPatch(nameof(LobbyManager.createAndJoinRoom))]
    [HarmonyPrefix]
    // ReSharper disable once InconsistentNaming
    public static void CreateAndJoinRoomPrefix(RoomOptionData ___createRoomConfig, ref bool isPrivate)
    {
        Single2Multi.Log(___createRoomConfig.sceneName);
        if (PrivateRooms.Contains(___createRoomConfig.sceneName))
        {
            isPrivate = true;
        }
    }
}