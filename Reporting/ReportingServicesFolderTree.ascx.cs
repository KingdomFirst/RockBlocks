using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

using Rock;
using Rock.Attribute;
using Rock.Web.UI;
namespace RockWeb.Plugins.com_kfs.Reporting
{
    [DisplayName( "Reporting Services Folder Tree" )]
    [Category( "KFS > Reporting" )]
    [Description("SQL Server Reporting Services Folder Tree View")]

    [BooleanField("Show Hidden Folders", "Determines if hidden folders should be displayed. Default is false.", false, "Configuration", 1, "ShowHiddenFolders")]
    [BooleanField("Show Child Folders", "Determines if child folders/reecursion should be displayed. Default is true", true, "Configuration", 0, "ShowChildFolders")]
    [TextField("Root Folder", "Root/Base Folder", false, "/", "Configuration", 2, "RootFolder")]
    public partial class ReportingServicesFolderTree : RockBlock
    {
        protected void Page_Load( object sender, EventArgs e )
        {
            if(!Page.IsPostBack)
            {
                LoadConfigurationFields();

            }
        }

        private void LoadConfigurationFields()
        {
            hfShowHidden.Value = GetAttributeValue( "ShowHiddenFolders" );
            hfRecursive.Value = GetAttributeValue( "ShowChildFolders" );
            hfRootPath.Value = GetAttributeValue( "RootFolder" ).UrlEncode();
            //lPath.Text = ResolveUrl( "~/api/com.kfs/ReportingServices/GetFolderList/" );
        }
    }
}