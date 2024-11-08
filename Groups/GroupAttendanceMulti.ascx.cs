// <copyright>
// Copyright 2024 by Kingdom First Solutions
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>
//
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Web.UI.WebControls;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Lava;
using Rock.Model;
using Rock.Web.UI;
using Rock.Web.UI.Controls;

namespace Plugins.rocks_kfs.Groups
{
    [DisplayName( "Multiple Group Attendance" )]
    [Category( "KFS > Groups" )]
    [Description( "Allows you to take attendance for multiple groups at once. " )]

    [CustomEnhancedListField( "Groups to Display",
        Description = "Select the groups to display in this attendance block. You may also pass in a comma separated list of GroupId's via a PageParameter 'Groups'.",
        ListSource = @"SELECT 
        CASE WHEN ggpg.Name IS NOT NULL THEN
	        CONCAT(ggpg.name, ' > ',gpg.Name,' > ',pg.Name,' > ', g.Name)
        WHEN gpg.Name IS NOT NULL THEN
	        CONCAT(gpg.Name,' > ',pg.Name,' > ', g.Name)
        WHEN pg.Name IS NOT NULL THEN
	        CONCAT(pg.Name,' > ', g.Name)
        ELSE
	        g.Name 
        END as Text, g.Id as Value
        FROM [Group] g
            LEFT JOIN [Group] pg ON g.ParentGroupId = pg.Id
            LEFT JOIN [Group] gpg ON pg.ParentGroupId = gpg.Id
            LEFT JOIN [Group] ggpg ON gpg.ParentGroupId = ggpg.Id
        WHERE g.GroupTypeId NOT IN (1,10,11,12) 
        ORDER BY 
            CASE WHEN ggpg.Name IS NOT NULL THEN
	            CONCAT(ggpg.name, ' > ',gpg.Name,' > ',pg.Name,' > ', g.Name)
            WHEN gpg.Name IS NOT NULL THEN
	            CONCAT(gpg.Name,' > ',pg.Name,' > ', g.Name)
            WHEN pg.Name IS NOT NULL THEN
	            CONCAT(pg.Name,' > ', g.Name)
            ELSE
                g.Name 
        END",
        IsRequired = true,
        Key = AttributeKey.GroupsToDisplay )]

    [LavaField( "Attendee Lava Template",
        Description = "Lava template used per attendee to customize what the appearance should look like to choose the user.",
        IsRequired = true,
        DefaultValue = DefaultValue.AttendeeLavaTemplate,
        Key = AttributeKey.AttendeeLavaTemplate )]

    [TextField( "Checkbox Column Class",
        Description = "Column classes for width on various screen sizes, uses standard bootstrap 3 column classes. Default: col-xs-12 col-sm-6 col-md-3 col-lg-2",
        DefaultValue = "col-xs-12 col-sm-6 col-md-3 col-lg-2",
        Key = AttributeKey.CheckboxColumnClass )]

    [Rock.SystemGuid.BlockTypeGuid( "B8724DBC-F8FB-426D-9296-87A5944273B9" )]
    public partial class GroupAttendanceMulti : RockBlock
    {
        /// <summary>
        /// Keys to use for Block Attributes
        /// </summary>
        private static class AttributeKey
        {
            public const string GroupsToDisplay = "GroupsToDisplay";
            public const string AttendeeLavaTemplate = "AttendeeLavaTemplate";
            public const string CheckboxColumnClass = "CheckboxColumnClass";
        }

        private static class DefaultValue
        {
            public const string AttendeeLavaTemplate = @"<img src=""{{ GroupMember.Person.PhotoUrl }}"" class=""img-circle col-xs-3 p-0 mr-3 pull-left""> {{ GroupMember.Person.FullName }}";
        }

        #region Private Variables

        private RockContext _rockContext = null;
        private bool _canView = false;
        private List<Group> _groups = new List<Group>();
        private List<GroupMember> _members = new List<GroupMember>();

        #endregion

        #region Control Methods

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            this.BlockUpdated += Block_BlockUpdated;
            this.AddConfigurationUpdateTrigger( pnlContent );

            _rockContext = new RockContext();

            var groupIds = GetAttributeValues( AttributeKey.GroupsToDisplay ).AsIntegerList();
            _groups = new GroupService( _rockContext ).GetByIds( groupIds ).ToList();

            foreach ( var groupId in groupIds )
            {
                _members.AddRange( new GroupMemberService( _rockContext ).GetByGroupId( groupId ).ToList() );
            }

            _canView = true;
        }
        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );

            pnlContent.Visible = _canView;

            if ( !Page.IsPostBack && _canView )
            {
                BindFilter();
                BindRepeat();
            }
        }

        #endregion

        #region Events

        /// <summary>
        /// Handles the BlockUpdated event of the control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void Block_BlockUpdated( object sender, EventArgs e )
        {
            BindFilter();
            BindRepeat();
        }

        private void RptrAttendance_ItemDataBound( object sender, System.Web.UI.WebControls.RepeaterItemEventArgs e )
        {
            var groupMember = e.Item.DataItem as GroupMember;
            var cbAttendee = e.Item.FindControl( "cbAttendee" ) as RockCheckBox;
            var pnlCardCheckbox = e.Item.FindControl( "pnlCardCheckbox" ) as Panel;

            if ( groupMember != null && cbAttendee != null && pnlCardCheckbox != null )
            {
                pnlCardCheckbox.AddCssClass( GetAttributeValue( AttributeKey.CheckboxColumnClass ) );

                var lavaTemplate = GetAttributeValue( AttributeKey.AttendeeLavaTemplate );

                var mergeFields = Rock.Lava.LavaHelper.GetCommonMergeFields( this.RockPage, this.CurrentPerson );
                mergeFields.Add( "GroupMember", groupMember );

                cbAttendee.Text = lavaTemplate.ResolveMergeFields( mergeFields );
            }
        }


        #endregion

        #region Internal Methods

        /// <summary>
        /// Binds the filter.
        /// </summary>
        protected void BindFilter()
        {

        }

        /// <summary>
        /// Binds the group members grid.
        /// </summary>
        protected void BindRepeat()
        {
            rptrAttendance.ItemDataBound += RptrAttendance_ItemDataBound;
            rptrAttendance.DataSource = _members.OrderBy( gm => gm.Person.LastName )
                                                .ThenBy( gm => gm.Person.FirstName )
                                                .ThenBy( gm => gm.Group.Name );
            rptrAttendance.DataBind();
        }

        #endregion
    }


}