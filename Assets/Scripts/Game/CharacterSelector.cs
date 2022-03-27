using Unity.Netcode;
using UnityEngine;
using Unity.Collections;

public class CharacterSelector : NetworkBehaviour {
	[SerializeField]
	private Material catrionaMat;
	[SerializeField]
	private Material robertMat;

	public NetworkVariable<FixedString64Bytes> selectedCharacter = new NetworkVariable<FixedString64Bytes>("");

	[ServerRpc]
	public void SetSelectedCharacterServerRPC(string character) {
		selectedCharacter.Value = character;
	}

	private void OnCharacterSelected(FixedString64Bytes oldCharacter, FixedString64Bytes newCharacter) {
		// return when this is only server
		if (!IsClient) { return; }

		switch (newCharacter.ToString()) {
			case "CATRIONA":
				GetComponent<MeshRenderer>().material = catrionaMat;
				break;
			case "ROBERT":
				GetComponent<MeshRenderer>().material = robertMat;
				break;
			default:
				Debug.Log($"character string not set or unknown: {newCharacter}");
				break;
		}
	}

	private void OnEnable() {
		selectedCharacter.OnValueChanged += OnCharacterSelected;
	}

	private void OnDisable() {
		selectedCharacter.OnValueChanged = OnCharacterSelected;
	}
}
