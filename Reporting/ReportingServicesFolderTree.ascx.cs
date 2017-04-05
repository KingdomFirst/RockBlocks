using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

using com.kfs.Reporting.SQLReportingServices;

using Rock;
using Rock.Attribute;
using Rock.Web.UI;
using Rock.Web.UI.Controls;


namespace RockWeb.Plugins.com_kfs.Reporting
{
    [DisplayName( "Reporting Services Folder Tree" )]
    [Category( "KFS > Reporting" )]
    [Description( "SQL Server Reporting Services Folder Tree View" )]

    [BooleanField( "Show Hidden Folders", "Determines if hidden folders should be displayed. Default is false.", false, "Configuration", 1, "ShowHiddenFolders" )]
    [BooleanField( "Show Child Folders", "Determines if child folders/reecursion should be displayed. Default is true", true, "Configuration", 0, "ShowChildFolders" )]
    [TextField( "Root Folder", "Root/Base Folder", false, "/", "Configuration", 2, "RootFolder" )]
    public partial class ReportingServicesFolderTree : RockBlock
    {
        bool showHiddenFolders = false;
        bool showChildFolders = false;
        string rootFolder = null;

        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );
        }

        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );
            LoadAttributes();

            if ( !Page.IsPostBack )
            {
                string folderPath = PageParameter( "selectedpath" );
                hfSelectedFolder.Value = folderPath;
            }
            BuildTree();
        }

        private void BuildTree()
        {
            var folder = ReportingServiceItem.GetFoldersTree( rootFolder, showChildFolders, showHiddenFolders );

            var treeBuilder = new StringBuilder();

            treeBuilder.AppendLine( "<ul id=\"treeview\">" );

            BuildTreeNode( folder, ref treeBuilder );

            treeBuilder.AppendLine( "</ul>" );
            lFolders.Text = treeBuilder.ToString();
        }

        private void BuildTreeNode( ReportingServiceItem item, ref StringBuilder treeBuilder )
        {
            string nodeId = item.Path.Replace( "/", "_" );
            treeBuilder.AppendFormat(
                "<li data-expanded=\"{0}\"  data-modal=\"RSFolder\" data-id=\"{1}\"><span><span class=\"rollover-container\"><i class=\"fa fa-folder-o\">&nbsp;</i>{2}</span></span>{3}",
                ( hfSelectedFolder.Value.Contains( nodeId ) ).ToString().ToLower(),
                nodeId,
                item.Name,
                Environment.NewLine );

            if ( item.Children.Count() > 0 )
            {
                treeBuilder.AppendLine( "<ul>" );
                foreach ( ReportingServiceItem child in item.Children )
                {
                    BuildTreeNode( child, ref treeBuilder );
                }
                treeBuilder.AppendLine( "</ul>" );
            }
            treeBuilder.AppendLine( "</li>" );
        }

        private void LoadAttributes()
        {
            showHiddenFolders = GetAttributeValue( "ShowHiddenFolders" ).AsBoolean();
            showChildFolders = GetAttributeValue( "ShowChildFolders" ).AsBoolean();
            rootFolder = GetAttributeValue( "RootFolder" );


        }
    }
}