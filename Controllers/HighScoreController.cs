using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Showcase.DataService;
using Showcase.Models;
using System.Diagnostics;

namespace Showcase.Controllers
{
    [Authorize]
    public class HighScoreController : Controller
    {
        private readonly ILogger<HighScoreController> _logger;
        private readonly ITicTacToeDbService _dbService;

        public HighScoreController(ILogger<HighScoreController> logger, ITicTacToeDbService dbService)
        {
            _logger = logger;
            _dbService = dbService;
        }

        public async Task<IActionResult> Index()
        {
            var scores = await _dbService.GetAllHighScoresAsync();
            var sortedScores = scores.OrderByDescending(s => s.Wins).ToList();
            return View(sortedScores);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id)
        {
            var score = await _dbService.GetHighScoreByIdAsync(id);
            if (score == null) return NotFound();
            return View(score);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(HighScore score)
        {
            if (!ModelState.IsValid)
                return View(score);

            await _dbService.UpdateHighScoreAsync(score);
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var score = await _dbService.GetHighScoreByIdAsync(id);
            if (score == null) return NotFound();
            return View(score);
        }

        [HttpPost, ActionName("Delete")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await _dbService.DeleteHighScoreAsync(id);
            return RedirectToAction(nameof(Index));
        }


    }
}
