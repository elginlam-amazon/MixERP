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

using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using MimeKit.Text;
using MixERP.Net.Messaging.Email.Helpers;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MixERP.Net.Messaging.Email
{
    public sealed class Processor
    {
        private readonly EmailOptions options;

        public Processor(string catalog)
            : this(catalog, null)
        {
        }

        /// <summary>
        ///     <paramref name="options" /> carries the application-wide SMTP defaults bound from
        ///     the "Messaging:Email" configuration section. The per-catalog messaging parameters
        ///     stored in the database still take precedence over them.
        /// </summary>
        public Processor(string catalog, EmailOptions options)
        {
            this.Catalog = catalog;
            this.options = options;
        }

        public string Catalog { get; set; }

        [System.CLSCompliant(false)]
        public void Send(string sendTo, string subject, string body, IEnumerable<MimeEntity> attachments)
        {
            EmailMessage email;
            SmtpHost host;
            ICredentials credentials;

            this.BuildRequest(sendTo, subject, body, out email, out host, out credentials);

            this.Send(email, host, credentials, attachments);
        }

        [System.CLSCompliant(false)]
        public Task SendAsync(string sendTo, string subject, string body,
            IEnumerable<MimeEntity> attachments)
        {
            return this.SendAsync(sendTo, subject, body, attachments, CancellationToken.None);
        }

        [System.CLSCompliant(false)]
        public Task SendAsync(string sendTo, string subject, string body,
            IEnumerable<MimeEntity> attachments, CancellationToken cancellationToken)
        {
            EmailMessage email;
            SmtpHost host;
            ICredentials credentials;

            this.BuildRequest(sendTo, subject, body, out email, out host, out credentials);

            return this.SendAsync(email, host, credentials, attachments, cancellationToken);
        }

        /// <summary>
        ///     Dispatches the message without blocking the caller, which is the behavior the
        ///     former ThreadPool.QueueUserWorkItem call provided. Failures are observed and
        ///     logged rather than being lost on an unobserved task.
        /// </summary>
        [System.CLSCompliant(false)]
        public void Send(EmailMessage email, SmtpHost host, ICredentials credentials,
            IEnumerable<MimeEntity> attachments)
        {
            bool dispatch;

            try
            {
                dispatch = Prepare(email);
            }
            catch
            {
                DisposeAttachments(attachments);
                throw;
            }

            if (!dispatch)
            {
                DisposeAttachments(attachments);
                return;
            }

            Task.Run(async () =>
            {
                try
                {
                    await SendCoreAsync(email, host, credentials, attachments,
                        CancellationToken.None).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    //A background dispatch must never surface an unobserved task exception.
                    email.Status = Status.Failed;
                    Log.Warning(@"Could not send email to {To}. {Ex}. ", email.SentTo, ex);
                }
            });
        }

        [System.CLSCompliant(false)]
        public Task SendAsync(EmailMessage email, SmtpHost host, ICredentials credentials,
            IEnumerable<MimeEntity> attachments)
        {
            return this.SendAsync(email, host, credentials, attachments, CancellationToken.None);
        }

        [System.CLSCompliant(false)]
        public async Task SendAsync(EmailMessage email, SmtpHost host, ICredentials credentials,
            IEnumerable<MimeEntity> attachments, CancellationToken cancellationToken)
        {
            bool dispatch;

            try
            {
                dispatch = Prepare(email);
            }
            catch
            {
                DisposeAttachments(attachments);
                throw;
            }

            if (!dispatch)
            {
                DisposeAttachments(attachments);
                return;
            }

            await SendCoreAsync(email, host, credentials, attachments, cancellationToken)
                .ConfigureAwait(false);
        }

        /// <summary>
        ///     The attachment parts hold open file streams (they are disposed together with the
        ///     MimeMessage once the message is built). On any path that never builds a message
        ///     they must be released here, or the file handles leak until finalization.
        /// </summary>
        private static void DisposeAttachments(IEnumerable<MimeEntity> attachments)
        {
            if (attachments == null)
            {
                return;
            }

            foreach (MimeEntity attachment in attachments)
            {
                if (attachment != null)
                {
                    attachment.Dispose();
                }
            }
        }

        private void BuildRequest(string sendTo, string subject, string body,
            out EmailMessage email, out SmtpHost host, out ICredentials credentials)
        {
            Config config = new Config(this.Catalog, this.options);

            email = new EmailMessage
            {
                FromName = config.FromName,
                FromEmail = config.FromEmail,
                Subject = subject,
                SentTo = sendTo,
                Message = body,
                Type = Type.Outward,
                EventDateUTC = DateTime.UtcNow,
                Status = Status.Unknown
            };

            host = new SmtpHost
            {
                Address = config.SmtpHost,
                Port = config.SmtpPort,
                EnableSSL = config.EnableSsl,
                DeliveryMethod = config.DeliveryMethod,
                PickupDirectory = config.PickupDirectory
            };

            credentials = new SmtpCredentials
            {
                Username = config.SmtpUsername,
                Password = config.SmtpUserPassword
            };
        }

        /// <summary>
        ///     Validates and normalizes the recipient list. Returns false when the message must
        ///     not be dispatched, which is the original "return on first invalid address"
        ///     behavior.
        /// </summary>
        private static bool Prepare(EmailMessage email)
        {
            if (email == null)
            {
                throw new ArgumentNullException("email");
            }

            if (string.IsNullOrWhiteSpace(email.SentTo))
            {
                throw new ArgumentNullException("email.SentTo");
            }

            if (string.IsNullOrWhiteSpace(email.Message))
            {
                throw new ArgumentNullException("email.Message");
            }

            string[] addresses = email.SentTo.Split(',');

            foreach (string address in addresses)
            {
                Validator validator = new Validator(address);
                validator.Validate();

                if (!validator.IsValid)
                {
                    return false;
                }
            }

            addresses = addresses.Distinct().ToArray();
            email.SentTo = string.Join(",", addresses);
            email.Status = Status.Executing;

            return true;
        }

        private static async Task SendCoreAsync(EmailMessage email, SmtpHost host,
            ICredentials credentials, IEnumerable<MimeEntity> attachments,
            CancellationToken cancellationToken)
        {
            using (MimeMessage mail = new MimeMessage())
            {
                mail.From.Add(new MailboxAddress(email.FromName, email.FromEmail));

                foreach (string address in email.SentTo.Split(','))
                {
                    if (!string.IsNullOrWhiteSpace(address))
                    {
                        mail.To.Add(MailboxAddress.Parse(address.Trim()));
                    }
                }

                //Encoding.UTF8 for the subject, matching MailMessage.SubjectEncoding.
                mail.Headers.Replace(HeaderId.Subject, Encoding.UTF8, email.Subject ?? string.Empty);

                //IsBodyHtml = true.
                TextPart bodyPart = new TextPart(TextFormat.Html) { Text = email.Message };

                if (attachments != null && attachments.Any())
                {
                    Multipart multipart = new Multipart("mixed") { bodyPart };

                    foreach (MimeEntity attachment in attachments)
                    {
                        multipart.Add(attachment);
                    }

                    mail.Body = multipart;
                }
                else
                {
                    mail.Body = bodyPart;
                }

                try
                {
                    if (host.DeliveryMethod == EmailDeliveryMethod.SpecifiedPickupDirectory)
                    {
                        await WriteToPickupDirectoryAsync(mail, host.PickupDirectory, cancellationToken)
                            .ConfigureAwait(false);
                    }
                    else if (host.DeliveryMethod == EmailDeliveryMethod.PickupDirectoryFromIis)
                    {
                        //The IIS pickup directory is a Windows/IIS-only concept with no modern,
                        //cross-platform equivalent, so it cannot silently fall through to a
                        //network send (the host/port are typically unset for this method).
                        //A configured pickup directory is honored; otherwise the failure is
                        //explicit and logged.
                        if (!string.IsNullOrWhiteSpace(host.PickupDirectory))
                        {
                            await WriteToPickupDirectoryAsync(mail, host.PickupDirectory, cancellationToken)
                                .ConfigureAwait(false);
                        }
                        else
                        {
                            email.Status = Status.Failed;
                            Log.Warning(
                                @"Could not send email to {To}. The ""PickupDirectoryFromIis"" delivery method is not supported on .NET 10. Set the ""SmtpDeliveryMethod"" messaging parameter to ""SpecifiedPickupDirectory"" and configure ""SpecifiedPickupDirectoryLocation"", or use ""Network"".",
                                email.SentTo);
                            return;
                        }
                    }
                    else
                    {
                        await TransmitAsync(mail, host, credentials, cancellationToken)
                            .ConfigureAwait(false);
                    }

                    email.Status = Status.Completed;
                }
                catch (Exception ex) when (ex is SmtpCommandException
                                           || ex is SmtpProtocolException
                                           || ex is AuthenticationException
                                           || ex is SslHandshakeException
                                           || ex is MailKit.ServiceNotAuthenticatedException
                                           || ex is MailKit.ServiceNotConnectedException
                                           || ex is IOException)
                {
                    email.Status = Status.Failed;
                    Log.Warning(@"Could not send email to {To}. {Ex}. ", email.SentTo, ex);
                }
            }
        }

        private static async Task TransmitAsync(MimeMessage mail, SmtpHost host,
            ICredentials credentials, CancellationToken cancellationToken)
        {
            using (SmtpClient smtp = new SmtpClient())
            {
                //EnableSsl on the legacy SmtpClient meant STARTTLS on the submission ports;
                //the implicit-TLS port 465 requires SslOnConnect.
                SecureSocketOptions socketOptions;

                if (host.EnableSSL)
                {
                    socketOptions = host.Port == 465
                        ? SecureSocketOptions.SslOnConnect
                        : SecureSocketOptions.StartTls;
                }
                else
                {
                    socketOptions = SecureSocketOptions.None;
                }

                await smtp.ConnectAsync(host.Address, host.Port, socketOptions, cancellationToken)
                    .ConfigureAwait(false);

                //UseDefaultCredentials = false: authenticate only with the supplied credentials,
                //and only when they were actually configured.
                if (credentials != null && !string.IsNullOrWhiteSpace(credentials.Username))
                {
                    await smtp.AuthenticateAsync(credentials.Username,
                        credentials.Password ?? string.Empty, cancellationToken).ConfigureAwait(false);
                }

                await smtp.SendAsync(mail, cancellationToken).ConfigureAwait(false);
                await smtp.DisconnectAsync(true, cancellationToken).ConfigureAwait(false);
            }
        }

        private static async Task WriteToPickupDirectoryAsync(MimeMessage mail,
            string pickupDirectory, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(pickupDirectory))
            {
                throw new InvalidOperationException(
                    "The SpecifiedPickupDirectory delivery method requires the \"SpecifiedPickupDirectoryLocation\" messaging parameter to be configured.");
            }

            Directory.CreateDirectory(pickupDirectory);

            string path = Path.Combine(pickupDirectory, Guid.NewGuid().ToString("N") + ".eml");

            using (FileStream stream = File.Create(path))
            {
                await mail.WriteToAsync(stream, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
