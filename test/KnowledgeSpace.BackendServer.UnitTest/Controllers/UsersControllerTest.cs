using KnowledgeSpace.BackendServer.Controllers;
using KnowledgeSpace.BackendServer.Data;
using KnowledgeSpace.BackendServer.Data.Entities;
using KnowledgeSpace.ViewModels;
using KnowledgeSpace.ViewModels.Systems;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MockQueryable.Moq;
using Moq;

namespace KnowledgeSpace.BackendServer.UnitTest.Controllers
{
    public class UsersControllerTest

    {
        private readonly Mock<UserManager<User>> _mockUserManager;
        private readonly Mock<RoleManager<IdentityRole>> _mockRoleManager;
        private ApplicationDbContext _context;

        private List<User> _userSources = new List<User>()
                {
                    new User("1", "test1", "Test 1", "LastTest 1", "test1@gmail.com", "00111", DateTime.Now),
                    new User("2", "test2", "Test 2", "LastTest 2", "test2@gmail.com", "00111", DateTime.Now),
                    new User("3", "test3", "Test 3", "LastTest 3", "test3@gmail.com", "00111", DateTime.Now),
                    new User("4", "test4", "Test 4", "LastTest 4", "test4@gmail.com", "00111", DateTime.Now)
                };

        public UsersControllerTest()
        {
            var userStore = new Mock<IUserStore<User>>();
            _mockUserManager = new Mock<UserManager<User>>(userStore.Object, null, null, null, null, null, null, null, null);
        }

        [Fact]
        public void ShouldCreateInstance_NotNull_Success()
        {
            var usersController = new UsersController(_mockUserManager.Object, _mockRoleManager.Object, _context);
            Assert.NotNull(usersController);
        }

        [Fact]
        public async void PostUser_ValidInput_Success()
        {
            _mockUserManager.Setup(x => x.CreateAsync(It.IsAny<User>(), It.IsAny<string>())).ReturnsAsync(IdentityResult.Success);

            var usersController = new UsersController(_mockUserManager.Object, _mockRoleManager.Object, _context);
            var result = await usersController.PostUser(new UserCreateRequest()
            {
                UserName = "test"
            });

            Assert.NotNull(result);
            Assert.IsType<CreatedAtActionResult>(result);
        }

        [Fact]
        public async void PostUser_ValidInput_Failed()
        {
            _mockUserManager.Setup(x => x.CreateAsync(It.IsAny<User>(), It.IsAny<string>())).ReturnsAsync(IdentityResult.Failed(new IdentityError[] { }));

            var usersController = new UsersController(_mockUserManager.Object, _mockRoleManager.Object, _context);
            var result = await usersController.PostUser(new UserCreateRequest()
            {
                UserName = "test"
            });

            Assert.NotNull(result);
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async void GetUsers_HasData_ReturnSuccess()
        {
            _mockUserManager.Setup(x => x.Users)
                .Returns(_userSources.AsQueryable().BuildMock());
            var usersController = new UsersController(_mockUserManager.Object, _mockRoleManager.Object, _context);
            var result = await usersController.GetUsers();
            var okResult = result as OkObjectResult;
            var userVms = okResult.Value as IEnumerable<UserVm>;
            Assert.True(userVms.Count() > 0);
        }

        [Fact]
        public async void GetUsers_ThrowException_Failed()
        {
            _mockUserManager.Setup(x => x.Users).Throws<Exception>();

            var usersController = new UsersController(_mockUserManager.Object, _mockRoleManager.Object, _context);

            await Assert.ThrowsAnyAsync<Exception>(async () => await usersController.GetUsers());
        }

        [Fact]
        public async void GetUsersPaging_NoFilter_ReturnSuccess()
        {
            _mockUserManager.Setup(x => x.Users)
                .Returns(_userSources.AsQueryable().BuildMock());
            var usersController = new UsersController(_mockUserManager.Object, _mockRoleManager.Object, _context);
            var result = await usersController.GetUsersPaging(null, 1, 2);
            var okResult = result as OkObjectResult;
            var userVms = okResult.Value as Pagination<UserVm>;
            Assert.Equal(4, userVms.TotalRecords);
            Assert.Equal(2, userVms.Items.Count);
        }

        [Fact]
        public async void GetUsersPaging_HasFilter_ReturnSuccess()
        {
            _mockUserManager.Setup(x => x.Users)
                .Returns(_userSources.AsQueryable().BuildMock());
            var usersController = new UsersController(_mockUserManager.Object, _mockRoleManager.Object, _context);
            var result = await usersController.GetUsersPaging("test3", 1, 2);
            var okResult = result as OkObjectResult;
            var userVms = okResult.Value as Pagination<UserVm>;
            Assert.Equal(1, userVms.TotalRecords);
            Assert.Single(userVms.Items);
        }

        [Fact]
        public async void GetUsersPaging_ThrowException_Failed()
        {
            _mockUserManager.Setup(x => x.Users).Throws<Exception>();

            var usersController = new UsersController(_mockUserManager.Object, _mockRoleManager.Object, _context);

            await Assert.ThrowsAnyAsync<Exception>(async () => await usersController.GetUsersPaging(null, 1, 1));
        }

        [Fact]
        public async void GetById_HasData_ReturnSuccess()
        {
            _mockUserManager.Setup(x => x.FindByIdAsync(It.IsAny<string>()))
                .ReturnsAsync(new User()
                {
                    UserName = "test1",
                });
            var usersController = new UsersController(_mockUserManager.Object, _mockRoleManager.Object, _context);
            var result = await usersController.GetById("test1");
            var okResult = result as OkObjectResult;
            Assert.NotNull(okResult);

            var userVm = okResult.Value as UserVm;

            Assert.Equal("test1", userVm.UserName);
        }

        [Fact]
        public async void GetById_ThrowException_Failed()
        {
            _mockUserManager.Setup(x => x.FindByIdAsync(It.IsAny<string>())).Throws<Exception>();

            var usersController = new UsersController(_mockUserManager.Object, _mockRoleManager.Object, _context);

            await Assert.ThrowsAnyAsync<Exception>(async () => await usersController.GetById("test1"));
        }

        [Fact]
        public async void PutUser_ValidInput_Success()
        {
            _mockUserManager.Setup(x => x.FindByIdAsync(It.IsAny<string>())).ReturnsAsync(new User()
            {
                UserName = "test"
            });

            _mockUserManager.Setup(x => x.UpdateAsync(It.IsAny<User>())).ReturnsAsync(IdentityResult.Success);
            var usersController = new UsersController(_mockUserManager.Object, _mockRoleManager.Object, _context);
            var result = await usersController.PutUser("test", new UserCreateRequest()
            {
                UserName = "test2"
            });

            Assert.NotNull(result);
            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async void PutUser_ValidInput_Failed()
        {
            _mockUserManager.Setup(x => x.FindByIdAsync(It.IsAny<string>())).ReturnsAsync(new User()
            {
                UserName = "test2"
            });

            _mockUserManager.Setup(x => x.UpdateAsync(It.IsAny<User>())).ReturnsAsync(IdentityResult.Failed(new IdentityError[] { }));
            var usersController = new UsersController(_mockUserManager.Object, _mockRoleManager.Object, _context);
            var result = await usersController.PutUser("test", new UserCreateRequest()
            {
                UserName = "test2"
            });

            Assert.NotNull(result);
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async void DeleteUser_ValidInput_Success()
        {
            _mockUserManager.Setup(x => x.FindByIdAsync(It.IsAny<string>())).ReturnsAsync(new User()
            {
                UserName = "test1"
            });

            _mockUserManager.Setup(x => x.GetUsersInRoleAsync(It.IsAny<string>()))
             .ReturnsAsync(new List<User>(){
                new User()
                {
                    UserName = "test1"
                }});

            _mockUserManager.Setup(x => x.DeleteAsync(It.IsAny<User>())).ReturnsAsync(IdentityResult.Success);
            var usersController = new UsersController(_mockUserManager.Object, _mockRoleManager.Object, _context);
            var result = await usersController.DeleteUser("test");
            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async void DeleteUser_ValidInput_Failed()
        {
            _mockUserManager.Setup(x => x.FindByIdAsync(It.IsAny<string>())).ReturnsAsync(new User()
            {
                UserName = "test1"
            });

            _mockUserManager.Setup(x => x.GetUsersInRoleAsync(It.IsAny<string>()))
             .ReturnsAsync(new List<User>(){
                new User()
                {
                    UserName = "test1"
                }});

            _mockUserManager.Setup(x => x.DeleteAsync(It.IsAny<User>())).ReturnsAsync(IdentityResult.Failed(new IdentityError[] { }));
            var usersController = new UsersController(_mockUserManager.Object, _mockRoleManager.Object, _context);
            var result = await usersController.DeleteUser("test");
            Assert.IsType<BadRequestObjectResult>(result);
        }
    }
}
