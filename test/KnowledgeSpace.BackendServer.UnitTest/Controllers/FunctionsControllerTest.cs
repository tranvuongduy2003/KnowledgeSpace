using KnowledgeSpace.BackendServer.Controllers;
using KnowledgeSpace.BackendServer.Data;
using KnowledgeSpace.BackendServer.Data.Entities;
using KnowledgeSpace.ViewModels;
using KnowledgeSpace.ViewModels.Systems;
using Microsoft.AspNetCore.Mvc;

namespace KnowledgeSpace.BackendServer.UnitTest.Controllers
{
    public class FunctionsControllerTest
    {
        private ApplicationDbContext _context;

        public FunctionsControllerTest()
        {
            _context = new InMemoryDbContextFactory().GetApplicationDbContext();
        }

        [Fact]
        public void Should_Create_Instance_Not_Null_Success()
        {
            var functionsController = new FunctionsController(_context);
            Assert.NotNull(functionsController);
        }

        [Fact]
        public async void PostFunction_ValidInput_Success()
        {
            var functionsController = new FunctionsController(_context);
            var result = await functionsController.PostFunction(new FunctionCreateRequest()
            {
                Id = "PostFunction_ValidInput_Success",
                ParentId = null,
                Name = "PostFunction_ValidInput_Success",
                SortOrder = 5,
                Url = "/PostFunction_ValidInput_Success",
            });
            Assert.IsType<CreatedAtActionResult>(result);
        }

        [Fact]
        public async void PostFunction_ValidInput_Failed()
        {
            _context.Functions.AddRange(new List<Function>()
            {
                new Function()
                {
                    Id = "PostFunction_ValidInput_Failed",
                    ParentId = null,
                    Name = "PostFunction_ValidInput_Failed",
                    SortOrder = 1,
                    Url = "/PostFunction_ValidInput_Failed",
                },
            });
            await _context.SaveChangesAsync();

            var functionsController = new FunctionsController(_context);
            var result = await functionsController.PostFunction(new FunctionCreateRequest()
            {
                Id = "PostFunction_ValidInput_Failed",
                ParentId = null,
                Name = "PostFunction_ValidInput_Failed",
                SortOrder = 5,
                Url = "/PostFunction_ValidInput_Failed",
            });
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async void GetFunctions_HasData_ReturnSuccess()
        {
            _context.Functions.AddRange(new List<Function>()
            {
                new Function()
                {
                    Id = "GetFunctions_HasData_ReturnSuccess",
                    ParentId = null,
                    Name = "GetFunctions_HasData_ReturnSuccess",
                    SortOrder = 1,
                    Url = "/GetFunctions_HasData_ReturnSuccess",
                },
            });
            await _context.SaveChangesAsync();

            var functionsController = new FunctionsController(_context);
            var result = await functionsController.GetFunctions();
            var okResult = result as OkObjectResult;
            var functionVms = okResult.Value as IEnumerable<FunctionVm>;
            Assert.True(functionVms.Count() > 0);
        }

        [Fact]
        public async void GetFunctionsPaging_NoFilter_ReturnSuccess()
        {
            _context.Functions.AddRange(new List<Function>()
            {
                new Function()
                {
                    Id = "GetFunctionsPaging_NoFilter_ReturnSuccess1",
                    ParentId = null,
                    Name = "GetFunctionsPaging_NoFilter_ReturnSuccess1",
                    SortOrder = 1,
                    Url = "/GetFunctionsPaging_NoFilter_ReturnSuccess1",
                },
                new Function()
                {
                    Id = "GetFunctionsPaging_NoFilter_ReturnSuccess2",
                    ParentId = null,
                    Name = "GetFunctionsPaging_NoFilter_ReturnSuccess2",
                    SortOrder = 2,
                    Url = "/GetFunctionsPaging_NoFilter_ReturnSuccess2",
                },
                new Function()
                {
                    Id = "GetFunctionsPaging_NoFilter_ReturnSuccess3",
                    ParentId = null,
                    Name = "GetFunctionsPaging_NoFilter_ReturnSuccess3",
                    SortOrder = 3,
                    Url = "/GetFunctionsPaging_NoFilter_ReturnSuccess3",
                },
                new Function()
                {
                    Id = "GetFunctionsPaging_NoFilter_ReturnSuccess4",
                    ParentId = null,
                    Name = "GetFunctionsPaging_NoFilter_ReturnSuccess4",
                    SortOrder = 4,
                    Url = "/GetFunctionsPaging_NoFilter_ReturnSuccess4",
                }
            });
            await _context.SaveChangesAsync();

            var functionsController = new FunctionsController(_context);
            var result = await functionsController.GetFunctionsPaging(null, 1, 2);
            var okResult = result as OkObjectResult;
            var functionVms = okResult.Value as Pagination<FunctionVm>;
            Assert.Equal(4, functionVms.TotalRecords);
            Assert.Equal(2, functionVms.Items.Count);
        }

        [Fact]
        public async void GetFunctionsPaging_HasFilter_ReturnSuccess()
        {
            _context.Functions.AddRange(new List<Function>()
            {
                new Function()
                {
                    Id = "GetFunctionsPaging_HasFilter_ReturnSuccess3",
                    ParentId = null,
                    Name = "GetFunctionsPaging_HasFilter_ReturnSuccess3",
                    SortOrder = 3,
                    Url = "/GetFunctionsPaging_HasFilter_ReturnSuccess3",
                }
            });
            await _context.SaveChangesAsync();

            var functionsController = new FunctionsController(_context);
            var result = await functionsController.GetFunctionsPaging("GetFunctionsPaging_HasFilter_ReturnSuccess3", 1, 2);
            var okResult = result as OkObjectResult;
            var functionVms = okResult.Value as Pagination<FunctionVm>;
            Assert.Equal(1, functionVms.TotalRecords);
            Assert.Single(functionVms.Items);
        }

        [Fact]
        public async void GetById_HasData_ReturnSuccess()
        {
            _context.Functions.AddRange(new List<Function>()
            {
                new Function()
                {
                    Id = "GetById_HasData_ReturnSuccess",
                    ParentId = null,
                    Name = "GetById_HasData_ReturnSuccess",
                    SortOrder = 1,
                    Url = "/GetById_HasData_ReturnSuccess",
                }
            });
            await _context.SaveChangesAsync();

            var functionsController = new FunctionsController(_context);
            var result = await functionsController.GetById("GetById_HasData_ReturnSuccess");
            var okResult = result as OkObjectResult;
            Assert.NotNull(okResult);

            var functionVm = okResult.Value as FunctionVm;

            Assert.Equal("GetById_HasData_ReturnSuccess", functionVm.Id);
        }

        [Fact]
        public async void PutFunction_ValidInput_Success()
        {
            _context.Functions.AddRange(new List<Function>()
            {
                new Function()
                {
                    Id = "PutFunction_ValidInput_Success",
                    ParentId = null,
                    Name = "PutFunction_ValidInput_Success",
                    SortOrder = 1,
                    Url = "/PutFunction_ValidInput_Success",
                }
            });
            await _context.SaveChangesAsync();

            var functionsController = new FunctionsController(_context);
            var result = await functionsController.PutFunction("PutFunction_ValidInput_Success", new FunctionCreateRequest()
            {
                Id = "PutFunction_ValidInput_Success6",
                ParentId = null,
                Name = "PutFunction_ValidInput_Success6",
                SortOrder = 6,
                Url = "/PutFunction_ValidInput_Success6",
            });
            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async void PutFunction_ValidInput_Failed()
        {
            var functionsController = new FunctionsController(_context);
            var result = await functionsController.PutFunction("PutFunction_ValidInput_Failed", new FunctionCreateRequest()
            {
                Id = "PutFunction_ValidInput_Failed6",
                ParentId = null,
                Name = "PutFunction_ValidInput_Failed6",
                SortOrder = 6,
                Url = "/PutFunction_ValidInput_Failed6",
            });
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async void DeleteFunction_ValidInput_Success()
        {
            _context.Functions.AddRange(new List<Function>()
            {
                new Function()
                {
                    Id = "DeleteFunction_ValidInput_Success",
                    ParentId = null,
                    Name = "DeleteFunction_ValidInput_Success",
                    SortOrder = 1,
                    Url = "/DeleteFunction_ValidInput_Success",
                }
            });
            await _context.SaveChangesAsync();

            var functionsController = new FunctionsController(_context);
            var result = await functionsController.DeleteFunction("DeleteFunction_ValidInput_Success");
            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async void DeleteFunction_ValidInput_Failed()
        {
            var functionsController = new FunctionsController(_context);
            var result = await functionsController.DeleteFunction("DeleteFunction_ValidInput_Failed");
            Assert.IsType<NotFoundResult>(result);
        }
    }
}
