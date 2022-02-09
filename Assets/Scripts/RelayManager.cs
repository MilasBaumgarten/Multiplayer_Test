using UnityEngine;
using Unity.Netcode;
using System.Threading.Tasks;
using Unity.Services.Core;
using Unity.Services.Core.Environments;
using Unity.Services.Authentication;
using Unity.Services.Relay.Models;
using Unity.Services.Relay;

public class RelayManager : MonoBehaviour {
	[SerializeField]
	private string environment = "production";

	[SerializeField]
	private int maxConnections = 2;

	[HideInInspector]
	public string debugMessage = "";

	public bool isRelayEnabled => transport != null && transport.Protocol == UnityTransport.ProtocolType.RelayUnityTransport;

	public UnityTransport transport => NetworkManager.Singleton.gameObject.GetComponent<UnityTransport>();

	public async Task<RelayHostData> SetupRelay() {
		await Init();

		// create allocation
		Allocation allocation = await Relay.Instance.CreateAllocationAsync(maxConnections);

		// create relay host data
		RelayHostData relayHostData = new RelayHostData {
			Key = allocation.Key,
			Port = (ushort)allocation.RelayServer.Port,
			AllocationID = allocation.AllocationId,
			AllocationIDBytes = allocation.AllocationIdBytes,
			IPv4Address = allocation.RelayServer.IpV4,
			ConnectionData = allocation.ConnectionData
		};

		// create join code
		relayHostData.JoinCode = await Relay.Instance.GetJoinCodeAsync(relayHostData.AllocationID);

		transport.SetRelayServerData(
			relayHostData.IPv4Address,
			relayHostData.Port,
			relayHostData.AllocationIDBytes,
			relayHostData.Key,
			relayHostData.ConnectionData
		);

		WriteDebugMessage($"Relay Server generated with IP: {relayHostData.IPv4Address}:{relayHostData.Port}\nJoin Code: {relayHostData.JoinCode}");

		return relayHostData;
	}

	public async Task<RelayJoinData> JoinRelay(string joinCode) {
		await Init();

		WriteDebugMessage($"Trying to join with code: {joinCode}");

		JoinAllocation allocation = await Relay.Instance.JoinAllocationAsync(joinCode);

		WriteDebugMessage("Joint allocation");

		RelayJoinData relayJoinData = new RelayJoinData {
			Key = allocation.Key,
			Port = (ushort)allocation.RelayServer.Port,
			AllocationID = allocation.AllocationId,
			AllocationIDBytes = allocation.AllocationIdBytes,
			ConnectionData = allocation.ConnectionData,
			HostConnectionData = allocation.HostConnectionData,
			IPv4Address = allocation.RelayServer.IpV4,
			JoinCode = joinCode
		};

		transport.SetRelayServerData(
			relayJoinData.IPv4Address,
			relayJoinData.Port,
			relayJoinData.AllocationIDBytes,
			relayJoinData.Key,
			relayJoinData.ConnectionData,
			relayJoinData.HostConnectionData
		);

		WriteDebugMessage($"Client joined game with join code: {relayJoinData.JoinCode}");

		return relayJoinData;
	}

	private async Task Init() {
		WriteDebugMessage("Initializing ...");

		InitializationOptions options = new InitializationOptions()
					.SetEnvironmentName(environment);

		await UnityServices.InitializeAsync(options);

		// sign in if not signed in already
		if (!AuthenticationService.Instance.IsSignedIn) {
			await AuthenticationService.Instance.SignInAnonymouslyAsync();
		}

		WriteDebugMessage("Initialization complete");
	}

	private void WriteDebugMessage(string message) {
		Debug.Log(message);
		debugMessage = message;
	}
}

// TODO:
// https://youtu.be/82Lbho7S0OA?t=1409