using KnowledgeSpace.WebPortal.Models;
using KnowledgeSpace.WebPortal.Services;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace KnowledgeSpace.WebPortal.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IKnowledgeBaseApiClient _knowledgeBaseApiClient;
        private readonly ILabelApiClient _labelApiClient;

        public HomeController(ILogger<HomeController> logger,
            ILabelApiClient labelApiClient,
            IKnowledgeBaseApiClient knowledgeBaseApiClient)
        {
            _logger = logger;
            _labelApiClient = labelApiClient;
            _knowledgeBaseApiClient = knowledgeBaseApiClient;
        }

        public async Task<IActionResult> Index()
        {
            var latestKbs = await _knowledgeBaseApiClient.GetLatestKnowledgeBases(6);
            var popularKbs = await _knowledgeBaseApiClient.GetPopularKnowledgeBases(6);
            var labels = await _labelApiClient.GetPopularLabels(20);
            var viewModel = new HomeViewModel()
            {
                LatestKnowledgeBases = latestKbs,
                PopularKnowledgeBases = popularKbs,
                PopularLabels = labels,
            };

            return View(viewModel);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}