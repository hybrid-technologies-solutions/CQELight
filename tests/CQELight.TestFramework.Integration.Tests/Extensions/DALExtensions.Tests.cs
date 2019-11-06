using CQELight.DAL.Common;
using CQELight.DAL.Interfaces;
using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace CQELight.TestFramework.Integration.Tests.Extensions
{
    public class DALExtensionsTests : BaseUnitTestClass
    {
        #region Ctor & members

        public class TestEntity : PersistableEntity
        {
            public TestEntity()
            {
                Id = Guid.NewGuid();
            }
        }

        #endregion

        #region FakePersistenceId

        [Fact]
        public void FakePersistenceId_Should_Apply_DesiredId()
        {
            var a = new TestEntity();

            var fakeId = Guid.NewGuid();
            a.Id.Should().NotBeEmpty();
            a.Id.Should().NotBe(fakeId);

            a.FakePersistenceId(fakeId);
            a.Id.Should().Be(fakeId);
        }

        #endregion

        #region SetupSimpleGetReturns

        public interface IRepo : IDataReaderRepository<TestEntity> { }

        class Getter
        {
            public async Task<IEnumerable<object>> GetAsync(IRepo repo)
            {
                return await repo.GetAsync().ToListAsync();
            }
        }

        [Fact]
        public async Task SetupSimpleGetReturns_Should_Returns_RequiredData_And_VerifyThatGetHasBeenCalled()
        {
            var a = new TestEntity();
            var a2 = new TestEntity();
            var a3 = new TestEntity();

            var id = Guid.NewGuid();
            var id2 = Guid.NewGuid();
            var id3 = Guid.NewGuid();

            a.FakePersistenceId(id);
            a2.FakePersistenceId(id2);
            a3.FakePersistenceId(id3);

            var repoMock = new Mock<IRepo>();

            repoMock.SetupSimpleGetReturns(new[] { a, a2, a3 });

            var data = await new Getter().GetAsync(repoMock.Object);

            data.Should().HaveCount(3);
            data.Any(e => e == a).Should().BeTrue();
            data.Any(e => e == a2).Should().BeTrue();
            data.Any(e => e == a3).Should().BeTrue();

            repoMock.VerifyGetAsyncCalled<IRepo, TestEntity>();
        }

        #endregion

    }
}
