using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Unity.Services.Lobbies.Models;
using System.Collections.Generic;
using TMPro;

[RequireComponent(typeof(LobbyHelloWorld), typeof(RelayManager))]
public class GUILobbyManager : NetworkBehaviour {
	// Buttons
	[SerializeField]
	private Button createLobbyButton;
	[SerializeField]
	private Button joinRandomLobbyButton;
	[SerializeField]
	private Button openLobbyBrowserButton;
	[SerializeField]
	private Button closeLobbyButton;
	[SerializeField]
	private Button leaveLobbyButton;
	[SerializeField]
	private Button leaveLobbyBrowserButton;
	[SerializeField]
	private Button startGameButton;

	[Space(10)]
	[SerializeField]
	private GameObject lobbyBrowserContainer;
	[SerializeField]
	private GameObject lobbyEntryAsset;
	[SerializeField]
	private TMPro.TMP_InputField newLobbyName;

	// Menus
	[Space(10)]
	[SerializeField]
	private GameObject mpMenu;
	[SerializeField]
	private GameObject lobbyMenu;
	[SerializeField]
	private GameObject lobbyBrowser;
	[SerializeField]
	private GameObject loadingScreen;

	[Space(10)]
	[SerializeField]
	private string gameScene;

	private LobbyHelloWorld lobbyManager => GetComponent<LobbyHelloWorld>();
	private RelayManager relayManager => GetComponent<RelayManager>();

	private void Start() {
		// CREATE LOBBY
		createLobbyButton?.onClick.AddListener(() => {
			CreateLobby();

			ShowUISelective(MPState.LOADING);
		});

		// JOIN RANDOM LOBBY
		joinRandomLobbyButton?.onClick.AddListener(() => {
			JoinRandomLobby();

			ShowUISelective(MPState.LOADING);
		});

		// OPEN LOBBY BROWSER
		openLobbyBrowserButton?.onClick.AddListener(() => {
			SearchForLobbies();

			ShowUISelective(MPState.BROWSER);
		});

		// LEAVE LOBBY BROWSER
		leaveLobbyBrowserButton?.onClick.AddListener(() => {
			ShowUISelective(MPState.MENU);
		});

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


		// setup UI
		ShowUISelective(MPState.MENU);
	}

	public void SetNewLobbyName() {
		lobbyManager.newLobbyName = newLobbyName.text;
	}

	private async void SearchForLobbies() {
		List<Lobby> foundLobbies = await LobbyHelloWorld.SearchForLobbies();

		foreach(Lobby lobby in foundLobbies) {
			GameObject lobbyEntryGameObject = Instantiate(lobbyEntryAsset);
			lobbyEntryGameObject.transform.SetParent(lobbyBrowserContainer.transform, false);

			LobbyEntry LobbyEntry = lobbyEntryGameObject.GetComponent<LobbyEntry>();
			LobbyEntry.lobbyId = lobby.Id;
			LobbyEntry.guiLobbyManager = this;
			LobbyEntry.SetLobbyName(" " + lobby.Name);	// unschön um Abstand zu halten aber erstmal gut genug
		}
	}

	public async void JoinLobbyById(string lobbyId) {
		ShowUISelective(MPState.LOADING);

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
	}

	private void StartGame() {
		if (NetworkManager.Singleton.IsHost) {
			NetworkManager.Singleton.SceneManager.LoadScene(gameScene, LoadSceneMode.Single);
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
	}

	private async void JoinRandomLobby() {
		if (!await lobbyManager.SearchAndJoinLobby()) {
			// unable to join, return to menu
			ShowUISelective(MPState.MENU);
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
	}

	private async void LeaveLobby() {
		await lobbyManager.LeaveLobby();

		NetworkManager.Singleton.Shutdown();
		ShowUISelective(MPState.MENU);
	}

	private async void CloseLobby() {
		// scheint nicht zuverlässig Clients rauszuschmeißen
		await lobbyManager.CloseLobby();

		NetworkManager.Singleton.Shutdown();
		ShowUISelective(MPState.MENU);
	}

	// TODO:
	//	- add Lobby Browser

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

				closeLobbyButton.gameObject.SetActive(false);
				leaveLobbyButton.gameObject.SetActive(true);
				startGameButton.gameObject.SetActive(false);
				break;
			case MPState.LOBBY_HOST:
				mpMenu.SetActive(false);
				lobbyMenu.SetActive(true);
				lobbyBrowser.SetActive(false);
				loadingScreen.SetActive(false);

				closeLobbyButton.gameObject.SetActive(true);
				leaveLobbyButton.gameObject.SetActive(false);
				startGameButton.gameObject.SetActive(true);
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
