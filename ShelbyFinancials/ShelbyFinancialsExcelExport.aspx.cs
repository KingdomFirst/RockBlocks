namespace RockWeb.Plugins.com_kfs.ShelbyFinancials
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