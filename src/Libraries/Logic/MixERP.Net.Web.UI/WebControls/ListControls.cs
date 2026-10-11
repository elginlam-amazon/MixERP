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
along with MixERP.  If not, see <http://www.gnu.org/licenses />.
***********************************************************************************/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using MixERP.Net.Web.UI.HtmlControls;

namespace MixERP.Net.Web.UI.WebControls
{
    public sealed class ListItem
    {
        private string text;
        private string value;

        public ListItem() : this(string.Empty, null)
        {
        }

        public ListItem(string text) : this(text, null)
        {
        }

        public ListItem(string text, string value) : this(text, value, true)
        {
        }

        public ListItem(string text, string value, bool enabled)
        {
            this.text = text;
            this.value = value;
            this.Enabled = enabled;
            this.Attributes = new AttributeCollection();
        }

        public AttributeCollection Attributes { get; }
        public bool Enabled { get; set; }
        public bool Selected { get; set; }

        public string Text
        {
            get { return this.text ?? this.value ?? string.Empty; }
            set { this.text = value; }
        }

        public string Value
        {
            get { return this.value ?? this.text ?? string.Empty; }
            set { this.value = value; }
        }

        public static ListItem FromString(string s)
        {
            return new ListItem(s);
        }

        public override string ToString()
        {
            return this.Text;
        }

        internal void RenderOption(HtmlTextWriter writer)
        {
            writer.AddAttribute(HtmlTextWriterAttribute.Value, this.Value);

            if (this.Selected)
            {
                writer.AddAttribute(HtmlTextWriterAttribute.Selected, "selected");
            }

            if (!this.Enabled)
            {
                writer.AddAttribute(HtmlTextWriterAttribute.Disabled, "disabled");
            }

            this.Attributes.AddAttributes(writer);
            writer.RenderBeginTag(HtmlTextWriterTag.Option);
            writer.WriteEncodedText(this.Text);
            writer.RenderEndTag();
        }
    }

    public sealed class ListItemCollection : IList<ListItem>, IList
    {
        private readonly List<ListItem> items = new List<ListItem>();

        public int Count => this.items.Count;
        public bool IsReadOnly => false;
        public bool IsFixedSize => false;
        public bool IsSynchronized => false;
        public object SyncRoot => this;

        public ListItem this[int index]
        {
            get { return this.items[index]; }
            set { this.items[index] = value; }
        }

        object IList.this[int index]
        {
            get { return this.items[index]; }
            set { this.items[index] = (ListItem)value; }
        }

        public void Add(ListItem item)
        {
            this.items.Add(item);
        }

        public void Add(string item)
        {
            this.items.Add(new ListItem(item));
        }

        int IList.Add(object value)
        {
            this.items.Add(value as ListItem ?? new ListItem(Convert.ToString(value, CultureInfo.CurrentCulture)));
            return this.items.Count - 1;
        }

        public void AddRange(ListItem[] range)
        {
            this.items.AddRange(range);
        }

        public void Insert(int index, ListItem item)
        {
            this.items.Insert(index, item);
        }

        public void Insert(int index, string item)
        {
            this.items.Insert(index, new ListItem(item));
        }

        void IList.Insert(int index, object value)
        {
            this.items.Insert(index, (ListItem)value);
        }

        public bool Remove(ListItem item)
        {
            return this.items.Remove(item);
        }

        public void Remove(string item)
        {
            ListItem found = this.FindByText(item) ?? this.FindByValue(item);

            if (found != null)
            {
                this.items.Remove(found);
            }
        }

        void IList.Remove(object value)
        {
            this.items.Remove((ListItem)value);
        }

        public void RemoveAt(int index)
        {
            this.items.RemoveAt(index);
        }

        public void Clear()
        {
            this.items.Clear();
        }

        public bool Contains(ListItem item)
        {
            return this.items.Contains(item);
        }

        bool IList.Contains(object value)
        {
            return this.items.Contains(value as ListItem);
        }

        public int IndexOf(ListItem item)
        {
            return this.items.IndexOf(item);
        }

        int IList.IndexOf(object value)
        {
            return this.items.IndexOf(value as ListItem);
        }

        public ListItem FindByText(string text)
        {
            return this.items.FirstOrDefault(i => string.Equals(i.Text, text, StringComparison.Ordinal));
        }

        public ListItem FindByValue(string value)
        {
            return this.items.FirstOrDefault(i => string.Equals(i.Value, value, StringComparison.Ordinal));
        }

        public void CopyTo(ListItem[] array, int arrayIndex)
        {
            this.items.CopyTo(array, arrayIndex);
        }

        public void CopyTo(Array array, int index)
        {
            ((ICollection)this.items).CopyTo(array, index);
        }

        public IEnumerator<ListItem> GetEnumerator()
        {
            return this.items.ToList().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }

    /// <summary>
    ///     Base class of list controls (DropDownList, ListBox, RadioButtonList, CheckBoxList).
    /// </summary>
    public abstract class ListControl : WebControl, IPostBackDataHandler
    {
        protected ListControl(HtmlTextWriterTag tag) : base(tag)
        {
            this.Items = new ListItemCollection();
        }

        public event EventHandler SelectedIndexChanged;
        public event EventHandler TextChanged;
        public event EventHandler DataBound;

        public virtual ListItemCollection Items { get; }
        public virtual object DataSource { get; set; }
        public virtual string DataMember { get; set; }
        public virtual string DataTextField { get; set; } = string.Empty;
        public virtual string DataValueField { get; set; } = string.Empty;
        public virtual string DataTextFormatString { get; set; } = string.Empty;
        public virtual bool AppendDataBoundItems { get; set; }
        public virtual bool AutoPostBack { get; set; }
        public virtual bool CausesValidation { get; set; }
        public virtual string ValidationGroup { get; set; }

        protected virtual bool AllowsMultipleSelection => false;

        public virtual int SelectedIndex
        {
            get
            {
                for (int i = 0; i < this.Items.Count; i++)
                {
                    if (this.Items[i].Selected)
                    {
                        return i;
                    }
                }

                return -1;
            }
            set
            {
                for (int i = 0; i < this.Items.Count; i++)
                {
                    this.Items[i].Selected = i == value;
                }
            }
        }

        public virtual ListItem SelectedItem
        {
            get
            {
                int index = this.SelectedIndex;
                return index >= 0 ? this.Items[index] : null;
            }
        }

        public virtual string SelectedValue
        {
            get { return this.SelectedItem?.Value ?? string.Empty; }
            set
            {
                ListItem match = this.Items.FindByValue(value);

                if (match == null)
                {
                    return;
                }

                foreach (ListItem item in this.Items)
                {
                    item.Selected = ReferenceEquals(item, match);
                }
            }
        }

        public virtual string Text
        {
            get { return this.SelectedValue; }
            set { this.SelectedValue = value; }
        }

        public virtual void ClearSelection()
        {
            foreach (ListItem item in this.Items)
            {
                item.Selected = false;
            }
        }

        public override void DataBind()
        {
            this.OnDataBinding(EventArgs.Empty);
            IEnumerable source = DataSourceAccess.GetEnumerable(this.DataSource, this.DataMember);

            if (source != null)
            {
                if (!this.AppendDataBoundItems)
                {
                    this.Items.Clear();
                }

                foreach (object item in source)
                {
                    string text;
                    string value;

                    if (string.IsNullOrEmpty(this.DataTextField) && string.IsNullOrEmpty(this.DataValueField))
                    {
                        text = Convert.ToString(item, CultureInfo.CurrentCulture);
                        value = text;
                    }
                    else
                    {
                        object textValue = DataSourceAccess.GetObject(item, string.IsNullOrEmpty(this.DataTextField) ? this.DataValueField : this.DataTextField);
                        text = string.IsNullOrEmpty(this.DataTextFormatString)
                            ? Convert.ToString(textValue, CultureInfo.CurrentCulture)
                            : string.Format(CultureInfo.CurrentCulture, this.DataTextFormatString, textValue);
                        value = Convert.ToString(DataSourceAccess.GetObject(item, string.IsNullOrEmpty(this.DataValueField) ? this.DataTextField : this.DataValueField),
                            CultureInfo.CurrentCulture);
                    }

                    this.Items.Add(new ListItem(text, value));
                }
            }

            foreach (Control child in this.Controls.ToArray())
            {
                child.DataBind();
            }

            this.DataBound?.Invoke(this, EventArgs.Empty);
        }

        public virtual bool LoadPostData(string postDataKey, NameValueCollection postCollection)
        {
            string[] values = postCollection.GetValues(postDataKey);

            if (values == null)
            {
                return false;
            }

            int before = this.SelectedIndex;

            foreach (ListItem item in this.Items)
            {
                item.Selected = values.Contains(item.Value) && (this.AllowsMultipleSelection || values[0] == item.Value);
            }

            if (!this.AllowsMultipleSelection)
            {
                bool found = false;

                foreach (ListItem item in this.Items)
                {
                    if (item.Selected && !found)
                    {
                        found = true;
                        continue;
                    }

                    item.Selected = false;
                }
            }

            return before != this.SelectedIndex;
        }

        public virtual void RaisePostDataChangedEvent()
        {
            this.SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
            this.TextChanged?.Invoke(this, EventArgs.Empty);
        }

        protected override void AddAttributesToRender(HtmlTextWriter writer)
        {
            if (this.AutoPostBack && this.Page != null)
            {
                writer.AddAttribute(HtmlTextWriterAttribute.Onchange, this.Page.ClientScript.GetPostBackEventReference(this, string.Empty));
            }

            base.AddAttributesToRender(writer);
        }
    }

    public class DropDownList : ListControl
    {
        public DropDownList() : base(HtmlTextWriterTag.Select)
        {
        }

        public override int SelectedIndex
        {
            get
            {
                int index = base.SelectedIndex;
                return index < 0 && this.Items.Count > 0 ? 0 : index;
            }
            set { base.SelectedIndex = value; }
        }

        protected override void AddAttributesToRender(HtmlTextWriter writer)
        {
            writer.AddAttribute(HtmlTextWriterAttribute.Name, this.UniqueID);
            base.AddAttributesToRender(writer);
        }

        protected internal override void RenderContents(HtmlTextWriter writer)
        {
            foreach (ListItem item in this.Items)
            {
                item.RenderOption(writer);
            }
        }
    }

    public enum ListSelectionMode
    {
        Single = 0,
        Multiple = 1
    }

    public class ListBox : ListControl
    {
        public ListBox() : base(HtmlTextWriterTag.Select)
        {
        }

        public virtual ListSelectionMode SelectionMode { get; set; }
        public virtual int Rows { get; set; } = 4;

        protected override bool AllowsMultipleSelection => this.SelectionMode == ListSelectionMode.Multiple;

        protected override void AddAttributesToRender(HtmlTextWriter writer)
        {
            writer.AddAttribute(HtmlTextWriterAttribute.Name, this.UniqueID);
            writer.AddAttribute(HtmlTextWriterAttribute.Size, this.Rows.ToString(CultureInfo.InvariantCulture));

            if (this.AllowsMultipleSelection)
            {
                writer.AddAttribute(HtmlTextWriterAttribute.Multiple, "multiple");
            }

            base.AddAttributesToRender(writer);
        }

        protected internal override void RenderContents(HtmlTextWriter writer)
        {
            foreach (ListItem item in this.Items)
            {
                item.RenderOption(writer);
            }
        }
    }

    public enum RepeatDirection
    {
        Horizontal = 0,
        Vertical = 1
    }

    public enum RepeatLayout
    {
        Table = 0,
        Flow = 1,
        UnorderedList = 2,
        OrderedList = 3
    }

    /// <summary>
    ///     Base of RadioButtonList and CheckBoxList; renders each item as an input plus label.
    /// </summary>
    public abstract class RepeatingListControl : ListControl, INamingContainer
    {
        protected RepeatingListControl() : base(HtmlTextWriterTag.Span)
        {
        }

        public virtual RepeatDirection RepeatDirection { get; set; } = RepeatDirection.Vertical;
        public virtual RepeatLayout RepeatLayout { get; set; } = RepeatLayout.Flow;
        public virtual int RepeatColumns { get; set; }

        protected abstract string InputType { get; }

        protected virtual string GetItemName(int index)
        {
            return this.UniqueID;
        }

        protected internal override void RenderContents(HtmlTextWriter writer)
        {
            for (int i = 0; i < this.Items.Count; i++)
            {
                ListItem item = this.Items[i];
                string itemId = this.ClientID + "_" + i.ToString(CultureInfo.InvariantCulture);

                writer.AddAttribute(HtmlTextWriterAttribute.Id, itemId);
                writer.AddAttribute(HtmlTextWriterAttribute.Type, this.InputType);
                writer.AddAttribute(HtmlTextWriterAttribute.Name, this.GetItemName(i));
                writer.AddAttribute(HtmlTextWriterAttribute.Value, item.Value);

                if (item.Selected)
                {
                    writer.AddAttribute(HtmlTextWriterAttribute.Checked, "checked");
                }

                if (!item.Enabled || !this.Enabled)
                {
                    writer.AddAttribute(HtmlTextWriterAttribute.Disabled, "disabled");
                }

                item.Attributes.AddAttributes(writer);
                writer.RenderBeginTag(HtmlTextWriterTag.Input);
                writer.RenderEndTag();

                writer.AddAttribute(HtmlTextWriterAttribute.For, itemId);
                writer.RenderBeginTag(HtmlTextWriterTag.Label);
                writer.WriteEncodedText(item.Text);
                writer.RenderEndTag();

                if (this.RepeatDirection == RepeatDirection.Vertical && i < this.Items.Count - 1)
                {
                    writer.WriteBreak();
                }
            }
        }
    }

    public class RadioButtonList : RepeatingListControl
    {
        protected override string InputType => "radio";
    }

    public class CheckBoxList : RepeatingListControl
    {
        protected override string InputType => "checkbox";
        protected override bool AllowsMultipleSelection => true;

        protected override string GetItemName(int index)
        {
            return this.UniqueID;
        }
    }
}
