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
            return RedirectToAction("Index");
        }

        public IActionResult Reset()
        {
            return RedirectToAction("Index");
        }

    }
}
