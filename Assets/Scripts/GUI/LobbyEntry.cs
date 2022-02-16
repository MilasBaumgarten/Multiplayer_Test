using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class LobbyEntry : NetworkBehaviour {
	[HideInInspector]
	public string lobbyId;
	[HideInInspector]
	public GUILobbyManager guiLobbyManager;

	[SerializeField]
	private Button joinButton;
	[SerializeField]
	private TMP_Text lobbyNameLabel;

	void Start() {
		// setup JOIN button
		joinButton?.onClick.AddListener(() => {
			if (lobbyId == "" || !guiLobbyManager) {
				Debug.LogWarning("Lobby Id or GUI Lobby Manager were not set when initializing Lobby Entry!");
			}

			guiLobbyManager.JoinLobbyById(lobbyId);
		});
	}

	public void SetLobbyName(string lobbyName) {
		lobbyNameLabel.text = lobbyName;
	}
}
