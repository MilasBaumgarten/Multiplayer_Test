using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Netcode;

public class LobbyManager : NetworkBehaviour {
	// Inspector properties with initial values

	/// <summary>
	/// Used to set the lobby name in this example.
	/// </summary>
	public string newLobbyName = "LobbyHelloWorld" + Guid.NewGuid();

	/// <summary>
	/// Used to set the max number of players in this example.
	/// </summary>
	public int maxPlayers = 8;

	/// <summary>
	/// Used to determine if the lobby shall be private in this example.
	/// </summary>
	public bool isPrivate = false;

	// We'll only be in one lobby at once for this demo, so let's track it here
	public Lobby currentLobby { get; private set; }

	private Player loggedInPlayer;

	// TODO:
	//	- send Heartbeat to created Lobbby every 10-30 sec to assure, that it won't be marked as inactive
	//	- take care of HTTP Error 429 Too Many Requests

	async void Start() {
		try {
			/// init Unity Services
			await UnityServices.InitializeAsync();
			Debug.Log("Unity Services initialized");

			/// player log in
			loggedInPlayer = await GetPlayerFromAnonymousLoginAsync();
			Debug.Log("Player successfully logged in");

			// Add some data to our player
			// This data will be included in a lobby under players -> player.data
			loggedInPlayer.Data.Add("Ready", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, "No"));
		} catch (Exception ex) {
			Debug.Log($"{ex}");
		}
	}

	public async Task SetRelayCodeToLobby(string joinCode) {
		currentLobby.Data["JoinCode"] = new DataObject(DataObject.VisibilityOptions.Public, joinCode);

		// update lobby data
		currentLobby = await Lobbies.Instance.UpdateLobbyAsync(
			lobbyId: currentLobby.Id,
			options: new UpdateLobbyOptions() {
				Data = currentLobby.Data
			}
		);

		// Since we're the host, let's wait a second and then heartbeat the lobby
		await Task.Delay(1000);
		await Lobbies.Instance.SendHeartbeatPingAsync(currentLobby.Id);
	}

	public async Task SetOwnPlayerAllocationId(string allocationId) {
		// Update the lobby
		currentLobby = await Lobbies.Instance.UpdatePlayerAsync(
			lobbyId: currentLobby.Id,
			playerId: loggedInPlayer.Id,
			options: new UpdatePlayerOptions() {
				AllocationId = allocationId
			});

		// Let's poll for the lobby data again just to see what it looks like
		currentLobby = await Lobbies.Instance.GetLobbyAsync(currentLobby.Id);

		Debug.Log("Latest lobby info:\n" + JsonConvert.SerializeObject(currentLobby));
	}

	public async Task<bool> SetOwnPlayerCharacter(string selectedCharacter) {
		// poll the lobby to get accurate player count
		currentLobby = await Lobbies.Instance.GetLobbyAsync(currentLobby.Id);

		//check that two players are in the lobby
		if (currentLobby.Players.Count != 2) {
			Debug.LogWarning($"Expected a player count of 2, found: {currentLobby.Players.Count}!");
			return false;
		}
		print(selectedCharacter);
		UpdatePlayerCharactersClientRpc(selectedCharacter);
		return true;
	}

	[ClientRpc]
	private async void UpdatePlayerCharactersClientRpc(string hostSelectedCharacter) {
		if (NetworkManager.Singleton.IsHost) {
			SetPlayerCharacterInfo(loggedInPlayer, hostSelectedCharacter);
		} else {
			if (hostSelectedCharacter == Character.CATRIONA.ToString()) {
				SetPlayerCharacterInfo(loggedInPlayer, Character.ROBERT.ToString());
			} else {
				SetPlayerCharacterInfo(loggedInPlayer, Character.CATRIONA.ToString());
			}
		}

		// update player
		currentLobby = await Lobbies.Instance.UpdatePlayerAsync(
			lobbyId: currentLobby.Id,
			playerId: loggedInPlayer.Id,
			options: new UpdatePlayerOptions() {
				Data = loggedInPlayer.Data
			}
		);

		// Let's poll for the lobby data again just to see what it looks like
		currentLobby = await Lobbies.Instance.GetLobbyAsync(currentLobby.Id);

		Debug.Log("Latest lobby info:\n" + JsonConvert.SerializeObject(currentLobby));
	}

	private void SetPlayerCharacterInfo(Player player, string selectedCharacter) {
		if (player.Data.ContainsKey("Character")) {
			player.Data["Character"] = new PlayerDataObject(
				visibility: PlayerDataObject.VisibilityOptions.Public,
				value: selectedCharacter.ToString()
			);
		} else {
			player.Data.Add(
				"Character",
				new PlayerDataObject(
					visibility: PlayerDataObject.VisibilityOptions.Public,
					value: selectedCharacter.ToString()
				)
			);
		}
	}

	public async Task<bool> QuickJoin() {
		try {
			Debug.Log($"Trying to use Quick Join to find a lobby...");
			currentLobby = await Lobbies.Instance.QuickJoinLobbyAsync(new QuickJoinLobbyOptions {
				Player = loggedInPlayer, // Including the player here lets us join with data pre-populated
				Filter = new List<QueryFilter> {
                    // Let's search for lobbies with open slots and the right version
                    new QueryFilter(
						field: QueryFilter.FieldOptions.AvailableSlots,
						op: QueryFilter.OpOptions.GT,
						value: "0"),

					// check for version tag
					new QueryFilter(
						field: QueryFilter.FieldOptions.N1, // N1 = "Version"
						op: QueryFilter.OpOptions.EQ,
						value: "0.1"),
				}
			});
			return true;
		} catch {
			Debug.LogWarning("couldn't find a lobby");
			return false;
		}
	}

	public async Task LeaveLobby() {
		string localPlayerId = AuthenticationService.Instance.PlayerId;

		if (currentLobby != null && !currentLobby.HostId.Equals(localPlayerId)) {
			await Lobbies.Instance.RemovePlayerAsync(
						lobbyId: currentLobby.Id,
						playerId: loggedInPlayer.Id);

			Debug.Log($"Left lobby {currentLobby.Name} ({currentLobby.Id})");

			currentLobby = null;
		}
	}

	public async Task CloseLobby() {
		string localPlayerId = AuthenticationService.Instance.PlayerId;

		// This is so that orphan lobbies aren't left around in case the demo fails partway through
		if (currentLobby != null && currentLobby.HostId.Equals(localPlayerId)) {
			await Lobbies.Instance.DeleteLobbyAsync(currentLobby.Id);
			Debug.Log($"Deleted lobby {currentLobby.Name} ({currentLobby.Id})");
		}
	}

	public async Task CreateLobby() {
		// TODO:
		// Anscheinend muss hier irgendwas bei der Initialisierung reingepackt werden, weil currentLobby.Data sonst NULL ist
		// -> ohne komische Initialisierung kann JoinCode nicht geschrieben werden
		var lobbyData = new Dictionary<string, DataObject>() {
			["Version"] = new DataObject(DataObject.VisibilityOptions.Public, "0.1", DataObject.IndexOptions.N1)
		};

		// Create a new lobby
		currentLobby = await Lobbies.Instance.CreateLobbyAsync(
			lobbyName: newLobbyName,
			maxPlayers: maxPlayers,
			options: new CreateLobbyOptions() {
				Data = lobbyData,
				IsPrivate = isPrivate,
				Player = loggedInPlayer
			});

		Debug.Log($"Created new lobby {currentLobby.Name} ({currentLobby.Id})");
	}

	public async Task<bool> JoinLobbyById(string lobbyId){
		try {
			currentLobby = await Lobbies.Instance.JoinLobbyByIdAsync(
				lobbyId: lobbyId,
				options: new JoinLobbyByIdOptions() {
					Player = loggedInPlayer
				});

			Debug.Log($"Joined lobby {currentLobby.Name} ({currentLobby.Id})");
			return true;
		} catch {
			return false;
		}
	}

	public static async Task<List<Lobby>> SearchForLobbies() {
		// Query for existing lobbies

		// Use filters to only return lobbies which match specific conditions
		List<QueryFilter> queryFilters = new List<QueryFilter> {
            // Let's search for games with open slots (AvailableSlots greater than 0)
            new QueryFilter(
				field: QueryFilter.FieldOptions.AvailableSlots,
				op: QueryFilter.OpOptions.GT,
				value: "0"),

            // check for version tag
            new QueryFilter(
				field: QueryFilter.FieldOptions.N1, // N1 = "Version"
                op: QueryFilter.OpOptions.EQ,
				value: "0.1"),

		};

		// Query results can also be ordered
		// The query API supports multiple "order by x, then y, then..." options
		// Order results by available player slots (least first), then by lobby age, then by lobby name
		List<QueryOrder> queryOrdering = new List<QueryOrder> {
			new QueryOrder(true, QueryOrder.FieldOptions.AvailableSlots),
			new QueryOrder(false, QueryOrder.FieldOptions.Created),
			new QueryOrder(false, QueryOrder.FieldOptions.Name),
		};

		// Call the Query API
		QueryResponse response = await Lobbies.Instance.QueryLobbiesAsync(new QueryLobbiesOptions() {
			Count = 20, // Override default number of results to return
			Filters = queryFilters,
			Order = queryOrdering,
		});

		List<Lobby> foundLobbies = response.Results;
		return foundLobbies;
	}

	// Log in a player using Unity's "Anonymous Login" API and construct a Player object for use with the Lobbies APIs
	private async Task<Player> GetPlayerFromAnonymousLoginAsync() {
		if (!AuthenticationService.Instance.IsSignedIn) {
			Debug.Log($"Trying to log in a player ...");

			// Use Unity Authentication to log in
			await AuthenticationService.Instance.SignInAnonymouslyAsync();

			if (!AuthenticationService.Instance.IsSignedIn) {
				throw new InvalidOperationException("Player was not signed in successfully; unable to continue without a logged in player");
			}
		}

		Debug.Log("Player signed in as " + AuthenticationService.Instance.PlayerId);

		// Player objects have Get-only properties, so you need to initialize the data bag here if you want to use it
		return new Player(AuthenticationService.Instance.PlayerId, null, new Dictionary<string, PlayerDataObject>());
	}
}

public enum Character {
	ROBERT,
	CATRIONA
}
