using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Showcase.Models;
using System.Diagnostics;

namespace Showcase.Controllers
{
    [Authorize]
    public class TicTacToeController : Controller
    {
        private readonly ILogger<TicTacToeController> _logger;
        //private static GameModel _game = new GameModel();

        public TicTacToeController(ILogger<TicTacToeController> logger)
        {
            _logger = logger;
        }
    
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public IActionResult MakeMove(int row, int col)
        {
           // _game.MakeMove(row, col);
            return RedirectToAction("Index");
        }

        public IActionResult Reset()
        {
           // _game = new GameModel();
            return RedirectToAction("Index");
        }

    }
}
