using UnityEngine;
using UnityEditor;
using Unity.Netcode;

[RequireComponent(typeof(RelayManager))]
public class GUIRelayManager : MonoBehaviour {
	[SerializeField]
	private Rect guiSize = new Rect(10, 10, 300, 300);

	private RelayManager manager => GetComponent<RelayManager>();

	string joinCode = "Hello World";

	private void Awake() {
		guiSize = new Rect(
			Screen.width - guiSize.x - guiSize.width,
			guiSize.y,
			guiSize.width,
			guiSize.height
		);
	}

	void OnGUI() {
		GUILayout.BeginArea(guiSize);

		SetupRelayAsync();
		JoinRelay();

		joinCode = GUILayout.TextField(joinCode, 25);

		GUILayout.Label(manager.debugMessage);

		GUILayout.EndArea();
	}

	private async void SetupRelayAsync() {
		if (GUILayout.Button("Host")) {
			if (manager.isRelayEnabled){
				await manager.SetupRelay();
			}

			if (NetworkManager.Singleton.StartHost()) {
				manager.debugMessage += "\nHost started ...";
			} else {
				manager.debugMessage += "\nUnable to start host!";
			}
		}
	}

	private async void JoinRelay() {
		if (GUILayout.Button("Join")) {
			Debug.Log(joinCode);

			if (manager.isRelayEnabled) {
				await manager.JoinRelay(joinCode);
			}

			if (NetworkManager.Singleton.StartClient()) {
				manager.debugMessage += "\nClient started ...";
			} else {
				manager.debugMessage += "\nUnable to start client!";
			}
		}
	}
	
#if UNITY_EDITOR
	// Editor UI
	private void OnDrawGizmosSelected() {
		Rect rightAligned = new Rect(
			Screen.width - guiSize.x - guiSize.width,
			guiSize.y,
			guiSize.width,
			guiSize.height
		);

		Handles.BeginGUI();
		Handles.DrawSolidRectangleWithOutline(rightAligned, new Color(1,1,1, 0.1f), Color.black);
		Handles.EndGUI();
	}
#endif
}
