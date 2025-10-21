using System;
using System.Linq;
using IntelligenceHub.DAL;
using IntelligenceHub.DAL.Implementations;
using IntelligenceHub.DAL.Models;
using IntelligenceHub.DAL.Tenant;
using Microsoft.EntityFrameworkCore;

namespace IntelligenceHub.Tests.Unit.DAL
{
    public class GenericRepositoryTests
    {
        private TenantProvider _tenantProvider;

        public GenericRepositoryTests()
        {
            _tenantProvider = new TenantProvider();
        }

        private IntelligenceHubDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<IntelligenceHubDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new IntelligenceHubDbContext(options);
            
        }

        [Fact]
        public async Task AddUpdateDelete_GetAll_Works()
        {
            await using var context = CreateContext();
            var repo = new GenericRepository<DbMessage>(context, _tenantProvider);

            var message = new DbMessage { Content = "Hello", Role = "User", TimeStamp = DateTime.UtcNow };
            var added = await repo.AddAsync(message);
            Assert.NotEqual(0, added.Id);

            added.Content = "Updated";
            var updated = await repo.UpdateAsync(added);
            Assert.Equal("Updated", updated.Content);

            var all = await repo.GetAllAsync();
            Assert.Single(all);

            var deleted = await repo.DeleteAsync(updated);
            Assert.True(deleted);
            Assert.Empty(await repo.GetAllAsync());
        }

        [Fact]
        public async Task GetAll_WithPaging_ReturnsCorrectSubset()
        {
            await using var context = CreateContext();
            var repo = new GenericRepository<DbMessage>(context, _tenantProvider);
            for (int i = 0; i < 5; i++)
            {
                await repo.AddAsync(new DbMessage { Content = i.ToString(), Role = "User", TimeStamp = DateTime.UtcNow });
            }

            var page2 = await repo.GetAllAsync(2, 2);
            Assert.Equal(new[] { "2", "3" }, page2.Select(m => m.Content));
        }

        [Fact]
        public async Task AddAsync_AppendsTenantToName()
        {
            await using var context = CreateContext();
            _tenantProvider.TenantId = Guid.NewGuid();
            var repo = new GenericRepository<DbProfile>(context, _tenantProvider);

            var profile = new DbProfile { Name = "MyProfile" };
            var added = await repo.AddAsync(profile);

            Assert.StartsWith(_tenantProvider.TenantId.ToString(), added.Name);
        }
    }
}