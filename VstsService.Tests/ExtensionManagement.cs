using System.Net;
using AutoFixture;
using ExpectedObjects;
using Flurl.Http;
using SecurePipelineScan.VstsService.Response;
using Shouldly;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace SecurePipelineScan.VstsService.Tests
{
    [Trait("category", "integration")]
    public class ExtensionManagement : IClassFixture<TestConfig>
    {
        private readonly IVstsRestClient _client;
        private readonly Fixture _fixture = new Fixture();

        public ExtensionManagement(TestConfig config)
        {
            _client = new VstsRestClient(config.Organization, config.Token);
        }

        [Fact]
        public async Task PutExtensionDataReturnsId()
        {
            // Arrange
            var data = _fixture.Create<TestObject>();

            // Act
            var result = await _client.PutAsync(Requests.ExtensionManagement.ExtensionData<TestObject>(
                "ms",
                "azure-artifacts",
                "DevOps Demo"), data);

            // Assert
            result
                .Id
                .ShouldBe(data.Id);
        }

        [Fact]
        public async Task GetReturnsDataFromPut()
        {
            // Arrange
            var data = _fixture.Create<TestObject>();
            await _client.PutAsync(Requests.ExtensionManagement.ExtensionData<TestObject>(
                "ms",
                "azure-artifacts",
                "DevOps Demo"), data);

            var expected = new
            {
                data.Id,
                data.Name,
                data.Value,
                Etag = Expect.NotDefault<int>()
            }
                .ToExpectedObject();

            // Act
            var result = await _client.GetAsync(Requests.ExtensionManagement.ExtensionData<TestObject>(
                "ms",
                "azure-artifacts",
                "DevOps Demo",
                data.Id));

            // Assert
            expected.ShouldMatch(result);
        }

        [Fact]
        public async Task InvalidVersionThrowsException()
        {
            // Arrange
            var data = _fixture.Create<TestObject>();
            var result = await _client.PutAsync(Requests.ExtensionManagement.ExtensionData<TestObject>(
                "ms",
                "azure-artifacts",
                "DevOps Demo"), data);
            result.Etag += 10; // Intentionally change the etag to something invalid

            var ex = await Assert.ThrowsAsync<FlurlHttpException>(() => _client.PutAsync(
                Requests.ExtensionManagement.ExtensionData<TestObject>(
                    "ms",
                    "azure-artifacts",
                    "DevOps Demo"), result));
           
            ex.Call.Response.StatusCode.ShouldBe(400);
        }

        private class TestObject : ExtensionData
        {
            // ReSharper disable twice UnusedAutoPropertyAccessor.Local
            public string Name { get; set; }
            public string Value { get; set; }
        }
    }
}