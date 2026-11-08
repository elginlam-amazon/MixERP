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
using MixERP.Net.Common.PostgresHelper;
using MixERP.Net.Core.Modules.Sales.Data;
using MixERP.Net.DbFactory;
using MixERP.Net.Entities.Core;
using MixERP.Net.Entities.Models.Transactions;
using Npgsql;
using NpgsqlTypes;
using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;

namespace MixERP.Net.Core.Modules.Purchase.Data.Transactions
{
    internal static class NonGlStockTransaction
    {
        internal static long Add(string catalog, string book, DateTime valueDate, int officeId, int userId, long loginId, string referenceNumber, string statementReference, StockMaster stockMaster, Collection<StockDetail> details, Collection<long> transactionIdCollection, Collection<Attachment> attachments)
        {
            if (stockMaster == null)
            {
                return 0;
            }

            if (details == null)
            {
                return 0;
            }

            if (details.Count.Equals(0))
            {
                return 0;
            }

            string tranIds = ParameterHelper.CreateBigintArrayParameter(transactionIdCollection, "bigint", "@TranId");
            string detail = StockMasterDetailHelper.CreateStockMasterDetailParameter(details);
            string attachment = AttachmentHelper.CreateAttachmentModelParameter(attachments);

            string sql = string.Format(CultureInfo.InvariantCulture, "SELECT * FROM transactions.post_non_gl_transaction(@Book::national character varying(48), @OfficeId::integer, @UserId::integer, @LoginId::bigint, @ValueDate::date, @ReferenceNumber::national character varying(24), @StatementReference::text, @PartyCode::national character varying(12), @PriceTypeId::integer, false, @SalesPersonId::integer, @ShipperId::integer, @ShippingAddressCode::national character varying(12), @StoreId::integer,  ARRAY[{0}]::bigint[], ARRAY[{1}], ARRAY[{2}]);", tranIds, detail, attachment);

            using (NpgsqlCommand command = new NpgsqlCommand(sql))
            {
                command.Parameters.Add(new NpgsqlParameter("@Book", NpgsqlDbType.Varchar) { Value = (object)book ?? DBNull.Value });
                command.Parameters.AddWithValue("@OfficeId", officeId);
                command.Parameters.AddWithValue("@UserId", userId);
                command.Parameters.AddWithValue("@LoginId", loginId);

                // Npgsql 6+ infers the PostgreSQL type from DateTime.Kind: Utc maps to
                // "timestamptz" (whose ::date cast is evaluated in the session TimeZone and can
                // shift the calendar date) and Local can throw. The target parameter is cast to
                // ::date, so bind it explicitly as a date to preserve the Npgsql 2.x behavior.
                command.Parameters.Add(new NpgsqlParameter("@ValueDate", NpgsqlDbType.Date) { Value = valueDate.Date });

                // Npgsql 6+ rejects a CLR null passed to AddWithValue, so the nullable string
                // parameters are bound explicitly with DBNull.Value when absent.
                command.Parameters.Add(new NpgsqlParameter("@ReferenceNumber", NpgsqlDbType.Varchar) { Value = (object)referenceNumber ?? DBNull.Value });
                command.Parameters.Add(new NpgsqlParameter("@StatementReference", NpgsqlDbType.Text) { Value = (object)statementReference ?? DBNull.Value });
                command.Parameters.Add(new NpgsqlParameter("@PartyCode", NpgsqlDbType.Varchar) { Value = (object)stockMaster.PartyCode ?? DBNull.Value });

                if (stockMaster.PriceTypeId.Equals(0))
                {
                    command.Parameters.Add(new NpgsqlParameter("@PriceTypeId", NpgsqlDbType.Integer) { Value = DBNull.Value });
                }
                else
                {
                    command.Parameters.AddWithValue("@PriceTypeId", stockMaster.PriceTypeId);
                }

                if (stockMaster.SalespersonId.Equals(0))
                {
                    command.Parameters.Add(new NpgsqlParameter("@SalesPersonId", NpgsqlDbType.Integer) { Value = DBNull.Value });
                }
                else
                {
                    command.Parameters.AddWithValue("@SalesPersonId", stockMaster.SalespersonId);
                }

                if (stockMaster.ShipperId.Equals(0))
                {
                    command.Parameters.Add(new NpgsqlParameter("@ShipperId", NpgsqlDbType.Integer) { Value = DBNull.Value });
                }
                else
                {
                    command.Parameters.AddWithValue("@ShipperId", stockMaster.ShipperId);
                }

                command.Parameters.Add(new NpgsqlParameter("@ShippingAddressCode", NpgsqlDbType.Varchar) { Value = (object)stockMaster.ShippingAddressCode ?? DBNull.Value });

                if (stockMaster.StoreId.Equals(0))
                {
                    command.Parameters.Add(new NpgsqlParameter("@StoreId", NpgsqlDbType.Integer) { Value = DBNull.Value });
                }
                else
                {
                    command.Parameters.AddWithValue("@StoreId", stockMaster.StoreId);
                }

                command.Parameters.AddRange(ParameterHelper.AddBigintArrayParameter(transactionIdCollection, "@TranId").ToArray());
                command.Parameters.AddRange(StockMasterDetailHelper.AddStockMasterDetailParameter(details).ToArray());
                command.Parameters.AddRange(AttachmentHelper.AddAttachmentParameter(attachments).ToArray());

                long tranId = Conversion.TryCastLong(DbOperation.GetScalarValue(catalog, command));
                return tranId;
            }
        }
    }
}
