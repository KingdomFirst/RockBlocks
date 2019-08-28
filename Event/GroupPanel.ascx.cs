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
// * For use with KFS Advanced Events.
// </notice>
//
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Entity;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;

using Rock;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Web.UI.Controls;

namespace RockWeb.Plugins.rocks_kfs.Event
{
    #region Block Attributes

    [DisplayName( "Advanced Registration Group Detail" )]
    [Category( "KFS > Advanced Event Registration" )]
    [Description( "The Group Panel that displays for resource subgroups." )]

    #endregion

    /// <summary>
    /// The KFS Group Panel that displays for resource subgroups
    /// </summary>
    /// <see cref="RockWeb.Blocks.Groups.GroupMemberList"/>

    public partial class GroupPanel : UserControl
    {
        #region Private Variables

        private bool _expanded = false;
        private Group _group;
        private List<GroupTypeCache> _resourceTypes;
        private Dictionary<string, AttributeValueCache> _resources;

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the available attributes.
        /// </summary>
        /// <value>
        /// The available attributes.
        /// </value>
        public List<AttributeCache> AvailableAttributes { get; set; }

        /// <summary>
        /// Gets or sets the group.
        /// </summary>
        /// <value>
        /// The group.
        /// </value>
        public Group Group
        {
            get { return _group; }
            set { _group = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="KFSGroupPanel"/> is expanded.
        /// </summary>
        /// <value>
        ///   <c>true</c> if expanded; otherwise, <c>false</c>.
        /// </value>
        public bool Expanded
        {
            get { return _expanded; }
            set { _expanded = value; }
        }

        #endregion

        #region Events

        /// <summary>
        /// Occurs when [add button click].
        /// </summary>
        public event EventHandler AddButtonClick;

        /// <summary>
        /// Occurs when [edit button click].
        /// </summary>
        public event EventHandler EditButtonClick;

        /// <summary>
        /// Occurs when [delete button click].
        /// </summary>
        public event EventHandler DeleteButtonClick;

        /// <summary>
        /// Occurs when [assign group click].
        /// </summary>
        public event GridViewCommandEventHandler GroupRowCommand;

        /// <summary>
        /// Occurs when [row data bound].
        /// </summary>
        public event EventHandler GroupRowDataBound;

        /// <summary>
        /// Occurs when [group grid rebind].
        /// </summary>
        public event GridRebindEventHandler GroupGridRebind;

        #endregion

        #region Internal Methods

        

        /// <summary>
        /// Restores the view-state information from a previous user control request that was saved by the <see cref="M:System.Web.UI.UserControl.SaveViewState" /> method.
        /// </summary>
        /// <param name="savedState">An <see cref="T:System.Object" /> that represents the user control state to be restored.</param>
        protected override void LoadViewState( object savedState )
        {
            base.LoadViewState( savedState );
            AddDynamicControls();
        }

        /// <summary>
        /// Builds the control.
        /// </summary>
        /// <param name="group">The group.</param>
        /// <param name="resourceTypes">The resource types.</param>
        /// <param name="resources">The resources.</param>
        public void BuildControl( Group group, List<GroupTypeCache> resourceTypes, Dictionary<string, AttributeValueCache> resources )
        {
            if ( _group == null )
            {
                _group = group;
            }

            if ( _resourceTypes == null )
            {
                _resourceTypes = resourceTypes;
            }

            if ( _resources == null )
            {
                _resources = resources;
            }

            // Set up group panel
            SetGroupHeader( group );

            pnlGroupMembers.DataKeyNames = new string[] { "Id" };
            pnlGroupMembers.PersonIdField = "PersonId";
            pnlGroupMembers.RowSelected += pnlGroupMembers_EditClick; 
            pnlGroupMembers.RowDataBound += pnlGroupMembers_RowDataBound;
            pnlGroupMembers.GridRebind += pnlGroupMembers_GridRebind;
            pnlGroupMembers.Actions.AddClick += pnlGroupMembers_AddClick;
            pnlGroupMembers.GetRecipientMergeFields += pnlGroupMembers_GetRecipientMergeFields;
            pnlGroupMembers.RowItemText = _group.GroupType.GroupTerm + " " + _group.GroupType.GroupMemberTerm;
            pnlGroupMembers.ExportFilename = _group.Name;
            pnlGroupMembers.ExportSource = ExcelExportSource.ColumnOutput;
            pnlGroupMembers.AllowPaging = false;

            // custom javascript (see GroupMemberList.ascx ) handles deletes instead
            pnlGroupMembers.ShowConfirmDeleteDialog = false;
            pnlGroupMembers.Actions.ShowMergePerson = false;
            
            pnlGroupMembers.Actions.ShowAdd = true;
            pnlGroupMembers.IsDeleteEnabled = true;

            // if group is being sync'ed remove ability to add/delete members
            if ( _group != null && _group.GroupSyncs != null && _group.GroupSyncs.Count() > 0 )
            {
                pnlGroupMembers.IsDeleteEnabled = false;
                pnlGroupMembers.Actions.ShowAdd = false;
                hlSyncStatus.Visible = true;

                // link to the DataView
                var pageId = PageCache.Get( Rock.SystemGuid.Page.DATA_VIEWS.AsGuid() ).Id;
                var dataViewId = _group.GroupSyncs.FirstOrDefault().SyncDataViewId;
                hlSyncSource.NavigateUrl = System.Web.VirtualPathUtility.ToAbsolute( string.Format( "~/page/{0}?DataViewId={1}", pageId, dataViewId ) );
            }

            AddDynamicControls();
            BindGroupMembersGrid();
        }

        /// <summary>
        /// Builds the subgroup heading.
        /// </summary>
        /// <param name="group">The group.</param>
        private void SetGroupHeader( Group group )
        {
            var groupIconString = string.Empty;
            if ( !string.IsNullOrWhiteSpace( group.GroupType.IconCssClass ) )
            {
                pnlSubGroup.TitleIconCssClass = group.GroupType.IconCssClass;
            }

            pnlSubGroup.Title = string.Format( "<span class='span-panel-heading'>{0}</span>{1}{2}", group.Name, group.Description.Length > 0 ? " - " : string.Empty, group.Description.Truncate( 50 ) );
            var memCount = group.Members.Where( m => m.GroupMemberStatus == GroupMemberStatus.Active || m.GroupMemberStatus == GroupMemberStatus.Pending )
                .Where( m => !m.GroupRole.IsLeader ).Count();
            if ( group.GroupCapacity.HasValue && group.GroupCapacity > 0 )
            {
                var capacityRatio = string.Empty;
                if ( group.GroupCapacity == memCount )
                {
                    capacityRatio = string.Format( "&nbsp&nbsp<span class='label label-warning'>{0}/{1}</span>", memCount, group.GroupCapacity );
                }
                else if ( group.GroupCapacity < memCount )
                {
                    capacityRatio = string.Format( "&nbsp&nbsp<span class='label label-danger'>{0}/{1}</span>", memCount, group.GroupCapacity );
                }
                else
                {
                    capacityRatio = string.Format( "&nbsp&nbsp<span class='label label-success'>{0}/{1}</span>", memCount, group.GroupCapacity );
                }
                pnlSubGroup.Title += capacityRatio;
            }

            pnlSubGroup.Expanded = _expanded;
            lbGroupEdit.CommandArgument = string.Format( "{0}|{1}", group.Id.ToString(), group.Name );
            lbGroupDelete.CommandArgument = group.Id.ToString();
            pnlSubGroup.DataBind();
        }

        /// <summary>
        /// Adds the attribute columns.
        /// </summary>
        private void AddDynamicControls()
        {
            // remove Family Campus columns
            foreach ( var column in pnlGroupMembers.Columns
                .OfType<RockLiteralField>()
                .ToList() )
            {
                pnlGroupMembers.Columns.Remove( column );
            }

            // remove Group member attribute columns
            foreach ( var column in pnlGroupMembers.Columns
                .OfType<AttributeField>().ToList() )
            {
                pnlGroupMembers.Columns.Remove( column );
            }

            // remove Group member assignment columns
            foreach ( var column in pnlGroupMembers.Columns
                .OfType<LinkButtonField>().ToList() )
            {
                pnlGroupMembers.Columns.Remove( column );
            }

            // remove the edit field
            foreach ( var column in pnlGroupMembers.Columns
                .OfType<EditField>().ToList() )
            {
                pnlGroupMembers.Columns.Remove( column );
            }

            // Remove the delete field
            foreach ( var column in pnlGroupMembers.Columns
                .OfType<DeleteField>()
                .ToList() )
            {
                pnlGroupMembers.Columns.Remove( column );
            }

            // Set up group member attribute columns
            var rockContext = new RockContext();
            var attributeValueService = new AttributeValueService( rockContext );
            foreach ( var attribute in GetGroupAttributes() )
            {
                // add the attribute data column
                var attributeColumn = pnlGroupMembers.Columns.OfType<AttributeField>().FirstOrDefault( a => a.AttributeId == attribute.Id );
                if ( attributeColumn == null )
                {
                    var boundField = new AttributeField
                    {
                        DataField = attribute.Id + attribute.Key,
                        AttributeId = attribute.Id,
                        HeaderText = attribute.Name,
                        SortExpression = string.Format( "attribute:{0}", attribute.Id ),
                    };

                    boundField.ItemStyle.HorizontalAlign = HorizontalAlign.Center;

                    decimal needsFilled = 0;
                    if ( attribute.FieldType != null )
                    {
                        var groupMemberIds = _group.Members.Select( m => m.Id ).ToList();
                        var attributeValues = attributeValueService.GetByAttributeId( attribute.Id )
                            .Where( v => groupMemberIds.Contains( (int)v.EntityId )  && !( v.Value == null || v.Value.Trim() == string.Empty ) )
                            .Select( v => v.Value ).ToList();

                        // if the values are numeric, sum a number value
                        if ( attribute.FieldType.Guid.Equals( Rock.SystemGuid.FieldType.INTEGER.AsGuid() ) || attribute.FieldType.Guid.Equals( Rock.SystemGuid.FieldType.DECIMAL.AsGuid() ) )
                        {
                            needsFilled = attributeValues.Sum( v => v.AsDecimal() );
                        }
                        else if ( attribute.FieldType.Guid.Equals( Rock.SystemGuid.FieldType.MULTI_SELECT.AsGuid() ) )
                        {
                            // handles checkboxes and non-empty strings
                            needsFilled = attributeValues.Count( v => !string.IsNullOrWhiteSpace( v ) );
                        }
                        else
                        {
                            // handles single select and boolean
                            needsFilled = attributeValues.Count( v => v.AsBoolean() );
                        }
                    }

                    if ( needsFilled > 0 )
                    {
                        pnlGroupMembers.ShowFooter = true;
                        boundField.FooterText = needsFilled.ToString();
                    }

                    pnlGroupMembers.Columns.Add( boundField );
                }

                if ( pnlGroupMembers.ShowFooter )
                {
                    pnlGroupMembers.Columns[1].FooterText = "Total";
                    pnlGroupMembers.Columns[1].FooterStyle.HorizontalAlign = HorizontalAlign.Left;
                }
            }

            // Add dynamic assignment columns for volunteer groups
            if ( _resources != null && _group.GroupType.GroupTypePurposeValue != null && _group.GroupType.GroupTypePurposeValue.Value == "Serving Area" )
            {
                foreach ( var groupType in _resourceTypes.Where( gt => gt.GetAttributeValue( "AllowVolunteerAssignment" ).AsBoolean( true ) ) )
                {
                    if ( _resources.ContainsKey( groupType.Name ) )
                    {
                        var resourceGroupGuid = _resources[groupType.Name];
                        if ( resourceGroupGuid != null && !string.IsNullOrWhiteSpace( resourceGroupGuid.Value ) )
                        {
                            var parentGroup = new GroupService( rockContext ).Get( resourceGroupGuid.Value.AsGuid() );
                            if ( parentGroup != null && parentGroup.GroupTypeId != _group.GroupTypeId )
                            {
                                var groupAssignment = new LinkButtonField();
                                groupAssignment.ItemStyle.HorizontalAlign = HorizontalAlign.Center;
                                groupAssignment.ExcelExportBehavior = ExcelExportBehavior.NeverInclude;
                                groupAssignment.HeaderText = parentGroup.Name;
                                groupAssignment.HeaderStyle.CssClass = "";
                                pnlGroupMembers.Columns.Add( groupAssignment );

                                var assignmentExport = new RockLiteralField();
                                assignmentExport.ID = string.Format( "lAssignments_{0}", groupType.Id );
                                assignmentExport.ItemStyle.HorizontalAlign = HorizontalAlign.Center;
                                assignmentExport.ExcelExportBehavior = ExcelExportBehavior.AlwaysInclude;
                                assignmentExport.HeaderStyle.CssClass = "";
                                assignmentExport.HeaderText = parentGroup.Name;
                                assignmentExport.Visible = false;
                                pnlGroupMembers.Columns.Add( assignmentExport );
                            }
                        }
                    }
                }
            }

            // Add edit column
            var editField = new EditField();
            pnlGroupMembers.Columns.Add( editField );
            editField.Click += pnlGroupMembers_EditClick;

            // Add delete column
            var deleteField = new DeleteField();
            pnlGroupMembers.Columns.Add( deleteField );
            deleteField.Click += pnlGroupMembers_DeleteClick;
        }

        /// <summary>
        /// Makes the key unique to group.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns></returns>
        private string MakeKeyUniqueToGroup( string key )
        {
            if ( _group != null )
            {
                return string.Format( "{0}-{1}", _group.Id, key );
            }

            return key;
        }

        /// <summary>
        /// Resolves the values.
        /// </summary>
        /// <param name="values">The values.</param>
        /// <param name="checkBoxList">The check box list.</param>
        /// <returns></returns>
        private string ResolveValues( string values, System.Web.UI.WebControls.CheckBoxList checkBoxList )
        {
            var resolvedValues = new List<string>();

            foreach ( string value in values.Split( ';' ) )
            {
                var item = checkBoxList.Items.FindByValue( value );
                if ( item != null )
                {
                    resolvedValues.Add( item.Text );
                }
            }

            return resolvedValues.AsDelimited( ", " );
        }

        #endregion

        #region Bind Methods

        /// <summary>
        /// Binds the group members grid.
        /// </summary>
        /// <remarks>Multiple methods depend on the GroupMember object type</remarks>
        /// <param name="isExporting">if set to <c>true</c> [is exporting].</param>
        protected void BindGroupMembersGrid( bool isExporting = false )
        {
            if ( _group != null && _group.GroupType.Roles.Any() )
            {
                pnlGroupMembers.Visible = true;

                using ( var rockContext = new RockContext() )
                {
                    // Start query for group members
                    var qry = new GroupMemberService( rockContext )
                        .Queryable( "Person,GroupRole", true )
                        .Where( m => m.GroupId == _group.Id && m.Person != null );

                    // Sort the query
                    IOrderedQueryable<GroupMember> orderedQry = null;
                    var sortProperty = pnlGroupMembers.SortProperty;
                    if ( sortProperty != null )
                    {
                        orderedQry = qry.Sort( sortProperty );
                    }
                    else
                    {
                        orderedQry = qry
                            .OrderBy( r => r.Person.LastName )
                            .ThenBy( r => r.Person.NickName );
                    }

                    // increase the timeout just in case. A complex filter on the grid might slow things down
                    rockContext.Database.CommandTimeout = 180;

                    // Set the grids LinqDataSource which will run query and set results for current page
                    pnlGroupMembers.SetLinqDataSource( orderedQry );

                    var homePhoneType = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.PERSON_PHONE_TYPE_HOME );
                    var cellPhoneType = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.PERSON_PHONE_TYPE_MOBILE );

                    var groupAttributes = GetGroupAttributes();
                    if ( groupAttributes.Any() )
                    {
                        // Get the query results for the current page
                        var currentGroupMembers = pnlGroupMembers.DataSource as List<GroupMember>;
                        if ( currentGroupMembers != null )
                        {
                            // Get all the person ids in current page of query results
                            var personIds = currentGroupMembers
                                .Select( r => r.PersonId )
                                .Distinct()
                                .ToList();

                            var groupMemberIds = currentGroupMembers
                                .Select( r => r.Id )
                                .Distinct()
                                .ToList();

                            var groupMemberAttributesIds = groupAttributes.Select( a => a.Id ).Distinct().ToList();

                            // If there are any attributes that were selected to be displayed, we're going
                            // to try and read all attribute values in one query and then put them into a
                            // custom grid ObjectList property so that the AttributeField columns don't need
                            // to do the LoadAttributes and querying of values for each row/column
                            if ( groupMemberAttributesIds.Any() )
                            {
                                // Query the attribute values for all rows and attributes
                                var attributeValues = new AttributeValueService( rockContext )
                                    .Queryable( "Attribute" ).AsNoTracking()
                                    .Where( v =>
                                        v.EntityId.HasValue &&
                                        groupMemberAttributesIds.Contains( v.AttributeId ) &&
                                        groupMemberIds.Contains( v.EntityId.Value )
                                    )
                                    .ToList();

                                // Get the attributes to add to each row's object
                                var attributes = new Dictionary<string, AttributeCache>();
                                groupAttributes.ForEach( a =>
                                    attributes.Add( a.Id + a.Key, a ) );

                                // Initialize the grid's object list
                                pnlGroupMembers.ObjectList = new Dictionary<string, object>();
                                pnlGroupMembers.EntityTypeId = EntityTypeCache.Get( Rock.SystemGuid.EntityType.GROUP_MEMBER.AsGuid() ).Id;

                                // Loop through each of the current group's members and build an attribute
                                // field object for storing attributes and the values for each of the members
                                foreach ( var groupMember in currentGroupMembers )
                                {
                                    // Create a row attribute object
                                    var attributeFieldObject = new AttributeFieldObject
                                    {
                                        // Add the attributes to the attribute object
                                        Attributes = attributes
                                    };

                                    // Add any group member attribute values to object
                                    if ( groupMember.Id > 0 )
                                    {
                                        attributeValues
                                            .Where( v =>
                                                groupMemberAttributesIds.Contains( v.AttributeId ) &&
                                                v.EntityId.Value == groupMember.Id )
                                            .ToList()
                                            .ForEach( v => attributeFieldObject.AttributeValues
                                                .Add( v.AttributeId + v.Attribute.Key, new AttributeValueCache( v ) ) );
                                    }

                                    // Add row attribute object to grid's object list
                                    pnlGroupMembers.ObjectList.Add( groupMember.Id.ToString(), attributeFieldObject );
                                }
                            }
                        }
                    }

                    pnlGroupMembers.DataBind();
                }
            }
        }

        /// <summary>
        /// Handles the GetRecipientMergeFields event of the pnlGroupMembers control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="GetRecipientMergeFieldsEventArgs"/> instance containing the event data.</param>
        private void pnlGroupMembers_GetRecipientMergeFields( object sender, GetRecipientMergeFieldsEventArgs e )
        {
            dynamic groupMember = e.DataItem;
            if ( groupMember != null )
            {
                e.MergeValues.Add( "GroupRole", groupMember.GroupRole );
                e.MergeValues.Add( "GroupMemberStatus", ( (GroupMemberStatus)groupMember.GroupMemberStatus ).ConvertToString() );
            }
        }

        /// <summary>
        /// Gets the group attributes.
        /// </summary>
        private List<AttributeCache> GetGroupAttributes()
        {
            if ( AvailableAttributes != null )
            {
                return AvailableAttributes;
            }

            var updatedAttributes = new List<AttributeCache>();
            if ( _group != null )
            {
                var rockContext = new RockContext();
                var groupMemberTypeId = new GroupMember().TypeId;
                var groupQualifier = _group.Id.ToString();
                var groupTypeQualifier = _group.GroupTypeId.ToString();
                foreach ( var attribute in new AttributeService( rockContext ).Queryable()
                    .Where( a =>
                        a.EntityTypeId == groupMemberTypeId &&
                        a.IsGridColumn &&
                        ( ( a.EntityTypeQualifierColumn.Equals( "GroupId", StringComparison.OrdinalIgnoreCase ) && a.EntityTypeQualifierValue.Equals( groupQualifier ) ) ) )
                    .OrderByDescending( a => a.EntityTypeQualifierColumn )
                    .ThenBy( a => a.Order )
                    .ThenBy( a => a.Name )
                    .Select( a => a.Guid ) )
                {
                    updatedAttributes.Add( AttributeCache.Get( attribute, rockContext ) );
                }

                var inheritedAttributes = ( new GroupMember { GroupId = _group.Id } ).GetInheritedAttributes( rockContext ).Where( a => a.IsGridColumn && a.IsActive ).ToList();
                if ( inheritedAttributes.Count > 0 )
                {
                    updatedAttributes.AddRange( inheritedAttributes );
                }
            }

            AvailableAttributes = updatedAttributes;
            return updatedAttributes;
        }
        
        #endregion

        #region Event Methods

        /// <summary>
        /// Handles the GridRebind event of the pnlGroupMembers control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="GridRebindEventArgs" /> instance containing the event data.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        protected void pnlGroupMembers_GridRebind( object sender, GridRebindEventArgs e )
        {
            if ( GroupGridRebind != null )
            {
                GroupGridRebind( this, e );
            }

            //BindGroupMembersGrid( e.IsExporting );
        }

        /// <summary>
        /// Handles the AddClick event of the Actions control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void pnlGroupMembers_AddClick( object sender, EventArgs e )
        {
            if ( AddButtonClick != null )
            {
                AddButtonClick( this, EventArgs.Empty );
            }
        }

        /// <summary>
        /// Handles the EditClick event of the pnlGroupMembers control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RowEventArgs"/> instance containing the event data.</param>
        private void pnlGroupMembers_EditClick( object sender, RowEventArgs e )
        {
            if ( EditButtonClick != null )
            {
                EditButtonClick( this, e );
            }
        }

        /// <summary>
        /// Handles the DeleteClick event of the pnlGroupMembers control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RowEventArgs"/> instance containing the event data.</param>
        private void pnlGroupMembers_DeleteClick( object sender, RowEventArgs e )
        {
            if ( DeleteButtonClick != null )
            {
                DeleteButtonClick( this, e );
                BindGroupMembersGrid();
                SetGroupHeader( _group );
            }
        }

        /// <summary>
        /// Handles the RowDataBound event of the pnlGroupMembers control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void pnlGroupMembers_RowDataBound( object sender, EventArgs e )
        {
            if ( GroupRowDataBound != null )
            {
                GroupRowDataBound( this, e as GridViewRowEventArgs );
            }
        }

        #endregion
    }
}
