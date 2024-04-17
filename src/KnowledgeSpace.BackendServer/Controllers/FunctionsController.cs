using KnowledgeSpace.BackendServer.Authorization;
using KnowledgeSpace.BackendServer.Constants;
using KnowledgeSpace.BackendServer.Data;
using KnowledgeSpace.BackendServer.Data.Entities;
using KnowledgeSpace.BackendServer.Helpers;
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
        [ClaimRequirement(FunctionCode.SYSTEM_FUNCTION, CommandCode.CREATE)]
        [ApiValidationFilter]
        public async Task<IActionResult> PostFunction([FromBody] FunctionCreateRequest request)
        {
            var dbFunction = await _context.Functions.FindAsync(request.Id);
            if (dbFunction != null)
                return BadRequest(new ApiBadRequestResponse($"Function with id {request.Id} is existed."));

            var function = new Function()
            {
                Id = Guid.NewGuid().ToString(),
                Name = request.Name,
                Url = request.Url,
                SortOrder = request.SortOrder,
                ParentId = request.ParentId,
                Icon = request.Icon
            };
            _context.Functions.Add(function);
            var result = await _context.SaveChangesAsync();

            if (result > 0)
            {
                return CreatedAtAction(nameof(GetById), new { id = function.Id }, request);
            }
            else
            {
                return BadRequest(new ApiBadRequestResponse(""));
            }
        }

        [HttpGet]
        [ClaimRequirement(FunctionCode.SYSTEM_FUNCTION, CommandCode.VIEW)]
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
                Icon = f.Icon
            }).ToListAsync();

            return Ok(functionVms);
        }

        [HttpGet("{functionId}/parents")]
        [ClaimRequirement(FunctionCode.SYSTEM_FUNCTION, CommandCode.VIEW)]
        public async Task<IActionResult> GetFunctionsByParentId(string functionId)
        {
            var functions = _context.Functions.Where(x => x.ParentId == functionId);

            var functionvms = await functions.Select(u => new FunctionVm()
            {
                Id = u.Id,
                Name = u.Name,
                Url = u.Url,
                SortOrder = u.SortOrder,
                ParentId = u.ParentId,
                Icon = u.Icon
            }).ToListAsync();

            return Ok(functionvms);
        }

        [HttpGet("filter")]
        [ClaimRequirement(FunctionCode.SYSTEM_FUNCTION, CommandCode.VIEW)]
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
                    Icon = f.Icon
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
        [ClaimRequirement(FunctionCode.SYSTEM_FUNCTION, CommandCode.VIEW)]
        public async Task<IActionResult> GetById(string id)
        {
            var function = await _context.Functions.FindAsync(id);
            if (function == null)
                return NotFound(new ApiNotFoundResponse(""));

            var functionVm = new FunctionVm()
            {
                Id = function.Id,
                Name = function.Name,
                Url = function.Url,
                SortOrder = function.SortOrder,
                ParentId = function.ParentId,
                Icon = function.Icon
            };
            return Ok(functionVm);
        }

        [HttpPut("{id}")]
        [ClaimRequirement(FunctionCode.SYSTEM_FUNCTION, CommandCode.UPDATE)]
        [ApiValidationFilter]
        public async Task<IActionResult> PutFunction(string id, [FromBody] FunctionCreateRequest request)
        {
            var function = await _context.Functions.FindAsync(id);
            if (function == null)
                return NotFound(new ApiNotFoundResponse(""));

            function.Name = request.Name;
            function.Url = request.Url;
            function.SortOrder = request.SortOrder;
            function.ParentId = request.ParentId;
            function.Icon = request.Icon;

            _context.Functions.Update(function);
            var result = await _context.SaveChangesAsync();

            if (result > 0)
            {
                return NoContent();
            }
            return BadRequest(new ApiBadRequestResponse(""));
        }

        [HttpDelete("{id}")]
        [ClaimRequirement(FunctionCode.SYSTEM_FUNCTION, CommandCode.DELETE)]
        public async Task<IActionResult> DeleteFunction(string id)
        {
            var function = await _context.Functions.FindAsync(id);
            if (function == null)
                return NotFound(new ApiNotFoundResponse(""));

            _context.Functions.Remove(function);

            var commands = _context.CommandInFunctions.Where(x => x.FunctionId == id);
            _context.CommandInFunctions.RemoveRange(commands);

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
                    Icon = function.Icon,
                };
                return Ok(functionvm);
            }
            return BadRequest(new ApiBadRequestResponse(""));
        }

        [HttpGet("{functionId}/commands")]
        [ClaimRequirement(FunctionCode.SYSTEM_FUNCTION, CommandCode.VIEW)]
        public async Task<IActionResult> GetCommandsInFunction(string functionId)
        {
            var query = from c in _context.Commands
                        join fc in _context.CommandInFunctions
                        on c.Id equals fc.FunctionId into result1
                        from commandInFunction in result1.DefaultIfEmpty()
                        join f in _context.Functions
                        on commandInFunction.FunctionId equals f.Id into result2
                        from function in result2.DefaultIfEmpty()
                        select new
                        {
                            Id = c.Id,
                            Name = c.Name,
                            commandInFunction.FunctionId,
                        };
            query = query.Where(x => x.FunctionId == functionId);

            var commandVms = await query.Select(x => new CommandVm
            {
                Id = x.Id,
                Name = x.Name,
            }).ToListAsync();

            return Ok(commandVms);
        }

        [HttpPost("{functionId}/commands")]
        [ClaimRequirement(FunctionCode.SYSTEM_FUNCTION, CommandCode.CREATE)]
        [ApiValidationFilter]
        public async Task<IActionResult> PostCommandToFunction(string functionId, [FromBody] CommandAssignRequest request)
        {
            foreach (var commandId in request.CommandIds)
            {
                if (await _context.CommandInFunctions.FindAsync(commandId, functionId) != null)
                    return BadRequest(new ApiBadRequestResponse("This command has been existed in function"));

                var entity = new CommandInFunction()
                {
                    CommandId = commandId,
                    FunctionId = functionId
                };

                _context.CommandInFunctions.Add(entity);
            }

            if (request.AddToAllFunctions)
            {
                var otherFunctions = _context.Functions.Where(x => x.Id != functionId);
                foreach (var function in otherFunctions)
                {
                    foreach (var commandId in request.CommandIds)
                    {
                        if (await _context.CommandInFunctions.FindAsync(request.CommandIds, function.Id) == null)
                        {
                            _context.CommandInFunctions.Add(new CommandInFunction()
                            {
                                CommandId = commandId,
                                FunctionId = function.Id
                            });
                        }
                    }
                }
            }
            var result = await _context.SaveChangesAsync();

            if (result > 0)
            {
                return CreatedAtAction(nameof(GetById), new { request.CommandIds, functionId });
            }
            else
            {
                return BadRequest(new ApiBadRequestResponse("Add command to function failed"));
            }
        }

        [HttpDelete("{functionId}/commands")]
        [ClaimRequirement(FunctionCode.SYSTEM_FUNCTION, CommandCode.DELETE)]
        public async Task<IActionResult> DeleteCommandToFunction(string functionId, [FromQuery] CommandAssignRequest request)
        {
            foreach (var commandId in request.CommandIds)
            {
                var entity = await _context.CommandInFunctions.FindAsync(commandId, functionId);
                if (entity == null)
                    return BadRequest(new ApiBadRequestResponse("This command is not existed in function"));

                _context.CommandInFunctions.Remove(entity);
            }
            var result = await _context.SaveChangesAsync();

            if (result > 0)
            {
                return Ok();
            }
            else
            {
                return BadRequest(new ApiBadRequestResponse(""));
            }
        }
    }
}
