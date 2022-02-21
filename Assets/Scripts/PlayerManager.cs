using Unity.Netcode;
using Unity.Services.Lobbies.Models;
using UnityEngine;

public class PlayerManager : NetworkBehaviour {
	[SerializeField]
	private Material catrionaMat;
	[SerializeField]
	private Material robertMat;

	[SerializeField]
	private string lobbySceneName;

	[HideInInspector]
	public Player localPlayer;

	public static PlayerManager Instance { get; private set; }

	private void Start() {
		if (Instance == null) {
			Instance = this;
			DontDestroyOnLoad(this);
		} else {
			Destroy(this);
		}
	}

	public void Init() {
		NetworkManager.Singleton.SceneManager.OnLoadComplete += SceneManager_OnLoadComplete;
	}

	public void Cleanup() {
		NetworkManager.Singleton.SceneManager.OnLoadComplete -= SceneManager_OnLoadComplete;
	}

	private void SceneManager_OnLoadComplete(ulong clientId, string sceneName, UnityEngine.SceneManagement.LoadSceneMode loadSceneMode) {
		if (sceneName == lobbySceneName) {
			return;
		}

		Debug.LogWarning($"Scene loaded for client: {clientId}");

		// DEBUG
		if (!localPlayer.Data.ContainsKey("Character")) {
			SetPlayerVisualsServerRpc(NetworkManager.Singleton.LocalClient.PlayerObject.NetworkObjectId, "CATRIONA");
			return;
		}

		SetPlayerVisualsServerRpc(NetworkManager.Singleton.LocalClient.PlayerObject.NetworkObjectId, localPlayer.Data["Character"].Value);
	}

	[ServerRpc]
	private void SetPlayerVisualsServerRpc(ulong playerObjectId, string character) {
		//Only the owner can invoke a ServerRpc that requires ownership!
		//	UnityEngine.Debug:LogError(object)
		//PlayerManager: SetPlayerVisualsServerRpc(ulong, string)(at Assets / Scripts / PlayerManager.cs:54)
		//PlayerManager: SceneManager_OnLoadComplete(ulong, string, UnityEngine.SceneManagement.LoadSceneMode)(at Assets / Scripts / PlayerManager.cs:49)
		//Unity.Netcode.NetworkSceneManager:OnClientLoadedScene(uint, UnityEngine.SceneManagement.Scene)(at Library / PackageCache / com.unity.netcode.gameobjects@1.0.0 - pre.5 / Runtime / SceneManagement / NetworkSceneManager.cs:1366)
		//Unity.Netcode.NetworkSceneManager:OnSceneLoaded(uint, string)(at Library / PackageCache / com.unity.netcode.gameobjects@1.0.0 - pre.5 / Runtime / SceneManagement / NetworkSceneManager.cs:1280)
		//Unity.Netcode.NetworkSceneManager /<> c__DisplayClass85_0:< OnClientSceneLoadingEvent > b__0(UnityEngine.AsyncOperation)(at Library / PackageCache / com.unity.netcode.gameobjects@1.0.0 - pre.5 / Runtime / SceneManagement / NetworkSceneManager.cs:1214)
		//UnityEngine.AsyncOperation:InvokeCompletionEvent()
		NetworkObject playerObject = NetworkManager.Singleton.SpawnManager.SpawnedObjects[playerObjectId];

		print($"Setting {playerObjectId} to {character}");

		switch (character) {
			case "CATRIONA":
				playerObject.GetComponent<MeshRenderer>().material = catrionaMat;
				break;
			case "ROBERT":
				playerObject.GetComponent<MeshRenderer>().material = robertMat;
				break;
			default:
				Debug.Log($"character string not set or unknown: {localPlayer.Data["Character"].Value}");
				break;
		}
	}
}
