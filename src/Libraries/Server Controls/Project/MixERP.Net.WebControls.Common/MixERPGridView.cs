using MixERP.Net.Common;
using MixERP.Net.i18n.Resources;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace MixERP.Net.WebControls.Common
{
    public sealed class MixERPGridView : WebControlBase
    {
        private readonly bool htmlEncode;

        public new string CssClass { get; set; }
        public dynamic GridLines { get; set; }
        public new string EmptyDataText { get; set; }
        public new List<object> Rows { get; } = new List<object>();
        public object HeaderRow { get; set; }
        public event EventHandler<System.Web.UI.WebControls.GridViewRowEventArgs> RowDataBound;
        public event EventHandler<System.Web.UI.WebControls.GridViewRowEventArgs> DataBound;
        public event EventHandler<System.Web.UI.WebControls.GridViewRowEventArgs> RowCreated;
        public dynamic BorderStyle { get; set; }

        public MixERPGridView()
        {
            this.htmlEncode = true;
            this.Initialize();
        }

        public MixERPGridView(bool htmlEncode)
        {
            this.htmlEncode = htmlEncode;
            this.Initialize();
        }

        private void Initialize()
        {
            this.CssClass = "ui striped table ";
            this.EmptyDataText = CommonResource.NoRecordFound;
        }

        protected void OnDataBound(EventArgs e) { }

        protected void OnRowDataBound(object e) { }
    }
}
