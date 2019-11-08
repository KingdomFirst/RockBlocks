namespace RockWeb.Plugins.com_kfs.Finance
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using System.Text;
	using System.Web;
	using System.Web.UI;
	using System.Web.UI.WebControls;

	public partial class GLExport : System.Web.UI.Page
	{
		const string _prefix = "ContributionGeneralLedgerExport_";
		protected void Page_Load(object sender, EventArgs e)
		{
			if (!Request.IsAuthenticated)
			{
                litPlaceholder.Text = "You must be logged in to export batches.";
				return;
			}

			if (Session["GLExportLineItems"] != null && !string.IsNullOrEmpty(Session["GLExportLineItems"].ToString()))
			{
				string output = Session["GLExportLineItems"].ToString();
				Session["GLExportLineItems"] = null;
				MemoryStream ms = new MemoryStream(Encoding.ASCII.GetBytes(output));
				Response.ClearContent();
				Response.ClearHeaders();
				Response.ContentType = "application/x-unknown";
				Response.AddHeader("Content-Disposition", "attachment; filename=GLTRN2000.txt");
				ms.WriteTo(Response.OutputStream);
				Response.End();
			}
		}
	}
}