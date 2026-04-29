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
    using Rock.Utility;

    public partial class IntacctCsvExport : System.Web.UI.Page
    {
        protected void Page_Load( object sender, EventArgs e )
        {
            if ( !Request.IsAuthenticated )
            {
                litPlaceholder.Text = "You must be logged in to export batches.";
                return;
            }

            if ( Session["IntacctCsvExport"] != null && !string.IsNullOrEmpty( Session["IntacctCsvExport"].ToString() ) && Session["IntacctFileId"] != null && !string.IsNullOrEmpty( Session["IntacctFileId"].ToString() ) )
            {
                var filename = string.Format( "Intacct_{0}.csv", Session["IntacctFileId"] );
                var output = Session["IntacctCsvExport"].ToString();
                Session["IntacctCsvExport"] = null;
                Session["IntacctFileId"] = null;
                var ms = new MemoryStream( Encoding.ASCII.GetBytes( output ) );
                Response.ClearContent();
                Response.ClearHeaders();
                Response.ContentType = "text/csv";
                Response.AddHeader( "Content-Disposition", string.Format( "attachment; filename={0}", filename ) );
                ms.WriteTo( Response.OutputStream );
                Response.End();
            }
        }
    }
}