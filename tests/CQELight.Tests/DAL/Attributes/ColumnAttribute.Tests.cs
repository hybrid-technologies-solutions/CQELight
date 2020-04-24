﻿using CQELight.DAL.Attributes;
using CQELight.TestFramework;
using FluentAssertions;
using Xunit;

namespace CQELight.Tests.DAL.Attributes
{
    public class ColumneAttributeTests : BaseUnitTestClass
    {
        #region Ctor & members

        #endregion

        #region Ctor

        [Fact]
        public void ColumnAttribute_ctor_AsExpected()
        {
            var c = new ColumnAttribute("test");
            c.ColumnName.Should().Be("test");
        }

        #endregion

    }
}
