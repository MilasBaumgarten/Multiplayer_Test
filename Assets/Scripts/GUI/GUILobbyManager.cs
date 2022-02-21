using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Unity.Services.Lobbies.Models;
using System.Collections.Generic;

[RequireComponent(typeof(LobbyManager), typeof(RelayManager))]
public class GUILobbyManager : NetworkBehaviour {
#region Variables
	[Header("MP Menu")]
	[SerializeField]
	private Button createLobbyButton;
	[SerializeField]
	private Button quickJoinButton;
	[SerializeField]
	private Button openLobbyBrowserButton;

	[Header("Lobby Browser")]
	[SerializeField]
	private Button leaveLobbyBrowserButton;

	[Space(5)]
	[SerializeField]
	private GameObject lobbyBrowserContainer;
	[SerializeField]
	private GameObject lobbyEntryAsset;
	[SerializeField]
	private TMPro.TMP_InputField newLobbyName;

	[Header("Lobby")]
	[SerializeField]
	private Button closeLobbyButton;
	[SerializeField]
	private Button leaveLobbyButton;
	[SerializeField]
	private Button startGameButton;
	[SerializeField]
	private Button selectCatrionaButton;
	[SerializeField]
	private Button selectRobertButton;

	[Space(5)]
	[SerializeField]
	private GameObject hostContainer;
	[SerializeField]
	private GameObject clientContainer;

	[Header("Menus")]
	[SerializeField]
	private GameObject mpMenu;
	[SerializeField]
	private GameObject lobbyMenu;
	[SerializeField]
	private GameObject lobbyBrowser;
	[SerializeField]
	private GameObject loadingScreen;

	[Header("Game")]
	[SerializeField]
	private string gameScene;

	private LobbyManager lobbyManager => GetComponent<LobbyManager>();
	private RelayManager relayManager => GetComponent<RelayManager>();

	#endregion

	private async void Start() {
		#region MP Menu
		// CREATE LOBBY
		createLobbyButton?.onClick.AddListener(() => {
			CreateLobby();

			ShowUISelective(MPState.LOADING);
		});

		// JOIN RANDOM LOBBY
		quickJoinButton?.onClick.AddListener(() => {
			Quickjoin();

			ShowUISelective(MPState.LOADING);
		});

		// OPEN LOBBY BROWSER
		openLobbyBrowserButton?.onClick.AddListener(() => {
			SearchForLobbies();

			ShowUISelective(MPState.BROWSER);
		});
		#endregion

		#region Lobby Browser
		// LEAVE LOBBY BROWSER
		leaveLobbyBrowserButton?.onClick.AddListener(() => {
			ShowUISelective(MPState.MENU);
		});
		#endregion

		#region Lobby
		// CLOSE LOBBY
		closeLobbyButton?.onClick.AddListener(() => {
			CloseLobby();

			ShowUISelective(MPState.LOADING);
		});

		// LEAVE LOBBY
		leaveLobbyButton?.onClick.AddListener(() => {
			LeaveLobby();

			ShowUISelective(MPState.LOADING);
		});

		// START GAME
		startGameButton?.onClick.AddListener(() => {
			StartGame();

			ShowUISelective(MPState.LOADING);
		});

		// SELECT CATRIONA
		selectCatrionaButton?.onClick.AddListener(() => {
			selectCatrionaButton.interactable = false;
			selectRobertButton.interactable = true;

			SelectCharacter(Character.CATRIONA);
		});

		// SELECT ROBERT
		selectRobertButton?.onClick.AddListener(() => {
			selectRobertButton.interactable = false;
			selectCatrionaButton.interactable = true;

			SelectCharacter(Character.ROBERT);
		});
		#endregion

		// setup MP and UI
		ShowUISelective(MPState.LOADING);
		await lobbyManager.Init();
		ShowUISelective(MPState.MENU);
	}

	// MP MENU
	public void SetNewLobbyName() {
		lobbyManager.newLobbyName = newLobbyName.text;
	}


	// LOBBY BROWSER
	private async void SearchForLobbies() {
		List<Lobby> foundLobbies = await LobbyManager.SearchForLobbies();

		foreach (Lobby lobby in foundLobbies) {
			GameObject lobbyEntryGameObject = Instantiate(lobbyEntryAsset);
			lobbyEntryGameObject.transform.SetParent(lobbyBrowserContainer.transform, false);

			GUILobbyEntry LobbyEntry = lobbyEntryGameObject.GetComponent<GUILobbyEntry>();
			LobbyEntry.lobbyId = lobby.Id;
			LobbyEntry.guiLobbyManager = this;
			LobbyEntry.SetLobbyName(" " + lobby.Name);  // unschön um Abstand zu halten aber erstmal gut genug
		}
	}

	public async void JoinLobbyById(string lobbyId) {
		ShowUISelective(MPState.LOADING);

		print(lobbyId);

		if (await lobbyManager.JoinLobbyById(lobbyId)) {
			ShowUISelective(MPState.LOADING);
		} else {
			SearchForLobbies();
			ShowUISelective(MPState.BROWSER);
		}

		// get relay code from lobby
		string joinCode = lobbyManager.currentLobby.Data["JoinCode"].Value;

		// join relay
		RelayJoinData relayJoinData = await relayManager.JoinRelay(joinCode);

		// set allocationID in lobby for client
		await lobbyManager.SetOwnPlayerAllocationId(relayJoinData.AllocationID.ToString());

		if (NetworkManager.Singleton.StartClient()) {
			Debug.Log("\nClient started ...");
			ShowUISelective(MPState.LOBBY_CLIENT);
		} else {
			Debug.Log("\nUnable to start client!");
			ShowUISelective(MPState.MENU);
		}

		PlayerManager.Instance.Init();
	}

	// LOBBY
	private void StartGame() {
		if (NetworkManager.Singleton.IsHost) {
			NetworkManager.Singleton.SceneManager.LoadScene(gameScene, LoadSceneMode.Single);
		}
	}

	public async void SelectCharacter(Character selectedCharacter) {
		if(!await lobbyManager.SetOwnPlayerCharacter(selectedCharacter.ToString())){
			// reset selection buttons if this was unsuccessfull
			selectRobertButton.interactable = true;
			selectCatrionaButton.interactable = true;
		}
	}

	private async void CreateLobby() {
		await lobbyManager.CreateLobby();

		RelayHostData relayHostData = await relayManager.SetupRelay();

		// save join code in lobby
		await lobbyManager.SetRelayCodeToLobby(relayHostData.JoinCode);

		// set allocationID for host
		await lobbyManager.SetOwnPlayerAllocationId(relayHostData.AllocationID.ToString());

		if (NetworkManager.Singleton.StartHost()) {
			Debug.Log("\nHost started ...");
			ShowUISelective(MPState.LOBBY_HOST);
		} else {
			Debug.Log("\nUnable to start host!");
			ShowUISelective(MPState.MENU);
		}

		PlayerManager.Instance.Init();
	}

	private async void Quickjoin() {
		if (!await lobbyManager.QuickJoin()) {
			// unable to join, return to menu
			ShowUISelective(MPState.MENU);
			return;
		}

		// get relay code from lobby
		string joinCode = lobbyManager.currentLobby.Data["JoinCode"].Value;

		// join relay
		RelayJoinData relayJoinData = await relayManager.JoinRelay(joinCode);

		// set allocationID in lobby for client
		await lobbyManager.SetOwnPlayerAllocationId(relayJoinData.AllocationID.ToString());

		if (NetworkManager.Singleton.StartClient()) {
			Debug.Log("\nClient started ...");
			ShowUISelective(MPState.LOBBY_CLIENT);
		} else {
			Debug.Log("\nUnable to start client!");
			ShowUISelective(MPState.MENU);
		}

		PlayerManager.Instance.Init();
	}

	private async void LeaveLobby() {
		PlayerManager.Instance.Cleanup();
		await lobbyManager.LeaveLobby();

		NetworkManager.Singleton.Shutdown();
		ShowUISelective(MPState.MENU);
	}

	private async void CloseLobby() {
		PlayerManager.Instance.Cleanup();
		// scheint nicht zuverlässig Clients rauszuschmeißen
		await lobbyManager.CloseLobby();

		NetworkManager.Singleton.Shutdown();
		ShowUISelective(MPState.MENU);
	}

	private void ShowUISelective(MPState state) {
		switch (state) {
			case MPState.MENU:
				mpMenu.SetActive(true);
				lobbyMenu.SetActive(false);
				lobbyBrowser.SetActive(false);
				loadingScreen.SetActive(false);
				break;
			case MPState.BROWSER:
				mpMenu.SetActive(false);
				lobbyMenu.SetActive(false);
				lobbyBrowser.SetActive(true);
				loadingScreen.SetActive(false);
				break;
			case MPState.LOBBY_CLIENT:
				mpMenu.SetActive(false);
				lobbyMenu.SetActive(true);
				lobbyBrowser.SetActive(false);
				loadingScreen.SetActive(false);

				clientContainer.gameObject.SetActive(true);
				hostContainer.gameObject.SetActive(false);
				break;
			case MPState.LOBBY_HOST:
				mpMenu.SetActive(false);
				lobbyMenu.SetActive(true);
				lobbyBrowser.SetActive(false);
				loadingScreen.SetActive(false);

				clientContainer.gameObject.SetActive(false);
				hostContainer.gameObject.SetActive(true);
				break;
			case MPState.LOADING:
				mpMenu.SetActive(false);
				lobbyMenu.SetActive(false);
				lobbyBrowser.SetActive(false);
				loadingScreen.SetActive(true);
				break;
		}
	}
}

enum MPState {
	MENU,
	BROWSER,
	LOBBY_HOST,
	LOBBY_CLIENT,
	LOADING
}
