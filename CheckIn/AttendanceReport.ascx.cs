// <copyright>
// Copyright 2019 by Kingdom First Solutions
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
using Rock.Web.Cache;
using Rock.Web.UI;

namespace RockWeb.Plugins.rocks_kfs.CheckIn
{
    /// <summary>
    /// Block that provides attendance reporting.
    /// </summary>

    #region Block Attributes

    [DisplayName( "Attendance Report" )]
    [Category( "KFS > Check-in" )]
    [Description( "Block that provides attendance reporting." )]

    #endregion

    #region Block Settings

    [GroupTypeField(
        name: "Default Check-In Group Type",
        description: "The default Check-In Configuration when the page loads.",
        required: true,
        groupTypePurposeValueGuid: Rock.SystemGuid.DefinedValue.GROUPTYPE_PURPOSE_CHECKIN_TEMPLATE,
        defaultGroupTypeGuid: Rock.SystemGuid.GroupType.GROUPTYPE_WEEKLY_SERVICE_CHECKIN_AREA,
        order: 0,
        key: AttributeKeys.DefaultCheckInGroupType )]
    [CategoryField(
        name: "Schedule Category(s)",
        description: "The optional schedule categories that should be included as an option to filter attendance for. If a category is not selected, all schedules will be included.",
        allowMultiple: true,
        entityTypeName: "Rock.Model.Schedule",
        entityTypeQualifierColumn: "",
        entityTypeQualifierValue: "",
        required: false,
        defaultValue: "",
        category: "",
        order: 1,
        key: AttributeKeys.ScheduleCategories )]
    [BooleanField(
        name: "Show Campus Filter",
        defaultValue: true,
        order: 2,
        key: AttributeKeys.ShowCampusFilter )]
    [CampusField( "Default Campus", "An optional default campus to set filter groups.", false, "", "", 3, AttributeKeys.DefaultCampus )]

    #endregion

    public partial class AttendanceReport : RockBlock
    {
        private static class AttributeKeys
        {
            public const string DefaultCheckInGroupType = "DefaultCheckInGroupType";
            public const string ScheduleCategories = "ScheduleCategories";
            public const string ShowCampusFilter = "ShowCampusFilter";
            public const string DefaultCampus = "DefaultCampus";
        }

        #region Fields

        private RockContext _rockContext = null;

        #endregion

        #region Properties

        public DateTime? SelectedDate;
        public string SelectedCampus;
        public string CheckInConfigurationTypeId;
        public string CheckInAreaTypeId;
        public string SelectedSchedule;

        public Dictionary<int, string> Schedules
        {
            get { return ViewState["Schedules"] as Dictionary<int, string> ?? GetSchedules(); }
            set { ViewState["Schedules"] = value; }
        }

        #endregion

        #region Base Control Methods

        /// <summary>
        /// Restores the view-state information from a previous user control request that was saved by the <see cref="M:System.Web.UI.UserControl.SaveViewState" /> method.
        /// </summary>
        /// <param name="savedState">An <see cref="T:System.Object" /> that represents the user control state to be restored.</param>
        protected override void LoadViewState( object savedState )
        {
            base.LoadViewState( savedState );

            SelectedDate = ViewState["SelectedDate"] as DateTime?;
            SelectedCampus = ViewState["SelectedCampus"] as string;
            CheckInConfigurationTypeId = ViewState["CheckInConfigurationTypeId"] as string;
            CheckInAreaTypeId = ViewState["CheckInAreaTypeId"] as string;
            SelectedSchedule = ViewState["SelectedSchedule"] as string;
        }

        /// <summary>
        /// Saves any user control view-state changes that have occurred since the last page postback.
        /// </summary>
        /// <returns>
        /// Returns the user control's current view state. If there is no view state associated with the control, it returns null.
        /// </returns>
        protected override object SaveViewState()
        {
            ViewState["SelectedDate"] = dpDate.SelectedDate;
            ViewState["SelectedCampus"] = cpCampus.SelectedValue;
            ViewState["CheckInConfigurationTypeId"] = ddlCheckInConfiguration.SelectedValue;
            ViewState["CheckInAreaTypeId"] = ddlCheckInArea.SelectedValue;
            ViewState["SelectedSchedule"] = ddlSchedule.SelectedValue;

            return base.SaveViewState();
        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            _rockContext = new RockContext();

            this.BlockUpdated += Block_BlockUpdated;
            this.AddConfigurationUpdateTrigger( upnlContent );
        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );

            if ( !IsPostBack )
            {
                if ( !SelectedDate.HasValue )
                {
                    SelectedDate = RockDateTime.Today;
                    dpDate.SelectedDate = SelectedDate;
                }
                SetCampusFilterVisibility();
                LoadCheckInConfigurationGroupTypes();
                LoadCheckInAreas();
                LoadSchedules();
                GetAttendees();
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
            SetCampusFilterVisibility();
            LoadSchedules();
            LoadCheckInConfigurationGroupTypes();
            ddlCheckInConfiguration.SelectedValue = GetDefaultGroupTypeId().ToString();
            LoadCheckInAreas();
        }

        /// <summary>
        /// Handles the SelectedIndexChanged event of the cpCampus control.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void cpCampus_SelectedIndexChanged( object sender, EventArgs e )
        {
            SelectedCampus = cpCampus.SelectedValue;

            if ( CheckInAreaTypeId.IsNotNullOrWhiteSpace() )
            {
                LoadCheckInGroups();
            }
        }

        /// <summary>
        /// Handles the SelectDate event of the dpDate control.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void dpDate_SelectDate( object sender, EventArgs e )
        {
            SelectedDate = dpDate.SelectedDate;

            if ( CheckInAreaTypeId.IsNotNullOrWhiteSpace() )
            {
                LoadCheckInGroups();
            }
        }

        /// <summary>
        /// Handles the SelectedIndexChanged event of the ddlSchedule control.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void ddlSchedule_SelectedIndexChanged( object sender, EventArgs e )
        {
            SelectedSchedule = ddlSchedule.SelectedValue;

            if ( CheckInAreaTypeId.IsNotNullOrWhiteSpace() )
            {
                LoadCheckInGroups();
            }
        }

        /// <summary>
        /// Handles the SelectedIndexChanged event of the ddlCheckInGroupType control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void ddlCheckInConfiguration_SelectedIndexChanged( object sender, EventArgs e )
        {
            if ( ddlCheckInConfiguration.SelectedValue.IsNullOrWhiteSpace() )
            {
                ddlCheckInConfiguration.SelectedValue = GetDefaultGroupTypeId().ToString();
            }

            CheckInConfigurationTypeId = ddlCheckInConfiguration.SelectedValue;
            CheckInAreaTypeId = null;
            LoadCheckInAreas();
            LoadCheckInGroups();
        }

        /// <summary>
        /// Handles the SelectedIndexChanged event of the ddlCheckInArea control.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void ddlCheckInArea_SelectedIndexChanged( object sender, EventArgs e )
        {
            CheckInAreaTypeId = ddlCheckInArea.SelectedValue;
            LoadCheckInGroups();
        }

        /// <summary>
        /// Handles the Click event of the btnRunReport control.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void btnRunReport_Click( object sender, EventArgs e )
        {
            GetAttendees();
        }

        #endregion

        #region Methods

        /// <summary>
        /// Sets the campus filter visibility.
        /// </summary>
        private void SetCampusFilterVisibility()
        {
            cpCampus.Visible = GetAttributeValue( AttributeKeys.ShowCampusFilter ).AsBoolean();

            if ( cpCampus.Visible )
            {
                if ( GetAttributeValue( AttributeKeys.DefaultCampus ).IsNotNullOrWhiteSpace() )
                {
                    cpCampus.SelectedCampusId = CampusCache.Get( GetAttributeValue( AttributeKeys.DefaultCampus ) ).Id;
                    SelectedCampus = cpCampus.SelectedValue;
                }
            }
            else
            {
                cpCampus.SelectedValue = null;
                SelectedCampus = null;
                SetAttributeValue( AttributeKeys.DefaultCampus, null );
            }
        }

        /// <summary>
        /// Retrieves and binds the schedule dropdown list.
        /// </summary>
        private void LoadSchedules()
        {
            ddlSchedule.DataSource = this.Schedules;
            ddlSchedule.DataTextField = "Value";
            ddlSchedule.DataValueField = "Key";
            ddlSchedule.DataBind();
        }

        /// <summary>
        /// Gets a dictionary of schedule id and name for dropdown list.
        /// </summary>
        /// <returns></returns>
        private Dictionary<int, string> GetSchedules()
        {
            var categoryIds = new List<int>();
            var schedules = new Dictionary<int, string>();

            using ( var rockContext = new RockContext() )
            {
                var categoriesGuids = GetAttributeValues( AttributeKeys.ScheduleCategories ).AsGuidList();
                if ( categoriesGuids.Count > 0 )
                {
                    categoryIds = new CategoryService( rockContext ).GetByGuids( categoriesGuids ).Select( c => c.Id ).ToList();
                }

                schedules = new ScheduleService( rockContext )
                    .Queryable()
                    .AsNoTracking()
                    .Where( s => s.CategoryId.HasValue && categoryIds.Contains( s.CategoryId.Value ) )
                    .Distinct()
                    .ToDictionary( k => k.Id, v => v.Name );
            }

            return schedules;
        }

        /// <summary>
        /// Gets the default Check-In Configuration Type Id from the block setting.
        /// </summary>
        /// <returns></returns>
        private int GetDefaultGroupTypeId()
        {
            var guid = GetAttributeValue( AttributeKeys.DefaultCheckInGroupType ).AsGuid();
            return new GroupTypeService( _rockContext ).Get( guid ).Id;
        }

        /// <summary>
        /// Loads the Configuration Check-In Types for the filter dropdown.
        /// </summary>
        private void LoadCheckInConfigurationGroupTypes()
        {
            var groupTypePurposeGuid = Rock.SystemGuid.DefinedValue.GROUPTYPE_PURPOSE_CHECKIN_TEMPLATE.AsGuid();
            ddlCheckInConfiguration.GroupTypes = new GroupTypeService( _rockContext )
                .Queryable()
                .AsNoTracking()
                .Where( c => c.GroupTypePurposeValue.Guid == groupTypePurposeGuid )
                .Distinct()
                .OrderBy( c => c.Order )
                .ThenBy( c => c.Name )
                .ToList();
            ddlCheckInConfiguration.SelectedValue = GetDefaultGroupTypeId().ToString();
            CheckInConfigurationTypeId = ddlCheckInConfiguration.SelectedValue;
        }

        /// <summary>
        /// Loads the Check-In Areas for the filter dropdown.
        /// </summary>
        private void LoadCheckInAreas()
        {
            var checkinFilterId = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.GROUPTYPE_PURPOSE_CHECKIN_FILTER ).Id;
            ddlCheckInArea.GroupTypes = new GroupTypeService( _rockContext )
                .GetAllAssociatedDescendentsOrdered( CheckInConfigurationTypeId.AsInteger() )
                .Where( t => !t.GroupTypePurposeValueId.HasValue || ( t.GroupTypePurposeValueId.HasValue && t.GroupTypePurposeValueId != checkinFilterId ) )
                .Distinct()
                .OrderBy( a => a.Order )
                .ThenBy( a => a.Name )
                .ToList();
        }

        /// <summary>
        /// Loads the check in groups.
        /// </summary>
        private void LoadCheckInGroups()
        {
            var items = new Dictionary<int, string>();

            var occurrenceIds = GetOccurrencesQuery()
                .Select( o => o.Id )
                .ToList();

            var occurrencesAndGroups = GetOccurrencesQuery()
                .ToDictionary( k => k.Id, v => v.GroupId.Value );

            // if we have occurrences for the day, keep going
            if ( occurrenceIds.Count > 0 )
            {
                // get the attendance from the occurences
                var attendanceQuery = new AttendanceService( _rockContext )
                    .Queryable()
                    .AsNoTracking()
                    .Where( a => occurrenceIds.Contains( a.OccurrenceId ) );

                if ( SelectedCampus.IsNotNullOrWhiteSpace() )
                {
                    var campusId = SelectedCampus.AsInteger();
                    attendanceQuery = attendanceQuery.Where( a => a.CampusId.HasValue && a.CampusId.Value.Equals( campusId ) );
                }

                var attendanceDictionary = attendanceQuery
                    .ToDictionary( k => k.Id, v => v.OccurrenceId );

                // if we have attendance, keep going
                if ( attendanceDictionary.Count > 0 )
                {
                    // get groups with attendance
                    var attendanceGroups = occurrencesAndGroups
                        .Where( o => attendanceDictionary.ContainsValue( o.Key ) )
                        .Select( o => o.Value )
                        .Distinct()
                        .ToList();

                    // get the group types
                    var checkinAreaTypeId = CheckInAreaTypeId.AsInteger();
                    var checkinFilterId = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.GROUPTYPE_PURPOSE_CHECKIN_FILTER ).Id;
                    var groupTypeIds = new GroupTypeService( _rockContext )
                        .GetAllAssociatedDescendentsOrdered( checkinAreaTypeId )
                        .Where( t => !t.GroupTypePurposeValueId.HasValue || ( t.GroupTypePurposeValueId.HasValue && t.GroupTypePurposeValueId != checkinFilterId ) )
                        .Distinct()
                        .Select( t => t.Id )
                        .ToList();

                    if ( !groupTypeIds.Contains( checkinAreaTypeId ) )
                    {
                        groupTypeIds.Add( checkinAreaTypeId );
                    }

                    // get the groups with attendance
                    items = new GroupService( _rockContext )
                        .Queryable()
                        .AsNoTracking()
                        .Where( g => g.IsActive &&
                             groupTypeIds.Contains( g.GroupTypeId ) &&
                             attendanceGroups.Contains( g.Id ) )
                        .Distinct()
                        .OrderBy( g => g.Order )
                        .ThenBy( g => g.Name )
                        .ToDictionary( k => k.Id, v => v.Name );
                }
            }

            cblGroups.Items.Clear();
            cblGroups.DataSource = items;
            cblGroups.DataTextField = "Value";
            cblGroups.DataValueField = "Key";
            cblGroups.DataBind();

            GetAttendees();
        }

        /// <summary>
        /// Gets the attendees.
        /// </summary>
        private void GetAttendees()
        {
            var attendance = new List<Attendance>();

            var selectedGroups = cblGroups.SelectedValues.AsIntegerList();

            if ( selectedGroups.Count > 0 )
            {
                var occurrences = GetOccurrencesQuery()
                    .Where( o => selectedGroups.Contains( o.GroupId.Value ) )
                    .ToList();

                var occurrenceIds = GetOccurrencesQuery()
                    .Where( o => selectedGroups.Contains( o.GroupId.Value ) )
                    .Select( o => o.Id )
                    .ToList();

                attendance = new AttendanceService( _rockContext )
                    .Queryable()
                    .AsNoTracking()
                    .Where( a => occurrenceIds.Contains( a.OccurrenceId ) )
                    .ToList();
            }

            gAttendees.DataSource = attendance;
            gAttendees.AutoGenerateColumns = true;
            gAttendees.DataBind();
        }

        /// <summary>
        /// Gets the occurrences query.
        /// </summary>
        /// <returns></returns>
        private IQueryable<AttendanceOccurrence> GetOccurrencesQuery()
        {
            // get the occurrences for SelectedDate with Groups
            var occurrenceQuery = new AttendanceOccurrenceService( _rockContext )
                .Queryable()
                .AsNoTracking()
                .Where( o => o.OccurrenceDate == SelectedDate.Value && o.GroupId.HasValue );

            // if filtering by campus
            if ( SelectedSchedule.IsNotNullOrWhiteSpace() )
            {
                var scheduleId = SelectedSchedule.AsInteger();
                occurrenceQuery = occurrenceQuery.Where( o => o.ScheduleId.HasValue && o.ScheduleId.Value == scheduleId );
            }

            return occurrenceQuery;
        }

        #endregion
    }
}
