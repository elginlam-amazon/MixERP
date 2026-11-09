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

using System.Collections.ObjectModel;
using System.Data;

namespace MixERP.Net.WebControls.ReportEngine
{
    public partial class Report : WebControlBase
    {
        private dynamic bodyContentsLiteral;
        private dynamic bottomSectionLiteral;
        private dynamic closeImageButton;

        /// <summary>
        ///     CommandPanel is displayed at the top of the report.
        /// </summary>
        private dynamic commandPanel;

        private Collection<DataTable> dataTableCollection;
        private Collection<string> decimalFieldIndicesCollection;
        private bool disposed;
        private dynamic emailImageButton;
        private dynamic filterImageButton;
        private dynamic goBottomImageButton;
        private dynamic goTopImageButton;
        private dynamic gridPlaceHolder;
        private ReportHeader header;
        private dynamic printImageButton;

        /// <summary>
        ///     ReportBody is displayed below the CommandPanel and contains the following sections:
        ///     1. Report Header
        ///     2. Report Title
        ///     3. Top Section
        ///     4. Report Details (Grid)
        ///     5. Bottom Section
        /// </summary>
        private dynamic reportBody;

        private dynamic reportContainer;
        private dynamic reportHidden;
        private string reportPath;
        private dynamic reportTitleHidden;
        private dynamic reportTitleLiteral;
        private Collection<string> runningTotalFieldIndicesCollection;
        private Collection<int> runningTotalTextColumnIndexCollection;
        private dynamic topSectionLiteral;

        public void Dispose()
        {
            if (!this.disposed)
            {
                this.Dispose(true);
                base.Dispose();
            }
        }

        private void Dispose(bool disposing)
        {
            if (!disposing)
            {
                return;
            }

            if (this.bodyContentsLiteral != null)
            {
                this.bodyContentsLiteral.Dispose();
                this.bodyContentsLiteral = null;
            }

            if (this.bottomSectionLiteral != null)
            {
                this.bottomSectionLiteral.Dispose();
                this.bottomSectionLiteral = null;
            }

            if (this.closeImageButton != null)
            {
                this.closeImageButton.Dispose();
                this.closeImageButton = null;
            }

            if (this.commandPanel != null)
            {
                this.commandPanel.Dispose();
                this.commandPanel = null;
            }

            if (this.dataTableCollection != null && dataTableCollection.Count > 0)
            {
                for (int i = 0; i < dataTableCollection.Count; i++)
                {
                    if (dataTableCollection[i] != null)
                    {
                        dataTableCollection[i].Dispose();
                        dataTableCollection[i] = null;
                    }
                }

                dataTableCollection = null;
            }

            if (this.emailImageButton != null)
            {
                this.emailImageButton.Dispose();
                this.emailImageButton = null;
            }

            if (this.filterImageButton != null)
            {
                this.filterImageButton.Dispose();
                this.filterImageButton = null;
            }

            if (this.goBottomImageButton != null)
            {
                this.goBottomImageButton.Dispose();
                this.goBottomImageButton = null;
            }

            if (this.goTopImageButton != null)
            {
                this.goTopImageButton.Dispose();
                this.goTopImageButton = null;
            }

            if (this.gridPlaceHolder != null)
            {
                this.gridPlaceHolder.Dispose();
                this.gridPlaceHolder = null;
            }

            if (this.header != null)
            {
                this.header.Dispose();
                this.header = null;
            }

            if (this.printImageButton != null)
            {
                this.printImageButton.Dispose();
                this.printImageButton = null;
            }

            if (this.reportBody != null)
            {
                this.reportBody.Dispose();
                this.reportBody = null;
            }

            if (this.reportContainer != null)
            {
                this.reportContainer.Dispose();
                this.reportContainer = null;
            }


            if (this.reportHidden != null)
            {
                this.reportHidden.Dispose();
                this.reportHidden = null;
            }


            if (this.reportTitleHidden != null)
            {
                this.reportTitleHidden.Dispose();
                this.reportTitleHidden = null;
            }


            if (this.reportTitleLiteral != null)
            {
                this.reportTitleLiteral.Dispose();
                this.reportTitleLiteral = null;
            }


            if (this.topSectionLiteral != null)
            {
                this.topSectionLiteral.Dispose();
                this.topSectionLiteral = null;
            }

            this.disposed = true;
        }
    }
}