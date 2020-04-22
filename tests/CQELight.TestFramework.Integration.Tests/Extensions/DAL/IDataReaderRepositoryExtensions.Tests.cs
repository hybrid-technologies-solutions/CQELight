using CQELight.Abstractions.DAL.Interfaces;
using CQELight.DAL.Common;
using CQELight.TestFramework.Extensions.DAL;
using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace CQELight.TestFramework.Integration.Tests.Extensions.DAL
{
    public class IDataReaderRepositoryExtensionsTests
    {
        #region Ctor & members

        private class Model : PersistableEntity
        {
            public string Data { get; set; }
        }

        #endregion

        #region SetupGetAsyncReturns

        [Fact]
        public async Task SetupGetAsyncReturns_Should_Returns_DesiredContent()
        {
            var repoMock = new Mock<IDatabaseRepository>();
            repoMock.SetupGetAsyncReturns(new[] { new Model { Data = "test" } });

            var data = await repoMock.Object.GetAsync<Model>().ToListAsync();

            data.Should().HaveCount(1);
            data.First().Data.Should().Be("test");
        }

        [Fact]
        public async Task SetupGetAsyncReturns_Should_IgnoreLambda()
        {
            var repoMock = new Mock<IDatabaseRepository>();
            repoMock.SetupGetAsyncReturns(new[] { new Model { Data = "test" } });

            var data = await repoMock.Object.GetAsync<Model>(m => m.Data == "data").ToListAsync();

            data.Should().HaveCount(1);
            data.First().Data.Should().Be("test");
        }

        #endregion
    }
}
