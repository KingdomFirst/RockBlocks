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
namespace RockWeb.Plugins.rocks_kfs.FinancialEdge
{
    using System;
    using System.IO;
    using System.Text;

    public partial class FinancialEdgeCsvExport : System.Web.UI.Page
    {
        protected void Page_Load( object sender, EventArgs e )
        {
            if ( !Request.IsAuthenticated )
            {
                litPlaceholder.Text = "You must be logged in to export batches.";
                return;
            }

            if ( Session["FinancialEdgeCsvExport"] != null && !string.IsNullOrEmpty( Session["FinancialEdgeCsvExport"].ToString() ) && Session["FinancialEdgeFileId"] != null && !string.IsNullOrEmpty( Session["FinancialEdgeFileId"].ToString() ) )
            {
                var filename = string.Format( "FinancialEdge_{0}.csv", Session["FinancialEdgeFileId"] );
                var output = Session["FinancialEdgeCsvExport"].ToString();
                Session["FinancialEdgeCsvExport"] = null;
                Session["FinancialEdgeFileId"] = null;
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
