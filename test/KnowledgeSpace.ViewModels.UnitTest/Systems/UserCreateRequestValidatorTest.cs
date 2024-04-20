using KnowledgeSpace.ViewModels.Systems;

namespace KnowledgeSpace.ViewModels.UnitTest.Systems
{
    public class UserCreateRequestValidatorTest
    {
        private UserCreateRequestValidator validator;
        private UserCreateRequest request;

        public UserCreateRequestValidatorTest()
        {
            request = new UserCreateRequest
            {
                UserName = "test",
                Email = "test@gmai.com",
                FirstName = "Test",
                LastName = "Test",
                Password = "Test@123",
                PhoneNumber = "0123456789",
                Dob = DateTime.Now.ToString(),
            };
            validator = new UserCreateRequestValidator();
        }

        [Fact]
        public void Should_Valid_Result_When_Valid_Request()
        {
            var result = validator.Validate(request);
            Assert.True(result.IsValid);
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public void Should_Error_Result_When_Request_Miss_UserName(string userName)
        {
            request.UserName = userName;
            var result = validator.Validate(request);
            Assert.False(result.IsValid);
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public void Should_Error_Result_When_Request_Miss_Email(string email)
        {
            request.Email = email;
            var result = validator.Validate(request);
            Assert.False(result.IsValid);
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public void Should_Error_Result_When_Request_Miss_FirstName(string firstName)
        {
            request.FirstName = firstName;
            var result = validator.Validate(request);
            Assert.False(result.IsValid);
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public void Should_Error_Result_When_Request_Miss_LastName(string lastName)
        {
            request.LastName = lastName;
            var result = validator.Validate(request);
            Assert.False(result.IsValid);
        }

        [Theory]
        [InlineData("abcxyzghef")]
        [InlineData("123456789")]
        [InlineData("Test1234")]
        [InlineData("")]
        [InlineData(null)]
        public void Should_Error_Result_When_Password_Not_Match(string password)
        {
            request.Password = password;
            var result = validator.Validate(request);
            Assert.False(result.IsValid);
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public void Should_Error_Result_When_Request_Miss_PhoneNumber(string phoneNumber)
        {
            request.PhoneNumber = phoneNumber;
            var result = validator.Validate(request);
            Assert.False(result.IsValid);
        }
    }
}
