using KnowledgeSpace.BackendServer.Authorization;
using KnowledgeSpace.BackendServer.Constants;
using KnowledgeSpace.BackendServer.Data;
using KnowledgeSpace.BackendServer.Data.Entities;
using KnowledgeSpace.BackendServer.Helpers;
using KnowledgeSpace.ViewModels;
using KnowledgeSpace.ViewModels.Contents;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeSpace.BackendServer.Controllers
{
    public class CategoriesController : BaseController
    {
        private readonly ApplicationDbContext _context;

        public CategoriesController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        [ClaimRequirement(FunctionCode.CONTENT_CATEGORY, CommandCode.CREATE)]
        [ApiValidationFilter]
        public async Task<IActionResult> PostCategory([FromBody] CategoryCreateRequest request)
        {
            var category = new Category()
            {
                Name = request.Name,
                ParentId = request.ParentId,
                SeoAlias = request.SeoAlias,
                SeoDescription = request.SeoDescription,
                SortOrder = request.SortOrder
            };
            _context.Categories.Add(category);
            var result = await _context.SaveChangesAsync();

            if (result > 0)
            {
                return CreatedAtAction(nameof(GetById), new { id = category.Id }, request);
            }
            else
            {
                return BadRequest(new ApiBadRequestResponse(""));
            }
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetCategories()
        {
            var categoríe = _context.Categories;

            var categoryVms = await categoríe.Select(c => new CategoryVm()
            {
                Id = c.Id,
                Name = c.Name,
                ParentId = c.ParentId,
                SeoAlias = c.SeoAlias,
                SeoDescription = c.SeoDescription,
                SortOrder = c.SortOrder,
                NumberOfTickets = c.NumberOfTickets
            }).ToListAsync();

            return Ok(categoryVms);
        }

        [HttpGet("filter")]
        [ClaimRequirement(FunctionCode.CONTENT_CATEGORY, CommandCode.VIEW)]
        public async Task<IActionResult> GetCategoriesPaging([FromQuery] string filter, [FromQuery] int pageIndex, [FromQuery] int pageSize)
        {
            var query = _context.Categories.AsQueryable();
            if (!string.IsNullOrEmpty(filter))
            {
                query = query.Where(x => x.Name.Contains(filter));
            }
            var totalRecords = await query.CountAsync();
            var items = await query.Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .Select(c => new CategoryVm()
                {
                    Id = c.Id,
                    Name = c.Name,
                    ParentId = c.ParentId,
                    SeoAlias = c.SeoAlias,
                    SeoDescription = c.SeoDescription,
                    SortOrder = c.SortOrder,
                    NumberOfTickets = c.NumberOfTickets
                })
                .ToListAsync();

            var pagination = new Pagination<CategoryVm>
            {
                Items = items,
                TotalRecords = totalRecords,
            };
            return Ok(pagination);
        }

        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetById(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null)
                return NotFound(new ApiNotFoundResponse(""));

            var categoryVm = new CategoryVm()
            {
                Id = category.Id,
                Name = category.Name,
                ParentId = category.ParentId,
                SeoAlias = category.SeoAlias,
                SeoDescription = category.SeoDescription,
                SortOrder = category.SortOrder,
                NumberOfTickets = category.NumberOfTickets,
            };
            return Ok(categoryVm);
        }

        [HttpPut("{id}")]
        [ClaimRequirement(FunctionCode.CONTENT_CATEGORY, CommandCode.UPDATE)]
        [ApiValidationFilter]
        public async Task<IActionResult> PutCategory(int id, [FromBody] CategoryCreateRequest request)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null)
                return NotFound(new ApiNotFoundResponse(""));

            if (id == request.ParentId)
            {
                return BadRequest(new ApiBadRequestResponse("Category cannot be a child itself."));
            }

            category.Name = category.Name;
            category.ParentId = category.ParentId;
            category.SeoAlias = category.SeoAlias;
            category.SeoDescription = category.SeoDescription;
            category.SortOrder = category.SortOrder;

            _context.Categories.Update(category);
            var result = await _context.SaveChangesAsync();

            if (result > 0)
            {
                return NoContent();
            }
            return BadRequest(new ApiBadRequestResponse(""));
        }

        [HttpDelete("{id}")]
        [ClaimRequirement(FunctionCode.CONTENT_CATEGORY, CommandCode.DELETE)]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null)
                return NotFound(new ApiNotFoundResponse(""));

            _context.Categories.Remove(category);
            var result = await _context.SaveChangesAsync();

            if (result > 0)
            {
                var categoryvm = new CategoryVm()
                {
                    Id = category.Id,
                    Name = category.Name,
                    ParentId = category.ParentId,
                    SeoAlias = category.SeoAlias,
                    SeoDescription = category.SeoDescription,
                    SortOrder = category.SortOrder,
                    NumberOfTickets = category.NumberOfTickets,
                };
                return Ok(categoryvm);
            }
            return BadRequest(new ApiBadRequestResponse(""));
        }
    }
}
