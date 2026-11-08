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
using Npgsql;
using NpgsqlTypes;
using System;

namespace MixERP.Net.Core.Modules.Sales.Data.Transactions
{
    public static class Receipt
    {
        public static long PostTransaction(string catalog, int userId, int officeId, long loginId, string partyCode,
            string currencyCode,
            decimal amount, decimal debitExchangeRate, decimal creditExchangeRate, string referenceNumber,
            string statementReference, int costCenterId, int cashRepositoryId, DateTime? postedDate, long bankAccountId,
            int paymentCardId, string bankInstrumentCode, string bankTransactionCode)
        {
            const string sql = "SELECT transactions.post_receipt" +
                               "(" +
                               "@UserId::integer, " +
                               "@OfficeId::integer, " +
                               "@LoginId::bigint, " +
                               "@PartyCode::national character varying(12), " +
                               "@CurrencyCode::national character varying(12), " +
                               "@Amount::public.money_strict, " +
                               "@DebitExchangeRate::public.decimal_strict, " +
                               "@CreditExchangeRate::public.decimal_strict, " +
                               "@ReferenceNumber::national character varying(24), " +
                               "@StatementReference::national character varying(128), " +
                               "@CostCenterId::integer, " +
                               "@CashRepositoryId::integer, " +
                               "@PostedDate::date, " +
                               "@BankAccountId::bigint, " +
                               "@PaymentCardId::integer, " +
                               "@BankInstrumentCode::national character varying(128), " +
                               "@BankTransactionCode::national character varying(128)" +
                               "); ";


            using (NpgsqlCommand command = new NpgsqlCommand(sql))
            {
                command.Parameters.AddWithValue("@UserId", userId);
                command.Parameters.AddWithValue("@OfficeId", officeId);
                command.Parameters.AddWithValue("@LoginId", loginId);
                command.Parameters.AddWithValue("@PartyCode", partyCode ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@CurrencyCode", currencyCode ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@Amount", amount);
                command.Parameters.AddWithValue("@DebitExchangeRate", debitExchangeRate);
                command.Parameters.AddWithValue("@CreditExchangeRate", creditExchangeRate);
                command.Parameters.AddWithValue("@ReferenceNumber", referenceNumber ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@StatementReference", statementReference ?? (object)DBNull.Value);

                if (costCenterId.Equals(0))
                {
                    command.Parameters.AddWithValue("@CostCenterId", DBNull.Value);
                }
                else
                {
                    command.Parameters.AddWithValue("@CostCenterId", costCenterId);
                }

                if (cashRepositoryId.Equals(0))
                {
                    command.Parameters.AddWithValue("@CashRepositoryId", DBNull.Value);
                }
                else
                {
                    command.Parameters.AddWithValue("@CashRepositoryId", cashRepositoryId);
                }

                if (bankAccountId.Equals(0))
                {
                    command.Parameters.AddWithValue("@BankAccountId", DBNull.Value);
                }
                else
                {
                    command.Parameters.AddWithValue("@BankAccountId", bankAccountId);
                }

                if (paymentCardId.Equals(0))
                {
                    command.Parameters.AddWithValue("@PaymentCardId", DBNull.Value);
                }
                else
                {
                    command.Parameters.AddWithValue("@PaymentCardId", paymentCardId);
                }

                //Npgsql 6+ maps the PostgreSQL "date" type to DateOnly, so the nullable DateTime
                //must be bound with an explicit NpgsqlDbType.Date for @PostedDate::date to resolve.
                NpgsqlParameter postedDateParameter = new NpgsqlParameter("@PostedDate", NpgsqlDbType.Date);

                if (postedDate == null)
                {
                    postedDateParameter.Value = DBNull.Value;
                }
                else
                {
                    postedDateParameter.Value = postedDate.Value.Date;
                }

                command.Parameters.Add(postedDateParameter);

                command.Parameters.AddWithValue("@BankInstrumentCode", bankInstrumentCode ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@BankTransactionCode", bankTransactionCode ?? (object)DBNull.Value);

                return Conversion.TryCastLong(DbFactory.DbOperation.GetScalarValue(catalog, command));
            }
        }
    }
}