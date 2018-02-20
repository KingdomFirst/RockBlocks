// KFS Group Panel

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Entity;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;
using RestSharp.Extensions;
using Rock;
using Rock.Data;
using Rock.Model;
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
    [Description( "The KFS Group Panel that displays for resource subgroups." )]
    public partial class KFSGroupPanel : System.Web.UI.UserControl
    {
        /// <summary>
        /// The group, resources, and registration instance
        /// </summary>
        private Group _group;

        private List<GroupTypeCache> _resourceTypes;

        private RegistrationInstance _instance;

        /// <summary>
        /// The expanded
        /// </summary>
        private bool _expanded = false;

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
        public event EventHandler AssignGroupButtonClick;

        /// <summary>
        /// Builds the control.
        /// </summary>
        public void BuildControl()
        {
            if ( _group != null && _resourceTypes != null && _instance != null )
            {
                BuildControl( _group, _resourceTypes, _instance );
            }
        }

        /// <summary>
        /// Builds the control.
        /// </summary>
        /// <param name="group">The group.</param>
        /// <param name="resourceGroupTypes">The resource group types.</param>
        /// <param name="instance">The registration instance.</param>
        public void BuildControl( Group group, List<GroupTypeCache> resourceGroupTypes, RegistrationInstance instance  )
        {
            if ( _group == null )
            {
                _group = group;
            }

            if ( _resourceTypes == null )
            {
                _resourceTypes = resourceGroupTypes;
            }

            if ( _instance == null )
            {
                _instance = instance;
            }

            // Set up group Panel widget
            BuildSubgroupHeading( group );
            pnlGroupDescription.Visible = !string.IsNullOrWhiteSpace( group.Description );
            lblGroupDescription.Text = group.Description;
            pnlSubGroup.Expanded = _expanded;
            hfGroupId.Value = group.Id.ToString();
            lbGroupEdit.CommandArgument = group.Id.ToString();
            lbGroupDelete.CommandArgument = group.Id.ToString();
            pnlSubGroup.DataBind();

            // Set up Member Grid
            gGroupMembers.DataKeyNames = new string[] { "Id" };
            gGroupMembers.Actions.AddClick += gGroupMembers_AddClick;
            gGroupMembers.Actions.ShowAdd = true;
            gGroupMembers.IsDeleteEnabled = true;
            gGroupMembers.RowDataBound += gGroupMembers_RowDataBound;

            // Set up Family Campus 
            var lFamilyCampus = new RockLiteralField
            {
                ID = "lFamilyCampus",
                HeaderText = "Family Campus"
            };
            gGroupMembers.Columns.Add( lFamilyCampus );
            
            // Set up group member attributes
            var rockContext = new RockContext();
            var groupMemberAttributes = new List<AttributeCache>();
            if ( _group != null )
            {
                var entityTypeId = new GroupMember().TypeId;
                var groupQualifier = _group.Id.ToString();
                var groupTypeQualifier = _group.GroupTypeId.ToString();

                foreach ( var attributeModel in new AttributeService( rockContext ).Queryable()
                    .Where( a => a.EntityTypeId == entityTypeId && a.IsGridColumn &&
                        ( ( a.EntityTypeQualifierColumn.Equals( "GroupId", StringComparison.OrdinalIgnoreCase ) && a.EntityTypeQualifierValue.Equals( groupQualifier ) ) ||
                          ( a.EntityTypeQualifierColumn.Equals( "GroupTypeId", StringComparison.OrdinalIgnoreCase ) && a.EntityTypeQualifierValue.Equals( groupTypeQualifier ) ) ) )
                    .OrderByDescending( a => a.EntityTypeQualifierColumn )
                    .ThenBy( a => a.Order )
                    .ThenBy( a => a.Name ) )
                {
                    groupMemberAttributes.Add( AttributeCache.Read( attributeModel ) );
                }
            }

            // Remove current attribute columns
            gGroupMembers.Columns.OfType<AttributeField>().ToList().ForEach( c => gGroupMembers.Columns.Remove( c ) );

            var attributeValueService = new AttributeValueService( rockContext );
            foreach ( var attribute in groupMemberAttributes )
            {
                var attributeColumn = gGroupMembers.Columns.OfType<AttributeField>().FirstOrDefault( a => a.AttributeId == attribute.Id );
                if ( attributeColumn == null )
                {
                    var boundField = new AttributeField();
                    boundField.DataField = attribute.Key;
                    boundField.AttributeId = attribute.Id;
                    boundField.HeaderText = attribute.Name;

                    decimal needsFilled = 0;
                    if ( attribute.FieldType != null )
                    {
                        boundField.ItemStyle.HorizontalAlign = HorizontalAlign.Center;

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

            ////// Add dynamic columns for sub groups
            if ( instance != null && group.GroupType.GroupTypePurposeValue != null && group.GroupType.GroupTypePurposeValue.Value == "Serving Area" )
            {
                if ( instance.Attributes == null || !instance.Attributes.Any() )
                {
                    instance.LoadAttributes();
                }

                foreach ( var resourceType in _resourceTypes.Where( gt => gt.GetAttributeValue( "AllowVolunteerAssignment" ).AsBoolean( true ) ) )
                {
                    if ( instance.AttributeValues.ContainsKey( resourceType.Name ) )
                    {
                        var resourceGroupGuids = instance.AttributeValues[resourceType.Name];
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

            gGroupMembers.DataSource = group.Members;
            gGroupMembers.DataBind();
        }


        /// <summary>
        /// Handles the RowDataBound event of the gGroupMembers control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="GridViewRowEventArgs"/> instance containing the event data.</param>
        private void gGroupMembers_RowDataBound( object sender, GridViewRowEventArgs e )
        {
            var groupMember = e.Row.DataItem as GroupMember;
            if ( groupMember != null )
            {
                var campus = groupMember.Person.GetCampus();
                var lCampus = e.Row.FindControl( "lFamilyCampus" ) as Literal;
                if ( lCampus != null && campus != null )
                {
                    lCampus.Text = campus.Name;
                }

                // Build subgroup button grid for Volunteers
                if ( groupMember.Group.GroupType.GroupTypePurposeValue != null && groupMember.Group.GroupType.GroupTypePurposeValue.Value == "Serving Area" )
                {
                    using ( var rockContext = new RockContext() )
                    {
                        var groupService = new GroupService( rockContext );
                        var memberService = new GroupMemberService( rockContext );

                        if ( _instance.Attributes == null || !_instance.Attributes.Any() )
                        {
                            _instance.LoadAttributes();
                        }

                        var firstResourceColumn = gGroupMembers.Columns.OfType<LinkButtonField>().FirstOrDefault();
                        var columnIndex = gGroupMembers.Columns.IndexOf( firstResourceColumn ).ToString().AsInteger();

                        foreach ( var groupType in _resourceTypes )
                        {
                            var resourceGroupGuid = _instance.AttributeValues[groupType.Name];
                            if ( resourceGroupGuid != null && !Guid.Empty.Equals( resourceGroupGuid.Value.AsGuid() ) )
                            {
                                var parentGroup = groupService.Get( resourceGroupGuid.Value.AsGuid() );
                                if ( parentGroup != null && columnIndex > 0 && e.Row.Cells.Count >= columnIndex )
                                {
                                    var groupAssignments = e.Row.Cells[columnIndex];
                                    var headerText = gGroupMembers.HeaderRow.Cells[columnIndex].Text;
                                    var columnsMatch = ( parentGroup.Name == headerText || parentGroup.GroupType.Name == headerText );
                                    var btnGroupAssignment = groupAssignments.Controls.Count > 0 ? groupAssignments.Controls[0] as LinkButton : null;
                                    if ( columnsMatch && btnGroupAssignment != null )
                                    {
                                        if ( parentGroup.Groups.Any() )
                                        {
                                            if ( parentGroup.GroupType.Attributes == null || !parentGroup.GroupType.Attributes.Any() )
                                            {
                                                parentGroup.GroupType.LoadAttributes( rockContext );
                                            }

                                            var subGroupIds = parentGroup.Groups.Select( g => g.Id ).ToList();
                                            var groupMemberships = memberService.Queryable().AsNoTracking()
                                                .Where( m => m.PersonId == groupMember.PersonId && subGroupIds.Contains( m.GroupId ) ).ToList();
                                            if ( !groupMemberships.Any() )
                                            {
                                                btnGroupAssignment.CssClass = "btn-add btn btn-default btn-sm";

                                                using ( var literalControl = new LiteralControl( "<i class='fa fa-plus-circle'></i>" ) )
                                                {
                                                    btnGroupAssignment.Controls.Add( literalControl );
                                                }
                                                using ( var literalControl = new LiteralControl( "<span class='grid-btn-assign-text'> Assign</span>" ) )
                                                {
                                                    btnGroupAssignment.Controls.Add( literalControl );
                                                }
                                                btnGroupAssignment.CommandName = "AssignSubGroup";
                                                btnGroupAssignment.CommandArgument = string.Format( "{0}|{1}", parentGroup.Id.ToString(), groupMember.Id.ToString() );
                                            }
                                            else if ( parentGroup.GroupType.GetAttributeValue( "AllowMultipleRegistrations" ).AsBoolean() )
                                            {
                                                var subGroupControls = e.Row.Cells[columnIndex].Controls;

                                                // add any group memberships
                                                foreach ( var member in groupMemberships )
                                                {
                                                    using ( var subGroupButton = new LinkButton() )
                                                    {
                                                        subGroupButton.OnClientClick = "javascript: __doPostBack( 'btnGroupRegistrations', 'select-subgroup:" + member.Id + "' ); return false;";
                                                        subGroupButton.Text = " " + member.Group.Name + "  ";
                                                        subGroupControls.AddAt( 0, subGroupButton );
                                                    }
                                                }

                                                using ( var literalControl = new LiteralControl( "<i class='fa fa-plus-circle'></i>" ) )
                                                {
                                                    btnGroupAssignment.Controls.Add( literalControl );
                                                }

                                                btnGroupAssignment.OnClientClick = string.Format( "javascript: __doPostBack( 'btnGroupRegistrations', 'select-subgroup:{0}|{1}' ); return false;", parentGroup.Id, groupMember.Id );
                                                btnGroupAssignment.CssClass = "btn-add btn btn-default btn-sm";
                                            }
                                            else
                                            {
                                                var currentMember = groupMemberships.FirstOrDefault();
                                                btnGroupAssignment.Text = currentMember.Group.Name;
                                                btnGroupAssignment.CommandName = "ChangeSubGroup";
                                                btnGroupAssignment.CommandArgument = currentMember.Id.ToString();
                                            }
                                        }
                                        else
                                        {
                                            using ( var literalControl = new LiteralControl( "<i class='fa fa-minus-circle'></i>" ) )
                                            {
                                                btnGroupAssignment.Controls.Add( literalControl );
                                            }
                                            using ( var literalControl = new LiteralControl( string.Format( "<span class='grid-btn-assign-text'> No {0}</span>", parentGroup.GroupType.GroupTerm.Pluralize() ) ) )
                                            {
                                                btnGroupAssignment.Controls.Add( literalControl );
                                            }
                                            btnGroupAssignment.Enabled = false;
                                        }
                                    }
                                }
                            }

                            columnIndex++;
                        }
                    }
                }
            }
        }


        /// <summary>
        /// Builds the subgroup heading.
        /// </summary>
        /// <param name="group">The group.</param>
        private void BuildSubgroupHeading( Group group )
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
        /// Handles the AssignClick event of the gGroupMembers control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void gGroupMembers_AssignGroupClick( object sender, EventArgs e )
        {
            if ( AssignGroupButtonClick != null )
            {
                AssignGroupButtonClick( this, e );
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
                BuildSubgroupHeading( group );
            }
        }
    }


}
