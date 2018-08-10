namespace RockWeb.Plugins.com_kfs.FinancialEdge
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

            if ( Session["FinancialEdgeCsvExport"] != null && !string.IsNullOrEmpty( Session["FinancialEdgeCsvExport"].ToString() ) && Session["FinancialEdgeBatchId"] != null && !string.IsNullOrEmpty( Session["FinancialEdgeBatchId"].ToString() ) )
            {
                var filename = string.Format( "FinancialEdge_{0}.csv", Session["FinancialEdgeBatchId"] );
                var output = Session["FinancialEdgeCsvExport"].ToString();
                Session["FinancialEdgeCsvExport"] = null;
                Session["FinancialEdgeBatchId"] = null;
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