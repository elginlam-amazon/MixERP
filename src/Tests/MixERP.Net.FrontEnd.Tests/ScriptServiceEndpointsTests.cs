using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using MixERP.Net.FrontEnd.Application;
using MixERP.Net.Framework.WebServices;
using Xunit;

namespace MixERP.Net.FrontEnd.Tests
{
    [WebService(Namespace = "http://tempuri.org/")]
    [ScriptService]
    public class CalculatorService : WebService
    {
        [WebMethod]
        public int Add(int a, int b)
        {
            return a + b;
        }

        [WebMethod]
        public string Fail(string message)
        {
            throw new InvalidOperationException(message);
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Xml)]
        public bool IsPositive(int value)
        {
            return value > 0;
        }

        public int NotExposed()
        {
            return 0;
        }
    }

    public sealed class ScriptServiceEndpointsTests : IAsyncLifetime
    {
        private WebApplication app;
        private HttpClient client;

        public async Task InitializeAsync()
        {
            WebApplicationBuilder builder = WebApplication.CreateBuilder(new WebApplicationOptions
            {
                ContentRootPath = AppContext.BaseDirectory
            });

            builder.WebHost.UseTestServer();
            builder.Services.AddSystemWebAdapters();

            this.app = builder.Build();
            this.app.UseRouting();
            this.app.UseSystemWebAdapters();
            this.app.MapScriptServices(new List<Assembly>(), new Dictionary<string, Type>
            {
                {"/Services/Calculator.asmx", typeof(CalculatorService)}
            });

            await this.app.StartAsync();
            this.client = this.app.GetTestClient();
        }

        public async Task DisposeAsync()
        {
            this.client.Dispose();
            await this.app.DisposeAsync();
        }

        [Fact]
        public async Task JsonRequestReturnsResultWrappedInD()
        {
            HttpResponseMessage response = await this.client.PostAsync("/Services/Calculator.asmx/Add",
                new StringContent("{\"a\":2,\"b\":3}", Encoding.UTF8, "application/json"));

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("{\"d\":5}", await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task JsonRequestWithMissingParameterReturnsJsonError()
        {
            HttpResponseMessage response = await this.client.PostAsync("/Services/Calculator.asmx/Add",
                new StringContent("{\"a\":2}", Encoding.UTF8, "application/json"));

            string body = await response.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
            Assert.Contains("\"Message\":\"Invalid web service call, missing value for parameter: \\u0027b\\u0027.\"", body);
        }

        [Fact]
        public async Task ExceptionsThrownByTheServiceAreReturnedAsJsonMessage()
        {
            HttpResponseMessage response = await this.client.PostAsync("/Services/Calculator.asmx/Fail",
                new StringContent("{\"message\":\"Access is denied.\"}", Encoding.UTF8, "application/json"));

            string body = await response.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
            Assert.Contains("Access is denied.", body);
            Assert.Contains("System.InvalidOperationException", body);
        }

        [Fact]
        public async Task FormPostReturnsXmlResult()
        {
            HttpResponseMessage response = await this.client.PostAsync("/Services/Calculator.asmx/IsPositive",
                new FormUrlEncodedContent(new Dictionary<string, string> {{"value", "4"}}));

            string body = await response.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains("<boolean xmlns=\"http://tempuri.org/\">true</boolean>", body);
        }

        [Fact]
        public async Task MethodsWithoutWebMethodAttributeAreNotExposed()
        {
            HttpResponseMessage response = await this.client.PostAsync("/Services/Calculator.asmx/NotExposed",
                new StringContent("{}", Encoding.UTF8, "application/json"));

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
    }
}
