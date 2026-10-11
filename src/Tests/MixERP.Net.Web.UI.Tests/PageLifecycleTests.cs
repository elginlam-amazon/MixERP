using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SystemWebAdapters;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MixERP.Net.Web.UI.HtmlControls;
using MixERP.Net.Web.UI.WebControls;
using Xunit;

namespace MixERP.Net.Web.UI.Tests
{
    /// <summary>
    ///     Runs pages through an in-memory ASP.NET Core host with System.Web adapters, like the FrontEnd host does.
    /// </summary>
    public sealed class PageLifecycleTests : IAsyncLifetime
    {
        private IHost host;
        private HttpClient client;

        public async Task InitializeAsync()
        {
            ControlRegistry.Register("~/Test.Master", typeof(TestMaster));
            ControlRegistry.Register("~/Target.aspx", typeof(TransferTargetPage));

            this.host = await new HostBuilder()
                .ConfigureWebHost(web => web
                    .UseTestServer()
                    .ConfigureServices(services =>
                    {
                        services.AddRouting();
                        services.AddSystemWebAdapters();
                    })
                    .Configure(app =>
                    {
                        app.UseRouting();
                        app.UseSystemWebAdapters();
                        app.UseEndpoints(endpoints => endpoints.MapFallback("{**path}", Execute).PreBufferRequestStream().BufferResponseStream());
                    }))
                .StartAsync();

            this.client = this.host.GetTestClient();
        }

        public async Task DisposeAsync()
        {
            this.client.Dispose();
            await this.host.StopAsync();
            this.host.Dispose();
        }

        private static Task Execute(HttpContext context)
        {
            Page page;

            switch (context.Request.Path.Value)
            {
                case "/postback":
                    page = new PostBackPage();
                    break;
                case "/master":
                    page = new ContentPage();
                    break;
                case "/transfer":
                    page = new TransferSourcePage();
                    break;
                case "/redirect":
                    page = new RedirectPage();
                    break;
                default:
                    context.Response.StatusCode = StatusCodes.Status404NotFound;
                    return Task.CompletedTask;
            }

            page.ProcessRequest(context.AsSystemWeb());
            return Task.CompletedTask;
        }

        [Fact]
        public async Task GetRendersTheServerFormWithPostBackFields()
        {
            string html = await this.client.GetStringAsync("/postback");

            Assert.Contains("<form id=\"form1\" name=\"form1\" method=\"post\" action=\"/postback\">", html);
            Assert.Contains("name=\"__EVENTTARGET\"", html);
            Assert.Contains("<input name=\"NameTextBox\" type=\"text\" id=\"NameTextBox\" />", html);
            Assert.Contains("Loaded by Page_Load", html);
            Assert.Contains("Late control loaded", html);
        }

        [Fact]
        public async Task PostBackLoadsPostedValuesAndRaisesTheClickedButton()
        {
            FormUrlEncodedContent form = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                {"__EVENTTARGET", string.Empty},
                {"__EVENTARGUMENT", string.Empty},
                {"NameTextBox", "John"},
                {"SaveButton", "Save"}
            });

            HttpResponseMessage response = await this.client.PostAsync("/postback", form);
            string html = await response.Content.ReadAsStringAsync();

            Assert.Contains("<span id=\"ResultLabel\">Hello John</span>", html);
            Assert.Contains("value=\"John\"", html);
        }

        [Fact]
        public async Task PostBackRunsValidatorsBeforeTheClickHandler()
        {
            FormUrlEncodedContent form = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                {"__EVENTTARGET", string.Empty},
                {"NameTextBox", string.Empty},
                {"SaveButton", "Save"}
            });

            HttpResponseMessage response = await this.client.PostAsync("/postback", form);
            string html = await response.Content.ReadAsStringAsync();

            Assert.Contains("<span id=\"ResultLabel\">Invalid</span>", html);
            Assert.Contains("<span id=\"NameRequired\">Required</span>", html);
        }

        [Fact]
        public async Task MasterPageContentPlaceHoldersAreFilledByTheContentControls()
        {
            string html = await this.client.GetStringAsync("/master");

            Assert.Equal("<html><body><main>Page body</main><footer>Default footer</footer></body></html>", html);
        }

        [Fact]
        public async Task TransferExecutesTheTargetPageInstead()
        {
            string html = await this.client.GetStringAsync("/transfer");

            Assert.Equal("Target page", html);
        }

        [Fact]
        public async Task RedirectStopsTheLifecycleAndReturnsTheLocation()
        {
            using (HttpResponseMessage response = await this.client.GetAsync("/redirect"))
            {
                string body = await response.Content.ReadAsStringAsync();

                Assert.Equal(System.Net.HttpStatusCode.Redirect, response.StatusCode);
                Assert.Equal("/SignIn.aspx", response.Headers.Location?.OriginalString);
                Assert.DoesNotContain("Rendered", body);
            }
        }

        private sealed class RedirectPage : Page
        {
            protected override void FrameworkInitialize()
            {
                base.FrameworkInitialize();
                this.Controls.Add(new LiteralControl("Rendered"));
            }

            protected override void OnInit(EventArgs e)
            {
                this.Response.Redirect("~/SignIn.aspx");
                base.OnInit(e);
            }
        }

        private sealed class PostBackPage : Page
        {
            private Label resultLabel;
            private TextBox nameTextBox;
            private Literal loadLiteral;
            private PlaceHolder placeHolder;

            protected override void FrameworkInitialize()
            {
                base.FrameworkInitialize();

                HtmlForm form = new HtmlForm {ID = "form1"};
                this.nameTextBox = new TextBox {ID = "NameTextBox"};
                RequiredFieldValidator validator = new RequiredFieldValidator {ID = "NameRequired", ControlToValidate = "NameTextBox", ErrorMessage = "Required"};
                Button button = new Button {ID = "SaveButton", Text = "Save"};
                button.Click += this.SaveButton_Click;
                this.resultLabel = new Label {ID = "ResultLabel"};
                this.loadLiteral = new Literal();
                this.placeHolder = new PlaceHolder();

                form.Controls.Add(this.nameTextBox);
                form.Controls.Add(validator);
                form.Controls.Add(button);
                form.Controls.Add(this.resultLabel);
                form.Controls.Add(this.loadLiteral);
                form.Controls.Add(this.placeHolder);
                this.Controls.Add(form);
            }

            private void Page_Load(object sender, EventArgs e)
            {
                this.loadLiteral.Text = "Loaded by Page_Load";
                this.placeHolder.Controls.Add(new LateControl());
            }

            private void SaveButton_Click(object sender, EventArgs e)
            {
                this.resultLabel.Text = this.IsValid ? "Hello " + this.nameTextBox.Text : "Invalid";
            }
        }

        private sealed class LateControl : UserControl
        {
            private readonly Literal literal = new Literal();

            protected override void FrameworkInitialize()
            {
                base.FrameworkInitialize();
                this.Controls.Add(this.literal);
            }

            protected override void OnLoad(EventArgs e)
            {
                this.literal.Text = "Late control loaded";
                base.OnLoad(e);
            }
        }

        private sealed class TestMaster : MasterPage
        {
            protected override void FrameworkInitialize()
            {
                base.FrameworkInitialize();
                this.Controls.Add(new LiteralControl("<html><body><main>"));
                this.Controls.Add(new ContentPlaceHolder {ID = "Body"});
                this.Controls.Add(new LiteralControl("</main><footer>"));
                ContentPlaceHolder footer = new ContentPlaceHolder {ID = "Footer"};
                footer.Controls.Add(new LiteralControl("Default footer"));
                this.Controls.Add(footer);
                this.Controls.Add(new LiteralControl("</footer></body></html>"));
            }
        }

        private sealed class ContentPage : Page
        {
            protected override void FrameworkInitialize()
            {
                base.FrameworkInitialize();
                this.MasterPageFile = "~/Test.Master";
                Content content = new Content {ContentPlaceHolderID = "Body"};
                content.Controls.Add(new LiteralControl("Page body"));
                this.Controls.Add(content);
            }
        }

        private sealed class TransferSourcePage : Page
        {
            protected override void FrameworkInitialize()
            {
                base.FrameworkInitialize();
                this.Controls.Add(new LiteralControl("Source page"));
            }

            protected override void OnLoad(EventArgs e)
            {
                this.Transfer("~/Target.aspx");
            }
        }

        private sealed class TransferTargetPage : Page
        {
            protected override void FrameworkInitialize()
            {
                base.FrameworkInitialize();
                this.Controls.Add(new LiteralControl("Target page"));
            }
        }
    }
}
