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

using MixERP.Net.Common.Extensions;
using MixERP.Net.Common.Helpers;
using MixERP.Net.i18n.Resources;
using MixERP.Net.WebControls.ScrudFactory.Data;
using MixERP.Net.WebControls.ScrudFactory.Helpers;
using System;
using System.Data;
using System.Linq;

namespace MixERP.Net.WebControls.ScrudFactory.Controls.ListControls
{
    ////////////////////////////////////////////////////////////////////////////////////////////////////
    /// <summary>
    ///     Builds the foreign-key field of the schema-driven scrud form: the drop-down list of
    ///     parent rows plus its item-selector popup link. This is the control the whole
    ///     referential-integrity UX hangs off, so every one of the original's five private helpers
    ///     is ported.
    ///     <para>
    ///         <c>System.Web.UI.WebControls.DropDownList</c>, <c>ListItem</c> and
    ///         <c>System.Web.UI.HtmlControls.HtmlAnchor</c> are all removed on net10.0, so this
    ///         produces a <see cref="ScrudFieldKind.DropDownList" /> field (options in
    ///         <see cref="ScrudField.Items" />, link in <see cref="ScrudField.ItemSelector" />) that
    ///         <c>ScrudFieldRow.razor</c> renders as the same
    ///         <c>&lt;select id="{column}_dropdownlist"&gt;</c> and <c>&lt;a role="item-selector"&gt;</c>.
    ///     </para>
    /// </summary>
    ////////////////////////////////////////////////////////////////////////////////////////////////////
    internal static class ScrudDropDownList
    {
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        ///     Port of <c>AddDropDownList</c> (16 parameters), in the ORIGINAL ORDER, which matters:
        ///     the options are loaded and the selection applied BEFORE the blank item is inserted at
        ///     index 0 for a nullable column, so inserting it cannot disturb the chosen value.
        /// </summary>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        internal static void AddDropDownList(string catalog, ScrudFieldTable fieldTable, string resourceClassName, string itemSelectorPath, string columnName, bool isNullable, string tableSchema, string tableName, string tableColumn, string defaultValue, string displayFields, string displayViews, bool useDisplayViewsAsParent, string selectedValues, string errorCssClass, bool disabled)
        {
            if (fieldTable == null)
            {
                return;
            }

            string label = ScrudLocalizationHelper.GetResourceString(resourceClassName, columnName);

            ScrudField field = GetDropDownList(columnName + "_dropdownlist");

            if (field == null)
            {
                return;
            }

            field.ColumnName = columnName;
            field.IsNullable = isNullable;
            field.ValidatorCssClass = errorCssClass;

            if (disabled)
            {
                //Attributes.Add("disabled", "disabled") - a select, unlike a text box, was truly
                //disabled rather than made read-only.
                field.Disabled = true;
            }

            using (DataTable table = GetTable(catalog, tableSchema, tableName, tableColumn, displayViews, useDisplayViewsAsParent))
            {
                SetDisplayFields(catalog, field, table, tableSchema, tableName, tableColumn, displayFields);

                ScrudItemSelectorLink itemSelectorAnchor = GetItemSelector(catalog, field.ElementId, itemSelectorPath, tableSchema, tableName, tableColumn, displayViews, resourceClassName, label);

                if (itemSelectorAnchor != null && disabled)
                {
                    //Attributes.Add("style", "pointer-events:none;")
                    itemSelectorAnchor.Disabled = true;
                }

                field.ItemSelector = itemSelectorAnchor;

                SetSelectedValue(catalog, field, tableSchema, tableName, tableColumn, defaultValue, selectedValues);

                if (isNullable)
                {
                    field.Items.Insert(0, new ScrudListItem(String.Empty, String.Empty));
                    field.Label = label;
                    fieldTable.Add(field);
                }
                else
                {
                    field.Label = label + Titles.RequiredFieldIndicator;
                    field.AddRule(ScrudFactoryHelper.GetRequiredFieldValidator(field, errorCssClass));
                    fieldTable.Add(field);
                }
            }
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        ///     Port of <c>GetDropDownList(string id)</c>. <c>ClientIDMode.Static</c> is replaced by
        ///     the <see cref="ScrudField.ElementId" /> contract — the select renders with the
        ///     unmangled <c>{column}_dropdownlist</c> id that <c>GetFormCollection</c> reads back by.
        /// </summary>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        private static ScrudField GetDropDownList(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return null;
            }

            ScrudField field = new ScrudField();
            field.ElementId = id;
            field.Kind = ScrudFieldKind.DropDownList;

            return field;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        ///     Ported unchanged — it is pure string logic over the scrud <c>ItemSeparator</c> and
        ///     <c>ExpressionSeparator</c> parameters. Given a comma-separated list of
        ///     <c>schema.table.column--&gt;expression</c> entries it returns the expression belonging
        ///     to one fully qualified relation, or an empty string.
        /// </summary>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        private static string GetExpressionValue(string catalog, string expressions, string schema, string table, string column)
        {
            if (new[] {expressions, schema, table, column}.AnyNullOrWhitespace())
            {
                return string.Empty;
            }

            //Fully qualified relation name (PostgreSQL Terminology).
            string relation = schema + "." + table + "." + column;

            char itemSeparator = char.Parse(DbConfig.GetScrudParameter(catalog, "ItemSeparator"));
            string expressionSeparator = DbConfig.GetScrudParameter(catalog, "ExpressionSeparator");

            foreach (string item in expressions.Split(itemSeparator))
            {
                //First, trim the field to remove whitespace.
                string expression = item.Trim();

                //Check whether this expression matches with the fully qualified column name.
                if (expression.StartsWith(relation, StringComparison.OrdinalIgnoreCase))
                {
                    //Remove the column name and expression separator and return the actual expression value.
                    return expression.Replace(relation + expressionSeparator, string.Empty);
                }
                //Probably that was not the field we were looking for.
            }

            return string.Empty;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        ///     Creates the item selector link which basically is an extender of the control. The
        ///     extender is an anchor which, when clicked, will open a popup window which allows
        ///     selection, filtering, search, etc. on the target table.
        ///     <para>
        ///         Port of <c>GetItemSelector</c>. The <c>HtmlAnchor</c> it returned is a removed
        ///         type, so the same attribute set (<c>class</c> from the
        ///         <c>ItemSelectorAnchorCssClass</c> scrud parameter, <c>role="item-selector"</c>,
        ///         <c>tabindex="10000"</c>, <c>data-title</c>) and the same
        ///         <c>?Schema=&amp;View=&amp;AssociatedControlId=&amp;ResourceClassName=</c> query
        ///         string are carried on <see cref="ScrudItemSelectorLink" /> and rendered by
        ///         <c>ScrudFieldRow.razor</c>.
        ///     </para>
        /// </summary>
        /// <param name="catalog"></param>
        /// <param name="associatedControlId">Element id of the drop-down list this control is associated to.</param>
        /// <param name="itemSelectorPath">Item Selector Target Url</param>
        /// <param name="tableSchema">Target Table Schema</param>
        /// <param name="tableName">Target Table</param>
        /// <param name="tableColumn"></param>
        /// <param name="displayViews">Scrud DisplayView Expressions</param>
        /// <param name="resourceClassName">The resource class name containing localization.</param>
        /// <param name="columnNameLocalized">Localized name of the column to which item selector is bound to.</param>
        /// <returns></returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        private static ScrudItemSelectorLink GetItemSelector(string catalog, string associatedControlId, string itemSelectorPath, string tableSchema, string tableName, string tableColumn, string displayViews, string resourceClassName, string columnNameLocalized)
        {
            if (string.IsNullOrWhiteSpace(displayViews))
            {
                return null;
            }

            //Get the expression value of display view from comma separated list of expressions.
            //The expression must be a valid fully qualified table or view name.
            string viewRelation = GetExpressionValue(catalog, displayViews, tableSchema, tableName, tableColumn);

            string schema = viewRelation.Split('.').First();
            string view = viewRelation.Split('.').Last();

            //Sanitize the schema and the view
            schema = Sanitizer.SanitizeIdentifierName(schema);
            view = Sanitizer.SanitizeIdentifierName(view);

            if (string.IsNullOrWhiteSpace(schema) || string.IsNullOrWhiteSpace(view))
            {
                return null;
            }

            ScrudItemSelectorLink itemSelectorAnchor = new ScrudItemSelectorLink();

            itemSelectorAnchor.CssClass = DbConfig.GetScrudParameter(catalog, "ItemSelectorAnchorCssClass");
            itemSelectorAnchor.Title = columnNameLocalized;

            itemSelectorAnchor.Href = itemSelectorPath + "?Schema=" + schema + "&View=" + view +
                                      "&AssociatedControlId=" + associatedControlId + "&ResourceClassName=" + resourceClassName;

            return itemSelectorAnchor;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        ///     Ported unchanged: the parent row set comes either from the display VIEW named by the
        ///     <c>DisplayViews</c> expression (when <paramref name="useDisplayViewsAsParent" />) or
        ///     from the referenced parent TABLE. The fallback to the parent table when the expression
        ///     does not resolve to a sanitizable schema/view pair is preserved.
        /// </summary>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        private static DataTable GetTable(string catalog, string tableSchema, string tableName, string tableColumn, string displayViews, bool useDisplayViewsAsParent)
        {
            if (useDisplayViewsAsParent)
            {
                //Get the expression value of display view from comma separated list of expressions.
                //The expression must be a valid fully qualified table or view name.
                string viewRelation = GetExpressionValue(catalog, displayViews, tableSchema, tableName, tableColumn);

                string schema = viewRelation.Split('.').First();
                string view = viewRelation.Split('.').Last();

                //Sanitize the schema and the view
                schema = Sanitizer.SanitizeIdentifierName(schema);
                view = Sanitizer.SanitizeIdentifierName(view);

                if (string.IsNullOrWhiteSpace(schema) || string.IsNullOrWhiteSpace(view))
                {
                    return FormHelper.GetTable(catalog, tableSchema, tableName, "1");
                }

                return FormHelper.GetTable(catalog, schema, view, "1");
            }

            return FormHelper.GetTable(catalog, tableSchema, tableName, "1");
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        ///     Port of <c>SetDisplayFields</c>. The original set
        ///     <c>DataSource</c>/<c>DataValueField</c>/<c>DataTextField</c> and called
        ///     <c>DataBind()</c>; there is no <c>ListControl</c> data-binding machinery on net10.0,
        ///     so the option list is materialized here from the same two columns — which is exactly
        ///     what <c>DataBind()</c> did.
        ///     <para>
        ///         The computed-column trick is preserved verbatim: when the display-field expression
        ///         is not an existing column it is added to the <see cref="DataTable" /> as a
        ///         computed column named <c>DataTextField</c> (so, for example,
        ///         <c>first_name || ' ' || last_name</c> becomes the option text).
        ///     </para>
        /// </summary>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        private static void SetDisplayFields(string catalog, ScrudField field, DataTable table, string tableSchema, string tableName, string tableColumn, string displayFields)
        {
            //See DisplayFields Property for more information.

            if (table == null || field == null)
            {
                return;
            }

            if (table.Rows.Count > 0)
            {
                //Get the expression value of display field from comma separated list of expressions.
                //The expression can be either the column name or a column expression.
                string columnOrExpression = GetExpressionValue(catalog, displayFields, tableSchema, tableName, tableColumn);

                //Let's check whether the display field expression really exists.
                //If it does not exist, it is probably an expression column.
                if (!table.Columns.Contains(columnOrExpression))
                {
                    //Add the expression as a new column in the datatable.
                    table.Columns.Add("DataTextField", typeof(string), columnOrExpression);
                    columnOrExpression = "DataTextField";
                }

                //DataBind(): one option per row, DataValueField -> value, DataTextField -> text.
                foreach (DataRow row in table.Rows)
                {
                    string value = table.Columns.Contains(tableColumn) && row[tableColumn] != DBNull.Value
                        ? row[tableColumn].ToString()
                        : string.Empty;

                    string text = row[columnOrExpression] == DBNull.Value
                        ? string.Empty
                        : row[columnOrExpression].ToString();

                    field.Items.Add(new ScrudListItem(text, value));
                }
            }
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        ///     Port of <c>SetSelectedValue</c>, branch for branch: a postback value wins outright;
        ///     otherwise the value is taken from the <c>SelectedValues</c> expression for this
        ///     relation, and an expression value wrapped in single quotes is resolved by matching the
        ///     option's TEXT (the former <c>ListItem.FindByText</c>) instead of its value.
        ///     <para>
        ///         <c>DropDownList.SelectedValue = x</c> becomes
        ///         <see cref="ScrudField.Value" /> = x plus the matching option marked selected —
        ///         which is what the rendered <c>&lt;option selected&gt;</c> and
        ///         <c>GetFormCollection</c> both read.
        ///     </para>
        /// </summary>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        private static void SetSelectedValue(string catalog, ScrudField field, string schema, string table, string column, string postbackValue, string selectedValueExpressions)
        {
            string selectedItemValue = string.Empty;

            if (field == null || new[] {schema, table, column}.AnyNullOrWhitespace())
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(postbackValue) && string.IsNullOrWhiteSpace(selectedValueExpressions))
            {
                return;
            }

            //If the post back contains a value, skip finding value from expressions.
            if (!string.IsNullOrWhiteSpace(postbackValue))
            {
                selectedItemValue = postbackValue;
            }
            else
            {
                //Find value from expressions.
                if (!string.IsNullOrWhiteSpace(selectedValueExpressions))
                {
                    string value = GetExpressionValue(catalog, selectedValueExpressions, schema, table, column);

                    if (value.StartsWith("'", StringComparison.OrdinalIgnoreCase))
                    {
                        //If the value starts with a quote, find the value by the text.
                        ScrudListItem item = FindByText(field, value.Replace("'", ""));

                        if (item != null)
                        {
                            selectedItemValue = item.Value;
                        }
                    }
                    else
                    {
                        selectedItemValue = value;
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(selectedItemValue))
            {
                field.Value = selectedItemValue;

                foreach (ScrudListItem item in field.Items)
                {
                    item.Selected = string.Equals(item.Value, selectedItemValue, StringComparison.Ordinal);
                }
            }
        }

        /// <summary>
        ///     Port of <c>ListItemCollection.FindByText(string)</c>, which matched on the item's
        ///     exact text.
        /// </summary>
        private static ScrudListItem FindByText(ScrudField field, string text)
        {
            return field.Items.FirstOrDefault(item => string.Equals(item.Text, text, StringComparison.Ordinal));
        }
    }
}
