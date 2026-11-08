/********************************************************************************
Copyright (C) Binod Nepal, Mix Open Foundation (http://mixof.org).

This file is part of MixERP.

MixERP is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

MixERP is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with MixERP.  If not, see <http://www.gnu.org/licenses/>.
***********************************************************************************/

using MixERP.Net.Common.Helpers;
using PuppeteerSharp;
using PuppeteerSharp.BrowserData;
using PuppeteerSharp.Media;
using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace MixERP.Net.HtmlParser.ImageSerializer
{
    /// <summary>
    ///     Cross-platform HTML rasterizer that replaces the .NET Framework rendering engines this
    ///     library used to depend on: the bare-DLL <c>TheArtOfDev.HtmlRenderer.WinForms.HtmlRender</c>
    ///     and the Windows-only <c>System.Windows.Forms.WebBrowser</c> (Internet Explorer) control.
    ///     Neither has a net10.0 build, so both serializers now rasterize through headless Chromium
    ///     (PuppeteerSharp), which preserves real HTML/CSS layout exactly as the browser control did.
    /// </summary>
    /// <remarks>
    ///     Deployment: the Chromium browser is an external dependency of this library. Install it in
    ///     the container/host image and point MixERP at it with either the
    ///     <c>PUPPETEER_EXECUTABLE_PATH</c> environment variable or the
    ///     <c>ChromiumExecutablePath</c> application setting. Only when neither is configured does
    ///     the engine fall back to downloading a Chromium build on first use, which needs outbound
    ///     internet access and writable disk and is therefore unsuitable for air-gapped hosts.
    /// </remarks>
    internal static class HtmlRenderingEngine
    {
        internal const int FallbackViewportWidth = 1024;

        private const string ExecutablePathSetting = "ChromiumExecutablePath";
        private const string ExecutablePathVariable = "PUPPETEER_EXECUTABLE_PATH";
        private const string ViewportWidthSetting = "HtmlParserViewportWidth";

        private static readonly SemaphoreSlim BrowserLock = new SemaphoreSlim(1, 1);
        private static string browserExecutablePath;
        private static IBrowser browser;

        /// <summary>
        ///     The viewport width the serializers rasterize at. Configurable through the
        ///     <c>HtmlParserViewportWidth</c> application setting so the legacy output dimensions
        ///     can be pinned if an e-mail layout regresses; defaults to
        ///     <see cref="FallbackViewportWidth" />.
        /// </summary>
        internal static int ViewportWidth
        {
            get
            {
                string configured = MixERPConfiguration.GetAppSetting(ViewportWidthSetting);
                int width;

                if (!string.IsNullOrWhiteSpace(configured)
                    && int.TryParse(configured, NumberStyles.Integer, CultureInfo.InvariantCulture, out width)
                    && width > 0)
                {
                    return width;
                }

                return FallbackViewportWidth;
            }
        }

        /// <summary>
        ///     Rasterizes an HTML fragment/document held in memory. Mirrors
        ///     <c>HtmlRender.RenderToImage(html)</c>.
        /// </summary>
        internal static byte[] RenderHtml(string html, int viewportWidth, double deviceScaleFactor)
        {
            return Render(html, null, viewportWidth, deviceScaleFactor);
        }

        /// <summary>
        ///     Rasterizes an HTML file from disk, which is what the Internet Explorer
        ///     <c>WebBrowser.Navigate(tempPath)</c> path did. The page-loaded wait is deterministic
        ///     here (Chromium reports network idle) instead of the old
        ///     <c>ReadyState</c>/<c>Application.DoEvents()</c> spin.
        /// </summary>
        internal static byte[] RenderFile(string filePath, int viewportWidth, double deviceScaleFactor)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentNullException("filePath");
            }

            return Render(null, new Uri(filePath).AbsoluteUri, viewportWidth, deviceScaleFactor);
        }

        private static byte[] Render(string html, string url, int viewportWidth, double deviceScaleFactor)
        {
            //IHtmlImageSerializer.Serialize() is a synchronous, public contract that existing
            //callers (the transaction checklist e-mail helper) rely on: the image file must exist
            //and ImageSaved must have been raised by the time Serialize() returns. The legacy code
            //achieved that with an STA thread plus Thread.Join(); the STA apartment is meaningless
            //on net10.0, so the equivalent here is to run the async render on the thread pool and
            //wait for it. Task.Run isolates the wait from any ambient synchronization context.
            return Task.Run(() => RenderAsync(html, url, viewportWidth, deviceScaleFactor)).GetAwaiter().GetResult();
        }

        private static async Task<byte[]> RenderAsync(string html, string url, int viewportWidth, double deviceScaleFactor)
        {
            IBrowser instance = await GetBrowserAsync().ConfigureAwait(false);

            await using (IPage page = await instance.NewPageAsync().ConfigureAwait(false))
            {
                await page.SetViewportAsync(new ViewPortOptions
                {
                    Width = viewportWidth,
                    Height = 768,
                    DeviceScaleFactor = deviceScaleFactor
                }).ConfigureAwait(false);

                NavigationOptions navigation = new NavigationOptions
                {
                    WaitUntil = new[] { WaitUntilNavigation.Networkidle0 }
                };

                if (url == null)
                {
                    //SetContent ignores the networkidle wait conditions, so wait for the load event
                    //and then explicitly for network idle (images/CSS referenced by the HTML).
                    await page.SetContentAsync(html ?? string.Empty, new SetContentOptions
                    {
                        WaitUntil = new[] { WaitUntilNavigation.Load }
                    }).ConfigureAwait(false);
                    await page.WaitForNetworkIdleAsync().ConfigureAwait(false);
                }
                else
                {
                    await page.GoToAsync(url, navigation).ConfigureAwait(false);
                }

                ScreenshotOptions screenshot = new ScreenshotOptions
                {
                    FullPage = true,
                    Type = ScreenshotType.Png,
                    OmitBackground = false
                };

                return await page.ScreenshotDataAsync(screenshot).ConfigureAwait(false);
            }
        }

        /// <summary>
        ///     Returns the shared headless browser, launching it on first use. One Chromium process
        ///     is reused for every render (a page per image) instead of launching one per image.
        /// </summary>
        private static async Task<IBrowser> GetBrowserAsync()
        {
            IBrowser instance = browser;

            if (instance != null && instance.IsConnected)
            {
                return instance;
            }

            await BrowserLock.WaitAsync().ConfigureAwait(false);

            try
            {
                if (browser == null || !browser.IsConnected)
                {
                    browser = await Puppeteer.LaunchAsync(new LaunchOptions
                    {
                        Headless = true,
                        ExecutablePath = await ResolveExecutablePathAsync().ConfigureAwait(false),
                        Args = new[] { "--no-sandbox", "--disable-dev-shm-usage", "--hide-scrollbars" }
                    }).ConfigureAwait(false);
                }

                return browser;
            }
            finally
            {
                BrowserLock.Release();
            }
        }

        /// <summary>
        ///     Resolves the Chromium executable: an explicitly configured path wins (environment
        ///     variable, then application setting), so a host that ships Chromium in its image never
        ///     reaches out to the network. Only an unconfigured host falls back to fetching a build.
        /// </summary>
        private static async Task<string> ResolveExecutablePathAsync()
        {
            if (!string.IsNullOrEmpty(browserExecutablePath))
            {
                return browserExecutablePath;
            }

            string configured = Environment.GetEnvironmentVariable(ExecutablePathVariable);

            if (string.IsNullOrWhiteSpace(configured))
            {
                configured = MixERPConfiguration.GetAppSetting(ExecutablePathSetting);
            }

            if (!string.IsNullOrWhiteSpace(configured))
            {
                browserExecutablePath = configured.Trim();
                return browserExecutablePath;
            }

            BrowserFetcher fetcher = new BrowserFetcher();
            InstalledBrowser installed = await fetcher.DownloadAsync().ConfigureAwait(false);
            browserExecutablePath = installed.GetExecutablePath();

            return browserExecutablePath;
        }
    }
}
