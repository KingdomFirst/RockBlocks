// KFS Group Panel

using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
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
    /// <seealso cref="System.Web.UI.UserControl" />
    public partial class KFSGroupPanel : System.Web.UI.UserControl
    {
        /// <summary>
        /// The group
        /// </summary>
        private Group _group;

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
        /// Builds the control.
        /// </summary>
        public void BuildControl()
        {
            if ( _group != null )
            {
                BuildControl( _group );
            }
        }

        /// <summary>
        /// Builds the control.
        /// </summary>
        /// <param name="group">The group.</param>
        public void BuildControl( Group group )
        {
            if ( _group == null )
            {
                _group = group;
            }
            // Set up group Panel widget
            pnlGroupDescription.Visible = !string.IsNullOrWhiteSpace( group.Description );
            lblGroupDescription.Text = group.Description;
            BuildSubgroupHeading( group );
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

            foreach ( var attribute in groupMemberAttributes )
            {
                var columnExists = gGroupMembers.Columns.OfType<AttributeField>().FirstOrDefault( a => a.AttributeId == attribute.Id ) != null;
                if ( !columnExists )
                {
                    var boundField = new AttributeField();
                    boundField.DataField = attribute.Key;
                    boundField.AttributeId = attribute.Id;
                    boundField.HeaderText = attribute.Name;

                    if ( attribute.FieldType != null )
                    {
                        boundField.ItemStyle.HorizontalAlign = attribute.FieldType.Field.AlignValue;
                    }

                    gGroupMembers.Columns.Add( boundField );
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
        /// Binds the member grid.
        /// </summary>
        public void BindGroupMembersGrid()
        {
            gGroupMembers.DataSource = _group.Members;
            gGroupMembers.DataBind();
        }

        /// <summary>
        /// Builds the subgroup heading.
        /// </summary>
        /// <param name="group">The group.</param>
        private void BuildSubgroupHeading( Group group )
        {
            pnlSubGroup.Title = string.Format( "<span class='span-panel-heading'>{0}</span>", group.Name );
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
        /// Handles the Click event of the DeleteGroupMember control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RowEventArgs"/> instance containing the event data.</param>
        private void gGroupMembers_DeleteClick( object sender, RowEventArgs e )
        {
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
