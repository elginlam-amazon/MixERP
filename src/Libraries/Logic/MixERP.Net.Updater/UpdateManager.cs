using MixERP.Net.Updater.Api;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Serilog;

namespace MixERP.Net.Updater
{
    public class UpdateManager
    {
        private static Version GetCurrentVersion()
        {
            return Assembly.GetExecutingAssembly().GetName().Version;
            //return new Version("1.0.0.0");
        }

        public async Task<Release> GetLatestRelease()
        {
            IEnumerable<Release> releases = await Releases.GetReleases();

            if (releases == null)
            {
                return null;
            }

            //FirstOrDefault rather than First: "no newer release available" is a normal outcome
            //and must return null, not throw InvalidOperationException.
            Release release = releases
                .OrderBy(x => x.PublishedAt)
                .FirstOrDefault(x => IsNewerThanCurrent(x.TagName));

            return release;
        }

        private static bool IsNewerThanCurrent(string tagName)
        {
            if (string.IsNullOrWhiteSpace(tagName))
            {
                return false;
            }

            Version version;

            //Release tags are conventionally prefixed ("v1.2.0"), which Version.TryParse
            //rejects; strip the prefix before parsing.
            string candidate = tagName.Trim().TrimStart('v', 'V');

            //A release tag that is still not a parsable version must not abort the whole
            //lookup the way "new Version(x.TagName)" did — but it must not vanish silently
            //either, or the updater would report "up to date" forever with no trace.
            if (!Version.TryParse(candidate, out version))
            {
                Log.Warning("Ignoring release tag {TagName}: not a parsable version.", tagName);
                return false;
            }

            return version > GetCurrentVersion();
        }
    }
}