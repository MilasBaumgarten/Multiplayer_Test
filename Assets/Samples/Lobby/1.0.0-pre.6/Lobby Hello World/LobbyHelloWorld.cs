using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Random = UnityEngine.Random;

public class LobbyHelloWorld : MonoBehaviour {
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

	[HideInInspector]
	public string debugMessage = "";

	async void Start() {
		try {
			/// init Unity Services
			await UnityServices.InitializeAsync();
			WriteDebugMessage("Unity Services initialized");

			/// player log in
			loggedInPlayer = await GetPlayerFromAnonymousLoginAsync();
			WriteDebugMessage("Player successfully logged in");

			// Add some data to our player
			// This data will be included in a lobby under players -> player.data
			loggedInPlayer.Data.Add("Ready", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, "No"));
		} catch (Exception ex) {
			WriteDebugMessage($"{ex}");
		}
	}

	// TODO:
	//	- send Heartbeat to created Lobbby every 10-30 sec to assure, that it won't be marked as inactive
	//	- take care of HTTP Error 429 Too Many Requests
	public async Task<bool> SearchAndJoinLobby() {
		List<Lobby> foundLobbies = await SearchForLobbies();
		if (foundLobbies.Any()) {
			await JoinRandomLobby(foundLobbies);
			return true;
		} else {
			WriteDebugMessage("No Lobby found.");
			return false;
		}
	}

	// #####################
	// # Example Functions #
	// #####################
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
				//Data = loggedInPlayer.Data
			});

		// Let's poll for the lobby data again just to see what it looks like
		currentLobby = await Lobbies.Instance.GetLobbyAsync(currentLobby.Id);

		WriteDebugMessage("Latest lobby info:\n" + JsonConvert.SerializeObject(currentLobby));
	}

	// ####################
	// # Helper Functions #
	// ####################
	private async Task QuickJoin() {
		// Now, let's try the QuickJoin API, which just puts our player in a matching lobby automatically
		// This is fast and reliable (as long as matching lobbies are available), but removes some user
		//   interactivity (can't choose from a list of lobbies)
		// You can use filters to specify which types of lobbies can be joined just like a Query call
		// This also shows an example of how to catch lobby exceptions
		// Note that the QueryJoin API will throw exceptions on failures to find a matchmaking lobby,
		//   so it's more likely to fail than other API calls
		WriteDebugMessage($"Trying to use Quick Join to find a lobby...");
		currentLobby = await Lobbies.Instance.QuickJoinLobbyAsync(new QuickJoinLobbyOptions {
			Player = loggedInPlayer, // Including the player here lets us join with data pre-populated
			Filter = new List<QueryFilter> {
                    // Let's search for lobbies with a specific name
                    new QueryFilter(
						field: QueryFilter.FieldOptions.Name,
						op: QueryFilter.OpOptions.EQ,
						value: "My New Lobby"),

                    // You can add more filters here, such as filters on custom data fields
                }
		});
	}

	public async Task LeaveLobby() {
		string localPlayerId = AuthenticationService.Instance.PlayerId;

		if (currentLobby != null && !currentLobby.HostId.Equals(localPlayerId)) {
			await Lobbies.Instance.RemovePlayerAsync(
						lobbyId: currentLobby.Id,
						playerId: loggedInPlayer.Id);

			WriteDebugMessage($"Left lobby {currentLobby.Name} ({currentLobby.Id})");

			currentLobby = null;
		}
	}

	public async Task CloseLobby() {
		string localPlayerId = AuthenticationService.Instance.PlayerId;

		// This is so that orphan lobbies aren't left around in case the demo fails partway through
		if (currentLobby != null && currentLobby.HostId.Equals(localPlayerId)) {
			await Lobbies.Instance.DeleteLobbyAsync(currentLobby.Id);
			WriteDebugMessage($"Deleted lobby {currentLobby.Name} ({currentLobby.Id})");
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

		WriteDebugMessage($"Created new lobby {currentLobby.Name} ({currentLobby.Id})");
	}

	public async Task<bool> JoinLobbyById(string lobbyId){
		try {
			currentLobby = await Lobbies.Instance.JoinLobbyByIdAsync(
				lobbyId: lobbyId,
				options: new JoinLobbyByIdOptions() {
					Player = loggedInPlayer
				});

			WriteDebugMessage($"Joined lobby {currentLobby.Name} ({currentLobby.Id})");
			return true;
		} catch {
			return false;
		}
	}

	private async Task JoinRandomLobby(List<Lobby> foundLobbies) {
		WriteDebugMessage("Found lobbies:\n" + JsonConvert.SerializeObject(foundLobbies));

		// Let's pick a random lobby to join
		var randomLobby = foundLobbies[Random.Range(0, foundLobbies.Count)];

		// Try to join the lobby
		// Player is optional because the service can pull the player data from the auth token
		// However, if your player has custom data, you will want to pass the Player object into this call
		// This will save you having to do a Join call followed by an UpdatePlayer call
		currentLobby = await Lobbies.Instance.JoinLobbyByIdAsync(
			lobbyId: randomLobby.Id,
			options: new JoinLobbyByIdOptions() {
				Player = loggedInPlayer
			});

		WriteDebugMessage($"Joined lobby {currentLobby.Name} ({currentLobby.Id})");

		// You can also join via a Lobby Code instead of a lobby ID
		// Lobby Codes are a short, unique codes that map to a specific lobby ID
		// EX:
		// currentLobby = await Lobbies.Instance.JoinLobbyByCodeAsync("myLobbyJoinCode");
	}

	public static async Task<List<Lobby>> SearchForLobbies() {
		// Query for existing lobbies

		// Use filters to only return lobbies which match specific conditions
		// You can only filter on built-in properties (Ex: AvailableSlots) or indexed custom data (S1, N1, etc.)
		// Take a look at the API for other built-in fields you can filter on
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
			WriteDebugMessage($"Trying to log in a player ...");

			// Use Unity Authentication to log in
			await AuthenticationService.Instance.SignInAnonymouslyAsync();

			if (!AuthenticationService.Instance.IsSignedIn) {
				throw new InvalidOperationException("Player was not signed in successfully; unable to continue without a logged in player");
			}
		}

		WriteDebugMessage("Player signed in as " + AuthenticationService.Instance.PlayerId);

		// Player objects have Get-only properties, so you need to initialize the data bag here if you want to use it
		return new Player(AuthenticationService.Instance.PlayerId, null, new Dictionary<string, PlayerDataObject>());
	}

	private void WriteDebugMessage(string message) {
		Debug.Log(message);
		debugMessage = message;
	}
}
