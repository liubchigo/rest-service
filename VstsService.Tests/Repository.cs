using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Shouldly;
using Xunit;
using YamlDotNet.Serialization;

namespace SecurePipelineScan.VstsService.Tests
{
    [Trait("category", "integration")]
    public class Repository: IClassFixture<TestConfig>
    {
        private readonly TestConfig _config;
        private readonly IVstsRestClient _client;

        public Repository(TestConfig config)
        {
            _config = config;
            _client = new VstsRestClient(config.Organization, config.Token);
        }

        [Fact]
        public void QueryRepository()
        {
            var repository = _client.Get(Requests.Repository.Repositories(_config.Project)).First(r => r.Id == _config.RepositoryId);

            repository.Name.ShouldNotBeNullOrEmpty();
            repository.Id.ShouldNotBeNullOrEmpty();
            repository.Project.ShouldNotBeNull();
            repository.Project.Name.ShouldNotBeNullOrEmpty();
            repository.DefaultBranch.ShouldNotBeNullOrEmpty();
        }

        [Fact]
        public void QueryPushes()
        {
            // Arrange
            // Act
            var pushes = _client.Get(Requests.Repository.Pushes(_config.Project, _config.RepositoryId)).ToList();  

            // Assert
            pushes.ShouldNotBeEmpty();
            var push = pushes.First();
            push.PushId.ShouldNotBe(0);
            push.Date.ShouldNotBe(default);
        }

        [Fact]
        public void GetGitRefs()
        {
            //Arrange
            //Act
            var gitRefs = _client.Get(Requests.Repository.Refs(_config.Project, _config.RepositoryId)).ToList();

            //Assert
            gitRefs.ShouldNotBeEmpty();
            var gitref = gitRefs.First();
            gitref.Name.ShouldNotBeNull();
        }

        [Fact]
        public async Task GetGitItem()
        {
            var gitItem = await _client.GetAsync(Requests.Repository.GitItem(_config.Project,
                _config.RepositoryId, _config.GitItemFilePath)
                .AsJson()).ConfigureAwait(false);

            gitItem.ShouldNotBeNull();
        }

        [Fact]
        public void PushThrowsFor404()
        {
            var repositoryId = Guid.NewGuid().ToString();
            var ex = Should.Throw<NotFoundException>(() => _client.Get(Requests.Repository.Pushes(_config.Project, repositoryId)).ToList());
            ex.Message.ShouldContain(repositoryId);
        }
    }
}
