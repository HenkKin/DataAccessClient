using System.Collections.Generic;
using System.Threading.Tasks;
using DataAccessClient;
using DataAccessClient.Configuration;
using DataAccessClient.Searching;
using DataAccessClientExample.DataLayer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DataAccessClientExample.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ValuesController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IRepository<ExampleEntity> _exampleEntityRepository;
        private readonly IRepository<ExampleSecondEntity> _exampleSecondEntityRepository;
        private readonly IQueryableSearcher<ExampleEntity> _exampleEntityQueryableSearcher;
        private readonly IQueryableSearcher<ExampleSecondEntity> _exampleSecondEntityQueryableSearcher;
        private readonly ISoftDeletableConfiguration _softDeletableConfiguration;
        private readonly IMultiTenancyConfiguration _multiTenancyConfiguration;

        public ValuesController(
            IUnitOfWork unitOfWork, 
            IRepository<ExampleEntity> exampleEntityRepository, 
            IRepository<ExampleSecondEntity> exampleSecondEntityRepository, 
            IQueryableSearcher<ExampleEntity> exampleEntityQueryableSearcher, 
            IQueryableSearcher<ExampleSecondEntity> exampleSecondEntityQueryableSearcher,
            ISoftDeletableConfiguration softDeletableConfiguration,
            IMultiTenancyConfiguration multiTenancyConfiguration)
        {
            _unitOfWork = unitOfWork;
            _exampleEntityRepository = exampleEntityRepository;
            _exampleSecondEntityRepository = exampleSecondEntityRepository;
            _exampleEntityQueryableSearcher = exampleEntityQueryableSearcher;
            _exampleSecondEntityQueryableSearcher = exampleSecondEntityQueryableSearcher;
            _softDeletableConfiguration = softDeletableConfiguration;
            _multiTenancyConfiguration = multiTenancyConfiguration;
        }

        [Route("Test")]
        [HttpGet]
        public async Task<IActionResult> Test()
        {
            var exampleEntity1 = new ExampleEntity
            {
                Name = "Henk Kin"
            };

            var exampleEntity2TranslationNlNl = new ExampleEntityTranslation { Description = "Omschrijving", Language = "nl-NL" };
            var exampleEntity2TranslationEnGb = new ExampleEntityTranslation { Description = "Description", Language = "en-GB" };
            var exampleEntity2 = new ExampleEntity
            {
                Name = "Kin Henk",
                Translations = new List<ExampleEntityTranslation>
                {
                    exampleEntity2TranslationNlNl,
                    exampleEntity2TranslationEnGb
                }
            };

            _exampleEntityRepository.Add(exampleEntity1);
            _exampleEntityRepository.Add(exampleEntity2);

            var exampleSecondEntity1 = new ExampleSecondEntity
            {
                Name = "Henk Kin",
            };

            var exampleSecondEntity2 = new ExampleSecondEntity
            {
                Name = "Kin Henk"
            };

            _exampleSecondEntityRepository.Add(exampleSecondEntity1);
            _exampleSecondEntityRepository.Add(exampleSecondEntity2);

            await _unitOfWork.SaveAsync();

            exampleEntity2TranslationNlNl.Description += " geupdated";
            exampleEntity2.Translations.Add(new ExampleEntityTranslation{ Description = "Comment", Language = "fr-FR"});
            exampleEntity2.Name = "Updated example";

            exampleSecondEntity2.Name = "Updated example second";

            await _unitOfWork.SaveAsync();

            _exampleEntityRepository.Remove(exampleEntity1);
            _exampleSecondEntityRepository.Remove(exampleSecondEntity1);

            await _unitOfWork.SaveAsync();

            var criteria = new Criteria
            {
                OrderBy = "Id",
                OrderByDirection = OrderByDirection.Ascending,
                Page = 1,
                PageSize = 10,
                Search = "Updated"
            };
            criteria.Includes.Add("Translations");

            var secondCriteria = new Criteria
            {
                OrderBy = "Id",
                OrderByDirection = OrderByDirection.Ascending,
                Page = 1,
                PageSize = 10,
                Search = "Updated"
            };

            var exampleEntitiesSearchResults = await _exampleEntityQueryableSearcher.ExecuteAsync(_exampleEntityRepository.GetReadOnlyQuery(), criteria);
            var exampleSecondEntitiesSearchResults = await _exampleSecondEntityQueryableSearcher.ExecuteAsync(_exampleSecondEntityRepository.GetReadOnlyQuery(), secondCriteria);

            return Json(new{ exampleEntitiesSearchResults, exampleSecondEntitiesSearchResults });
        }

        [Route("get-all")]
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var exampleEntities = await _exampleEntityRepository.GetReadOnlyQuery()
                .ToListAsync();

            var exampleSecondEntities = await _exampleSecondEntityRepository.GetReadOnlyQuery()
                .ToListAsync();

            return Json(new { exampleEntities, exampleSecondEntities });
        }

        [Route("get-all2")]
        [HttpGet]
        public async Task<IActionResult> GetAll2()
        {
            var criteria = new Criteria
            {
                OrderBy = "Id",
                OrderByDirection = OrderByDirection.Ascending,
                Page = 1,
                PageSize = 10,
                Search = "Updated"
            };
            criteria.Includes.Add("Translations");

            var secondCriteria = new Criteria
            {
                OrderBy = "Id",
                OrderByDirection = OrderByDirection.Ascending,
                Page = 1,
                PageSize = 10,
                Search = "Updated"
            };

            var exampleEntitiesSearchResults = await _exampleEntityQueryableSearcher.ExecuteAsync(_exampleEntityRepository.GetReadOnlyQuery(), criteria);
            var exampleSecondEntitiesSearchResults = await _exampleSecondEntityQueryableSearcher.ExecuteAsync(_exampleSecondEntityRepository.GetReadOnlyQuery(), secondCriteria);

            return Json(new { exampleEntitiesSearchResults, exampleSecondEntitiesSearchResults });
        }

        [Route("disable-softdeletable")]
        [HttpGet]
        public async Task<IActionResult> DisableSoftDeletable()
        {
            var criteria = new Criteria
            {
                OrderBy = "Id",
                OrderByDirection = OrderByDirection.Ascending,
                Page = 1,
                PageSize = 10,
                Search = "Updated"
            };
            criteria.Includes.Add("Translations");

            var secondCriteria = new Criteria
            {
                OrderBy = "Id",
                OrderByDirection = OrderByDirection.Ascending,
                Page = 1,
                PageSize = 10,
                Search = "Updated"
            };

            CriteriaResult<ExampleEntity> exampleEntitiesSearchResults;
            CriteriaResult<ExampleSecondEntity> exampleSecondEntitiesSearchResults;

            bool beforeIsQueryFilterEnabled = _softDeletableConfiguration.IsQueryFilterEnabled;
            bool duringIsQueryFilterEnabled;

            using (_softDeletableConfiguration.DisableQueryFilter())
            {
                duringIsQueryFilterEnabled = _softDeletableConfiguration.IsQueryFilterEnabled;

                exampleEntitiesSearchResults = await _exampleEntityQueryableSearcher.ExecuteAsync(_exampleEntityRepository.GetReadOnlyQuery(), criteria);
                exampleSecondEntitiesSearchResults = await _exampleSecondEntityQueryableSearcher.ExecuteAsync(_exampleSecondEntityRepository.GetReadOnlyQuery(), secondCriteria);
            }
            bool afterIsQueryFilterEnabled = _softDeletableConfiguration.IsQueryFilterEnabled;

            return Json(new { exampleEntitiesSearchResults, exampleSecondEntitiesSearchResults,
                BeforeIsQueryFilterEnabled = beforeIsQueryFilterEnabled,
                DuringIsQueryFilterEnabled = duringIsQueryFilterEnabled,
                AfterIsQueryFilterEnabled = afterIsQueryFilterEnabled
            });

        }
    }
}
