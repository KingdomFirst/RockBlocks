// <copyright>
// Copyright 2026 by Kingdom First Solutions
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
namespace RockWeb.Plugins.rocks_kfs.Intacct
{
    using System;
    using System.IO;
    using System.Text;
    using OfficeOpenXml;
    using Rock.Utility;

    public partial class IntacctExcelExport : System.Web.UI.Page
    {
        protected void Page_Load( object sender, EventArgs e )
        {
            if ( !Request.IsAuthenticated )
            {
                litPlaceholder.Text = "You must be logged in to export batches.";
                return;
            }

            if ( Session["IntacctExcelExport"] != null && !string.IsNullOrEmpty( Session["IntacctExcelExport"].ToString() ) && Session["IntacctFileId"] != null && !string.IsNullOrEmpty( Session["IntacctFileId"].ToString() ) )
            {
                var filename = string.Format( "Intacct_{0}.xlsx", Session["IntacctFileId"] );
                var excel = Session["IntacctExcelExport"] as ExcelPackage;

                Session["IntacctExcelExport"] = null;
                Session["IntacctFileId"] = null;

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