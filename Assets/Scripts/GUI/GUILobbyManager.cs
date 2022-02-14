using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(LobbyHelloWorld), typeof(RelayManager))]
public class GUILobbyManager : NetworkBehaviour {
	// Buttons
	[SerializeField]
	private Button createLobbyButton;
	[SerializeField]
	private Button joinRandomLobbyButton;
	[SerializeField]
	private Button closeLobbyButton;
	[SerializeField]
	private Button leaveLobbyButton;
	[SerializeField]
	private Button startGameButton;

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

	private LobbyHelloWorld lobby => GetComponent<LobbyHelloWorld>();
	private RelayManager relay => GetComponent<RelayManager>();

	private void Start() {
		// CREATE LOBBY
		createLobbyButton?.onClick.AddListener(() => {
			CreateLobby();

			ShowUISelective(MPState.LOADING);
			closeLobbyButton.gameObject.SetActive(true);
			leaveLobbyButton.gameObject.SetActive(false);
			startGameButton.gameObject.SetActive(true);
		});

		// JOIN RANDOM LOBBY
		joinRandomLobbyButton?.onClick.AddListener(() => {
			JoinRandomLobby();

			ShowUISelective(MPState.LOADING);
			closeLobbyButton.gameObject.SetActive(false);
			leaveLobbyButton.gameObject.SetActive(true);
			startGameButton.gameObject.SetActive(false);
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

	private void StartGame() {
		if (NetworkManager.Singleton.IsHost) {
			NetworkManager.Singleton.SceneManager.LoadScene(gameScene, LoadSceneMode.Single);
		}
	}

	private async void CreateLobby() {
		await lobby.CreateLobby();

		RelayHostData relayHostData = await relay.SetupRelay();

		// save join code in lobby
		await lobby.SetRelayCodeToLobby(relayHostData.JoinCode);

		// set allocationID for host
		await lobby.SetOwnPlayerAllocationId(relayHostData.AllocationID.ToString());

		if (NetworkManager.Singleton.StartHost()) {
			Debug.Log("\nHost started ...");
			ShowUISelective(MPState.LOBBY);
		} else {
			Debug.Log("\nUnable to start host!");
			ShowUISelective(MPState.MENU);
		}
	}

	private async void JoinRandomLobby() {
		if (!await lobby.SearchAndJoinLobby()) {
			// unable to join, return to menu
			ShowUISelective(MPState.MENU);
		}

		// get relay code from lobby
		string joinCode = lobby.currentLobby.Data["JoinCode"].Value;

		// join relay
		RelayJoinData relayJoinData = await relay.JoinRelay(joinCode);

		// set allocationID in lobby for client
		await lobby.SetOwnPlayerAllocationId(relayJoinData.AllocationID.ToString());

		if (NetworkManager.Singleton.StartClient()) {
			Debug.Log("\nClient started ...");
			ShowUISelective(MPState.LOBBY);
		} else {
			Debug.Log("\nUnable to start client!");
			ShowUISelective(MPState.MENU);
		}
	}

	private async void LeaveLobby() {
		await lobby.LeaveLobby();

		NetworkManager.Singleton.Shutdown();
		ShowUISelective(MPState.MENU);
	}

	private async void CloseLobby() {
		// scheint nicht zuverl‰ssig Clients rauszuschmeiﬂen
		await lobby.CloseLobby();

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
			case MPState.LOBBY:
				mpMenu.SetActive(false);
				lobbyMenu.SetActive(true);
				lobbyBrowser.SetActive(false);
				loadingScreen.SetActive(false);
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
	LOBBY,
	LOADING
}
