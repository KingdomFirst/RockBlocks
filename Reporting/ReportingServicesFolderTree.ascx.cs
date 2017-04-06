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
    [Description( "SQL Server Reporting Services Tree View" )]

    [BooleanField( "Show Hidden Items", "Determines if hidden items should be displayed. Default is false.", false, "Configuration", 1, "ShowHiddenItems" )]
    [BooleanField( "Show Child Items", "Determines if child items should be displayed. Default is true", true, "Configuration", 0, "ShowChildItems" )]
    [TextField( "Root Folder", "Root/Base Folder", false, "/", "Configuration", 2, "RootFolder" )]
    [CustomRadioListField("Selection Mode", "Reporting Services Tree selection mode.", "Folder,Report", true, "Folder", "Configuration", 1, "SelectionMode")]
    public partial class ReportingServicesFolderTree : RockBlock
    {
        bool showHiddenItems = false;
        bool showChildItems = false;
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
                hfSelectedItem.Value = folderPath;
            }
            BuildTree();
        }

        private void BuildTree()
        {
           
            ReportingServiceItem rsItem = null;
            if ( hfSelectionType.Value.Equals( "Folder" ) )
            {
                rsItem = ReportingServiceItem.GetFoldersTree( rootFolder, showChildItems, showHiddenItems );
            }
            else if(hfSelectionType.Value.Equals("Report"))
            {
                rsItem = ReportingServiceItem.GetReportTree( rootFolder, showChildItems, showHiddenItems );
            }

            var treeBuilder = new StringBuilder();

            treeBuilder.AppendLine( "<ul id=\"treeview\">" );

            BuildTreeNode( rsItem, ref treeBuilder );

            treeBuilder.AppendLine( "</ul>" );
            lFolders.Text = treeBuilder.ToString();
        }

        private void BuildTreeNode( ReportingServiceItem item, ref StringBuilder treeBuilder )
        {
            string iconClass;
            string nodeIdPrefix;
            switch ( item.Type )
            {
                case ItemType.Folder:
                    iconClass = "fa-folder-o";
                    nodeIdPrefix = "f-";
                    break;
                case ItemType.Report:
                    iconClass = "fa-file-text-o";
                    nodeIdPrefix = "r-";
                    break;
                default:
                    return;
                  
            }
            string nodeId = string.Concat( nodeIdPrefix, item.Path.Replace( "/", "_" ) );
            
            treeBuilder.AppendFormat(
                "<li data-expanded=\"{0}\"  data-modal=\"RSItem\" data-id=\"{1}\" data-type=\"{5}\"><span><span class=\"rollover-container\"><i class=\"fa {4}\">&nbsp;</i>{2}</span></span>{3}",
                ( hfSelectedItem.Value.Contains( nodeId ) ).ToString().ToLower(),
                nodeId,
                item.Name,
                Environment.NewLine,
                iconClass,
                item.Type.ToString());

            if ( item.Children != null && item.Children.Where(c => c.Type != ItemType.DataSource).Count()> 0 )
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
            showHiddenItems = GetAttributeValue( "ShowHiddenItems" ).AsBoolean();
            showChildItems = GetAttributeValue( "ShowChildItems" ).AsBoolean();
            rootFolder = GetAttributeValue( "RootFolder" );
            hfSelectionType.Value = GetAttributeValue( "SelectionMode" );


        }
    }
}