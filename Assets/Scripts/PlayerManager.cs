using System.Collections.Generic;
using Unity.Netcode;
using Unity.Services.Lobbies.Models;
using UnityEngine;

public class PlayerManager : NetworkBehaviour {
	[SerializeField]
	private string lobbySceneName;

	[HideInInspector]
	public Player player;

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

		if (player.Data.ContainsKey("Character")) {
			NetworkObject playerCharacter = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject();
			playerCharacter.GetComponent<CharacterSelector>().SetSelectedCharacterServerRPC(player.Data["Character"].Value);
		}
	}
}
