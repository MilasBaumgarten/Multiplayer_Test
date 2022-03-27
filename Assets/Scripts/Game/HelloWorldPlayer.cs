using UnityEngine;
using Unity.Netcode;

public class HelloWorldPlayer : NetworkBehaviour {
	public override void OnNetworkSpawn() {
		if (IsOwner) {
			Move();
		}
	}

	public void Move() {
		var randomPosition = GetRandomPositionOnPlane();
		transform.position = randomPosition;

		if (NetworkManager.Singleton.IsServer) {
			SubmitPositionRequestClientRpc(randomPosition);
		} else {
			SubmitPositionRequestServerRpc(randomPosition);
		}
	}

	static Vector3 GetRandomPositionOnPlane() {
		return new Vector3(Random.Range(-3f, 3f), 1f, Random.Range(-3f, 3f));
	}

	[ServerRpc]
	private void SubmitPositionRequestServerRpc(Vector3 position) {
		transform.position = position;
	}

	[ClientRpc]
	private void SubmitPositionRequestClientRpc(Vector3 position) {
		transform.position = position;
	}
}
