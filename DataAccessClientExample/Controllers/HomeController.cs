﻿using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using DataAccessClient;
using DataAccessClient.Searching;
using DataAccessClientExample.DataLayer;
using Microsoft.AspNetCore.Mvc;
using DataAccessClientExample.Models;
using Microsoft.EntityFrameworkCore;

namespace DataAccessClientExample.Controllers
{
    public class HomeController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IRepository<ExampleEntity> _exampleEntityRepository;
        private readonly IRepository<ExampleSecondEntity> _exampleSecondEntityRepository;
        private readonly IQueryableSearcher<ExampleEntity> _exampleEntityQueryableSearcher;
        private readonly IQueryableSearcher<ExampleSecondEntity> _exampleSecondEntityQueryableSearcher;

        public HomeController(IUnitOfWork unitOfWork, IRepository<ExampleEntity> exampleEntityRepository, IRepository<ExampleSecondEntity> exampleSecondEntityRepository, IQueryableSearcher<ExampleEntity> exampleEntityQueryableSearcher, IQueryableSearcher<ExampleSecondEntity> exampleSecondEntityQueryableSearcher)
        {
            _unitOfWork = unitOfWork;
            _exampleEntityRepository = exampleEntityRepository;
            _exampleSecondEntityRepository = exampleSecondEntityRepository;
            _exampleEntityQueryableSearcher = exampleEntityQueryableSearcher;
            _exampleSecondEntityQueryableSearcher = exampleSecondEntityQueryableSearcher;
        }

        [HttpGet]
        public async Task<IActionResult> Test()
        {
            var exampleEntity1 = new ExampleEntity
            {
                Name = "Henk Kin"
            };

            var exampleEntity2 = new ExampleEntity
            {
                Name = "Kin Henk"
            };

            _exampleEntityRepository.Add(exampleEntity1);
            _exampleEntityRepository.Add(exampleEntity2);

            var exampleSecondEntity1 = new ExampleSecondEntity
            {
                Name = "Henk Kin"
            };

            var exampleSecondEntity2 = new ExampleSecondEntity
            {
                Name = "Kin Henk"
            };

            _exampleSecondEntityRepository.Add(exampleSecondEntity1);
            _exampleSecondEntityRepository.Add(exampleSecondEntity2);

            await _unitOfWork.SaveAsync();

            exampleEntity2.Name = "Updated example";
            exampleSecondEntity2.Name = "Updated example second";

            await _unitOfWork.SaveAsync();

            var exampleEntities = await _exampleEntityRepository.GetChangeTrackingQuery()
                .ToListAsync();

            var exampleSecondEntities = await _exampleSecondEntityRepository.GetChangeTrackingQuery()
                .ToListAsync();

            _exampleEntityRepository.Remove(exampleEntity1);
            _exampleSecondEntityRepository.Remove(exampleSecondEntity1);

            await _unitOfWork.SaveAsync();

            var criteria = new Criteria();
            criteria.OrderBy = "Id";
            criteria.OrderByDirection = OrderByDirection.Ascending;
            criteria.Page = 1;
            criteria.PageSize = 10;
            criteria.Search = "Data Access Client";

            var exampleEntitiesSearchResults = await _exampleEntityQueryableSearcher.ExecuteAsync(_exampleEntityRepository.GetReadOnlyQuery(), criteria);
            var exampleSecondEntitiesSearchResults = await _exampleSecondEntityQueryableSearcher.ExecuteAsync(_exampleSecondEntityRepository.GetReadOnlyQuery(), criteria);

            return Json(new{ exampleEntitiesSearchResults, exampleSecondEntitiesSearchResults });
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var exampleEntities = await _exampleEntityRepository.GetReadOnlyQuery()
                .Where(e => !e.IsDeleted)
                .ToListAsync();

            var exampleSecondEntities = await _exampleSecondEntityRepository.GetReadOnlyQuery()
                .Where(e => !e.IsDeleted)
                .ToListAsync();

            return Json(new { exampleEntities, exampleSecondEntities });
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
