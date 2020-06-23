using FluentAssertions;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Xunit;

namespace CQELight.ASPCore.Tests
{
    public class CQELightASPNetCore2_1Tests : IDisposable
    {
        private readonly IWebDriver _driver;
        private readonly Process process;

        public CQELightASPNetCore2_1Tests()
        {
            _driver = new ChromeDriver(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));
        }

        public void Dispose()
        {
            _driver.Quit();
            _driver.Dispose();
        }

        [Fact]
        public void ASPNetCore2_1_Should_Run()
        {
            _driver.Navigate()
                   .GoToUrl("http://localhost:9999");

            _driver.Title.Should().StartWith("CQELight demo website");
            _driver.PageSource.Should().Contain("If you see this, it means that your website is running with CQELight !");
        }
    }
}
