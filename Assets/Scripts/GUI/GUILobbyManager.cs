using UnityEngine;
using UnityEditor;
using Unity.Netcode;

[RequireComponent(typeof(LobbyHelloWorld))]
public class GUILobbyManager : MonoBehaviour {
	[SerializeField]
	private Rect guiSize = new Rect(10, 10, 300, 300);

	private LobbyHelloWorld manager => GetComponent<LobbyHelloWorld>();

	void OnGUI() {
		GUILayout.BeginArea(guiSize);

		// if not client or server
		if (!NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsHost) {
			CreateLobby();
			JoinRandomLobby();
		} else {
			if (!NetworkManager.Singleton.IsHost) {
				CloseLobby();
			} else {
				LeaveLobby();
			}

			StatusLabels();
		}

		GUILayout.Label(manager.debugMessage);

		GUILayout.EndArea();
	}

	static void StatusLabels() {
		string mode = NetworkManager.Singleton.IsHost ?
			"Host" : "Client";

		GUILayout.Label("Transport: " +
			NetworkManager.Singleton.NetworkConfig.NetworkTransport.GetType().Name);
		GUILayout.Label("Mode: " + mode);
	}

	private async void CreateLobby() {
		if (GUILayout.Button("Create Lobby")) {
			await manager.CreateLobby();

			if (NetworkManager.Singleton.StartHost()) {
				manager.debugMessage += "\nHost started ...";
			} else {
				manager.debugMessage += "\nUnable to start host!";
			}
		}
	}

	private async void JoinRandomLobby() {
		if (GUILayout.Button("Join a random Lobby")) {
			await manager.SearchAndJoinLobby();

			if (NetworkManager.Singleton.StartClient()) {
				manager.debugMessage += "\nClient started ...";
			} else {
				manager.debugMessage += "\nUnable to start client!";
			}
		}
	}

	private async void LeaveLobby() {
		if (GUILayout.Button("Leave lobby")) await manager.LeaveLobby();
	}

	private async void CloseLobby() {
		if (GUILayout.Button("Close lobby")) await manager.CloseLobby();
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
