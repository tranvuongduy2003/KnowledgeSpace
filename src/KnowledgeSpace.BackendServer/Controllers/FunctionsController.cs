﻿using KnowledgeSpace.BackendServer.Data;
using KnowledgeSpace.BackendServer.Data.Entities;
using KnowledgeSpace.ViewModels;
using KnowledgeSpace.ViewModels.Systems;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeSpace.BackendServer.Controllers
{
    public class FunctionsController : BaseController
    {
        private readonly ApplicationDbContext _context;

        public FunctionsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> PostFunction([FromBody] FunctionCreateRequest request)
        {
            var dbFunction = await _context.Functions.FindAsync(request.Id);
            if (dbFunction != null)
                return BadRequest($"Function with id {request.Id} is existed.");

            var function = new Function()
            {
                Id = Guid.NewGuid().ToString(),
                Name = request.Name,
                Url = request.Url,
                SortOrder = request.SortOrder,
                ParentId = request.ParentId,
            };
            _context.Functions.Add(function);
            var result = await _context.SaveChangesAsync();

            if (result > 0)
            {
                return CreatedAtAction(nameof(GetById), new { id = function.Id }, request);
            }
            else
            {
                return BadRequest();
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetFunctions()
        {
            var functions = _context.Functions;

            var functionVms = await functions.Select(f => new FunctionVm()
            {
                Id = f.Id,
                Name = f.Name,
                Url = f.Url,
                SortOrder = f.SortOrder,
                ParentId = f.ParentId,
            }).ToListAsync();

            return Ok(functionVms);
        }

        [HttpGet("filter")]
        public async Task<IActionResult> GetFunctionsPaging([FromQuery] string filter, [FromQuery] int pageIndex, [FromQuery] int pageSize)
        {
            var query = _context.Functions.AsQueryable();
            if (!string.IsNullOrEmpty(filter))
            {
                query = query.Where(
                    x => x.Id.Contains(filter) || x.Name.Contains(filter) || x.Url.Contains(filter));
            }
            var totalRecords = await query.CountAsync();
            var items = await query.Skip((pageIndex - 1 * pageSize))
                .Take(pageSize)
                .Select(f => new FunctionVm()
                {
                    Id = f.Id,
                    Name = f.Name,
                    Url = f.Url,
                    SortOrder = f.SortOrder,
                    ParentId = f.ParentId,
                })
                .ToListAsync();

            var pagination = new Pagination<FunctionVm>
            {
                Items = items,
                TotalRecords = totalRecords,
            };
            return Ok(pagination);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var function = await _context.Functions.FindAsync(id);
            if (function == null)
                return NotFound();

            var functionVm = new FunctionVm()
            {
                Id = function.Id,
                Name = function.Name,
                Url = function.Url,
                SortOrder = function.SortOrder,
                ParentId = function.ParentId,
            };
            return Ok(functionVm);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutFunction(string id, [FromBody] FunctionCreateRequest request)
        {
            var function = await _context.Functions.FindAsync(id);
            if (function == null)
                return NotFound();

            function.Name = function.Name;
            function.Url = function.Url;
            function.SortOrder = function.SortOrder;
            function.ParentId = function.ParentId;

            _context.Functions.Update(function);
            var result = await _context.SaveChangesAsync();

            if (result > 0)
            {
                return NoContent();
            }
            return BadRequest();
        }

        //URL: DELETE: http://localhost:5001/api/functions/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteFunction(string id)
        {
            var function = await _context.Functions.FindAsync(id);
            if (function == null)
                return NotFound();

            _context.Functions.Remove(function);
            var result = await _context.SaveChangesAsync();

            if (result > 0)
            {
                var functionvm = new FunctionVm()
                {
                    Id = function.Id,
                    Name = function.Name,
                    Url = function.Url,
                    SortOrder = function.SortOrder,
                    ParentId = function.ParentId,
                };
                return Ok(functionvm);
            }
            return BadRequest();
        }
    }
}
