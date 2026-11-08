using Markdig;
using MixERP.Net.Common;
using System;
using System.Collections.Generic;

namespace MixERP.Net.Updater.Api
{
    public class Release
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string TagName { get; set; }
        public string Body { get; set; }
        public bool Draft { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? PublishedAt { get; set; }
        public IEnumerable<Asset> Assets { get; set; }

        public static Release Parse(dynamic toParse, string assetsKey)
        {
            List<Asset> assets = new List<Asset>();

            foreach(dynamic asset in toParse[assetsKey])
            {
                assets.Add(Asset.Parse(asset));
            }

            var release = new Release
            {
                Assets = assets,
                CreatedAt = Conversion.TryCastDate(toParse[Config.CreatedAtKey]),
                Draft = toParse[Config.DraftKey],
                Id = toParse[Config.IdKey],
                Name = toParse[Config.NameKey],
                TagName = toParse[Config.TagNameKey],
                Body = toParse[Config.BodyKey],
                PublishedAt = Conversion.TryCastDate(toParse[Config.PublishedAtKey])
            };

            release.Body = ParseMarkdown(release.Body);
            return release;
        }

        /// <remarks>
        ///     MarkdownDeep.NET only ever shipped .NET Framework 3.5 assemblies, so it cannot be
        ///     restored for net10.0. Markdig is the maintained replacement. MarkdownDeep's
        ///     <c>ExtraMode</c> (Markdown Extra: tables, fenced code, definition lists, footnotes)
        ///     maps onto Markdig's advanced extensions, and <c>SafeMode = false</c> — raw HTML passed
        ///     through untouched — is Markdig's default behavior.
        /// </remarks>
        private static string ParseMarkdown(string markdown)
        {
            if (string.IsNullOrEmpty(markdown))
            {
                return markdown;
            }

            return Markdig.Markdown.ToHtml(markdown, MarkdownPipeline);
        }

        private static readonly Markdig.MarkdownPipeline MarkdownPipeline =
            new Markdig.MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
    }
}