using BepInEx;
using BepInEx.IL2CPP;
using HarmonyLib;
using UnityEngine;
using SteamworksNative;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using GameLoop = MonoBehaviourPublicObInLi1GagasmLi1GaUnique;
using PlayerMovement = MonoBehaviourPublicGaplfoGaTrorplTrRiBoUnique;
using SteamManager = MonoBehaviourPublicObInUIgaStCSBoStcuCSUnique;
using GameManager = MonoBehaviourPublicDi2UIObacspDi2UIObUnique;
using LobbyManager = MonoBehaviourPublicCSDi2UIInstObUIloDiUnique;
using ServerSend = MonoBehaviourPublicInInUnique;
using PlayerManager = MonoBehaviourPublicCSstReshTrheObplBojuUnique;
using GameModeTag = GameModePublicLi1UIUnique;
using GameServer = MonoBehaviourPublicObInCoIE85SiAwVoFoCoUnique;
using GameUI = MonoBehaviourPublicGaroloGaObInCacachGaUnique;
using Chatbox = MonoBehaviourPublicRaovTMinTemeColoonCoUnique;
using LobbySettings = MonoBehaviourPublicObjomaOblogaTMObseprUnique;
using GameModeManager = MonoBehaviourPublicGadealGaLi1pralObInUnique;
using MapManager = MonoBehaviourPublicObInMamaLi1plMadeMaUnique;
using ServerConfig = ObjectPublicInSiInInInInInInInInUnique;
using Client = ObjectPublicBoInBoCSItBoInSiBySiUnique;
using ServerHandle = MonoBehaviourPublicPlVoUI9GaVoUI9UsPlUnique;

namespace Manhunt
{
    public struct MapInfo
    {
        public string name;
        public int id;
        public int minPlayers;
        public int maxPlayers;
        public int roundTime;
    }

    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, "1.0.0")]
    public class Plugin : BasePlugin
    {
        // Game state variables
        public static int gameState;
        public static int prevGameState;
        public static bool canStartGame = true;
        public static bool canAfkCheck = true;

        // Timer variables
        public static float gameTimer;
        public static float afkTimer = 10f;
        public static float freezeTimer = 3f;

        // Player variables
        public static List<ulong> alivePlayers = new();
        public static List<ulong> afkPlayers = new();
        public static List<ulong> hunted = new();
        public static List<ulong> hunters = new();
        public static ulong firstHunted;
        public static bool huntersFrozen;
        public static Dictionary<ulong, Vector3> hunterFreezePositions = new();
        public static Dictionary<ulong, Vector3> playerRotations = new();
        public static bool toggleAutoStart = true;
        public static bool toggleAfk = false;

        // Map variables
        public static Dictionary<int, MapInfo> mapDictionary = new();
        public static int randomMapID = 0;
        public static int prevMapID = 0;

        public override void Load()
        {
            Harmony.CreateAndPatchAll(typeof(Plugin));
            Log.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
            Log.LogInfo("Mod created by JMac");
        }

        public static void CheckGameState()
        {
            /*if (LobbyManager.Instance == null || GameManager.Instance == null) return;

            int lobbyManagerState = (int)LobbyManager.Instance.state;
            int gameManagerState = (int)GameManager.Instance.gameMode.modeState;*/

            int lobbyManagerState = 0;
            int gameManagerState = 0;

            if (LobbyManager.Instance) lobbyManagerState = (int)LobbyManager.Instance.state;
            if (GameManager.Instance) gameManagerState = (int)GameManager.Instance.gameMode.modeState;

            if (lobbyManagerState == 0 && gameState != 0) //MainMenu
            {
                gameState = 0;
                prevGameState = 0;
                canStartGame = true;
            }
            else if (lobbyManagerState == 2 && gameManagerState == 0 && GetModeID() == 0 && gameState != 1) //Lobby
            {
                gameState = 1;
                prevGameState = 1;
            }
            else if (lobbyManagerState == 1 && alivePlayers.Count == 0 && gameState != 2) //Loading
            {
                gameState = 2;
                prevGameState = 2;
            }
            else if (lobbyManagerState == 2 && gameManagerState == 0 && GetModeID() != 0 && prevGameState != 1 && gameState != 3) //Frozen
            {
                gameState = 3;
                prevGameState = 3;
            }
            else if ((lobbyManagerState == 4 || lobbyManagerState == 2) && gameManagerState == 1 && gameState != 4) //Playing
            {
                gameState = 4;
                prevGameState = 4;
            }
            else if (lobbyManagerState == 2 && gameManagerState == 2 && gameState != 5) //Ended
            {
                gameState = 5;
                prevGameState = 5;
            }
            else if (lobbyManagerState == 4 && (gameManagerState == 2 || gameManagerState == 3) && gameState != 6) //GameOver
            {
                gameState = 6;
                prevGameState = 6;
            }

            //Debug.Log(gameState.ToString());
        }

        public static int GetMapID()
        {
            return LobbyManager.Instance.map.id;
        }

        public static int GetModeID()
        {
            return LobbyManager.Instance.gameMode.id;
        }

        public static int GetTotalNumOfPlayers()
        {
            return GameManager.Instance.activePlayers.Count + GameManager.Instance.spectators.Count;
        }

        public static ulong GetMyID()
        {
            return SteamManager.Instance.field_Private_CSteamID_0.m_SteamID;
        }

        public static ulong GetHostID()
        {
            return SteamManager.Instance.field_Private_CSteamID_1.m_SteamID;
        }

        public static bool IsHost()
        {
            return SteamManager.Instance.IsLobbyOwner() && !LobbyManager.Instance.Method_Public_Boolean_0();
        }

        public static Rigidbody GetPlayerRigidBody(ulong id)
        {
            if (id == GetMyID()) return PlayerMovement.prop_MonoBehaviourPublicGaplfoGaTrorplTrRiBoUnique_0.GetRb();
            else return GameManager.Instance.activePlayers[id].prop_MonoBehaviourPublicObVeSiVeRiSiAnVeanTrUnique_0.field_Private_Rigidbody_0;
        }

        public static Vector3 GetPlayerRotation(ulong id)
        {
            if (id == GetMyID()) return PlayerInput.Instance.cameraRot;
            else return new Vector3(GameManager.Instance.activePlayers[id].field_Private_MonoBehaviourPublicObVeSiVeRiSiAnVeanTrUnique_0.xRot, GameManager.Instance.activePlayers[id].field_Private_MonoBehaviourPublicObVeSiVeRiSiAnVeanTrUnique_0.yRot, 0f);
        }

        public static List<ulong> GetAlivePlayers()
        {
            List<ulong> list = new();
            foreach (var player in GameManager.Instance.activePlayers)
            {
                if (player == null || player.Value.dead) continue;
                list.Add(player.Key);
            }
            return list;
        }

        public static void BoundsCheck(ulong id)
        {
            float maxDistance = 0f;

            switch (prevMapID)
            {
                case 0:
                    maxDistance = 100;
                    break;
                case 1:
                    maxDistance = 110;
                    break;
                case 2:
                    maxDistance = 90;
                    break;
                case 3:
                    maxDistance = 100;
                    break;
                case 5:
                    maxDistance = 200;
                    break;
                case 6:
                    maxDistance = 100;
                    break;
                case 7:
                    maxDistance = 100;
                    break;
                case 8:
                    maxDistance = 160;
                    break;
                case 9:
                    maxDistance = 100;
                    break;
                case 10:
                    maxDistance = 80;
                    break;
                case 11:
                    maxDistance = 170;
                    break;
                case 13:
                    maxDistance = 170;
                    break;
                case 14:
                    maxDistance = 80;
                    break;
                case 15:
                    maxDistance = 90;
                    break;
                case 17:
                    maxDistance = 210;
                    break;
                case 18:
                    maxDistance = 70;
                    break;
                case 19:
                    maxDistance = 70;
                    break;
                case 20:
                    maxDistance = 100;
                    break;
                case 21:
                    maxDistance = 80;
                    break;
                case 22:
                    maxDistance = 200;
                    break;
                case 23:
                    maxDistance = 70;
                    break;
                case 24:
                    maxDistance = 80;
                    break;
                case 25:
                    maxDistance = 70;
                    break;
                case 26:
                    maxDistance = 125;
                    break;
                case 27:
                    maxDistance = 125;
                    break;
                case 28:
                    maxDistance = 50;
                    break;
                case 29:
                    maxDistance = 150;
                    break;
                case 30:
                    maxDistance = 70;
                    break;
                case 31:
                    maxDistance = 70;
                    break;
                case 32:
                    maxDistance = 70;
                    break;
                case 33:
                    maxDistance = 150;
                    break;
                case 34:
                    maxDistance = 80;
                    break;
                case 35:
                    maxDistance = 80;
                    break;
                case 36:
                    maxDistance = 100;
                    break;
                case 37:
                    maxDistance = 60;
                    break;
                case 38:
                    maxDistance = 60;
                    break;
                case 39:
                    maxDistance = 60;
                    break;
                case 40:
                    maxDistance = 60;
                    break;
                case 41:
                    maxDistance = 80;
                    break;
                case 42:
                    maxDistance = 80;
                    break;
                case 43:
                    maxDistance = 80;
                    break;
                case 44:
                    maxDistance = 80;
                    break;
                case 45:
                    maxDistance = 170;
                    break;
                case 46:
                    maxDistance = 130;
                    break;
                case 47:
                    maxDistance = 100;
                    break;
                case 48:
                    maxDistance = 100;
                    break;
                case 49:
                    maxDistance = 100;
                    break;
                case 50:
                    maxDistance = 100;
                    break;
                case 51:
                    maxDistance = 100;
                    break;
                case 52:
                    maxDistance = 120;
                    break;
                case 53:
                    maxDistance = 90;
                    break;
                case 54:
                    maxDistance = 220;
                    break;
                case 55:
                    maxDistance = 190;
                    break;
                case 56:
                    maxDistance = 210;
                    break;
                case 57:
                    maxDistance = 700;
                    break;
                case 59:
                    maxDistance = 80;
                    break;
                case 60:
                    maxDistance = 80;
                    break;
                case 61:
                    maxDistance = 80;
                    break;
            }

            if (Vector3.Distance(GetPlayerRigidBody(id).position, Vector3.zero) > maxDistance)
            {
                GameServer.PlayerDied(id, 1, Vector3.zero);
                //ServerSend.RespawnPlayer(id, Vector3.zero);
            }
        }

        public static void GlitchingCheck(ulong id)
        {
            if (prevMapID == 3)// Big Color Climb
            {
                if (Vector3.Distance(GetPlayerRigidBody(id).position, new Vector3(9.4f, -25.1f, -9.4f)) < 1f)
                {
                    ServerSend.RespawnPlayer(id, new Vector3(13.0f, -25.2f, -7.4f));
                }
                else if (Vector3.Distance(GetPlayerRigidBody(id).position, new Vector3(-9.4f, -28.1f, 9.4f)) < 1f)
                {
                    ServerSend.RespawnPlayer(id, new Vector3(-10.6f, -22.1f, 10.6f));
                }
                else if (Vector3.Distance(GetPlayerRigidBody(id).position, new Vector3(-9.4f, -28.1f, -9.4f)) < 1f)
                {
                    ServerSend.RespawnPlayer(id, new Vector3(-9f, -28.1f, -13f));
                }
            }
            else if (prevMapID == 29)// Snow Top
            {
                if (Vector3.Distance(GetPlayerRigidBody(id).position, new Vector3(10.4f, 69.9f, -6.4f)) < 1f)
                {
                    ServerSend.RespawnPlayer(id, new Vector3(14.8f, 69.9f, -5.1f));
                }
                else if (Vector3.Distance(GetPlayerRigidBody(id).position, new Vector3(-40.6f, 59.9f, 21.5f)) < 1f)
                {
                    ServerSend.RespawnPlayer(id, new Vector3(-44.8f, 59.9f, 21.4f));
                }
                else if (Vector3.Distance(GetPlayerRigidBody(id).position, new Vector3(54.5f, 79.0f, 14.6f)) < 1f)
                {
                    ServerSend.RespawnPlayer(id, new Vector3(56.0f, 68.7f, 15.0f));
                }
            }
            else if (prevMapID == 36)// Small Beach
            {
                if (Vector3.Distance(GetPlayerRigidBody(id).position, new Vector3(20.8f, -1.1f, -15.8f)) < 1f)
                {
                    ServerSend.RespawnPlayer(id, new Vector3(19.2f, -1.1f, -17.3f));
                }
                else if (Vector3.Distance(GetPlayerRigidBody(id).position, new Vector3(-10.6f, -4.1f, 14.4f)) < 1f)
                {
                    ServerSend.RespawnPlayer(id, new Vector3(-14.4f, -4.1f, 14.4f));
                }
            }
            else if (prevMapID == 0)// Bitter Beach
            {
                if (Vector3.Distance(GetPlayerRigidBody(id).position, new Vector3(25.3f, -1.1f, -20.8f)) < 1f)
                {
                    ServerSend.RespawnPlayer(id, new Vector3(23.2f, -1.1f, -22.2f));
                }
                else if (Vector3.Distance(GetPlayerRigidBody(id).position, new Vector3(-6.6f, -4.1f, 9.4f)) < 1f)
                {
                    ServerSend.RespawnPlayer(id, new Vector3(-11.3f, -4.1f, 9.4f));
                }
            }
        }

        public static void InitializeMapData()
        {
            string path = Path.Combine(Directory.GetCurrentDirectory(), "BepInEx", "plugins", "ManhuntModConfig.txt");

            if (!File.Exists(path))
            {
                Debug.Log("Trying To Create File");

                Dictionary<int, (int min, int max)> defaultMapSettings = new()
                {
                    { 0,  (5, 40) },
                    { 3,  (5, 40) },
                    { 7,  (5, 40) },
                    { 15, (5, 40) },
                    { 18, (0, 5)  },
                    { 20, (5, 40) },
                    { 29, (5, 40) },
                    { 32, (0, 10) },
                    { 35, (0, 5)  },
                    { 36, (0, 5)  },
                    { 55, (5, 40) },
                    { 56, (5, 40) },
                };

                using (StreamWriter writer = new(path))
                {
                    Map[] mapArray = MapManager.Instance.maps;
                    foreach (var map in mapArray)
                    {
                        int id = map.id;
                        (int min, int max) = defaultMapSettings.GetValueOrDefault(id, (0, 40));

                        MapInfo mapInfo = new()
                        {
                            name = map.mapName,
                            id = id,
                            minPlayers = min,
                            maxPlayers = max,
                            roundTime = 60,
                        };
                        mapDictionary.Add(id, mapInfo);

                        writer.WriteLine($"Map Name = {map.mapName}; Map ID = {map.id}; Min Players = {mapInfo.minPlayers}; Max Players = {mapInfo.maxPlayers}; Round Time = {mapInfo.roundTime};");
                        writer.WriteLine();
                    }
                }
                Debug.Log("File Created");
            }
            else
            {
                Debug.Log("Trying To Read File");
                StreamReader reader = new(path);
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    Match match = Regex.Match(line, @"Map Name = (?<name>[^;]+); Map ID = (?<id>\d+); Min Players = (?<min>\d+); Max Players = (?<max>\d+); Round Time = (?<round>\d+);");
                    if (match.Success)
                    {
                        string mapName = match.Groups["name"].Value;
                        int id = int.Parse(match.Groups["id"].Value);
                        int minPlayers = int.Parse(match.Groups["min"].Value);
                        int maxPlayers = int.Parse(match.Groups["max"].Value);
                        int roundTime = int.Parse(match.Groups["round"].Value);

                        MapInfo mapInfo = new()
                        {
                            name = mapName,
                            id = id,
                            minPlayers = minPlayers,
                            maxPlayers = maxPlayers,
                            roundTime = roundTime,
                        };
                        mapDictionary.Add(id, mapInfo);
                    }
                }
                Debug.Log("File Read");
            }
        }

        public static void ChangeMap()
        {
            hunted.Clear();
            hunters.Clear();
            hunterFreezePositions.Clear();
            afkPlayers.Clear();
            playerRotations.Clear();
            GameLoop.Instance.ResetAllInventories();
            LobbyManager.Instance.started = true;

            int numOfPlayers = GetTotalNumOfPlayers();
            List<int> tempMapIDs = new();
            randomMapID = 0;

            if (MapManager.Instance.playableMaps.Count > 0)
            {
                foreach (var map in MapManager.Instance.playableMaps)
                {
                    if (numOfPlayers >= mapDictionary[map.id].minPlayers && numOfPlayers <= mapDictionary[map.id].maxPlayers)
                    {
                        tempMapIDs.Add(map.id);
                    }
                }

                randomMapID = tempMapIDs[new System.Random().Next(0, tempMapIDs.Count)];
                while (randomMapID == prevMapID && tempMapIDs.Count > 1)
                {
                    randomMapID = tempMapIDs[new System.Random().Next(0, tempMapIDs.Count)];
                }
                prevMapID = randomMapID;
            }

            ServerSend.LoadMap(randomMapID, 4);
            Debug.Log("Map Changed");
        }

        public static void CheckGameOver()
        {
            if (!IsHost() || gameState != 4) return;

            if ((hunted.Count == 0 && hunters.Count == 1) || (hunted.Count == 1 && hunters.Count == 0) || GameManager.Instance.activePlayers.Count <= 1)
            {
                ServerSend.GameOver(0);
                canStartGame = true;
                Debug.Log("Game Over");
            }
        }

        [HarmonyPatch(typeof(SteamManager), nameof(SteamManager.Start))]
        [HarmonyPostfix]
        public static void SteamManagerStart()
        {
            InitializeMapData();
            //__instance.serverNameField.text = "Manhunt Mod";
            //__instance.maxPlayers.slider.value = 15;

            MapManager.Instance.playableMaps.Clear();
            var defaultMaps = new int[] { 0, 3, 7, 15, 18, 20, 29, 32, 35, 36, 55, 56 };
            foreach (var mapIndex in defaultMaps) MapManager.Instance.playableMaps.Add(MapManager.Instance.maps[mapIndex]);

            GameModeManager.Instance.allPlayableGameModes.Clear();
            GameModeManager.Instance.allPlayableGameModes.Add(GameModeManager.Instance.allGameModes[4]);

            ServerConfig.field_Public_Static_Int32_5 = 6; // round start freeze
            ServerConfig.field_Public_Static_Int32_6 = 6; // round stop cinematic
            ServerConfig.field_Public_Static_Int32_7 = 5; // round end timeout
            ServerConfig.field_Public_Static_Int32_8 = 5; // game over timeout
            //ServerConfig.field_Public_Static_Int32_9 = 5; // load time before kicked
            //ServerConfig.field_Public_Static_Single_0 // speak after death time
        }

        [HarmonyPatch(typeof(SteamManager), nameof(SteamManager.Update))]
        [HarmonyPostfix]
        public static void SteamManagerUpdate()
        {
            CheckGameState();
            if (!IsHost()) return;

            /*if (IsHost() && Input.GetKeyDown(KeyCode.UpArrow))
            {
                ChangeMap();
            }*/

            // Start Game
            if (canStartGame && toggleAutoStart && gameState == 1 && ((toggleAfk && GetTotalNumOfPlayers() > 2) || (!toggleAfk && GetTotalNumOfPlayers() > 1)))
            {
                ChangeMap();
                canStartGame = false;
                Debug.Log("Game Started");
            }
        }

        [HarmonyPatch(typeof(GameMode), nameof(GameMode.Update))]
        [HarmonyPostfix]
        public static void GameModeUpdate(GameMode __instance)
        {
            if (!IsHost()) return;

            alivePlayers = GetAlivePlayers();
            gameTimer = __instance.freezeTimer.field_Private_Single_0;
            
            if (GetModeID() != 4) return;
            CheckGameOver();

            if (gameState == 4)
            {
                // Check Player Position
                foreach (ulong id in alivePlayers)
                {
                    BoundsCheck(id);
                    GlitchingCheck(id);
                }

                // Hunted Effect
                foreach (ulong id in hunted)
                {
                    ServerSend.PlayerDamage(id, id, 0, Vector3.zero, 5);
                }

                // Pick New Hunted
                if (hunted.Count == 0 && hunters.Count >= 2)
                {
                    ulong randomPlayer = hunters[new System.Random().Next(hunters.Count)];
                    hunted.Add(randomPlayer);
                    //ServerSend.TagPlayer(randomPlayer, 0);
                    ServerSend.TagPlayer(0, randomPlayer);
                    ServerSend.SendChatMessage(1, $"{GameManager.Instance.activePlayers[randomPlayer].username} is now being hunted");
                    firstHunted = randomPlayer;
                    Debug.Log("Picked a new hunted player");
                }

                // Freeze Hunters
                freezeTimer -= Time.deltaTime;
                if (huntersFrozen)
                {
                    foreach (var hunter in hunterFreezePositions)
                    {
                        ServerSend.RespawnPlayer(hunter.Key, hunter.Value);
                    }

                    if (freezeTimer <= 0)
                    {
                        Debug.Log("hunters unfrozen");
                        ServerSend.SendChatMessage(1, "The hunters are no longer frozen, RUN!");
                        huntersFrozen = false;
                    }
                }

                // Afk Check
                afkTimer -= Time.deltaTime;
                if (afkTimer <= 0 && canAfkCheck)
                {
                    foreach (ulong id in alivePlayers)
                    {
                        if (GetPlayerRotation(id) == playerRotations[id])
                        {
                            afkPlayers.Add(id);
                            if (hunted.Contains(id)) hunted.Remove(id);
                            if (hunters.Contains(id)) hunters.Remove(id);
                            GameServer.PlayerDied(id, 1, Vector3.zero);
                            ServerSend.SendChatMessage(1, $"{GameManager.Instance.activePlayers[id].username} was killed for being afk");
                        }
                    }
                    canAfkCheck = false;
                }
            }
        }

        [HarmonyPatch(typeof(ServerHandle), nameof(ServerHandle.GameRequestToSpawn))]
        [HarmonyPrefix]
        public static void ServerHandleGameRequestToSpawn(ulong param_0)
        {
            if (!IsHost()) return;

            if (param_0 == GetMyID() && toggleAfk) LobbyManager.Instance.GetClient(param_0).field_Public_Boolean_0 = false; // active player
            else LobbyManager.Instance.GetClient(param_0).field_Public_Boolean_0 = true; // active player
        }

        [HarmonyPatch(typeof(GameMode), nameof(GameMode.Init))]
        [HarmonyPostfix]
        public static void GameModeInit()
        {
            if (!IsHost() || mapDictionary[randomMapID].roundTime < 1) return;

            LobbyManager.Instance.gameMode.shortModeTime = mapDictionary[randomMapID].roundTime;
            LobbyManager.Instance.gameMode.longModeTime = mapDictionary[randomMapID].roundTime;
            LobbyManager.Instance.gameMode.mediumModeTime = mapDictionary[randomMapID].roundTime;
        }

        [HarmonyPatch(typeof(GameLoop), nameof(GameLoop.CheckGameOver))]
        [HarmonyPrefix]
        public static bool GameLoopCheckGameOver()
        {
            if (!IsHost() || GetModeID() != 4) return true;
            return false;
        }

        [HarmonyPatch(typeof(GameModeTag), nameof(GameModeTag.CheckGameOver))]
        [HarmonyPrefix]
        public static bool GameModeTagCheckGameOver()
        {
            if (!IsHost() || GetModeID() != 4) return true;
            return false;
        }

        [HarmonyPatch(typeof(GameLoop), nameof(GameLoop.StartGames))]
        [HarmonyPrefix]
        public static bool GameLoopStartGames()
        {
            if (!IsHost()) return true;
            return false;
        }

        [HarmonyPatch(typeof(GameLoop), nameof(GameLoop.NextGame))]
        [HarmonyPrefix]
        public static bool GameLoopNextGame()
        {
            if (!IsHost() || GetModeID() != 4) return true;

            if (toggleAutoStart && ((toggleAfk && GetTotalNumOfPlayers() > 2) || (!toggleAfk && GetTotalNumOfPlayers() > 1)))
            {
                ChangeMap();
                Debug.Log("Started New Game");
            }
            else
            {
                GameLoop.Instance.RestartLobby();
            }
            return false;
        }

        [HarmonyPatch(typeof(ServerSend), nameof(ServerSend.SendWinner))]
        [HarmonyPrefix]
        public static bool ServerSendSendWinner()
        {
            if (!IsHost() || GetModeID() != 4) return true;

            if (toggleAutoStart && ((toggleAfk && GetTotalNumOfPlayers() > 2) || (!toggleAfk && GetTotalNumOfPlayers() > 1)))
            {
                ChangeMap();
                Debug.Log("Started New Game");
            }
            else
            {
                GameLoop.Instance.RestartLobby();
                Debug.Log("Skipped Win Screen");
            }
            return false;
        }

        [HarmonyPatch(typeof(GameLoop), nameof(GameLoop.RestartLobby))]
        [HarmonyPrefix]
        public static bool GameLoopRestartLobby()
        {
            if (!IsHost() || GetModeID() != 4) return true;

            if (toggleAutoStart && ((toggleAfk && GetTotalNumOfPlayers() > 2) || (!toggleAfk && GetTotalNumOfPlayers() > 1)))
            {
                ChangeMap();
                Debug.Log("Started New Game");
                return false;
            }
            else
            {
                canStartGame = true;
                return true;
            }
        }

        [HarmonyPatch(typeof(GameManager), nameof(GameManager.PlayerDied))]
        [HarmonyPostfix]
        public static void GameManagerPlayerDied(ulong param_1)
        {
            if (!IsHost() || GetModeID() != 4) return;
            if (afkPlayers.Contains(param_1) || gameState != 4) return;

            if (hunted.Contains(param_1))
            {
                ServerSend.SendChatMessage(1, $"{GameManager.Instance.activePlayers[param_1].username} died and is now a hunter");
                ulong randomPlayer = hunters[new System.Random().Next(hunters.Count)];
                //ServerSend.TagPlayer(randomPlayer, param_1);
                ServerSend.TagPlayer(param_1, randomPlayer);
                ServerSend.SendChatMessage(1, $"{GameManager.Instance.activePlayers[randomPlayer].username} is now being hunted");
                hunted.Add(randomPlayer);
                hunted.Remove(param_1);
                hunters.Add(param_1);
                hunters.Remove(randomPlayer);
            }

            GameServer.Instance.QueueRespawn(param_1, 3);
        }

        [HarmonyPatch(typeof(ServerSend), nameof(ServerSend.RespawnPlayer))]
        [HarmonyPrefix]
        public static bool ServerSendRespawnPlayer(ulong param_0)
        {
            if (!IsHost() || GetModeID() != 4) return true;
            if (gameState == 5 || gameState == 6) return false;
            return true;
        }

        [HarmonyPatch(typeof(GameManager), nameof(GameManager.PunchPlayer))]
        [HarmonyPostfix]
        public static void GameManagerPunchPlayer(ulong param_1, ulong param_2)
        {
            if (!IsHost() || GetModeID() != 4) return;
            if (huntersFrozen) return;
            if (!GameManager.Instance.activePlayers.ContainsKey(param_2)) return;

            if (hunters.Contains(param_1) && hunted.Contains(param_2))
            {
                //ServerSend.TagPlayer(param_1, param_2);
                ServerSend.TagPlayer(param_2, param_1);
                hunted.Add(param_1);
                hunted.Remove(param_2);
                hunters.Add(param_2);
                hunters.Remove(param_1);
                
                ServerSend.SendChatMessage(1, $"{GameManager.Instance.activePlayers[param_1].username} is now being hunted");
            }
        }

        [HarmonyPatch(typeof(GameModeTag), nameof(GameModeTag.OnFreezeOver))]
        [HarmonyPrefix]
        public static bool GameModeTagOnFreezeOver()
        {
            if (!IsHost() || GetModeID() != 4) return true;
            if (alivePlayers.Count <= 1) return false;

            ServerSend.SendChatMessage(1, $"Punch the red person");

            GameServer.ForceRemoveAllWeapons();

            List<ulong> tempList = alivePlayers;
            ulong randomPlayer = tempList[new System.Random().Next(tempList.Count)];
            while (randomPlayer == firstHunted)
            {
                randomPlayer = tempList[new System.Random().Next(tempList.Count)];
                
            }
            ServerSend.TagPlayer(0, randomPlayer);

            hunted.Add(randomPlayer);
            firstHunted = randomPlayer;
            tempList.Remove(randomPlayer);
            ServerSend.SendChatMessage(1, $"{GameManager.Instance.activePlayers[randomPlayer].username} is being hunted");

            foreach (ulong id in tempList)
            {
                hunters.Add(id);
                hunterFreezePositions.Add(id, GetPlayerRigidBody(id).position);
                //ServerSend.TagPlayer(0, id);
            }

            foreach (ulong id in alivePlayers)
            {
                playerRotations.Add(id, GetPlayerRotation(id));
            }

            freezeTimer = 3f;
            huntersFrozen = true;
            afkTimer = 10f;
            canAfkCheck = true;

            return false;
        }

        [HarmonyPatch(typeof(GameModeTag), nameof(GameModeTag.OnRoundOver))]
        [HarmonyPrefix]
        public static bool GameModeTagOnRoundOver()
        {
            if (!IsHost() || GetModeID() != 4) return true;

            foreach (ulong id in hunters)
            {
                GameServer.PlayerDied(id, 1, Vector3.zero);
            }

            if (hunted.Count == 1)
            {
                ServerSend.SendChatMessage(1, $"{GameManager.Instance.activePlayers[hunted[0]].username} wins!");
            }
            return false;
        }

        /*[HarmonyPatch(typeof(Chatbox), nameof(Chatbox.AppendMessage))]
        [HarmonyPrefix]
        public static bool ChatboxAppendMessage(ulong param_1, string param_2, string param_3)
        {
            if (!IsHost()) return true;
            if (lastServerMessage == param_2) return false;
            if (param_1 == 1 && (param_2.Contains("joined the server") || param_2.Contains("left the server")) && param_3 == "")
            {
                Debug.Log(param_2);
                lastServerMessage = param_2;
                ServerSend.SendChatMessage(1, param_2);
            }
            return true;
        }*/

        [HarmonyPatch(typeof(ServerSend), nameof(ServerSend.SendChatMessage))]
        [HarmonyPrefix]
        public static bool ServerSendSendChatMessagePre(ulong param_0, string param_1)
        {
            if (!IsHost()) return true;
            string msg = param_1.ToLower();
            if (param_0 == GetMyID() && msg.StartsWith("!"))
            {
                switch (msg)
                {
                    case "!start":
                        toggleAutoStart = !toggleAutoStart;
                        if (toggleAutoStart) Chatbox.Instance.AppendMessage(1, "Auto Start ON", "");
                        else Chatbox.Instance.AppendMessage(1, "Auto Start OFF", "");
                        break;
                    case "!afk":
                        toggleAfk = !toggleAfk;
                        if (toggleAfk) Chatbox.Instance.AppendMessage(1, "Afk ON", "");
                        else Chatbox.Instance.AppendMessage(1, "Afk OFF", "");
                        break;
                    case "!help":
                        Chatbox.Instance.AppendMessage(1, "!start", "");
                        Chatbox.Instance.AppendMessage(1, "!afk", "");
                        break;
                    default:
                        Chatbox.Instance.AppendMessage(1, "Invalid Command", "");
                        break;
                }
                return false;
            }
            else return true;
        }

        [HarmonyPatch(typeof(ServerSend), nameof(ServerSend.SendChatMessage))]
        [HarmonyPostfix]
        public static void ServerSendSendChatMessagePost(string param_1)
        {
            string msg = param_1.ToLower();
            if (msg == "!creator") // !creator
            {
                ServerSend.SendChatMessage(1, "Mod created by JMac");
            }
        }

        [HarmonyPatch(typeof(LobbyManager), nameof(LobbyManager.OnPlayerJoinLeaveUpdate))]
        [HarmonyPostfix]
        public static void LobbyManagerOnPlayerJoinLeave(CSteamID param_1, bool param_2)
        {
            if (IsHost() && GetModeID() == 4 && !param_2)
            {
                if (hunted.Contains(param_1.m_SteamID)) hunted.Remove(param_1.m_SteamID);
                if (hunters.Contains(param_1.m_SteamID)) hunters.Remove(param_1.m_SteamID);
            }
        }

        [HarmonyPatch(typeof(MonoBehaviourPublicGataInefObInUnique), "Method_Private_Void_GameObject_Boolean_Vector3_Quaternion_0")]
        [HarmonyPatch(typeof(MonoBehaviourPublicCSDi2UIInstObUIloDiUnique), "Method_Private_Void_0")]
        [HarmonyPatch(typeof(MonoBehaviourPublicVesnUnique), "Method_Private_Void_0")]
        [HarmonyPatch(typeof(MonoBehaviourPublicObjomaOblogaTMObseprUnique), "Method_Public_Void_PDM_2")]
        [HarmonyPatch(typeof(MonoBehaviourPublicTeplUnique), "Method_Private_Void_PDM_32")]
        [HarmonyPrefix]
        public static bool Prefix(System.Reflection.MethodBase __originalMethod)
        {
            return false;
        }
    }
}
