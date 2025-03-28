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
using System.Data.Entity;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;

using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Lava;
using Rock.Model;
using Rock.Security;
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
        Order = 1,
        Key = AttributeKey.GroupsToDisplay )]

    [CustomEnhancedListField( "Group Type Roles to Display",
        Description = "Select the group type role(s) to display in this attendance block. You may also pass in a comma separated list of GroupTypeRoleId's via a PageParameter 'GroupRoles'.",
        ListSource = @"SELECT gtr.[Id] as Value, CONCAT(gt.[Name],' > ',gtr.[Name]) as Text 
            FROM GroupTypeRole gtr 
            JOIN GroupType gt ON gt.Id = gtr.GroupTypeId
            ORDER BY gt.[Name], gtr.[Order]",
        IsRequired = false,
        Order = 1,
        Key = AttributeKey.GroupTypeRolesToDisplay )]

    [LavaField( "Attendee Lava Template",
        Description = "Lava template used to customize appearance of individual attendee selectors.",
        IsRequired = true,
        DefaultValue = DefaultValue.AttendeeLavaTemplate,
        Order = 2,
        Key = AttributeKey.AttendeeLavaTemplate )]

    [TextField( "Checkbox Column Class",
        Description = "The Bootstrap 3 CSS classes to use for column width on various screen sizes. Default: col-xs-12 col-sm-6 col-md-3 col-lg-2",
        DefaultValue = "col-xs-12 col-sm-6 col-md-3 col-lg-2",
        Order = 3,
        Key = AttributeKey.CheckboxColumnClass )]

    [BooleanField( "Display Group Names",
        Description = "Display the group names after the block name in the panel title. Default: Yes",
        DefaultBooleanValue = true,
        Order = 4,
        Key = AttributeKey.DisplayGroupNames )]

    [BooleanField( "Allow Groups from Page Parameter",
        Description = "Allow GroupId's to be passed in via Page Parameter 'Groups' as a comma separated list. The current user must have permission to the groups for members to display. Default: No",
        DefaultBooleanValue = false,
        Order = 5,
        Key = AttributeKey.AllowGroupsPageParameter )]

    [LavaField( "Intro Lava Template",
        Description = "Lava template used to display instructions or group information.",
        IsRequired = false,
        DefaultValue = DefaultValue.IntroLavaTemplate,
        Order = 6,
        Key = AttributeKey.IntroLavaTemplate )]

    [BooleanField( "Display LastName buttons",
        Description = "Display a row of buttons with the first letter of LastName buttons. Default: Yes",
        DefaultBooleanValue = true,
        Order = 7,
        Key = AttributeKey.DisplayLastNameButtons )]

    [BooleanField( "Allow Adding Person",
        Description = "Should block support adding new people as attendees?",
        DefaultBooleanValue = false,
        Order = 8,
        Key = AttributeKey.AllowAddingPerson )]

    [BooleanField( "Combine Group Attendance",
        Description = "Should the block display only one record per person? This will result in a group attendance for that person in each associated group.",
        DefaultBooleanValue = false,
        Order = 9,
        Key = AttributeKey.CombineGroupAttendance )]

    [CustomDropdownListField( "Schedule Selection Mode",
        Description = "Should the block display the schedule picker?",
        ListSource = "Hide,Always allow editing,Only if Single Schedule/Group",
        DefaultValue = "Hide",
        Key = AttributeKey.ScheduleSelectionMode,
        Order = 11 )]

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
            public const string DisplayGroupNames = "DisplayGroupNames";
            public const string AllowGroupsPageParameter = "AllowGroupsPageParameter";
            public const string IntroLavaTemplate = "IntroLavaTemplate";
            public const string DisplayLastNameButtons = "DisplayLastNameButtons";
            public const string AllowAddingPerson = "AllowAddingPerson";
            public const string CombineGroupAttendance = "CombineGroupAttendance";
            public const string GroupTypeRolesToDisplay = "GroupTypeRolesToDisplay";
            public const string ScheduleSelectionMode = "ScheduleSelectionMode";
        }

        /// <summary>
        /// Long default values to use for Block Attributes
        /// </summary>
        private static class DefaultValue
        {
            public const string AttendeeLavaTemplate = @"{% comment %}
  This is the lava template for each attendee item in the GroupAttendanceMulti block
   Available Lava Fields:

   + Person (the person from the group)
   + Attended (whether or not the person is marked DidAttend = true)
   + GroupMembers (the group member records for the Person)
   + Groups (the group(s) the person is a member of)
{% endcomment %}
<img src=""{{ Person.PhotoUrl }}"" class=""img-circle col-xs-3 p-0 mr-3 pull-left""> {{ Person.FullName }}<br>
<small class=""text-muted"">{{ Groups | Join:', ' }}</small>";
            public const string IntroLavaTemplate = @"{% comment %}
  This is the lava template for the introduction of GroupAttendanceMulti block
   Available Lava Fields:

   + Groups (the group(s) that are selected from the block settings)
{% endcomment %}";
        }

        /// <summary>
        /// Keys to use for Page Parameters
        /// </summary>
        private static class PageParameterKey
        {
            public const string Date = "Date";
            public const string Groups = "Groups";
            public const string GroupRoles = "GroupRoles";
        }

        #region Private Variables

        private RockContext _rockContext = null;
        private List<Group> _groups = new List<Group>();
        private List<GroupMember> _members = new List<GroupMember>();
        private List<AttendanceAttendee> _attendees;
        private DateTime? _attendanceDate = null;
        private string _lastnameLetter = "A";
        private bool _combineGroupAttendance = false;

        #endregion

        #region Control Methods

        /// <summary>
        /// Restores the view-state information from a previous user control request that was saved by the <see cref="M:System.Web.UI.UserControl.SaveViewState" /> method.
        /// </summary>
        /// <param name="savedState">An <see cref="T:System.Object" /> that represents the user control state to be restored.</param>
        protected override void LoadViewState( object savedState )
        {
            base.LoadViewState( savedState );
            _attendees = ViewState["Attendees"] as List<AttendanceAttendee>;
        }
        /// <summary>
        /// Saves any user control view-state changes that have occurred since the last page postback.
        /// </summary>
        /// <returns>
        /// Returns the user control's current view state. If there is no view state associated with the control, it returns <see langword="null" />.
        /// </returns>
        protected override object SaveViewState()
        {
            ViewState["Attendees"] = _attendees;
            return base.SaveViewState();
        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            this.BlockUpdated += Block_BlockUpdated;
            this.AddConfigurationUpdateTrigger( pnlContent );
            tbSearch.Attributes["onkeyup"] = $"clearTimeout(window.{tbSearch.ClientID}); window.{tbSearch.ClientID} = setTimeout('__doPostBack(\\'{tbSearch.UniqueID}\\',\\'\\')', 1000)";

            _rockContext = new RockContext();

            _combineGroupAttendance = GetAttributeValue( AttributeKey.CombineGroupAttendance ).AsBoolean();
            var allowGroupsPageParameter = GetAttributeValue( AttributeKey.AllowGroupsPageParameter ).AsBoolean();
            var groupIds = GetAttributeValues( AttributeKey.GroupsToDisplay ).AsIntegerList();
            var groupRoleIds = GetAttributeValues( AttributeKey.GroupTypeRolesToDisplay ).AsIntegerList();

            var pageParamGroups = PageParameter( PageParameterKey.Groups );
            if ( allowGroupsPageParameter && pageParamGroups.IsNotNullOrWhiteSpace() )
            {
                groupIds = pageParamGroups.Split( ',' ).AsIntegerList();
            }

            var pageParamGroupRoles = PageParameter( PageParameterKey.GroupRoles );
            if ( allowGroupsPageParameter && pageParamGroupRoles.IsNotNullOrWhiteSpace() )
            {
                groupRoleIds = pageParamGroupRoles.Split( ',' ).AsIntegerList();
            }

            _groups = new GroupService( _rockContext ).GetByIds( groupIds ).ToList();

            foreach ( var group in _groups )
            {
                if ( group != null && ( group.IsAuthorized( Authorization.MANAGE_MEMBERS, CurrentPerson ) || group.IsAuthorized( Authorization.EDIT, CurrentPerson ) ) )
                {
                    if ( groupRoleIds.Any() )
                    {
                        var members = group.ActiveMembers();
                        _members.AddRange( members.Where( m => groupRoleIds.Contains( m.GroupRoleId ) ) );
                    }
                    else
                    {
                        _members.AddRange( group.ActiveMembers() );
                    }
                }
            }
        }
        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );

            if ( !Page.IsPostBack )
            {
                BuildIntroLava();
                BindFields();
                BindRepeat();
            }
            else
            {
                if ( _attendees != null )
                {
                    foreach ( RepeaterItem item in rptrAttendance.Items )
                    {
                        var hdnAttendeeId = item.FindControl( "hdnAttendeeId" ) as HiddenField;
                        var cbAttendee = item.FindControl( "cbAttendee" ) as RockCheckBox;

                        if ( hdnAttendeeId != null && cbAttendee != null )
                        {
                            var attendeeId = hdnAttendeeId.Value.SplitDelimitedValues( false );
                            var personId = attendeeId[0].ToIntSafe( -1 );
                            var groupId = attendeeId.Length > 1 ? attendeeId[1].ToIntSafe( -1 ) : -1;

                            var attendance = _attendees.Where( a => ( _combineGroupAttendance && a.PersonId == personId ) || ( a.PersonId == personId && a.Groups.Contains( groupId ) ) ).FirstOrDefault();
                            if ( attendance != null )
                            {
                                attendance.Attended = cbAttendee.Checked;
                            }
                        }
                    }
                }
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
            BuildIntroLava();
            BindFields();
            BindRepeat();
        }

        /// <summary>
        /// Handles the SelectDate event of the dpAttendanceDate control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void dpAttendanceDate_SelectDate( object sender, EventArgs e )
        {
            BindFields();
            BindRepeat();
        }

        /// <summary>
        /// Handles the TextChanged event of the tbSearch control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void tbSearch_TextChanged( object sender, EventArgs e )
        {
            BindFields();
            BindRepeat();
            tbSearch.Focus();
        }

        /// <summary>
        /// Handles the ItemDataBound event of the RptrAttendance control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Web.UI.WebControls.RepeaterItemEventArgs"/> instance containing the event data.</param>
        private void RptrAttendance_ItemDataBound( object sender, System.Web.UI.WebControls.RepeaterItemEventArgs e )
        {
            var attendee = e.Item.DataItem as AttendanceAttendee;
            var hdnAttendeeId = e.Item.FindControl( "hdnAttendeeId" ) as HiddenField;
            var cbAttendee = e.Item.FindControl( "cbAttendee" ) as RockCheckBox;
            var pnlCardCheckbox = e.Item.FindControl( "pnlCardCheckbox" ) as Panel;
            var lAnchor = e.Item.FindControl( "lAnchor" ) as Literal;

            if ( attendee != null && cbAttendee != null && pnlCardCheckbox != null )
            {
                if ( !_combineGroupAttendance )
                {
                    hdnAttendeeId.Value = $"{attendee.PersonId}|{attendee.Groups.FirstOrDefault().ToString()}";
                }
                pnlCardCheckbox.AddCssClass( GetAttributeValue( AttributeKey.CheckboxColumnClass ) );
                cbAttendee.Checked = attendee.Attended;

                var lavaTemplate = GetAttributeValue( AttributeKey.AttendeeLavaTemplate );

                var person = _members.Where( gm => gm.PersonId == attendee.PersonId ).Select( gm => gm.Person ).FirstOrDefault();

                if ( person == null )
                {
                    person = new PersonService( _rockContext ).Get( attendee.PersonId );
                }

                if ( person.LastName.Left( 1 ) != _lastnameLetter )
                {
                    _lastnameLetter = person.LastName.Left( 1 ).ToUpper();
                    lAnchor.Visible = true;
                    lAnchor.Text = $"<a name='lastname{_lastnameLetter}' id='lastname{_lastnameLetter}'></a>";
                }

                var mergeFields = LavaHelper.GetCommonMergeFields( this.RockPage, this.CurrentPerson );
                mergeFields.Add( "Person", person );
                mergeFields.Add( "Attended", attendee.Attended );
                mergeFields.Add( "GroupMembers", _members.Where( gm => attendee.GroupMemberIds.Contains( gm.Id ) ) );
                mergeFields.Add( "Groups", _groups.Where( g => attendee.Groups.Contains( g.Id ) ) );

                cbAttendee.Text = lavaTemplate.ResolveMergeFields( mergeFields );

                if ( cbAttendee.Text.IsNullOrWhiteSpace() )
                {
                    pnlCardCheckbox.Visible = false;
                }
            }
        }

        /// <summary>
        /// Handles the CheckedChanged event of the cbAttendee control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void cbAttendee_CheckedChanged( object sender, EventArgs e )
        {
            var cbAttendee = sender as RockCheckBox;

            if ( cbAttendee != null )
            {
                SaveAttendance();
            }
        }

        /// <summary>
        /// Handles the SelectedIndexChanged event of the ddlAddPersonGroup control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void ddlAddPersonGroup_SelectedIndexChanged( object sender, EventArgs e )
        {
            if ( ddlAddPersonGroup.SelectedValue != "" )
            {
                ddlAddPersonGroup.Visible = false;
                ppAddPerson.Visible = true;
                lbClearPerson.Visible = true;
                ppAddPerson.PersonName = $"Add New {ddlAddPersonGroup.SelectedItem.Text} Attendee";
                ScriptManager.RegisterStartupScript( Page, Page.GetType(), "OpenPersonPicker", "setTimeout(function() { $('.js-personpicker-toggle').click(); },500);", true );
            }
        }

        /// <summary>
        /// Handles the SelectPerson event of the ppAddPerson control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void ppAddPerson_SelectPerson( object sender, EventArgs e )
        {
            if ( ppAddPerson.PersonId.HasValue )
            {
                var groupId = ddlAddPersonGroup.SelectedValue.ToIntSafe();
                if ( _attendees != null && !_attendees.Any( a => a.PersonId == ppAddPerson.PersonId.Value && a.Groups.Contains( groupId ) ) )
                {
                    var person = new PersonService( _rockContext ).Get( ppAddPerson.PersonId.Value );
                    if ( person != null )
                    {
                        var attendee = new AttendanceAttendee();
                        attendee.PersonId = person.Id;
                        attendee.Attended = true;
                        attendee.FirstName = person.FirstName;
                        attendee.LastName = person.LastName;
                        attendee.NickName = person.NickName;
                        attendee.Groups = new List<int> { groupId };
                        attendee.GroupName = ddlAddPersonGroup.SelectedItem.Text;

                        _attendees.Add( attendee );
                        SaveAttendance();
                        BindRepeat();
                    }
                }
            }

            ppAddPerson.SelectedValue = null;
            ppAddPerson.Visible = false;
            lbClearPerson.Visible = false;
            ddlAddPersonGroup.SelectedIndex = 0;
            ddlAddPersonGroup.Visible = true;
            ppAddPerson.PersonName = "Add New Attendee";
        }

        /// <summary>
        /// Handles the Click event of the lbClearPerson control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void lbClearPerson_Click( object sender, EventArgs e )
        {
            ppAddPerson.SelectedValue = null;
            ppAddPerson.Visible = false;
            lbClearPerson.Visible = false;
            ddlAddPersonGroup.SelectedIndex = 0;
            ddlAddPersonGroup.Visible = true;
            ppAddPerson.PersonName = "Add New Attendee";
        }
        #endregion

        #region Internal Methods

        /// <summary>
        /// Binds the fields.
        /// </summary>
        protected void BindFields()
        {
            divLastnameButtonRow.Visible = GetAttributeValue( AttributeKey.DisplayLastNameButtons ).AsBoolean();
            pnlAddPerson.Visible = GetAttributeValue( AttributeKey.AllowAddingPerson ).AsBoolean();
            ppAddPerson.PersonName = "Add New Attendee";
            var displayGroupName = GetAttributeValue( AttributeKey.DisplayGroupNames ).AsBoolean();
            if ( displayGroupName )
            {
                lHeading.Text = $"{BlockName} - {_groups.Select( g => g.Name ).JoinStringsWithCommaAnd()}";
            }
            else
            {
                lHeading.Text = BlockName;
            }

            if ( !_members.Any() )
            {
                nbNotice.Text = "There are no members to display for the selected group(s). Please check if you have permission to Manage Members or Edit the group(s) selected.";
            }

            _attendanceDate = PageParameter( PageParameterKey.Date ).AsDateTime();

            if ( _attendanceDate.HasValue )
            {
                lAttendanceDate.Text = _attendanceDate.ToShortDateString();
                lAttendanceDate.Visible = true;
                dpAttendanceDate.Visible = false;
            }
            else
            {
                dpAttendanceDate.SelectedDate = dpAttendanceDate.SelectedDate ?? RockDateTime.Now;
                _attendanceDate = dpAttendanceDate.SelectedDate;
                dpAttendanceDate.Visible = true;
                lAttendanceDate.Visible = false;
            }

            lSchedule.Text = _groups.Where( g => g.Schedule != null ).ToList().Select( g => g.Schedule.FriendlyScheduleText ).Distinct().JoinStrings( ", " );

            lSchedule.Visible = lSchedule.Text.IsNotNullOrWhiteSpace();

            var scheduleSelectionMode = GetAttributeValue( AttributeKey.ScheduleSelectionMode );

            if ( scheduleSelectionMode == "Always allow editing" || ( !lSchedule.Text.Contains( ',' ) && scheduleSelectionMode != "Hide" ) )
            {
                spSchedule.Visible = true;
                lSchedule.Visible = false;
            }
            else
            {
                spSchedule.Visible = false;
            }

            if ( !lSchedule.Text.Contains( ',' ) )
            {
                spSchedule.SetValue( _groups.FirstOrDefault( g => g.Schedule != null )?.Schedule );
                spSchedule.ItemName = lSchedule.Text;
            }

            ddlAddPersonGroup.DataSource = _groups.OrderBy( g => g.Name ).ToList();
            ddlAddPersonGroup.DataValueField = "Id";
            ddlAddPersonGroup.DataTextField = "Name";
            ddlAddPersonGroup.DataBind();

            ddlAddPersonGroup.Items.Insert( 0, new ListItem( "Add New Attendee to Group", "" ) );
            ddlAddPersonGroup.Visible = true;
            ppAddPerson.Visible = false;
            lbClearPerson.Visible = false;
        }

        /// <summary>
        /// Binds the group members/attendees repeater.
        /// </summary>
        protected void BindRepeat()
        {
            var groupIds = _groups.Select( g => g.Id ).ToList();
            _attendees = new List<AttendanceAttendee>();
            var attended = new AttendanceService( _rockContext )
                .Queryable().AsNoTracking()
                .Where( a =>
                    DbFunctions.DiffDays( a.StartDateTime, _attendanceDate ) == 0 &&
                    a.DidAttend.HasValue &&
                    a.DidAttend.Value &&
                    a.Occurrence != null &&
                    groupIds.Contains( a.Occurrence.GroupId.Value ) &&
                    a.PersonAlias != null )
                .Select( a => new AttendanceAttendee
                {
                    PersonId = a.PersonAlias.PersonId,
                    GroupMemberIds = a.Occurrence.Group.Members.Where( gm => gm.PersonId == a.PersonAlias.PersonId ).Select( gm => gm.Id ).ToList(),
                    Attended = a.DidAttend ?? false,
                    Groups = new List<int> { a.Occurrence.GroupId.Value },
                    Schedules = new List<int> { a.Occurrence.ScheduleId ?? 0 },
                    FirstName = a.PersonAlias.Person.FirstName,
                    NickName = a.PersonAlias.Person.NickName,
                    LastName = a.PersonAlias.Person.LastName,
                    GroupName = ( a.Occurrence != null && a.Occurrence.Group != null ) ? a.Occurrence.Group.Name : ""
                } ).ToList();

            if ( _combineGroupAttendance )
            {

                _attendees.AddRange( _members.GroupBy( gm => gm.PersonId )
                                         .Select( g => new AttendanceAttendee
                                         {
                                             PersonId = g.Key,
                                             GroupMemberIds = g.Select( gm => gm.Id ).ToList(),
                                             Attended = attended.Any( a => a.PersonId == g.Key ),
                                             Groups = g.Select( gm => gm.GroupId ).ToList(),
                                             Schedules = attended.Where( a => a.PersonId == g.Key ).SelectMany( a => a.Schedules ).ToList(),
                                             FirstName = g.Min( gm => gm.Person.FirstName ),
                                             NickName = g.Min( gm => gm.Person.NickName ),
                                             LastName = g.Min( gm => gm.Person.LastName ),
                                             GroupName = g.Min( gm => gm.Group.Name )
                                         } )
                                         .ToList() );

                _attendees.AddRange( attended.Where( at => !_attendees.Any( a => a.PersonId == at.PersonId && at.Groups.Any( g => a.Groups.Contains( g ) ) ) )
                                         .GroupBy( gm => gm.PersonId )
                                         .Select( at => new AttendanceAttendee
                                         {
                                             PersonId = at.Key,
                                             GroupMemberIds = at.SelectMany( aa => aa.GroupMemberIds ).ToList(),
                                             Attended = attended.Any( a => a.PersonId == at.Key ),
                                             Groups = at.SelectMany( aa => aa.Groups ).ToList(),
                                             Schedules = at.SelectMany( aa => aa.Schedules ).ToList(),
                                             FirstName = at.Min( aa => aa.FirstName ),
                                             NickName = at.Min( aa => aa.NickName ),
                                             LastName = at.Min( aa => aa.LastName ),
                                             GroupName = at.Min( aa => aa.GroupName )
                                         } )
                                         .ToList() );
            }
            else
            {
                _attendees.AddRange( _members.Where( gm => !_attendees.Any( a => a.PersonId == gm.PersonId && a.Groups.Contains( gm.GroupId ) ) )
                             .Select( gm => new AttendanceAttendee
                             {
                                 PersonId = gm.PersonId,
                                 GroupMemberIds = new List<int> { gm.Id },
                                 Attended = attended.Any( a => a.PersonId == gm.PersonId && a.Groups.Contains( gm.GroupId ) ),
                                 Groups = new List<int> { gm.GroupId },
                                 Schedules = attended.Where( a => a.PersonId == gm.PersonId && a.Groups.Contains( gm.GroupId ) ).SelectMany( a => a.Schedules ).ToList(),
                                 FirstName = gm.Person.FirstName,
                                 NickName = gm.Person.NickName,
                                 LastName = gm.Person.LastName,
                                 GroupName = gm.Group.Name
                             } )
                             .ToList() );

                _attendees.AddRange( attended.Where( at => !_attendees.Any( a => a.PersonId == at.PersonId && at.Groups.Any( g => a.Groups.Contains( g ) ) ) ) );
            }

            var searchParts = tbSearch.Text.ToLower().SplitDelimitedValues();
            _attendees = _attendees.Where( a => tbSearch.Text.IsNullOrWhiteSpace() ||
                                          ( searchParts.Length == 1 && a.LastName.ToLower().StartsWith( searchParts[0] ) ) ||
                                          ( searchParts.Length > 1 && ( a.FirstName.ToLower().StartsWith( searchParts[0] ) ||
                                                                        a.NickName.ToLower().StartsWith( searchParts[0] )
                                                                      ) && a.LastName.ToLower().StartsWith( searchParts[searchParts.Length - 1] )
                                          ) ||
                                          ( searchParts.Length > 1 && ( a.FirstName.ToLower().StartsWith( searchParts[searchParts.Length - 1] ) ||
                                                                        a.NickName.ToLower().StartsWith( searchParts[searchParts.Length - 1] )
                                                                      ) && a.LastName.ToLower().StartsWith( searchParts[0] )
                                          )
                                    )
                                    .OrderBy( a => a.LastName )
                                    .ThenBy( a => a.FirstName )
                                    .ThenBy( a => a.GroupName )
                                    .ToList();

            lCount.Text = _attendees.Count( a => a.Attended ).ToString();

            if ( lCount.Text != "0" )
            {
                var attendeeSchedule = _attendees.Where( a => a.Schedules.Any() ).OrderBy( a => a.Attended ).Select( a => a.Schedules ).FirstOrDefault( a => a.Any( s => s != 0 ) ).FirstOrDefault();
                if ( attendeeSchedule != 0 )
                {
                    var schedule = new ScheduleService( _rockContext ).Get( attendeeSchedule );
                    if ( schedule != null )
                    {
                        spSchedule.SetValue( schedule );
                        spSchedule.ItemName = spSchedule.ItemName.IsNullOrWhiteSpace() ? schedule.FriendlyScheduleText : spSchedule.ItemName;
                    }
                }
            }

            rptrAttendance.ItemDataBound += RptrAttendance_ItemDataBound;
            rptrAttendance.DataSource = _attendees;
            rptrAttendance.DataBind();
        }

        /// <summary>
        /// Saves the attendance.
        /// </summary>
        /// <returns>boolean</returns>
        private bool SaveAttendance()
        {
            // Loading a new RockContext when saving data.
            using ( var rockContext = new RockContext() )
            {
                var occurrenceService = new AttendanceOccurrenceService( rockContext );
                var attendanceService = new AttendanceService( rockContext );
                var personAliasService = new PersonAliasService( rockContext );
                var occurrences = new Dictionary<string, AttendanceOccurrence>();

                if ( dpAttendanceDate.Visible && dpAttendanceDate.SelectedDate.HasValue )
                {
                    _attendanceDate = dpAttendanceDate.SelectedDate.Value;
                }
                if ( !_attendanceDate.HasValue )
                {
                    nbNotice.Text = "You must choose a date before selecting a person to record attendance.";
                    return false;
                }
                if ( _attendees != null )
                {
                    foreach ( var attendee in _attendees )
                    {
                        var attendeeGroups = _groups.Where( g => attendee.Groups.Contains( g.Id ) );
                        foreach ( var group in attendeeGroups )
                        {
                            var scheduleId = spSchedule.SelectedValueAsInt();
                            if ( !scheduleId.HasValue )
                            {
                                scheduleId = group.ScheduleId;
                            }

                            AttendanceOccurrence occurrence = null;
                            var locationId = group.GroupLocations.FirstOrDefault()?.LocationId;

                            var occurrenceKey = $"{group.Id}_{scheduleId}_{locationId}";

                            if ( occurrence == null && occurrences.ContainsKey( occurrenceKey ) )
                            {
                                occurrence = occurrences.GetValueOrNull( occurrenceKey );
                            }

                            if ( occurrence == null && _attendanceDate.HasValue )
                            {
                                occurrence = occurrenceService.Get( _attendanceDate.Value.Date, group.Id, locationId, scheduleId );
                            }

                            if ( occurrence == null )
                            {
                                occurrence = new AttendanceOccurrence();
                                occurrence.GroupId = group.Id;
                                occurrence.ScheduleId = scheduleId;
                                occurrence.LocationId = locationId;
                                occurrence.OccurrenceDate = _attendanceDate.Value;
                                occurrenceService.Add( occurrence );
                            }

                            var existingAttendees = occurrence.Attendees.ToList();


                            occurrence.Schedule = occurrence.Schedule == null && occurrence.ScheduleId.HasValue ? new ScheduleService( rockContext ).Get( occurrence.ScheduleId.Value ) : occurrence.Schedule;

                            cvAttendance.IsValid = occurrence.IsValid;
                            if ( !cvAttendance.IsValid )
                            {
                                cvAttendance.ErrorMessage = occurrence.ValidationResults.Select( a => a.ErrorMessage ).ToList().AsDelimited( "<br />" );
                                return false;
                            }

                            var attendance = existingAttendees
                                .Where( a => a.PersonAlias != null && a.PersonAlias.PersonId == attendee.PersonId )
                                .FirstOrDefault();

                            if ( attendance == null )
                            {
                                int? personAliasId = personAliasService.GetPrimaryAliasId( attendee.PersonId );
                                if ( personAliasId.HasValue )
                                {
                                    attendance = new Attendance();
                                    attendance.PersonAliasId = personAliasId;
                                    attendance.CampusId = group.CampusId;
                                    attendance.StartDateTime = occurrence.Schedule != null && occurrence.Schedule.HasSchedule() ? occurrence.OccurrenceDate.Date.Add( occurrence.Schedule.StartTimeOfDay ) : occurrence.OccurrenceDate;
                                    attendance.DidAttend = attendee.Attended;

                                    cvAttendance.IsValid = attendance.IsValid;
                                    if ( !cvAttendance.IsValid )
                                    {
                                        cvAttendance.ErrorMessage = attendance.ValidationResults.Select( a => a.ErrorMessage ).ToList().AsDelimited( "<br />" );
                                        return false;
                                    }

                                    occurrence.Attendees.Add( attendance );
                                }
                            }
                            else
                            {
                                attendance.DidAttend = attendee.Attended;
                            }

                            occurrences.AddOrReplace( occurrenceKey, occurrence );
                        }
                    }
                    rockContext.SaveChanges();

                    lCount.Text = _attendees.Count( a => a.Attended ).ToString();

                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// Builds the intro lava.
        /// </summary>
        private void BuildIntroLava()
        {
            var introLavaTemplate = GetAttributeValue( AttributeKey.IntroLavaTemplate );

            if ( introLavaTemplate.IsNotNullOrWhiteSpace() )
            {
                var mergeFields = LavaHelper.GetCommonMergeFields( this.RockPage, this.CurrentPerson );
                mergeFields.Add( "Groups", _groups );

                lIntroLava.Text = introLavaTemplate.ResolveMergeFields( mergeFields );
            }
        }

        #endregion
    }

    [Serializable]
    public class AttendanceAttendee
    {
        public int PersonId { get; set; }

        public List<int> GroupMemberIds { get; set; }

        public bool Attended { get; set; } = false;

        public List<int> Groups { get; set; }

        public List<int> Schedules { get; set; }

        public string FirstName { get; set; }

        public string NickName { get; set; }

        public string LastName { get; set; }

        public string GroupName { get; set; }
    }
}