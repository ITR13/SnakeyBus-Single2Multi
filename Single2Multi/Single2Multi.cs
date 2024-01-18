using HarmonyLib;
using MelonLoader;
using Photon.Pun;
//using Rewired;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

/*
 * Game Name: snakeybus
 * Game Developer: Stovetop
 * Unity Version: 2019.4.30f1
 * Game Version: 1.24.1
 */
[assembly: MelonInfo(typeof(Single2Multi.Single2Multi), "Single2Multi", "1.0.0", "ITR")]
[assembly: MelonGame("Stovetop", "snakeybus")]

namespace Single2Multi;

public class Single2Multi : MelonMod
{
    public static Action<string> Log { get; private set; } = null!;
    /*private static Player? _rewiredPlayer;
    private static Teleport? _teleport;
    private float _respawnClock;*/

    public override void OnInitializeMelon()
    {
        Log = LoggerInstance.Msg;
    }

    public override void OnSceneWasLoaded(int buildIndex, string sceneName)
    {
        MaybeAddMapOption();

        if (buildIndex <= 2 || PhotonNetwork.CurrentRoom == null) return;
        FixAllSpawning(buildIndex);
        MaybeAddMultiplayerStuff();
    }

    public override void OnSceneWasInitialized(int buildIndex, string sceneName)
    {
        MaybeMoveMapSelectUi();
        MaybeShortenGameDescription();
        MaybeAddJoinButton();

        //_rewiredPlayer = Rewired.ReInput.players?.GetPlayer(0);
        //_respawnClock = 5;
    }

    private void MaybeAddMapOption()
    {
        var mapOptions = GameObject.Find("map-options");
        if (mapOptions == null)
        {
            //Log("No map-options in scene");
            return;
        }

        var optionGroup = mapOptions.GetComponent<M_RoomOptionGroup>();
        var list = new List<M_RoomOptionButton>(optionGroup.buttons);

        var presetButtonSample = mapOptions.GetComponentInChildren<RoomOptionPresetButton>();

        foreach (var (id, name) in new[]
                 {
                     (7, "coriolis"),
                     (13, "dorm"),
                     // (19, "floating"), Physically not in the build, thus unloadable
                     (9, "intersect"),
                     (4, "miami"),
                     (5, "museum"),
                     (6, "ring"),
                     (11, "seattle"),
                 })
        {
            var newChild = Object.Instantiate(presetButtonSample, mapOptions.transform);
            newChild.numberValue = id;
            newChild.stringValue = name;

            list.Add(newChild.GetComponent<M_RoomOptionButton>());
            optionGroup.buttons = list.ToArray();

            var text = newChild.GetComponentInChildren<Text>();
            text.text = name.ToUpper();
            text.color = Color.white;
        }

        //Log("Added Singplayer GameModes");
    }
    
    private void MaybeAddMultiplayerStuff()
    {
        CreateCommunicatorPrefab();
        if (!PhotonNetwork.IsMasterClient) return;

        var hasDropOffCommunicator = Object.FindObjectOfType<DropOffCommunicator>() != null;
        var hasBusCommunicator = Object.FindObjectOfType<BusStopCommunicator>() != null;

        var dropOffManager = Object.FindObjectOfType<DropOffManager>();
        if (dropOffManager != null && !hasDropOffCommunicator)
        {
            CallDropOffAwakeEarly(dropOffManager);
        }

        if (hasDropOffCommunicator || hasBusCommunicator)
        {
            //Log("Found DropOffCommunicator or BusCommunicator!");
        }
        else
        {
            //Log("Creating Communicators!");
            CreateCommmunicators();
        }
    }

    private static void CreateCommmunicators()
    {
        var go = PhotonNetwork.InstantiateSceneObject("communicators", Vector3.zero, Quaternion.identity);
        go.SetActive(true);
    }

    private static void CallDropOffAwakeEarly(DropOffManager dropOffManager)
    {
        //Log("Calling dropOffManager.Awake early");
        Traverse.Create(dropOffManager).Method("Awake").GetValue();
    }

    private static void CreateCommunicatorPrefab()
    {
        var pool = PhotonNetwork.PrefabPool;
        if (pool is not DefaultPool dPool)
        {
            Log("No default pool!");
            return;
        }

        //Log("Adding communicator to default pool");
        if (dPool.ResourceCache.TryGetValue("communicators", out var key))
        {
            if (key != null)
            {
                //Log("Communicators already in pool");
                return;
            }

            //Log("Deleting old communicators prefab");
            dPool.ResourceCache.Remove("communicators");
        }

        var communicatorGo = new GameObject(
            "CommunicatorsModded",
            typeof(PhotonView),
            typeof(DropOffCommunicator),
            typeof(BusStopCommunicator),
            typeof(FixDropOffs)
        );
        communicatorGo.SetActive(false);
        dPool.ResourceCache.Add("communicators", communicatorGo);
    }

    private void MaybeMoveMapSelectUi()
    {
        var mapOptions = GameObject.Find("map-options");
        if (mapOptions == null)
        {
            //Log("No map-options in scene");
            return;
        }

        var transform = (RectTransform)mapOptions.transform;
        var localPos = transform.localPosition;
        localPos.y = -10;
        transform.localPosition = localPos;

        var verticalLayoutGroup = mapOptions.GetComponentInChildren<VerticalLayoutGroup>();
        if (verticalLayoutGroup != null)
        {
            verticalLayoutGroup.spacing = 2;
        }
    }

    private void MaybeShortenGameDescription()
    {
        var gameDescription = GameObject.Find("game-description/Text");
        if (gameDescription == null) return;
        var transform = (RectTransform)gameDescription.transform;
        transform.anchorMax = new Vector2(0.8f, 1);
    }

    private void MaybeAddJoinButton()
    {
        var settings = GameObject.Find("Universal Options/Right Anchor (3)/settings");
        if (settings == null) return;

        var parent = settings.transform.parent;
        var join = Object.Instantiate(settings, parent);
        join.transform.SetAsFirstSibling();

        var text = join.GetComponent<TextMeshProUGUI>();
        text.text = "Join Game";

        var button = join.GetComponent<Button>();
        button.onClick.RemoveAllListeners();

        var inputField = GameObject.Find("Canvas Lobby/room-options/name/InputField");
        if (inputField == null)
        {
            Log("Failed to find inputfield");
            return;
        }

        var joinName = Object.Instantiate(inputField, parent);
        joinName.transform.SetAsFirstSibling();
        var layoutElement = joinName.AddComponent<LayoutElement>();
        layoutElement.minWidth = 120;

        var nameInputField = joinName.GetComponent<InputField>();
        nameInputField.text = "";
        nameInputField.ActivateInputField();
        nameInputField.placeholder.GetComponent<Text>().text = "Room #";
        nameInputField.placeholder.gameObject.SetActive(true);
        // nameInputField.characterLimit = 5;

        var lobbyManager = Object.FindObjectOfType<LobbyManager>();
        if (lobbyManager == null)
        {
            Log("Failed to find lobbyManager");
            return;
        }

        button.onClick.AddListener(() => lobbyManager.joinRoom(nameInputField.text));
    }

    private void FixAllSpawning(int buildIndex)
    {
        var noDeathZones = Array.Empty<(Vector3, Vector3)>();
        var noExtraSpawnpoints = Array.Empty<(Vector3, Vector3)>();
        switch (buildIndex)
        {
            case 3: // suburbs deathpoint bc it glitches sometimes
                FixSpawning(
                    new[] { (new Vector3(-1000, -60.5f, -1000), new Vector3(1000, -62, 1000)) },
                    noExtraSpawnpoints
                );
                break;
            case 7: // coriolis
            {
                Vector3 corner1 = new Vector3(71.5169f, 134.0901f, 314.5493f);
                Vector3 corner2 = new Vector3(-77.0042f, 290.9694f, -326.1835f);
                FixSpawning(
                    Enumerable.Range(0, 2)
                        .SelectMany(
                            firstCoord => Enumerable.Range(firstCoord + 1, 3 - firstCoord - 1)
                                .SelectMany(
                                    secondCoord =>
                                        new[]
                                        {
                                            (
                                                new Vector3(corner1.x, corner1.y, corner1.z),
                                                new Vector3(
                                                    (firstCoord == 0 || secondCoord == 0) ? corner2.x : corner1.x,
                                                    (firstCoord == 1 || secondCoord == 1) ? corner2.y : corner1.y,
                                                    (firstCoord == 2 || secondCoord == 2) ? corner2.z : corner1.z
                                                )),
                                            (
                                                new Vector3(
                                                    (firstCoord == 0 || secondCoord == 0) ? corner1.x : corner2.x,
                                                    (firstCoord == 1 || secondCoord == 1) ? corner1.y : corner2.y,
                                                    (firstCoord == 2 || secondCoord == 2) ? corner1.z : corner2.z
                                                ),
                                                new Vector3(corner2.x, corner2.y, corner2.z)
                                            ),
                                        }
                                )
                        )
                        .ToArray(),
                    new[]
                    {
                        (new Vector3(-57.6889f, 209.6495f, -78.1245f), new Vector3(350.0316f, 351.5273f, 255f)),
                        (new Vector3(59.2424f, 209.0384f, -84.6742f), new Vector3(3.8315f, 6.5273f, 100f)),
                        (new Vector3(50.605f, 173.1043f, 84.6812f), new Vector3(353.932f, 180.2277f, 300f)),
                    }
                );
                break;
            }
            case 13: // dorm
                FixSpawning(
                    noDeathZones,
                    new[]
                    {
                        (new Vector3(-74.4816f, 80.8958f, 67.6943f), Vector3.up * 270),
                        (new Vector3(132.5975f, 3.8669f, 55.8232f), Vector3.up * 180),
                        (new Vector3(-28.2826f, 2.5781f, -53.428f), Vector3.zero),
                    }
                );
                break;
            case 9: // intersect
                FixSpawning(
                    new[] { (new Vector3(-1000, -20, -1000), new Vector3(1000, -20, 1000)) },
                    new[]
                    {
                        (new Vector3(-1.579f, -2.3713f, 84.9815f), new Vector3(8.1018f, 6.0433f, 0f)),
                        (new Vector3(75.4062f, -2.9717f, 0.2171f), new Vector3(7.8018f, 92.1433f, 0f)),
                        (new Vector3(-1.878f, -3.026f, -74.0505f), new Vector3(10.5021f, 180.3433f, 0f)),
                    }
                );
                break;
            case 4: // miami
                FixSpawning(
                    new[] { (new Vector3(-1000, -2, -1000), new Vector3(1000, -1, 1000)) },
                    new[]
                    {
                        (new Vector3(-92.3314f, 1.2229f, -341.6127f), new Vector3(349.666f, 6.5773f, 0f)),
                        (new Vector3(-10.8721f, 1.5114f, 82.5343f), new Vector3(350.2662f, 175.4773f, 0f)),
                        (new Vector3(-29.4526f, 2.6772f, 83.1152f), new Vector3(355.9662f, 179.3773f, 0f)),
                    }
                );
                break;
            case 5: // museum
                FixSpawning(
                    new[]
                    {
                        (new Vector3(-1000, -85, -1000), new Vector3(1000, -82, 1000)),
                        (new Vector3(-1000, 373, -1000), new Vector3(1000, 378, 1000))
                    },
                    new[]
                    {
                        (new Vector3(96.2378f, 1.9046f, 177.1369f), new Vector3(356.5771f, 244.8648f, 0f)),
                        (new Vector3(-148.8479f, 1.0446f, -134.3941f), new Vector3(359.877f, 1.2649f, 0f)),
                        (new Vector3(143.7811f, 1.7237f, 89.3041f), new Vector3(355.377f, 220.5648f, 0f)),
                    }
                );
                break;
            case 6: // ring
                FixSpawning(
                    new[] { (new Vector3(-1000, -110, -1000), new Vector3(1000, -111, 1000)) },
                    new[]
                    {
                        (new Vector3(115.15f, -20.5f, 23.2f), new Vector3(7.456f, 198.1334f, 0f)),
                        (new Vector3(49.3856f, -7.0929f, -80.9543f), new Vector3(1.9767f, 253.2658f, 0f)),
                        (new Vector3(80.971f, 15.9062f, -121.8442f), new Vector3(352.9766f, 242.4656f, 0f)),
                    }
                );
                break;
            case 11: // seattle
                FixSpawning(
                    new[] { (new Vector3(-1000, -3, -1000), new Vector3(1000, -2, 1000)) },
                    new[]
                    {
                        (new Vector3(488.8715f, 13.3652f, -120.6926f), new Vector3(1.7759f, 179.6451f, 0f)),
                        (new Vector3(488.8581f, 26.1171f, -121.3768f), new Vector3(4.776f, 1.1451f, 0f)),
                        (new Vector3(192.1753f, 41.7524f, -19.0532f), new Vector3(352.7759f, 178.4448f, 0f)),
                    }
                );
                break;
        }
    }

    private void FixSpawning((Vector3, Vector3)[] deathBoxes, (Vector3, Vector3)[] spawnpoints)
    {
        var playerSpawnPoints = Object.FindObjectOfType<PlayerSpawnPoints>();
        if (playerSpawnPoints == null)
        {
            Log("No player spawnpoints");
            return;
        }

        var busHeadOnlyLayer = LayerMask.NameToLayer("SeeBusHeadOnly");

        var teleport = Object.FindObjectOfType<Teleport>();
        if (teleport == null)
        {
            var teleportGo = new GameObject("ModdedTeleport", typeof(Rigidbody), typeof(Teleport))
            {
                tag = "teleport",
                layer = busHeadOnlyLayer,
            };
            var rb = teleportGo.GetComponent<Rigidbody>();
            rb.isKinematic = true;
            teleport = teleportGo.GetComponent<Teleport>();
        }

        // _teleport = teleport;

        if (spawnpoints.Length > 0 || teleport.respawnPositions == null || teleport.respawnPositions.Length == 0)
        {
            var spawnParent = playerSpawnPoints.transform;
            foreach (var spawnpoint in spawnpoints)
            {
                var go = new GameObject("ModdedSpawnpoint");
                var transform = go.transform;
                transform.parent = spawnParent;
                transform.rotation = Quaternion.Euler(spawnpoint.Item2);
                transform.position = spawnpoint.Item1 + go.transform.up * 0.5f;
            }

            var spawnPoints = Enumerable
                .Range(0, spawnParent.childCount)
                .Select(i => spawnParent.GetChild(i))
                .ToArray();
            teleport.respawnPositions = spawnPoints;
            PlayerSpawnPoints.spawnPoints = spawnPoints;
        }

        var boxParent = teleport.transform;
        foreach (var deathBox in deathBoxes)
        {
            var boxGo = new GameObject("ModDeathBox", typeof(BoxCollider))
            {
                tag = "teleport",
                layer = busHeadOnlyLayer,
                transform =
                {
                    parent = boxParent,
                    position = (deathBox.Item1 + deathBox.Item2) / 2,
                },
            };
            var collider = boxGo.GetComponent<BoxCollider>();
            collider.size = deathBox.Item2 - deathBox.Item1;
            collider.isTrigger = true;
        }
    }
/*
    // Attempt to make restart button work, but couldn't figure it out
    public override void OnUpdate()
    {
        if (PhotonNetwork.CurrentRoom == null || _rewiredPlayer == null || _teleport == null) return;
        _respawnClock -= Time.deltaTime;

        if (_rewiredPlayer.GetButtonDown(22) && _respawnClock < 0)
        {
            _respawnClock = 5;

            var busses = Object.FindObjectsOfType<Bus>();
            foreach (var bus in busses)
            {
                if (bus == null || !bus.isLocalBus) continue;
                var collider = bus.GetComponentInChildren<Collider>();
                Traverse.Create(_teleport)
                    .Method("OnTriggerEnter", typeof(Collider))
                    .GetValue(collider);
            }
        }
    }
*/
}