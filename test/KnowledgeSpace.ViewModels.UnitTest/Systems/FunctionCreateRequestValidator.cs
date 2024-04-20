using KnowledgeSpace.ViewModels.Systems;

namespace KnowledgeSpace.ViewModels.UnitTest.Systems
{
    public class FunctionCreateRequestValidatorTest
    {
        private FunctionCreateRequestValidator validator;
        private FunctionCreateRequest request;

        public FunctionCreateRequestValidatorTest()
        {
            request = new FunctionCreateRequest
            {
                Id = "test1",
                ParentId = null,
                Name = "test1",
                SortOrder = 1,
                Url = "/test1",
            };
            validator = new FunctionCreateRequestValidator();
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
        public void Should_Error_Result_When_Request_Miss_Id(string id)
        {
            request.Id = id;
            var result = validator.Validate(request);
            Assert.False(result.IsValid);
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public void Should_Error_Result_When_Request_Miss_Name(string name)
        {
            request.Name = name;
            var result = validator.Validate(request);
            Assert.False(result.IsValid);
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public void Should_Error_Result_When_Request_Miss_Url(string url)
        {
            request.Url = url;
            var result = validator.Validate(request);
            Assert.False(result.IsValid);
        }
    }
}
