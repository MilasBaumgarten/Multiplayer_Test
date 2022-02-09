using UnityEngine;
using UnityEditor;
using Unity.Netcode;

[RequireComponent(typeof(LobbyHelloWorld), typeof(RelayManager))]
public class GUILobbyManager : NetworkBehaviour {
	[SerializeField]
	private Rect guiSize = new Rect(10, 10, 300, 300);

	private LobbyHelloWorld lobby => GetComponent<LobbyHelloWorld>();
	private RelayManager relay => GetComponent<RelayManager>();

	void OnGUI() {
		GUILayout.BeginArea(guiSize);

		// if not client or server
		if (!NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsHost) {
			CreateLobby();
			JoinRandomLobby();
		} else {
			if (NetworkManager.Singleton.IsHost) {
				CloseLobby();
			} else {
				LeaveLobby();
			}

			StatusLabels();
		}

		GUILayout.Label(lobby.debugMessage);

		GUILayout.EndArea();
	}

	private void StatusLabels() {
		string mode = NetworkManager.Singleton.IsHost ?
			"Host" : "Client";

		GUILayout.Label("Transport: " +
			NetworkManager.Singleton.NetworkConfig.NetworkTransport.GetType().Name);
		GUILayout.Label("Mode: " + mode);
	}

	private async void CreateLobby() {
		if (GUILayout.Button("Create Lobby")) {
			await lobby.CreateLobby();

			RelayHostData relayHostData = await relay.SetupRelay();

			// save join code in lobby
			await lobby.SetRelayCodeToLobby(relayHostData.JoinCode);

			// set allocationID for host
			await lobby.SetOwnPlayerAllocationId(relayHostData.AllocationID.ToString());

			if (NetworkManager.Singleton.StartHost()) {
				lobby.debugMessage += "\nHost started ...";
			} else {
				lobby.debugMessage += "\nUnable to start host!";
			}
		}
	}

	private async void JoinRandomLobby() {
		if (GUILayout.Button("Join a random Lobby")) {
			await lobby.SearchAndJoinLobby();

			// get relay code from lobby
			string joinCode = lobby.currentLobby.Data["JoinCode"].Value;

			// join relay
			RelayJoinData relayJoinData = await relay.JoinRelay(joinCode);

			// set allocationID in lobby for client
			await lobby.SetOwnPlayerAllocationId(relayJoinData.AllocationID.ToString());

			if (NetworkManager.Singleton.StartClient()) {
				lobby.debugMessage += "\nClient started ...";
			} else {
				lobby.debugMessage += "\nUnable to start client!";
			}
		}
	}

	private async void LeaveLobby() {
		if (GUILayout.Button("Leave lobby")) {
			await lobby.LeaveLobby();
			NetworkManager.Singleton.Shutdown();
		}
	}

	private async void CloseLobby() {
		if (GUILayout.Button("Close lobby")) {
			// scheint nicht zuverl‰ssig Clients rauszuschmeiﬂen
			await lobby.CloseLobby();

			NetworkManager.Singleton.Shutdown();
		}
	}

#if UNITY_EDITOR
	// Editor UI
	private void OnDrawGizmosSelected() {
		Handles.BeginGUI();
		Handles.DrawSolidRectangleWithOutline(guiSize, new Color(1, 1, 1, 0.1f), Color.black);
		Handles.EndGUI();
	}
#endif
}
