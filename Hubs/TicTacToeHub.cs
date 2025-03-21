using Microsoft.AspNetCore.SignalR;
using Microsoft.CodeAnalysis.Elfie.Model;
using Showcase.DataService;
using Showcase.Models;
using Showcase.Models.TicTacToeModels;
using System.Diagnostics;

namespace Showcase.Hubs
{
    public class TicTacToeHub : Hub
    {
        private readonly SharedDb _shared;
        private static Dictionary<string, string> currentTurn = new(); // Track whose turn it is
        private static Dictionary<string, string[]> gameBoards = new();
        private static Dictionary<string, string> pendingResets = new();
        private static Dictionary<string, string> pendingRematches = new();


        public TicTacToeHub(IServiceProvider serviceProvider)
        {
            _shared = serviceProvider.GetRequiredService<SharedDb>();
        }


        public async Task JoinTicTacToeGameRoom(UserConnection conn)
        {
            await Clients.All
                .SendAsync("ReceiveMessage", "admin", $"{conn.Username} has joined");
        }

        public async Task JoinSpecificTicTacToeGameRoom(UserConnection conn)
        {
            // Get current players in the room
            var playersInRoom = _shared.connections
                .Where(e => e.Value.TicTacToeGameRoom == conn.TicTacToeGameRoom)
                .ToList();

            // Allow only up to 2 players
            if (playersInRoom.Count >= 2)
            {
                await Clients.Caller.SendAsync("RoomFull");
                return;
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, conn.TicTacToeGameRoom);
            _shared.connections[Context.ConnectionId] = conn;

            playersInRoom = _shared.connections
                .Where(e => e.Value.TicTacToeGameRoom == conn.TicTacToeGameRoom)
                .ToList();

            await Clients.Group(conn.TicTacToeGameRoom)
                .SendAsync("PlayerCountUpdate", playersInRoom.Count);

            await Clients.Group(conn.TicTacToeGameRoom)
              .SendAsync("JoinSpecificTicTacToeGameRoom", "admin", $"{conn.Username} has joined {conn.TicTacToeGameRoom}");


            if (playersInRoom.Count == 1)
            {
                // First player joins: assign "x" and set turn
                Console.WriteLine($"Assigning symbol 'x' to {Context.ConnectionId}");
                conn.PlayerSymbol = "x";
                await Clients.Caller.SendAsync("AssignSymbol", conn.PlayerSymbol);
                currentTurn[conn.TicTacToeGameRoom] = Context.ConnectionId;
                await Clients.Caller.SendAsync("UpdateTurn", new { ConnectionId = Context.ConnectionId});
            }
            else if (playersInRoom.Count == 2)
            {
                // Second player joins: assign "o"
                Console.WriteLine($"Assigning symbol 'o' to {Context.ConnectionId}");
                conn.PlayerSymbol = "o";
                await Clients.Caller.SendAsync("AssignSymbol", conn.PlayerSymbol);
                var firstPlayer = playersInRoom.First();
                await Clients.Caller.SendAsync("UpdateTurn", new { ConnectionId = firstPlayer.Key});
            }



        }

        public async Task SendMessage(string msg)
        {
            if(_shared.connections.TryGetValue(Context.ConnectionId, out UserConnection conn))
            {
                await Clients.Group(conn.TicTacToeGameRoom)
                .SendAsync("ReceiveSpecificMessage",conn.Username, msg);
            }          
        }

        public async Task SendMove(Move move)
        {
            if (_shared.connections.TryGetValue(Context.ConnectionId, out UserConnection conn))
            {
                var room = conn.TicTacToeGameRoom;

                // Check turn
                if (currentTurn.TryGetValue(room, out var currentPlayerId) && currentPlayerId == Context.ConnectionId)
                {
                    // Initialize board if not already
                    if (!gameBoards.ContainsKey(room))
                        gameBoards[room] = new string[9];

                    var board = gameBoards[room];

                    // Validate move
                    if (move.Index < 0 || move.Index >= 9 || !string.IsNullOrEmpty(board[move.Index]))
                        return;

                    // Apply move
                    board[move.Index] = move.Symbol;

                    // Broadcast move
                    await Clients.Group(room).SendAsync("ReceiveMove", move);

                    // Check for winner
                    string winner = CheckWinner(board);
                    if (winner != null)
                    {
                        await Clients.Group(room).SendAsync("GameWon", winner);
                        return;
                    }

                    // Check for draw
                    if (board.All(cell => !string.IsNullOrEmpty(cell)))
                    {
                        await Clients.Group(room).SendAsync("GameDraw");
                        return;
                    }

                    // Switch turns
                    var nextPlayer = _shared.connections
                        .Where(c => c.Value.TicTacToeGameRoom == room && c.Key != Context.ConnectionId)
                        .Select(c => c.Key)
                        .FirstOrDefault();

                    currentTurn[room] = nextPlayer;
                    await Clients.Group(room).SendAsync("UpdateTurn", new { ConnectionId = nextPlayer });
                }
            }
        }

        private string? CheckWinner(string[] board)
        {
            int[][] wins = new int[][]
            {
        new[]{0,1,2}, new[]{3,4,5}, new[]{6,7,8},
        new[]{0,3,6}, new[]{1,4,7}, new[]{2,5,8},
        new[]{0,4,8}, new[]{2,4,6}
            };

            foreach (var combo in wins)
            {
                var (a, b, c) = (combo[0], combo[1], combo[2]);
                if (!string.IsNullOrEmpty(board[a]) && board[a] == board[b] && board[b] == board[c])
                {
                    return board[a]; // "x" or "o"
                }
            }
            return null;
        }


        public async Task GameEnd(string gameRoom)
        {

            var playersInRoom = _shared.connections.Where(e => e.Value.TicTacToeGameRoom == gameRoom).ToList();

            if (_shared.connections.TryGetValue(Context.ConnectionId, out UserConnection conn))
            {
                var player = playersInRoom.First().Key.ToString();
                currentTurn[gameRoom] = player;
                await Clients.Group(gameRoom).SendAsync("ResetGame");
                await Clients.Group(conn.TicTacToeGameRoom).SendAsync("UpdateTurn", new { ConnectionId = player });
            }
        }

        public async Task RequestReset(string gameRoom)
        {
            var players = _shared.connections
                .Where(c => c.Value.TicTacToeGameRoom == gameRoom)
                .Select(c => c.Key)
                .ToList();

            if (players.Count == 2)
            {
                var otherPlayer = players.FirstOrDefault(p => p != Context.ConnectionId);
                if (otherPlayer != null)
                {
                    pendingResets[gameRoom] = Context.ConnectionId;
                    await Clients.Client(otherPlayer).SendAsync("ConfirmResetRequest");
                }
            }
        }

        public async Task ConfirmReset(string gameRoom, bool confirm)
        {
            if (confirm && pendingResets.TryGetValue(gameRoom, out var initiator))
            {
                // Reset game state
                gameBoards.Remove(gameRoom);
                currentTurn[gameRoom] = initiator;
                pendingResets.Remove(gameRoom);

                await Clients.Group(gameRoom).SendAsync("ResetGame");
                await Clients.Group(gameRoom).SendAsync("UpdateTurn", new { ConnectionId = initiator });
            }
            else
            {
                pendingResets.Remove(gameRoom);
                await Clients.Caller.SendAsync("ResetRequestRejected");
            }
        }

        public Task<string> GetConnectionId()
        {
            return Task.FromResult(Context.ConnectionId);
        }

        public async Task RequestRematch(string gameRoom)
        {
            var players = _shared.connections
                .Where(c => c.Value.TicTacToeGameRoom == gameRoom)
                .Select(c => c.Key)
                .ToList();

            if (players.Count == 2)
            {
                if (!pendingRematches.ContainsKey(gameRoom))
                {
                    pendingRematches[gameRoom] = Context.ConnectionId;
                    var otherPlayer = players.First(p => p != Context.ConnectionId);
                    await Clients.Client(otherPlayer).SendAsync("RematchRequested");
                }
                else if (pendingRematches[gameRoom] != Context.ConnectionId)
                {
                    // Both players agreed
                    gameBoards.Remove(gameRoom);
                    currentTurn[gameRoom] = pendingRematches[gameRoom];
                    pendingRematches.Remove(gameRoom);

                    await Clients.Group(gameRoom).SendAsync("RematchAccepted");
                    await Clients.Group(gameRoom).SendAsync("ResetGame");
                    await Clients.Group(gameRoom).SendAsync("UpdateTurn",
                        new { ConnectionId = currentTurn[gameRoom] });
                }
            }
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            if (_shared.connections.TryRemove(Context.ConnectionId, out UserConnection conn))
            {
                var playersInRoom = _shared.connections
                    .Where(e => e.Value.TicTacToeGameRoom == conn.TicTacToeGameRoom)
                    .ToList();

                await Clients.Group(conn.TicTacToeGameRoom)
                    .SendAsync("RedirectToLobby");

                // Cleanup game state
                gameBoards.Remove(conn.TicTacToeGameRoom);
                currentTurn.Remove(conn.TicTacToeGameRoom);
                pendingResets.Remove(conn.TicTacToeGameRoom);
                pendingRematches.Remove(conn.TicTacToeGameRoom);
            }
            await base.OnDisconnectedAsync(exception);
        }
    }
}
