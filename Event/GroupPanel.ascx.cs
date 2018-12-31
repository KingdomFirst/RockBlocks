// KFS Group Panel

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

namespace RockWeb.Plugins.com_kfs.Event
{
    /// <summary>
    /// The KFS Group Panel that displays for resource subgroups
    /// </summary>
    /// <see cref="RockWeb.Blocks.Groups.GroupMemberList"/>

    #region Block Attributes

    [DisplayName( "Advanced Registration Group Detail" )]
    [Category( "KFS > Advanced Event Registration" )]
    [Description( "The Group Panel that displays for resource subgroups." )]

    #endregion

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
        /// Occurs when [group row selected].
        /// </summary>
        public event EventHandler GroupRowSelected;

        /// <summary>
        /// Occurs when [assign group click].
        /// </summary>
        public event GridViewCommandEventHandler GroupRowCommand;

        /// <summary>
        /// Occurs when [row data bound].
        /// </summary>
        public event EventHandler GroupRowDataBound;

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

            //rFilter.ApplyFilterClick += rFilter_ApplyFilterClick;
            pnlGroupMembers.DataKeyNames = new string[] { "Id" };
            pnlGroupMembers.PersonIdField = "PersonId";
            pnlGroupMembers.RowDataBound += pnlGroupMembers_RowDataBound;
            pnlGroupMembers.GetRecipientMergeFields += pnlGroupMembers_GetRecipientMergeFields;
            pnlGroupMembers.Actions.AddClick += pnlGroupMembers_AddClick;
            pnlGroupMembers.GridRebind += pnlGroupMembers_GridRebind;
            pnlGroupMembers.RowItemText = _group.GroupType.GroupTerm + " " + _group.GroupType.GroupMemberTerm;
            pnlGroupMembers.ExportFilename = _group.Name;
            pnlGroupMembers.ExportSource = ExcelExportSource.ColumnOutput;
            pnlGroupMembers.AllowPaging = false;

            // we'll have custom javascript (see GroupMemberList.ascx ) do this instead
            pnlGroupMembers.ShowConfirmDeleteDialog = false;
            pnlGroupMembers.Actions.ShowMergePerson = false;

            pnlGroupMembers.RowCommand += pnlGroupMembers_RowCommand;
            //pnlGroupMembers.RowSelected += pnlGroupMembers_RowSelected;

            // v7: make sure they have Auth to edit the block OR edit to the Group
            //bool canEditBlock = IsUserAuthorized( Authorization.EDIT ) || _group.IsAuthorized( Authorization.EDIT, this.CurrentPerson ) || _group.IsAuthorized( Authorization.MANAGE_MEMBERS, this.CurrentPerson );
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
                groupIconString = string.Format( "<i class='{0}'></i> ", group.GroupType.IconCssClass );
            }

            pnlSubGroup.Title = string.Format( "{0} <span class='span-panel-heading'>{1}</span>", groupIconString, group.Name );
            var memCount = group.Members.Count( m => m.GroupMemberStatus == GroupMemberStatus.Active || m.GroupMemberStatus == GroupMemberStatus.Pending );
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

            pnlGroupDescription.Visible = !string.IsNullOrWhiteSpace( group.Description );
            pnlSubGroup.Expanded = _expanded;

            lblGroupDescription.Text = group.Description;
            lbGroupEdit.CommandArgument = group.Id.ToString();
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

            // Add Family Campus
            var lFamilyCampus = new RockLiteralField
            {
                ID = "lFamilyCampus",
                HeaderText = "Family Campus",
                SortExpression = "FamilyCampus"
            };
            pnlGroupMembers.Columns.Add( lFamilyCampus );

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
                        DataField = attribute.Key,
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
        /// <param name="isExporting">if set to <c>true</c> [is exporting].</param>
        protected void BindGroupMembersGrid( bool isExporting = false )
        {
            if ( _group != null && _group.GroupType.Roles.Any() )
            {
                int groupId = _group.Id;
                pnlGroupMembers.Visible = true;

                var rockContext = new RockContext();
                var groupMemberService = new GroupMemberService( rockContext );
                var qry = groupMemberService.Queryable( "Person,GroupRole", true ).AsNoTracking()
                    .Where( m => m.GroupId == _group.Id );

                var photoFormat = "<div class=\"photo-icon photo-round photo-round-xs pull-left margin-r-sm js-person-popover\" personid=\"{0}\" data-original=\"{1}&w=50\" style=\"background-image: url( '{2}' ); background-size: cover; background-repeat: no-repeat;\"></div>";
                var hasGroupRequirements = new GroupRequirementService( rockContext ).Queryable().Any( a => ( a.GroupId.HasValue && a.GroupId == _group.Id ) || ( a.GroupTypeId.HasValue && a.GroupTypeId == _group.GroupTypeId ) );

                // If there are group requirements that that member doesn't meet, show an icon in the grid
                var includeWarnings = false;
                var groupMemberIdsThatLackGroupRequirements = new GroupService( rockContext ).GroupMembersNotMeetingRequirements( _group, includeWarnings ).Select( a => a.Key.Id );

                List<GroupMember> groupMembersList = null;
                if ( pnlGroupMembers.SortProperty != null )
                {
                    groupMembersList = qry.Sort( pnlGroupMembers.SortProperty ).ToList();
                }
                else
                {
                    groupMembersList = qry.OrderBy( a => a.GroupRole.Order ).ThenBy( a => a.Person.LastName ).ThenBy( a => a.Person.FirstName ).ToList();
                }

                // Since we're not binding to actual group member list, but are using AttributeField columns,
                // we need to save the group members into the grid's object list
                pnlGroupMembers.ObjectList = new Dictionary<string, object>();
                groupMembersList.ForEach( m => pnlGroupMembers.ObjectList.Add( m.Id.ToString(), m ) );
                pnlGroupMembers.EntityTypeId = EntityTypeCache.Get( Rock.SystemGuid.EntityType.GROUP_MEMBER.AsGuid() ).Id;

                var homePhoneType = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.PERSON_PHONE_TYPE_HOME );
                var cellPhoneType = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.PERSON_PHONE_TYPE_MOBILE );

                // If exporting to Excel, the selectAll option will be true, and home location should be calculated
                var homeLocations = new Dictionary<int, Location>();
                if ( isExporting )
                {
                    foreach ( var m in groupMembersList )
                    {
                        homeLocations.Add( m.Id, m.Person.GetHomeLocation( rockContext ) );
                    }
                }

                var groupMemberIds = groupMembersList.Select( m => m.Id ).ToList();
                var dataSource = groupMembersList.Select( m => new GroupMemberDataRow
                {
                    Id = m.Id,
                    Guid = m.Guid,
                    PersonId = m.PersonId,
                    NickName = m.Person.NickName,
                    LastName = m.Person.LastName,
                    Name =
                    isExporting ? m.Person.LastName + ", " + m.Person.NickName : string.Format( photoFormat, m.PersonId, m.Person.PhotoUrl, ResolveUrl( "~/Assets/Images/person-no-photo-unknown.svg" ) )
                                + m.Person.NickName + " " + m.Person.LastName
                        + ( !string.IsNullOrWhiteSpace( m.Person.TopSignalColor ) ? " " + m.Person.GetSignalMarkup() : string.Empty )
                        + ( ( hasGroupRequirements && groupMemberIdsThatLackGroupRequirements.Contains( m.Id ) )
                            ? " <i class='fa fa-exclamation-triangle text-warning'></i>"
                            : string.Empty ),
                    BirthDate = m.Person.BirthDate,
                    Age = m.Person.Age,
                    Email = m.Person.Email,
                    Gender = isExporting ? m.Person.Gender.ToString() : string.Empty,
                    HomePhone = isExporting && homePhoneType != null ?
                        m.Person.PhoneNumbers
                            .Where( p => p.NumberTypeValueId.HasValue && p.NumberTypeValueId.Value == homePhoneType.Id )
                            .Select( p => p.NumberFormatted )
                            .FirstOrDefault() : string.Empty,
                    CellPhone = isExporting && cellPhoneType != null ?
                        m.Person.PhoneNumbers
                            .Where( p => p.NumberTypeValueId.HasValue && p.NumberTypeValueId.Value == cellPhoneType.Id )
                            .Select( p => p.NumberFormatted )
                            .FirstOrDefault() : string.Empty,
                    HomeAddress = homeLocations.ContainsKey( m.Id ) && homeLocations[m.Id] != null ?
                        homeLocations[m.Id].FormattedAddress : string.Empty,
                    GroupRole = m.GroupRole.Name,
                    GroupMemberStatus = m.GroupMemberStatus,
                    GroupRoleId = m.GroupRoleId,
                    IsArchived = m.IsArchived
                } ).ToList();

                pnlGroupMembers.DataSource = dataSource;
                pnlGroupMembers.DataBind();
            }
            else
            {
                pnlGroupMembers.Visible = false;
            }
        }

        /// <summary>
        /// Handles the GridRebind event of the pnlGroupMembers control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="GridRebindEventArgs" /> instance containing the event data.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        protected void pnlGroupMembers_GridRebind( object sender, GridRebindEventArgs e )
        {
            BindGroupMembersGrid( e.IsExporting );
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

        /// <summary>
        /// Registers the script.
        /// Not used; this is already triggered in RegistrationInstanceDetail
        /// </summary>
        private void RegisterScript()
        {
            var deleteScript = @"
    $('table.js-grid-group-members a.grid-delete-button').click(function( e ){
        var $btn = $(this);
        e.preventDefault();
        Rock.dialogs.confirm('Are you sure you want to delete this group member?', function (result) {
            if (result) {
                if ( $btn.closest('tr').hasClass('js-has-registration') ) {
                    Rock.dialogs.confirm('This group member was added through a registration. Are you sure that you want to delete this group member and remove the link from the registration? ', function (result) {
                        if (result) {
                            window.location = e.target.href ? e.target.href : e.target.parentElement.href;
                        }
                    });
                } else {
                    window.location = e.target.href ? e.target.href : e.target.parentElement.href;
                }
            }
        });
    });
";
            ScriptManager.RegisterStartupScript( pnlGroupMembers, pnlGroupMembers.GetType(), "deleteInstanceScript", deleteScript, true );
        }

        #endregion

        #region Click Methods

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
        /// Handles the Click event of the DeleteGroupMember control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RowEventArgs"/> instance containing the event data.</param>
        protected void pnlGroupMembers_DeleteClick( object sender, RowEventArgs e )
        {
            // TODO: enforce counselor delete support
            var rockContext = new RockContext();
            var groupMemberService = new GroupMemberService( rockContext );
            var groupMember = groupMemberService.Get( e.RowKeyId );
            if ( groupMember != null && groupMember.GroupId == _group.Id )
            {
                groupMemberService.Delete( groupMember );

                rockContext.SaveChanges();
                var group = new GroupService( rockContext ).Get( groupMember.GroupId );
                pnlGroupMembers.DataSource = group.Members;
                pnlGroupMembers.DataBind();
                SetGroupHeader( group );
            }
        }

        /// <summary>
        /// Handles the RowSelected event of the pnlGroupMembers control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RowEventArgs"/> instance containing the event data.</param>
        protected void pnlGroupMembers_RowSelected( object sender, RowEventArgs e )
        {
            if ( GroupRowSelected != null )
            {
                GroupRowSelected( this, e );
            }
        }

        /// <summary>
        /// Handles the EditClick event of the Actions control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void pnlGroupMembers_RowCommand( object sender, GridViewCommandEventArgs e )
        {
            if ( GroupRowCommand != null )
            {
                GroupRowCommand( this, e );
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

    /// <summary>
    ///
    /// </summary>
    /// <seealso cref="DotLiquid.Drop" />
    public class GroupMemberDataRow : DotLiquid.Drop
    {
        public int Id { get; set; }

        public Guid Guid { get; set; }

        public int PersonId { get; set; }

        public string NickName { get; set; }

        public string LastName { get; set; }

        public string Name { get; set; }

        public DateTime? BirthDate { get; set; }

        public int? Age { get; set; }

        public int? ConnectionStatusValueId { get; set; }

        public DateTime? DateTimeAdded { get; set; }

        public string Note { get; set; }

        public DateTime? FirstAttended { get; set; }

        public DateTime? LastAttended { get; set; }

        public string Email { get; set; }

        public string Gender { get; set; }

        public string HomePhone { get; set; }

        public string CellPhone { get; set; }

        public string HomeAddress { get; set; }

        public double? Latitude { get; set; }

        public double? Longitude { get; set; }

        public string GroupRole { get; set; }

        public GroupMemberStatus GroupMemberStatus { get; set; }

        public int? RecordStatusValueId { get; set; }

        public bool IsDeceased { get; set; }

        public int? MaritalStatusValueId { get; set; }

        public int GroupRoleId { get; set; }

        public bool IsArchived { get; set; }
    }
}
