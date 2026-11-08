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

namespace MixERP.Net.Messaging.Email.Helpers
{
    /// <summary>
    /// Strongly-typed SMTP settings bound from the "Messaging:Email" configuration
    /// section (appsettings.json / environment variables / user secrets).
    ///
    /// These act as the application-wide defaults. The per-catalog messaging
    /// parameters stored in the database continue to be read and override any value
    /// they actually supply, preserving the original multi-tenant behavior.
    /// </summary>
    public sealed class EmailOptions
    {
        public const string SectionName = "Messaging:Email";

        public string FromName { get; set; }
        public string FromEmail { get; set; }
        public string SmtpHost { get; set; }
        public int SmtpPort { get; set; }
        public bool EnableSsl { get; set; }
        public string SmtpUsername { get; set; }
        public string SmtpPassword { get; set; }
        public EmailDeliveryMethod DeliveryMethod { get; set; }
        public string PickupDirectory { get; set; }
    }
}
