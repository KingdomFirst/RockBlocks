// <copyright>
// Copyright by the Spark Development Network
//
// Licensed under the Rock Community License (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.rockrms.com/license
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>
//
// <notice>
// This file contains modifications by Kingdom First Solutions
// and is a derivative work.
//
// Modification (including but not limited to):
// * Modified Share Workflow to be able to be used to share Pages instead.
// </notice>
//
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Web.UI;
using System.Web.UI.WebControls;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Utility.EntityCoding;
using Rock.Web;
using Rock.Web.Cache;
using Rock.Web.UI;

namespace RockWeb.Plugins.rocks_kfs.Utility
{
    [DisplayName( "Share Page" )]
    [Category( "KFS > Utility" )]
    [Description( "Export and import pages from Rock." )]
    public partial class SharePage : Rock.Web.UI.RockBlock
    {
        #region Base Method Overrides

        /// <summary>
        /// Initialize basic information about the page structure and setup the default content.
        /// </summary>
        /// <param name="sender">Object that is generating this event.</param>
        /// <param name="e">Arguments that describe this event.</param>
        protected void Page_Load( object sender, EventArgs e )
        {
            ScriptManager.GetCurrent( this.Page ).RegisterPostBackControl( btnExport );
            if ( !Page.IsPostBack )
            {
                ppExport.SetValue( PageParameter( "PageId" ).AsInteger() );
            }
        }

        #endregion

        #region Core Methods

        /// <summary>
        /// Binds the preview grid.
        /// </summary>
        protected void BindPreviewGrid()
        {
            List<PreviewEntity> previewEntities = ( List<PreviewEntity> ) ViewState["PreviewEntities"];

            if ( previewEntities == null )
            {
                previewEntities = new List<PreviewEntity>();
            }

            var query = previewEntities.AsQueryable();

            if ( gPreview.SortProperty != null )
            {
                query = query.Sort( gPreview.SortProperty );
            }

            gPreview.DataSource = query;
            gPreview.DataBind();
        }

        /// <summary>
        /// Get a friendly name for the entity, optionally including the short name for the
        /// entity type. This attempts a ToString() on the entity and if that returns what
        /// appears to be a valid name (no &lt; character and less than 40 characters) then
        /// it is used as the name. Otherwise the Guid is used for the name.
        /// </summary>
        /// <param name="entity">The entity whose name we wish to retrieve.</param>
        /// <returns>A string that can be displayed to the user to identify this entity.</returns>
        static protected string EntityFriendlyName( IEntity entity )
        {
            string name;

            name = entity.ToString();
            if ( name.Length > 40 || name.Contains( "<" ) )
            {
                name = entity.Guid.ToString();
            }

            return name;
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Handles the Click event of the btnPreview control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void btnPreview_Click( object sender, EventArgs e )
        {

            // Clean-up UI
            gPreview.Visible = true;
            ltImportResults.Text = string.Empty;

            RockContext rockContext = new RockContext();
            var pageService = new PageService( rockContext );
            var page = pageService.Get( ppExport.SelectedValueAsId().Value );
            var coder = new EntityCoder( new RockContext() );
            var exporter = new PageExporter();
            coder.EnqueueEntity( page, exporter );

            List<PreviewEntity> previewEntities = new List<PreviewEntity>();

            foreach ( var qe in coder.Entities )
            {
                string shortType = CodingHelper.GetEntityType( qe.Entity ).Name;

                if ( shortType == "Attribute" || shortType == "AttributeValue" || shortType == "AttributeQualifier" || shortType == "WorkflowActionFormAttribute" )
                {
                    continue;
                }

                var preview = new PreviewEntity
                {
                    Guid = qe.Entity.Guid,
                    Name = EntityFriendlyName( qe.Entity ),
                    ShortType = shortType,
                    IsCritical = qe.IsCritical,
                    IsNewGuid = qe.RequiresNewGuid,
                    Paths = qe.ReferencePaths.Select( p => p.ToString() ).ToList()
                };

                previewEntities.Add( preview );
            }

            ViewState["PreviewEntities"] = previewEntities;

            BindPreviewGrid();
        }

        /// <summary>
        /// Handles the Click event of the btnExport control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void btnExport_Click( object sender, EventArgs e )
        {
            RockContext rockContext = new RockContext();
            var pageService = new PageService( rockContext );
            var page = pageService.Get( ppExport.SelectedValueAsId().Value );
            var coder = new EntityCoder( new RockContext() );
            coder.EnqueueEntity( page, new PageExporter() );

            var container = coder.GetExportedEntities();

            var htmlContentIndexes = new Dictionary<EncodedEntity, EncodedEntity>();
            foreach ( var entity in container.Entities.Where( et => et.EntityType == "Rock.Model.HtmlContent" ) )
            {
                var index = container.Entities.IndexOf( entity );
                var nextBlock = container.Entities.Skip( index ).FirstOrDefault( b => b.EntityType == "Rock.Model.Block" );
                htmlContentIndexes.Add( entity, nextBlock );
            }

            foreach ( var indexPair in htmlContentIndexes )
            {
                var tempEntity = indexPair.Key;
                var tempBlock = indexPair.Value;
                container.Entities.Remove( tempEntity );
                container.Entities.Insert( container.Entities.IndexOf( tempBlock ) + 1, tempEntity );
            }

            Page.EnableViewState = false;
            Page.Response.Clear();
            Page.Response.ContentType = "application/json";
            Page.Response.AppendHeader( "Content-Disposition", string.Format( "attachment; filename=\"{0}_{1}.json\"", page.PageTitle.MakeValidFileName(), RockDateTime.Now.ToString( "yyyyMMddHHmm" ) ) );
            Page.Response.Write( Newtonsoft.Json.JsonConvert.SerializeObject( container, Newtonsoft.Json.Formatting.Indented ) );
            Page.Response.Flush();
            Page.Response.End();
        }

        /// <summary>
        /// Handles the Click event of the lbImport control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void lbImport_Click( object sender, EventArgs e )
        {
            gPreview.Visible = false;

            if ( !fuImport.BinaryFileId.HasValue )
            {
                return;
            }

            using ( var rockContext = new RockContext() )
            {
                var binaryFileService = new BinaryFileService( rockContext );
                var binaryFile = binaryFileService.Get( fuImport.BinaryFileId ?? 0 );
                var pageService = new PageService( rockContext );

                var container = Newtonsoft.Json.JsonConvert.DeserializeObject<ExportedEntitiesContainer>( binaryFile.ContentsToString() );
                List<string> messages;

                var decoder = new EntityDecoder( new RockContext() );
                if ( ppImportParentPage.SelectedValueAsId().HasValue )
                {
                    var selectedParentPage = pageService.Get( ppImportParentPage.SelectedValueAsId().Value );
                    decoder.UserValues.Add( "PageParent", selectedParentPage );
                }

                var success = decoder.Import( container, cbDryRun.Checked, out messages );

                ltImportResults.Text = string.Empty;
                foreach ( var msg in messages )
                {
                    ltImportResults.Text += string.Format( "{0}\n", msg.EncodeHtml() );
                }

                pnlImportResults.Visible = true;

                if ( success )
                {
                    fuImport.BinaryFileId = null;
                }
            }
        }

        /// <summary>
        /// Handles the GridRebind event of the gPreview control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="Rock.Web.UI.Controls.GridRebindEventArgs"/> instance containing the event data.</param>
        protected void gPreview_GridRebind( object sender, Rock.Web.UI.Controls.GridRebindEventArgs e )
        {
            BindPreviewGrid();
        }

        #endregion

        [Serializable]
        protected class PreviewEntity
        {
            public Guid Guid { get; set; }

            public string Name { get; set; }

            public string ShortType { get; set; }

            public bool IsCritical { get; set; }

            public bool IsNewGuid { get; set; }

            public List<string> Paths { get; set; }
        }

        /// <summary>
        /// Defines the rules for exporting a Page.
        /// </summary>
        /// <seealso cref="EntityCoding.IExporter" />
        public class PageExporter : IExporter
        {
            /// <summary>
            /// Determines if the entity at the given path requires a new Guid value when it's imported
            /// onto the target system. On import, if an entity of that type and Guid already exists then
            /// it is not imported and a reference to the existing entity is used instead.
            /// </summary>
            /// <param name="path">The path to the queued entity object that is being checked.</param>
            /// <returns>
            ///   <c>true</c> if the path requires a new Guid value; otherwise, <c>false</c>
            /// </returns>
            public bool DoesPathNeedNewGuid( EntityPath path )
            {
                return ( path == "" ||
                    path == "AttributeTypes" ||
                    path == "AttributeTypes.AttributeQualifiers" ||
                    path == "AttributeValues" ||
                    path == "Blocks" ||
                    path == "Blocks.AttributeTypes" ||
                    path == "Blocks.AttributeValues" ||
                    path == "Pages" ||
                    path.ToString().EndsWith( "Pages.AttributeTypes" ) ||
                    path.ToString().EndsWith( "Pages.AttributeTypes.AttributeQualifiers" ) ||
                    path.ToString().EndsWith( "Pages.AttributeValues" ) ||
                    path.ToString().EndsWith( "Pages.Blocks" ) ||
                    path.ToString().EndsWith( "Pages.Blocks.AttributeTypes" ) ||
                    path.ToString().EndsWith( "Pages.Blocks.AttributeValues" ) ||
                    path.ToString().EndsWith( "Blocks.HtmlContents" ) ||
                    path.ToString().EndsWith( "Pages.Pages" ) );
            }

            /// <summary>
            /// Gets any custom references for the entity at the given path.
            /// </summary>
            /// <param name="parentEntity">The entity that will later be encoded.</param>
            /// <param name="path">The path to the parent entity.</param>
            /// <returns>
            /// A collection of references that should be applied to the encoded entity.
            /// </returns>
            public ICollection<Reference> GetUserReferencesForPath( IEntity parentEntity, EntityPath path )
            {
                if ( path == "" )
                {
                    return new List<Reference>
                {
                    Reference.UserDefinedReference( "ParentPageId", "PageParent" )
                };
                }

                return null;
            }

            /// <summary>
            /// Determines whether the path to an entity should be considered critical. A critical
            /// entity is one that MUST exist on the target system in order for the export/import to
            /// succeed, as such a critical entity is always included.
            /// </summary>
            /// <param name="path">The path to the queued entity object that is being checked.</param>
            /// <returns>
            ///   <c>true</c> if the path is critical; otherwise, <c>false</c>.
            /// </returns>
            public bool IsPathCritical( EntityPath path )
            {
                return ( DoesPathNeedNewGuid( path ) );
            }

            /// <summary>
            /// Determines if the property at the given path should be followed to it's referenced entity.
            /// This is called for both referenced entities and child entities.
            /// </summary>
            /// <param name="path">The path.</param>
            /// <returns></returns>
            public bool ShouldFollowPathProperty( EntityPath path )
            {
                if ( path == "ParentPageId" )
                {
                    return false;
                }
                if ( path == "SiteId" )
                {
                    return false;
                }
                if ( path.ToString().EndsWith( "LayoutId.SiteId" ) )
                {
                    return false;
                }
                if ( path.ToString().EndsWith( "Pages.SiteId" ) )
                {
                    return false;
                }
                if ( path.ToString().EndsWith( "HtmlContents.ApprovedByPersonAliasId" ) )
                {
                    return false;
                }

                if ( path.Count > 0 )
                {
                    var lastComponent = path.Last();

                    if ( lastComponent.Entity.TypeName == "Rock.Model.DefinedType" && lastComponent.PropertyName == "DefinedValues" )
                    {
                        return false;
                    }
                }

                return true;
            }
        }
    }
}
