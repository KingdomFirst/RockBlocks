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
using System.Web.UI.WebControls;

using Rock;
using Rock.Attribute;
using Rock.Data;
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
        }

        /// <summary>
        /// Keys to use for Page Parameters
        /// </summary>
        private static class PageParameterKey
        {
            public const string Date = "Date";
            public const string Groups = "Groups";
        }

        #region Private Variables

        private RockContext _rockContext = null;
        private List<Group> _groups = new List<Group>();
        private List<GroupMember> _members = new List<GroupMember>();
        private List<AttendanceAttendee> _attendees;
        private DateTime? _attendanceDate = null;

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

            var allowGroupsPageParameter = GetAttributeValue( AttributeKey.AllowGroupsPageParameter ).AsBoolean();
            var groupIds = GetAttributeValues( AttributeKey.GroupsToDisplay ).AsIntegerList();

            var pageParamGroups = PageParameter( PageParameterKey.Groups );
            if ( allowGroupsPageParameter && pageParamGroups.IsNotNullOrWhiteSpace() )
            {
                groupIds = pageParamGroups.Split( ',' ).AsIntegerList();
            }

            _groups = new GroupService( _rockContext ).GetByIds( groupIds ).ToList();

            foreach ( var group in _groups )
            {
                if ( group != null && ( group.IsAuthorized( Authorization.MANAGE_MEMBERS, CurrentPerson ) || group.IsAuthorized( Authorization.EDIT, CurrentPerson ) ) )
                {
                    _members.AddRange( group.ActiveMembers() );
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
                            int personId = hdnAttendeeId.ValueAsInt();

                            var attendance = _attendees.Where( a => a.PersonId == personId ).FirstOrDefault();
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
            var cbAttendee = e.Item.FindControl( "cbAttendee" ) as RockCheckBox;
            var pnlCardCheckbox = e.Item.FindControl( "pnlCardCheckbox" ) as Panel;

            if ( attendee != null && cbAttendee != null && pnlCardCheckbox != null )
            {
                pnlCardCheckbox.AddCssClass( GetAttributeValue( AttributeKey.CheckboxColumnClass ) );
                cbAttendee.Checked = attendee.Attended;

                var lavaTemplate = GetAttributeValue( AttributeKey.AttendeeLavaTemplate );

                var mergeFields = Rock.Lava.LavaHelper.GetCommonMergeFields( this.RockPage, this.CurrentPerson );
                mergeFields.Add( "Person", _members.Where( gm => gm.PersonId == attendee.PersonId ).Select( gm => gm.Person ).FirstOrDefault() );
                mergeFields.Add( "Attended", attendee.Attended );
                mergeFields.Add( "GroupMembers", _members.Where( gm => attendee.GroupMemberIds.Contains( gm.Id ) ) );
                mergeFields.Add( "Groups", _groups.Where( g => attendee.Groups.Contains( g.Id ) ) );

                cbAttendee.Text = lavaTemplate.ResolveMergeFields( mergeFields );
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

        #endregion

        #region Internal Methods

        /// <summary>
        /// Binds the fields.
        /// </summary>
        protected void BindFields()
        {
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
        }

        /// <summary>
        /// Binds the group members/attendees repeater.
        /// </summary>
        protected void BindRepeat()
        {
            var attendedIds = new List<int>();
            var groupIds = _groups.Select( g => g.Id ).ToList();
            attendedIds = new AttendanceService( _rockContext )
                .Queryable().AsNoTracking()
                .Where( a =>
                    DbFunctions.DiffDays( a.StartDateTime, _attendanceDate ) == 0 &&
                    a.DidAttend.HasValue &&
                    a.DidAttend.Value &&
                    a.Occurrence != null &&
                    groupIds.Contains( a.Occurrence.GroupId.Value ) &&
                    a.PersonAlias != null )
                .Select( a => a.PersonAlias.PersonId )
                .Distinct()
                .ToList();
            var searchParts = tbSearch.Text.ToLower().SplitDelimitedValues();
            _attendees = _members
                .Where( gm => tbSearch.Text.IsNullOrWhiteSpace() ||
                      ( searchParts.Length == 1 && gm.Person.LastName.ToLower().StartsWith( searchParts[0] ) ) ||
                      ( searchParts.Length > 1 && ( gm.Person.FirstName.ToLower().StartsWith( searchParts[0] ) ||
                                                    gm.Person.NickName.ToLower().StartsWith( searchParts[0] )
                                                  ) && gm.Person.LastName.ToLower().StartsWith( searchParts[searchParts.Length - 1] )
                      ) ||
                      ( searchParts.Length > 1 && ( gm.Person.FirstName.ToLower().StartsWith( searchParts[searchParts.Length - 1] ) ||
                                                    gm.Person.NickName.ToLower().StartsWith( searchParts[searchParts.Length - 1] )
                                                  ) && gm.Person.LastName.ToLower().StartsWith( searchParts[0] )
                      )
                )
                .OrderBy( gm => gm.Person.LastName )
                                                    .ThenBy( gm => gm.Person.FirstName )
                                                    .ThenBy( gm => gm.Group.Name )
                                                    .GroupBy( gm => gm.PersonId )
                                                    .Select( g => new AttendanceAttendee
                                                    {
                                                        PersonId = g.Key,
                                                        GroupMemberIds = g.Select( gm => gm.Id ).ToList(),
                                                        Attended = attendedIds.Contains( g.Key ),
                                                        Groups = g.Select( gm => gm.GroupId ).ToList()
                                                    } )
                                                    .ToList();

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
                            var scheduleId = group.ScheduleId;
                            AttendanceOccurrence occurrence = null;

                            var occurrenceKey = $"{group.Id}_{scheduleId}";

                            if ( occurrence == null && occurrences.ContainsKey( occurrenceKey ) )
                            {
                                occurrence = occurrences.GetValueOrNull( occurrenceKey );
                            }

                            if ( occurrence == null && _attendanceDate.HasValue )
                            {
                                occurrence = occurrenceService.Get( _attendanceDate.Value.Date, group.Id, null, scheduleId );
                            }

                            if ( occurrence == null )
                            {
                                occurrence = new AttendanceOccurrence();
                                occurrence.GroupId = group.Id;
                                occurrence.ScheduleId = scheduleId;
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

                    return true;
                }
                else
                {
                    return false;
                }
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
    }
}