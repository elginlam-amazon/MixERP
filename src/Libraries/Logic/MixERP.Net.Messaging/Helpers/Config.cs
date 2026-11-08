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

using MixERP.Net.Common;
using MixERP.Net.Common.Helpers;
using System;

namespace MixERP.Net.Messaging.Email.Helpers
{
    internal class Config
    {
        public Config(string catalog)
            : this(catalog, null)
        {
        }

        /// <summary>
        ///     Builds the effective SMTP configuration for a catalog.
        ///     <paramref name="options" /> supplies the application-wide defaults bound from the
        ///     "Messaging:Email" configuration section; the per-catalog messaging parameters stored
        ///     in the database override every value they actually supply, which preserves the
        ///     original per-tenant behavior.
        /// </summary>
        public Config(string catalog, EmailOptions options)
        {
            EmailOptions defaults = options ?? new EmailOptions();

            this.Catalog = catalog;

            this.FromName = Coalesce(this.GetParameter("FromDisplayName"), defaults.FromName);
            this.FromEmail = Coalesce(this.GetParameter("FromEmailAddress"), defaults.FromEmail);
            this.SmtpHost = Coalesce(this.GetParameter("SMTPHost"), defaults.SmtpHost);

            string enableSsl = this.GetParameter("SMTPEnableSSL");
            this.EnableSsl = string.IsNullOrWhiteSpace(enableSsl)
                ? defaults.EnableSsl
                : Conversion.TryCastBoolean(enableSsl);

            string smtpPort = this.GetParameter("SMTPPort");
            this.SmtpPort = string.IsNullOrWhiteSpace(smtpPort)
                ? defaults.SmtpPort
                : Conversion.TryCastInteger(smtpPort);

            this.SmtpUsername = Coalesce(this.GetParameter("SMTPUserName"), defaults.SmtpUsername);
            this.DeliveryMethod = this.GetSmtpDeliveryMethod(defaults.DeliveryMethod);
            this.SmtpUserPassword = this.GetSmtpUserPassword(defaults.SmtpPassword);

            //HostingEnvironment.MapPath is a System.Web API with no modern .NET equivalent.
            //MixERPConfiguration.MapPath is its cross-platform replacement: it resolves an
            //application relative ("~/x/y") path against the content root and normalizes
            //directory separators, while returning rooted paths unchanged.
            this.PickupDirectory = MixERPConfiguration.MapPath(
                Coalesce(this.GetParameter("SpecifiedPickupDirectoryLocation"), defaults.PickupDirectory));
        }

        public string Catalog { get; set; }
        public bool EnableSsl { get; set; }
        public string FromName { get; set; }
        public string FromEmail { get; set; }
        public EmailDeliveryMethod DeliveryMethod { get; set; }
        public string SmtpHost { get; set; }
        public int SmtpPort { get; set; }
        public string SmtpUsername { get; set; }
        public string SmtpUserPassword { get; set; }
        public string PickupDirectory { get; set; }

        private string GetParameter(string key)
        {
            return DbConfig.GetMessagingParameter(this.Catalog, key);
        }

        private static string Coalesce(string value, string fallback)
        {
            return string.IsNullOrWhiteSpace(value) ? fallback : value;
        }

        private EmailDeliveryMethod GetSmtpDeliveryMethod(EmailDeliveryMethod fallback)
        {
            EmailDeliveryMethod method;

            if (Enum.TryParse(this.GetParameter("SmtpDeliveryMethod"), true, out method))
            {
                return method;
            }

            return fallback;
        }

        private string GetSmtpUserPassword(string fallback)
        {
            //Unchanged behavior: a pickup-directory delivery never authenticates, so it
            //carries an empty password.
            if (this.DeliveryMethod == EmailDeliveryMethod.SpecifiedPickupDirectory)
            {
                return string.Empty;
            }

            //SecureString offers no protection on modern .NET and is documented as not to be
            //used for new work, so the password is held as a plain string for the duration of
            //the send instead.
            return Coalesce(this.GetParameter("SMTPPassword"), fallback) ?? string.Empty;
        }
    }
}
