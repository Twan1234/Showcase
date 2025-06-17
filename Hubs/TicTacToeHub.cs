using Microsoft.AspNetCore.SignalR;
using Microsoft.CodeAnalysis.Elfie.Model;
using Showcase.DataService;
using Showcase.Models;
using System.Diagnostics;

namespace Showcase.Hubs
{
    public class TicTacToeHub : Hub
    {
        private readonly ITicTacToeDbService _dbService;

        public TicTacToeHub(ITicTacToeDbService dbService)
        {
            _dbService = dbService;
        }

        public async Task JoinSpecificTicTacToeGameRoom(string roomCode, string username)
        {

            if (await _dbService.CheckOfGameVolIs(roomCode))
            {
                await Clients.Caller.SendAsync("RoomFull");
                return;
            }

            var session = await _dbService.CreateOrJoinGameSessionAsync(roomCode, username, Context.ConnectionId);

            await Groups.AddToGroupAsync(Context.ConnectionId, roomCode);

            await Clients.Group(roomCode)
                .SendAsync("PlayerCountUpdate", session.Players.Count);

            await Clients.Group(roomCode)
              .SendAsync("JoinSpecificTicTacToeGameRoom", "admin", $"{username} has joined {roomCode}");

            var currentPlayer = session.Players.FirstOrDefault(p => p.ConnectionId == Context.ConnectionId);

            await Clients.Caller.SendAsync("AssignSymbol", currentPlayer.PlayerSymbol);
            await Clients.Caller.SendAsync("UpdateTurn", new { ConnectionId = session.CurrentTurnConnectionId });
        }

        public async Task SendMessage(string msg)
        {
            var conn = await _dbService.GetConnectionByIdAsync(Context.ConnectionId);
            if (conn != null)
            {
                await Clients.Group(conn.GameSession.RoomCode)
                    .SendAsync("ReceiveSpecificMessage", conn.Username, msg);
            }
        }

        public async Task SendMove(int index, string symbol)
        {
            var conn = await _dbService.GetConnectionByIdAsync(Context.ConnectionId);
            if (conn == null) return;
            var session = conn.GameSession;

            if (session.CurrentTurnConnectionId != Context.ConnectionId)
                return;

            string[] board = new string[9];
            foreach (var move in session.Moves)
            {
                board[move.Index] = move.Symbol;
            }

            if (index < 0 || index >= 9 || !string.IsNullOrEmpty(board[index]))
                return;

            await _dbService.AddMoveAsync(Context.ConnectionId, index, symbol);

            await Clients.Group(session.RoomCode).SendAsync("ReceiveMove", new { Index = index, Symbol = conn.PlayerSymbol });

            board[index] = conn.PlayerSymbol;

            board[index] = conn.PlayerSymbol;
            string? winner = CheckWinner(board);
            if (winner != null)
            {
                session.IsGameOver = true;
                await _dbService.AddHighScoresAsync(conn.ConnectionId, winner);
                await _dbService.SaveChangesAsync();
                await Clients.Group(session.RoomCode).SendAsync("GameWon", winner);
                return;
            }

            if (board.All(cell => !string.IsNullOrEmpty(cell)))
            {
                await _dbService.AddHighScoresAsync(conn.ConnectionId, "DRAW");
                await Clients.Group(session.RoomCode).SendAsync("GameDraw");
                return;
            }

            var nextPlayer = session.Players.FirstOrDefault(p => p.ConnectionId != Context.ConnectionId);
            if (nextPlayer != null)
            {
                session.CurrentTurnConnectionId = nextPlayer.ConnectionId;
                await _dbService.SaveChangesAsync();
                await Clients.Group(session.RoomCode).SendAsync("UpdateTurn", new { ConnectionId = nextPlayer.ConnectionId });            
            }
        }

        public string? CheckWinner(string[] board)
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
                    return board[a];
                }
            }
            return null;
        }

        public async Task<string> GetConnectionId()
        {
            return await Task.FromResult(Context.ConnectionId);
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var conn = await _dbService.GetConnectionByIdAsync(Context.ConnectionId);
            await _dbService.RemoveConnectionAsync(Context.ConnectionId);
            await base.OnDisconnectedAsync(exception);
        }

        public async Task RequestReset(string roomCode)
        {
            var session = await _dbService.GetSessionByRoomCodeAsync(roomCode);
            if (session == null || session.Players.Count != 2)
                return;

            var otherPlayer = session.Players.FirstOrDefault(p => p.ConnectionId != Context.ConnectionId);
            if (otherPlayer != null)
                await Clients.Client(otherPlayer.ConnectionId).SendAsync("ConfirmResetRequest");
        }

        public async Task ConfirmReset(string roomCode, bool confirm)
        {
            var session = await _dbService.GetSessionByRoomCodeAsync(roomCode);
            if (session == null)
                return;

            if (confirm)
            {
                var playerX = session.Players.FirstOrDefault(x => x.PlayerSymbol == "x");

                session.Moves.Clear();
                session.CurrentTurnConnectionId = playerX.ConnectionId;
                await _dbService.SaveChangesAsync();

                await Clients.Group(roomCode).SendAsync("ResetGame");
                await Clients.Group(roomCode).SendAsync("UpdateTurn", new { ConnectionId = playerX.ConnectionId });
            }
            else
            {
                var otherPlayer = session.Players.FirstOrDefault(p => p.ConnectionId != Context.ConnectionId);
                await Clients.Client(otherPlayer.ConnectionId).SendAsync("ResetRequestRejected");              
            }
        }
        public async Task RequestRematch(string roomCode)
        {
            var session = await _dbService.GetSessionByRoomCodeAsync(roomCode);
            if (session == null || session.Players.Count != 2)
                return;

            var otherPlayer = session.Players.FirstOrDefault(p => p.ConnectionId != Context.ConnectionId);
            if (otherPlayer != null)
                await Clients.Client(otherPlayer.ConnectionId).SendAsync("ConfirmRematchRequest");
        }

        public async Task ConfirmRematch(string roomCode, bool confirm)
        {
            var session = await _dbService.GetSessionByRoomCodeAsync(roomCode);
            if (session == null)
                return;

            if (confirm)
            {
                var playerX = session.Players.FirstOrDefault(x =>
                    string.Equals(x.PlayerSymbol, "x", StringComparison.OrdinalIgnoreCase));
                session.CurrentTurnConnectionId = playerX.ConnectionId;
                session.Moves.Clear();
                await _dbService.SaveChangesAsync();

                await Clients.Group(roomCode).SendAsync("ResetGame");
                await Clients.Group(roomCode).SendAsync("UpdateTurn", new { ConnectionId = playerX.ConnectionId });
            }
            else
            {
                var otherPlayer = session.Players.FirstOrDefault(p => p.ConnectionId != Context.ConnectionId);
                await Clients.Client(otherPlayer.ConnectionId).SendAsync("RematchRequestRejected");
                await Clients.Client(Context.ConnectionId).SendAsync("RedirectToLobby");
            }
        }

    }

}

