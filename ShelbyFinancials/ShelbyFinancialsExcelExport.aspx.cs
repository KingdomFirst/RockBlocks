// <copyright>
// Copyright 2019 by Kingdom First Solutions
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>
//
namespace RockWeb.Plugins.rocks_kfs.ShelbyFinancials
{
    using System;
    using System.IO;
    using System.Text;
    using OfficeOpenXml;
    using Rock.Utility;

    public partial class ShelbyFinancialsExcelExport : System.Web.UI.Page
    {
        protected void Page_Load( object sender, EventArgs e )
        {
            if ( !Request.IsAuthenticated )
            {
                litPlaceholder.Text = "You must be logged in to export batches.";
                return;
            }

            if ( Session["ShelbyFinancialsExcelExport"] != null && !string.IsNullOrEmpty( Session["ShelbyFinancialsExcelExport"].ToString() ) && Session["ShelbyFinancialsFileId"] != null && !string.IsNullOrEmpty( Session["ShelbyFinancialsFileId"].ToString() ) )
            {
                var filename = string.Format( "ShelbyFinancials_{0}.xlsx", Session["ShelbyFinancialsFileId"] );
                var excel = Session["ShelbyFinancialsExcelExport"] as ExcelPackage;

                Session["ShelbyFinancialsExcelExport"] = null;
                Session["ShelbyFinancialsFileId"] = null;

                Response.Clear();
                Response.ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                Response.AppendHeader( "Content-Disposition", "attachment; filename=" + filename );
                Response.Charset = string.Empty;
                Response.BinaryWrite( excel.ToByteArray() );
                Response.Flush();
                Response.End();
            }
        }
    }
}