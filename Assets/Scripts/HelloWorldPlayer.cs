using UnityEngine;
using Unity.Netcode;

public class HelloWorldPlayer : NetworkBehaviour {

	public NetworkVariable<Vector3> position = new NetworkVariable<Vector3>();

	public override void OnNetworkSpawn() {
		if (IsOwner) {
			Move();
		}
	}

	public void Move() {
		if (NetworkManager.Singleton.IsServer) {
			var randomPosition = GetRandomPositionOnPlane();
			transform.position = randomPosition;
			position.Value = randomPosition;
		} else {
			SubmitPositionRequestServerRpc();
		}
	}

	static Vector3 GetRandomPositionOnPlane() {
		return new Vector3(Random.Range(-3f, 3f), 1f, Random.Range(-3f, 3f));
	}

	[ServerRpc]
	private void SubmitPositionRequestServerRpc(ServerRpcParams rpcParams = default) {
		position.Value = GetRandomPositionOnPlane();
	}

	private void Update() {
	// TODO: don'd move here, instead move after RPC
		transform.position = position.Value;
	}
}
