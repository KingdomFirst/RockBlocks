// KFS Group Panel

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Entity;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;
using Newtonsoft.Json;
using RestSharp.Extensions;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Security;
using Rock.Web.Cache;
using Rock.Web.UI;
using Rock.Web.UI.Controls;

namespace RockWeb.Plugins.com_kfs.Event
{
    /// <summary>
    /// The KFS Group Panel that displays for resource subgroups
    /// </summary>
    /// <seealso cref="System.Web.UI.UserControl" />

    [DisplayName( "Advanced Registration Group Detail" )]
    [Category( "KFS > Advanced Event Registration" )]
    [Description( "The Group Panel that displays for resource subgroups." )]
    [BooleanField( "Show Filter", "Setting to show/hide the group filter.", false )]
    public partial class GroupPanel : System.Web.UI.UserControl
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
        /// Occurs when [edit member button click].
        /// </summary>
        public event EventHandler EditMemberButtonClick;

        /// <summary>
        /// Occurs when [assign group button click].
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
            AvailableAttributes = ViewState["AvailableAttributes"] as List<AttributeCache>;
            AddDynamicControls();
        }

        /// <summary>
        /// Saves any user control view-state changes that have occurred since the last page postback.
        /// </summary>
        /// <returns>
        /// Returns the user control's current view state. If there is no view state associated with the control, it returns null.
        /// </returns>
        protected override object SaveViewState()
        {
            ViewState["AvailableAttributes"] = AvailableAttributes;
            return base.SaveViewState();
        }
        
        /// <summary>
        /// Builds the control.
        /// </summary>
        /// <param name="group">The group.</param>
        /// <param name="resourceTypes">The resource types.</param>
        /// <param name="resources">The resources.</param>
        public void BuildControl( Group group, List<GroupTypeCache> resourceTypes, Dictionary<string, AttributeValueCache> resources  )
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
            //hfGroupId.Value = group.Id.ToString();
            SetGroupHeader( group );
            
            rFilter.ApplyFilterClick += rFilter_ApplyFilterClick;
            gGroupMembers.DataKeyNames = new string[] { "Id" };
            gGroupMembers.PersonIdField = "PersonId";
            gGroupMembers.RowDataBound += gGroupMembers_RowDataBound;
            gGroupMembers.GetRecipientMergeFields += gGroupMembers_GetRecipientMergeFields;
            gGroupMembers.Actions.AddClick += gGroupMembers_AddClick;
            gGroupMembers.GridRebind += gGroupMembers_GridRebind;
            gGroupMembers.RowItemText = _group.GroupType.GroupTerm + " " + _group.GroupType.GroupMemberTerm;
            gGroupMembers.ExportFilename = _group.Name;
            gGroupMembers.ExportSource = ExcelExportSource.DataSource;
            gGroupMembers.ShowConfirmDeleteDialog = false;

            // TODO: make sure they have Auth to edit the block OR edit to the Group
            gGroupMembers.Actions.ShowAdd = true;
            gGroupMembers.IsDeleteEnabled = true;

            //SetFilter();
            //BindGroupMembersGrid();
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
        /// Binds the filter.
        /// </summary>
        private void SetFilter()
        {
            if ( _group != null )
            {
                cblRole.DataSource = _group.GroupType.Roles.OrderBy( a => a.Order ).ToList();
                cblRole.DataBind();
            }

            cblGroupMemberStatus.BindToEnum<GroupMemberStatus>();

            cpCampusFilter.Campuses = CampusCache.All();

            BindAttributes();
            AddDynamicControls();
            tbFirstName.Text = rFilter.GetUserPreference( MakeKeyUniqueToGroup( "First Name" ) );
            tbLastName.Text = rFilter.GetUserPreference( MakeKeyUniqueToGroup( "Last Name" ) );
            cpCampusFilter.SelectedCampusId = rFilter.GetUserPreference( MakeKeyUniqueToGroup( "Campus" ) ).AsIntegerOrNull();

            var genderValue = rFilter.GetUserPreference( MakeKeyUniqueToGroup( "Gender" ) );
            if ( !string.IsNullOrWhiteSpace( genderValue ) )
            {
                cblGenderFilter.SetValues( genderValue.Split( ';' ).ToList() );
            }

            var roleValue = rFilter.GetUserPreference( MakeKeyUniqueToGroup( "Role" ) );
            if ( !string.IsNullOrWhiteSpace( roleValue ) )
            {
                cblRole.SetValues( roleValue.Split( ';' ).ToList() );
            }

            var statusValue = rFilter.GetUserPreference( MakeKeyUniqueToGroup( "Status" ) );
            if ( !string.IsNullOrWhiteSpace( statusValue ) )
            {
                cblGroupMemberStatus.SetValues( statusValue.Split( ';' ).ToList() );
            }
        }

        /// <summary>
        /// Adds the attribute columns.
        /// </summary>
        private void AddDynamicControls()
        {
            // Add Family Campus 
            var lFamilyCampus = new RockLiteralField
            {
                ID = "lFamilyCampus",
                HeaderText = "Family Campus"
            };
            gGroupMembers.Columns.Add( lFamilyCampus );

            // Clear the filter controls
            phAttributeFilters.Controls.Clear();

            // Remove current attribute columns
            foreach ( var column in gGroupMembers.Columns.OfType<AttributeField>().ToList() )
            {
                gGroupMembers.Columns.Remove( column );
            }

            // replaces BindAttributes()
            //var groupMemberAttributes = new List<AttributeCache>();
            //if ( _group != null )
            //{
            //    var entityTypeId = new GroupMember().TypeId;
            //    var groupQualifier = _group.Id.ToString();
            //    var groupTypeQualifier = _group.GroupTypeId.ToString();

            //    foreach ( var attributeModel in new AttributeService( rockContext ).Queryable()
            //        .Where( a => a.EntityTypeId == entityTypeId && a.IsGridColumn &&
            //            ( ( a.EntityTypeQualifierColumn.Equals( "GroupId", StringComparison.OrdinalIgnoreCase ) && a.EntityTypeQualifierValue.Equals( groupQualifier ) ) ||
            //              ( a.EntityTypeQualifierColumn.Equals( "GroupTypeId", StringComparison.OrdinalIgnoreCase ) && a.EntityTypeQualifierValue.Equals( groupTypeQualifier ) ) ) )
            //        .OrderByDescending( a => a.EntityTypeQualifierColumn )
            //        .ThenBy( a => a.Order )
            //        .ThenBy( a => a.Name ) )
            //    {
            //        groupMemberAttributes.Add( AttributeCache.Read( attributeModel ) );
            //    }
            //}

            // Set up group member attribute columns
            var rockContext = new RockContext();
            var attributeValueService = new AttributeValueService( rockContext );
            foreach ( var attribute in AvailableAttributes )
            {
                // add filter control for that attribute
                var control = attribute.FieldType.Field.FilterControl( attribute.QualifierValues, "filter_" + attribute.Id.ToString(), false, Rock.Reporting.FilterMode.SimpleFilter );
                if ( control != null )
                {
                    if ( control is IRockControl )
                    {
                        var rockControl = (IRockControl)control;
                        rockControl.Label = attribute.Name;
                        rockControl.Help = attribute.Description;
                        phAttributeFilters.Controls.Add( control );
                    }
                    else
                    {
                        var wrapper = new RockControlWrapper
                        {
                            ID = control.ID + "_wrapper",
                            Label = attribute.Name
                        };
                        wrapper.Controls.Add( control );
                        phAttributeFilters.Controls.Add( wrapper );
                    }

                    var savedValue = rFilter.GetUserPreference( MakeKeyUniqueToGroup( attribute.Key ) );
                    if ( !string.IsNullOrWhiteSpace( savedValue ) )
                    {
                        
                        var values = JsonConvert.DeserializeObject<List<string>>( savedValue );
                        attribute.FieldType.Field.SetFilterValues( control, attribute.QualifierValues, values );
                        
                    }
                }

                // add the attribute data column
                var attributeColumn = gGroupMembers.Columns.OfType<AttributeField>().FirstOrDefault( a => a.AttributeId == attribute.Id );
                if ( attributeColumn == null )
                {
                    var boundField = new AttributeField();
                    boundField.DataField = attribute.Key;
                    boundField.AttributeId = attribute.Id;
                    boundField.HeaderText = attribute.Name;
                    boundField.ItemStyle.HorizontalAlign = HorizontalAlign.Center;

                    decimal needsFilled = 0;
                    if ( attribute.FieldType != null )
                    {
                        var attributeValues = attributeValueService.GetByAttributeId( attribute.Id )
                            .Where( v => !( v.Value == null || v.Value.Trim() == string.Empty ) )
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
                        gGroupMembers.ShowFooter = true;
                        boundField.FooterText = needsFilled.ToString();
                    }

                    gGroupMembers.Columns.Add( boundField );
                }

                if ( gGroupMembers.ShowFooter )
                {
                    gGroupMembers.Columns[1].FooterText = "Total";
                    gGroupMembers.Columns[1].FooterStyle.HorizontalAlign = HorizontalAlign.Left;
                }
            }
            
            // Add dynamic assignment columns for volunteer groups
            if ( _resources != null && _group.GroupType.GroupTypePurposeValue != null && _group.GroupType.GroupTypePurposeValue.Value == "Serving Area" )
            {
                foreach ( var resourceType in _resourceTypes.Where( gt => gt.GetAttributeValue( "AllowVolunteerAssignment" ).AsBoolean( true ) ) )
                {
                    if ( _resources.ContainsKey( resourceType.Name ) )
                    {
                        var resourceGroupGuids = _resources[resourceType.Name];
                        if ( resourceGroupGuids != null && !string.IsNullOrWhiteSpace( resourceGroupGuids.Value ) )
                        {
                            var parentGroup = new GroupService( rockContext ).Get( resourceGroupGuids.Value.AsGuid() );
                            if ( parentGroup != null && parentGroup.GroupTypeId != _group.GroupTypeId )
                            {
                                var subGroupColumn = new LinkButtonField();
                                subGroupColumn.ItemStyle.HorizontalAlign = HorizontalAlign.Center;
                                subGroupColumn.HeaderStyle.CssClass = "";
                                subGroupColumn.HeaderText = parentGroup.Name;
                                gGroupMembers.Columns.Add( subGroupColumn );
                            }
                        }
                    }
                }
            }

            // Add edit column
            var editField = new EditField();
            gGroupMembers.Columns.Add( editField );
            editField.Click += gGroupMembers_EditClick;

            // Add delete column
            var deleteField = new DeleteField();
            gGroupMembers.Columns.Add( deleteField );
            deleteField.Click += gGroupMembers_DeleteClick;
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
            //gGroupMembers.DataSource = _group.Members;
            //gGroupMembers.DataBind();
            
            if ( _group != null && _group.GroupType.Roles.Any() )
            {
                rFilter.Visible = true;
                gGroupMembers.Visible = true;

                var rockContext = new RockContext();
                    
                var groupMemberService = new GroupMemberService( rockContext );
                var qry = groupMemberService.Queryable( "Person,GroupRole", true ).AsNoTracking()
                    .Where( m => m.GroupId == _group.Id );

                // Filter by First Name
                var firstName = tbFirstName.Text;
                if ( !string.IsNullOrWhiteSpace( firstName ) )
                {
                    qry = qry.Where( m =>
                        m.Person.FirstName.StartsWith( firstName ) ||
                        m.Person.NickName.StartsWith( firstName ) );
                }

                // Filter by Last Name
                var lastName = tbLastName.Text;
                if ( !string.IsNullOrWhiteSpace( lastName ) )
                {
                    qry = qry.Where( m => m.Person.LastName.StartsWith( lastName ) );
                }

                // Filter by role
                var validGroupTypeRoles = _group.GroupType.Roles.Select( r => r.Id ).ToList();
                var roles = new List<int>();
                foreach ( var roleId in cblRole.SelectedValues.AsIntegerList() )
                {
                    if ( validGroupTypeRoles.Contains( roleId ) )
                    {
                        roles.Add( roleId );
                    }
                }

                if ( roles.Any() )
                {
                    qry = qry.Where( m => roles.Contains( m.GroupRoleId ) );
                }

                // Filter by Group Member Status
                var statuses = new List<GroupMemberStatus>();
                foreach ( string status in cblGroupMemberStatus.SelectedValues )
                {
                    if ( !string.IsNullOrWhiteSpace( status ) )
                    {
                        statuses.Add( status.ConvertToEnum<GroupMemberStatus>() );
                    }
                }

                if ( statuses.Any() )
                {
                    qry = qry.Where( m => statuses.Contains( m.GroupMemberStatus ) );
                }

                var genders = new List<Gender>();
                foreach ( var item in cblGenderFilter.SelectedValues )
                {
                    var gender = item.ConvertToEnum<Gender>();
                    genders.Add( gender );
                }

                if ( genders.Any() )
                {
                    qry = qry.Where( m => genders.Contains( m.Person.Gender ) );
                }

                // Filter by Campus
                if ( cpCampusFilter.SelectedCampusId.HasValue )
                {
                    var familyGuid = new Guid( Rock.SystemGuid.GroupType.GROUPTYPE_FAMILY );
                    var campusId = cpCampusFilter.SelectedCampusId.Value;
                    var qryFamilyMembersForCampus = new GroupMemberService( rockContext ).Queryable().Where( a => a.Group.GroupType.Guid == familyGuid && a.Group.CampusId == campusId );
                    qry = qry.Where( a => qryFamilyMembersForCampus.Any( f => f.PersonId == a.PersonId ) );
                }
                    
                // Filter query by any configured attribute filters
                if ( AvailableAttributes != null && AvailableAttributes.Any() )
                {
                    var attributeValueService = new AttributeValueService( rockContext );
                    var parameterExpression = attributeValueService.ParameterExpression;

                    foreach ( var attribute in AvailableAttributes )
                    {
                        var filterControl = phAttributeFilters.FindControl( "filter_" + attribute.Id.ToString() );
                        if ( filterControl != null )
                        {
                            var filterValues = attribute.FieldType.Field.GetFilterValues( filterControl, attribute.QualifierValues, Rock.Reporting.FilterMode.SimpleFilter );
                            var expression = attribute.FieldType.Field.AttributeFilterExpression( attribute.QualifierValues, filterValues, parameterExpression );
                            if ( expression != null )
                            {
                                var attributeValues = attributeValueService
                                    .Queryable()
                                    .Where( v => v.Attribute.Id == attribute.Id );

                                attributeValues = attributeValues.Where( parameterExpression, expression, null );

                                qry = qry.Where( w => attributeValues.Select( v => v.EntityId ).Contains( w.Id ) );
                            }
                        }
                    }
                }

                var sortProperty = gGroupMembers.SortProperty;

                var hasGroupRequirements = new GroupRequirementService( rockContext ).Queryable().Where( a => a.GroupId == _group.Id ).Any();

                // If there are group requirements that that member doesn't meet, show an icon in the grid
                var includeWarnings = false;
                var groupMemberIdsThatLackGroupRequirements = new GroupService( rockContext ).GroupMembersNotMeetingRequirements( _group.Id, includeWarnings ).Select( a => a.Key.Id );

                List<GroupMember> groupMembersList = null;
                if ( sortProperty != null && sortProperty.Property != "FirstAttended" && sortProperty.Property != "LastAttended" )
                {
                    groupMembersList = qry.Sort( sortProperty ).ToList();
                }
                else
                {
                    groupMembersList = qry.OrderBy( a => a.GroupRole.Order ).ThenBy( a => a.Person.LastName ).ThenBy( a => a.Person.FirstName ).ToList();
                }
                    
                // Since we're not binding to actual group member list, but are using AttributeField columns,
                // we need to save the group members into the grid's object list
                gGroupMembers.ObjectList = new Dictionary<string, object>();
                groupMembersList.ForEach( m => gGroupMembers.ObjectList.Add( m.Id.ToString(), m ) );
                gGroupMembers.EntityTypeId = EntityTypeCache.Read( Rock.SystemGuid.EntityType.GROUP_MEMBER.AsGuid() ).Id;

                var homePhoneType = DefinedValueCache.Read( Rock.SystemGuid.DefinedValue.PERSON_PHONE_TYPE_HOME );
                var cellPhoneType = DefinedValueCache.Read( Rock.SystemGuid.DefinedValue.PERSON_PHONE_TYPE_MOBILE );

                // If exporting to Excel, the selectAll option will be true, and home location should be calculated
                var homeLocations = new Dictionary<int, Location>();
                if ( isExporting )
                {
                    foreach ( var m in groupMembersList )
                    {
                        homeLocations.Add( m.Id, m.Person.GetHomeLocation( rockContext ) );
                    }
                }
                    
                var dataSource = groupMembersList.Select( m => new
                {
                    m.Id,
                    m.Guid,
                    m.PersonId,
                    m.Person.NickName,
                    m.Person.LastName,
                    Name =
                    ( isExporting ? m.Person.LastName + ", " + m.Person.NickName : 
                        m.Person.NickName + " " + m.Person.LastName
                        + ( ( hasGroupRequirements && groupMemberIdsThatLackGroupRequirements.Contains( m.Id ) )
                            ? " <i class='fa fa-exclamation-triangle text-warning'></i>"
                            : string.Empty )
                        + ( !string.IsNullOrEmpty( m.Note )
                            ? " <i class='fa fa-file-text-o text-info'></i>"
                            : string.Empty ) ),
                    m.Person.BirthDate,
                    m.Person.Age,
                    m.Person.ConnectionStatusValueId,
                    m.DateTimeAdded,
                    m.Person.Email,
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
                    Latitude = homeLocations.ContainsKey( m.Id ) && homeLocations[m.Id] != null ?
                        homeLocations[m.Id].Latitude : (double?)null,
                    Longitude = homeLocations.ContainsKey( m.Id ) && homeLocations[m.Id] != null ?
                        homeLocations[m.Id].Longitude : (double?)null,
                    GroupRole = m.GroupRole.Name,
                    m.GroupMemberStatus,
                    m.Person.RecordStatusValueId,
                    m.Person.IsDeceased
                } ).ToList();
                    
                gGroupMembers.DataSource = dataSource;
                gGroupMembers.DataBind();
            }
        }

        /// <summary>
        /// Handles the GridRebind event of the gGroupMembers control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="GridRebindEventArgs" /> instance containing the event data.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        protected void gGroupMembers_GridRebind( object sender, GridRebindEventArgs e )
        {
            BindGroupMembersGrid( e.IsExporting );
        }

        /// <summary>
        /// Handles the GetRecipientMergeFields event of the gGroupMembers control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="GetRecipientMergeFieldsEventArgs"/> instance containing the event data.</param>
        void gGroupMembers_GetRecipientMergeFields( object sender, GetRecipientMergeFieldsEventArgs e )
        {
            dynamic groupMember = e.DataItem;
            if ( groupMember != null )
            {
                e.MergeValues.Add( "GroupRole", groupMember.GroupRole );
                e.MergeValues.Add( "GroupMemberStatus", ( (GroupMemberStatus)groupMember.GroupMemberStatus ).ConvertToString() );
            }
        }

        /// <summary>
        /// Binds the attributes.
        /// </summary>
        private void BindAttributes()
        {
            // Parse the attribute filters 
            AvailableAttributes = new List<AttributeCache>();
            if ( _group != null )
            {
                var entityTypeId = new GroupMember().TypeId;
                var groupQualifier = _group.Id.ToString();
                var groupTypeQualifier = _group.GroupTypeId.ToString();
                foreach ( var attributeModel in new AttributeService( new RockContext() ).Queryable()
                    .Where( a =>
                        a.EntityTypeId == entityTypeId &&
                        a.IsGridColumn &&
                        ( ( a.EntityTypeQualifierColumn.Equals( "GroupId", StringComparison.OrdinalIgnoreCase ) && a.EntityTypeQualifierValue.Equals( groupQualifier ) ) ||
                         ( a.EntityTypeQualifierColumn.Equals( "GroupTypeId", StringComparison.OrdinalIgnoreCase ) && a.EntityTypeQualifierValue.Equals( groupTypeQualifier ) ) ) )
                    .OrderByDescending( a => a.EntityTypeQualifierColumn )
                    .ThenBy( a => a.Order )
                    .ThenBy( a => a.Name ) )
                {
                    AvailableAttributes.Add( AttributeCache.Read( attributeModel ) );
                }
            }
        }

        #endregion

        #region Click Methods

        /// <summary>
        /// Handles the ApplyFilterClick event of the rFilter control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        protected void rFilter_ApplyFilterClick( object sender, EventArgs e )
        {
            rFilter.SaveUserPreference( MakeKeyUniqueToGroup( "First Name" ), "First Name", tbFirstName.Text );
            rFilter.SaveUserPreference( MakeKeyUniqueToGroup( "Last Name" ), "Last Name", tbLastName.Text );
            rFilter.SaveUserPreference( MakeKeyUniqueToGroup( "Role" ), "Role", cblRole.SelectedValues.AsDelimited( ";" ) );
            rFilter.SaveUserPreference( MakeKeyUniqueToGroup( "Status" ), "Status", cblGroupMemberStatus.SelectedValues.AsDelimited( ";" ) );
            rFilter.SaveUserPreference( MakeKeyUniqueToGroup( "Campus" ), "Campus", cpCampusFilter.SelectedCampusId.ToString() );
            rFilter.SaveUserPreference( MakeKeyUniqueToGroup( "Gender" ), "Gender", cblGenderFilter.SelectedValues.AsDelimited( ";" ) );

            if ( AvailableAttributes != null )
            {
                foreach ( var attribute in AvailableAttributes )
                {
                    var filterControl = phAttributeFilters.FindControl( "filter_" + attribute.Id.ToString() );
                    if ( filterControl != null )
                    {
                        {
                            var values = attribute.FieldType.Field.GetFilterValues( filterControl, attribute.QualifierValues, Rock.Reporting.FilterMode.SimpleFilter );
                            rFilter.SaveUserPreference( MakeKeyUniqueToGroup( attribute.Key ), attribute.Name, attribute.FieldType.Field.GetFilterValues( filterControl, attribute.QualifierValues, Rock.Reporting.FilterMode.SimpleFilter ).ToJson() );
                        }
                    }
                }
            }

            BindGroupMembersGrid();
        }

        /// <summary>
        /// Rs the filter_ display filter value.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The e.</param>
        protected void rFilter_DisplayFilterValue( object sender, GridFilter.DisplayFilterValueArgs e )
        {
            if ( AvailableAttributes != null )
            {
                var attribute = AvailableAttributes.FirstOrDefault( a => MakeKeyUniqueToGroup( a.Key ) == e.Key );
                if ( attribute != null )
                {
                    {
                        var values = JsonConvert.DeserializeObject<List<string>>( e.Value );
                        e.Value = attribute.FieldType.Field.FormatFilterValues( attribute.QualifierValues, values );
                        return;
                    }
                }
            }

            if ( e.Key == MakeKeyUniqueToGroup( "First Name" ) )
            {
                return;
            }
            else if ( e.Key == MakeKeyUniqueToGroup( "Last Name" ) )
            {
                return;
            }
            else if ( e.Key == MakeKeyUniqueToGroup( "Role" ) )
            {
                e.Value = ResolveValues( e.Value, cblRole );
            }
            else if ( e.Key == MakeKeyUniqueToGroup( "Status" ) )
            {
                e.Value = ResolveValues( e.Value, cblGroupMemberStatus );
            }
            else if ( e.Key == MakeKeyUniqueToGroup( "Gender" ) )
            {
                e.Value = ResolveValues( e.Value, cblGenderFilter );
            }
            else if ( e.Key == MakeKeyUniqueToGroup( "Campus" ) )
            {
                var campusId = e.Value.AsIntegerOrNull();
                if ( campusId.HasValue )
                {
                    var campusCache = CampusCache.Read( campusId.Value );
                    e.Value = campusCache.Name;
                }
                else
                {
                    e.Value = null;
                }
            }
            else
            {
                e.Value = string.Empty;
            }
        }

        /// <summary>
        /// Handles the AddClick event of the Actions control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void gGroupMembers_AddClick( object sender, EventArgs e )
        {
            if ( AddButtonClick != null )
            {
                AddButtonClick( this, EventArgs.Empty );
            }
        }

        /// <summary>
        /// Handles the EditClick event of the Actions control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void gGroupMembers_EditClick( object sender, EventArgs e )
        {
            if ( EditMemberButtonClick != null )
            {
                EditMemberButtonClick( this, e );
            }
        }


        /// <summary>
        /// Handles the RowDataBound event of the gGroupMembers control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void gGroupMembers_RowDataBound( object sender, EventArgs e )
        {
            if ( GroupRowDataBound != null )
            {
                GroupRowDataBound( this, e as GridViewRowEventArgs );
            }
        }

        /// <summary>
        /// Handles the Click event of the DeleteGroupMember control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RowEventArgs"/> instance containing the event data.</param>
        private void gGroupMembers_DeleteClick( object sender, RowEventArgs e )
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
                gGroupMembers.DataSource = group.Members;
                gGroupMembers.DataBind();
                SetGroupHeader( group );
            }
        }
        
        #endregion
    }
}
