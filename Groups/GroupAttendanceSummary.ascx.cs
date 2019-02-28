using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.UI.WebControls;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Security;
using Rock.Web.Cache;
using Rock.Web.UI;
using Rock.Web.UI.Controls;

namespace RockWeb.Plugins.com_kfs.Groups
{
    [DisplayName( "Group Attendance List" )]
    [Category( "KFS > Groups" )]
    [Description( "Lists all the scheduled occurrences for a given group." )]
    public partial class GroupAttendanceSummary : RockBlock, ICustomGridColumns
    {
        #region Private Variables

        private RockContext _rockContext = null;
        private Group _group = null;
        private bool _canView = false;

        private List<DateTime> _possibleAttendances = null;
        private Dictionary<int, string> _scheduleNameLookup = null;

        private bool _currentlyExporting = false;

        #endregion

        #region Control Methods

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            _rockContext = new RockContext();

            int groupId = PageParameter( "GroupId" ).AsInteger();
            _group = new GroupService( _rockContext )
                .Queryable( "GroupLocations" ).AsNoTracking()
                .FirstOrDefault( g => g.Id == groupId );

            if ( _group != null && _group.IsAuthorized( Authorization.VIEW, CurrentPerson ) )
            {
                _group.LoadAttributes( _rockContext );
                _canView = true;
            }

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
                try
                {
                    BindAttendeesGrid();
                }
                catch ( Exception exception )
                {
                    LogAndShowException( exception );
                }

                gAttendeesAttendance.Actions.ShowBulkUpdate = false;
                gAttendeesAttendance.Actions.ShowCommunicate = false;
                gAttendeesAttendance.Actions.ShowMergePerson = false;
                gAttendeesAttendance.ShowHeader = false;
                gAttendeesAttendance.ShowActionsInHeader = false;
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
            BindAttendeesGrid();
        }

        /// <summary>
        /// Handles the RowDataBound event of the gOccurrences control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="GridViewRowEventArgs"/> instance containing the event data.</param>
        protected void gOccurrences_RowDataBound( object sender, GridViewRowEventArgs e )
        {
            if ( e.Row.RowType == DataControlRowType.DataRow )
            {
                var occurrence = e.Row.DataItem as AttendanceListOccurrence;
                if ( occurrence == null || occurrence.Id == 0 )
                {
                }
            }
        }

        /// <summary>
        /// Handles the RowDataBound event of the gAttendeesAttendance control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="GridViewRowEventArgs"/> instance containing the event data.</param>
        protected void gAttendeesAttendance_RowDataBound( object sender, GridViewRowEventArgs e )
        {
            var personDates = e.Row.DataItem as AttendeeResult;
            if ( personDates != null )
            {
                Literal lAttendanceCount = e.Row.FindControl( "lAttendanceCount" ) as Literal;
                if ( lAttendanceCount == null )
                {
                    // Since we have dynamic columns, the templatefields might not get created due some viewstate thingy
                    // so, if we lost the templatefield, force them to instantiate
                    var templateFields = gAttendeesAttendance.Columns.OfType<TemplateField>();
                    foreach ( var templateField in templateFields )
                    {
                        var cellIndex = gAttendeesAttendance.GetColumnIndex( templateField );
                        var cell = e.Row.Cells[cellIndex] as DataControlFieldCell;
                        templateField.InitializeCell( cell, DataControlCellType.DataCell, e.Row.RowState, e.Row.RowIndex );
                    }

                    lAttendanceCount = e.Row.FindControl( "lAttendanceCount" ) as Literal;
                }
                Literal lAttendancePercent = e.Row.FindControl( "lAttendancePercent" ) as Literal;

                int currentPersonId = personDates.PersonId;
                if ( gAttendeesAttendance.PersonIdField == "ParentId" )
                {
                    currentPersonId = personDates.ParentId;
                }
                else if ( gAttendeesAttendance.PersonIdField == "ChildId" )
                {
                    currentPersonId = personDates.ChildId;
                }

                bool isExporting = _currentlyExporting;
                if ( !isExporting && e is RockGridViewRowEventArgs )
                {
                    isExporting = ( ( RockGridViewRowEventArgs ) e ).IsExporting;
                }

                int attendanceSummaryCount = personDates.AttendanceSummary.Count();
                lAttendanceCount.Text = attendanceSummaryCount.ToString();

                int? attendencePossibleCount = _possibleAttendances != null ? _possibleAttendances.Count() : ( int? ) null;

                if ( attendencePossibleCount.HasValue && attendencePossibleCount > 0 )
                {
                    var attendancePerPossibleCount = ( decimal ) attendanceSummaryCount / attendencePossibleCount.Value;
                    if ( attendancePerPossibleCount > 1 )
                    {
                        attendancePerPossibleCount = 1;
                    }

                    lAttendancePercent.Text = string.Format( "{0:P0}", attendancePerPossibleCount );
                }
            }
        }

        /// <summary>
        /// Handles the GridRebind event of the GAttendeesAttendance control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="GridRebindEventArgs"/> instance containing the event data.</param>
        protected void gAttendeesAttendance_GridRebind( object sender, GridRebindEventArgs e )
        {
            BindAttendeesGrid( e.IsExporting );
        }

        #endregion

        #region Internal Methods

        /// <summary>
        /// Binds the attendees grid.
        /// </summary>
        private void BindAttendeesGrid( bool isExporting = false )
        {
            // Get Group Type filter
            var groupTypeIdList = new List<int>();
            int? groupId = PageParameter( "GroupId" ).AsIntegerOrNull();
            Group _specificGroup = null;
            var selectedGroupIds = new List<int>();
            if ( groupId.HasValue )
            {
                _specificGroup = new GroupService( _rockContext ).Get( groupId.Value );
                if ( _specificGroup != null )
                {
                    //lSpecificGroupName.Text = string.Format( ": {0}", _specificGroup.Name );
                }
            }
            if ( _specificGroup != null )
            {
                selectedGroupIds.Add( _specificGroup.Id );
                groupTypeIdList.Add( _specificGroup.GroupTypeId );
            }
            else
            {
                groupTypeIdList.Add( 0 );
            }

            // Get the daterange filter
            //var dateRange = SlidingDateRangePicker.CalculateDateRangeFromDelimitedValues( drpSlidingDateRange.DelimitedValues );
            //if ( dateRange.End == null )
            //{
            //    dateRange.End = RockDateTime.Now;
            //}
            var dateRange = new DateRange();
            var start = DateTime.Now.AddDays(-35);
            var end = DateTime.Now;
            dateRange.Start = start;
            dateRange.End = end;

            // Get the group filter
            var groupIdList = new List<int>();
            string groupIds = selectedGroupIds.AsDelimited( "," );
            if ( !string.IsNullOrWhiteSpace( groupIds ) )
            {
                groupIdList = groupIds.Split( ',' ).AsIntegerList();
            }            

            // Determine how dates should be grouped
            // ChartGroupBy groupBy = hfGroupBy.Value.ConvertToEnumOrNull<ChartGroupBy>() ?? ChartGroupBy.Week;
            ChartGroupBy groupBy = ChartGroupBy.Week;

            // Attendance results
            var allAttendeeVisits = new Dictionary<int, AttendeeVisits>();
            var allResults = new List<AttendeeResult>();

            // Collection of async queries to run before assembling data
            var qryTasks = new List<Task>();
            var taskInfos = new List<TaskInfo>();

            DataTable dtAttendeeLastAttendance = null;
            DataTable dtAttendees = null;
            //DataTable dtAttendeeFirstDates = null;
            var showNonAttenders = false;
            var includeParents = false;
            var includeChildren = false;

            if ( !showNonAttenders )
            {
                // Call the stored procedure to get all the person ids and their attendance dates for anyone
                // whith attendance that matches the selected criteria.
                qryTasks.Add( Task.Run( () =>
                {
                    var ti = new TaskInfo { name = "Get Attendee Dates", start = DateTime.Now };
                    taskInfos.Add( ti );

                    //DataTable dtAttendeeDates = AttendanceService.GetAttendanceAnalyticsAttendeeDates(
                    //    groupIdList, start, end, campusIdList, includeNullCampus, scheduleIdList ).Tables[0];
                    DataTable dtAttendeeDates = AttendanceService.GetAttendanceAnalyticsAttendeeDates(
                        groupIdList, start, end, null, true, null ).Tables[0];

                    foreach ( DataRow row in dtAttendeeDates.Rows )
                    {
                        int personId = ( int ) row["PersonId"];
                        allAttendeeVisits.AddOrIgnore( personId, new AttendeeVisits() );
                        var result = allAttendeeVisits[personId];
                        result.PersonId = personId;

                        DateTime summaryDate = DateTime.MinValue;
                        switch ( groupBy )
                        {
                            case ChartGroupBy.Week:
                                summaryDate = ( DateTime ) row["SundayDate"];
                                break;
                            case ChartGroupBy.Month:
                                summaryDate = ( DateTime ) row["MonthDate"];
                                break;
                            case ChartGroupBy.Year:
                                summaryDate = ( DateTime ) row["YearDate"];
                                break;
                        }
                        if ( !result.AttendanceSummary.Contains( summaryDate ) )
                        {
                            result.AttendanceSummary.Add( summaryDate );
                        }
                    }

                    ti.end = DateTime.Now;

                } ) );

                // Call the stored procedure to get the last attendance
                qryTasks.Add( Task.Run( () =>
                {
                    var ti = new TaskInfo { name = "Get Last Attendance", start = DateTime.Now };
                    taskInfos.Add( ti );

                    //dtAttendeeLastAttendance = AttendanceService.GetAttendanceAnalyticsAttendeeLastAttendance(
                    //    groupIdList, start, end, campusIdList, includeNullCampus, scheduleIdList ).Tables[0];
                    dtAttendeeLastAttendance = AttendanceService.GetAttendanceAnalyticsAttendeeLastAttendance(
                            groupIdList, start, end, null, true, null ).Tables[0];


                    ti.end = DateTime.Now;

                } ) );

                // Call the stored procedure to get the names/demographic info for attendees
                qryTasks.Add( Task.Run( () =>
                {
                    var ti = new TaskInfo { name = "Get Name/Demographic Data", start = DateTime.Now };
                    taskInfos.Add( ti );

                    //dtAttendees = AttendanceService.GetAttendanceAnalyticsAttendees(
                    //    groupIdList, start, end, campusIdList, includeNullCampus, scheduleIdList, includeParents, includeChildren ).Tables[0];
                    dtAttendees = AttendanceService.GetAttendanceAnalyticsAttendees(
                        groupIdList, start, end, null, true, null, includeParents, includeChildren ).Tables[0];

                    ti.end = DateTime.Now;

                } ) );

                // Call the stored procedure to get the first five dates that any person attended this group type
                //qryTasks.Add( Task.Run( () =>
                //{
                //    var ti = new TaskInfo { name = "Get First Five Dates", start = DateTime.Now };
                //    taskInfos.Add( ti );

                //    //dtAttendeeFirstDates = AttendanceService.GetAttendanceAnalyticsAttendeeFirstDates(
                //    //    groupTypeIdList, groupIdList, start, end, campusIdList, includeNullCampus, scheduleIdList ).Tables[0];
                //    dtAttendeeFirstDates = AttendanceService.GetAttendanceAnalyticsAttendeeFirstDates(
                //          groupTypeIdList, groupIdList, start, end, null, true, null ).Tables[0];

                //    ti.end = DateTime.Now;

                //} ) );
            }
            else
            {
                qryTasks.Add( Task.Run( () =>
                {
                    var ti = new TaskInfo { name = "Get Non-Attendees", start = DateTime.Now };
                    taskInfos.Add( ti );

                    //DataSet ds = AttendanceService.GetAttendanceAnalyticsNonAttendees(
                    //    groupTypeIdList, groupIdList, start, end, campusIdList, includeNullCampus, scheduleIdList, includeParents, includeChildren );
                    DataSet ds = AttendanceService.GetAttendanceAnalyticsNonAttendees(
                        groupTypeIdList, groupIdList, start, end, null, true, null, includeParents, includeChildren );

                    DataTable dtNonAttenders = ds.Tables[0];
                    //dtAttendeeFirstDates = ds.Tables[1];
                    dtAttendeeLastAttendance = ds.Tables[2];

                    foreach ( DataRow row in dtNonAttenders.Rows )
                    {
                        int personId = ( int ) row["Id"];

                        var result = new AttendeeResult();
                        result.PersonId = personId;

                        var person = new PersonInfo();
                        person.NickName = row["NickName"].ToString();
                        person.LastName = row["LastName"].ToString();
                        person.Gender = row["Gender"].ToString().ConvertToEnum<Gender>();
                        person.Email = row["Email"].ToString();
                        person.GivingId = row["GivingId"].ToString();
                        person.Birthdate = row["BirthDate"] as DateTime?;
                        person.Age = Person.GetAge( person.Birthdate );

                        person.ConnectionStatusValueId = row["ConnectionStatusValueId"] as int?;
                        result.Person = person;

                        if ( includeParents )
                        {
                            result.ParentId = ( int ) row["ParentId"];
                            var parent = new PersonInfo();
                            parent.NickName = row["ParentNickName"].ToString();
                            parent.LastName = row["ParentLastName"].ToString();
                            parent.Email = row["ParentEmail"].ToString();
                            parent.GivingId = row["ParentGivingId"].ToString();
                            parent.Birthdate = row["ParentBirthDate"] as DateTime?;
                            parent.Age = Person.GetAge( parent.Birthdate );
                            result.Parent = parent;
                        }

                        if ( includeChildren )
                        {
                            var child = new PersonInfo();
                            result.ChildId = ( int ) row["ChildId"];
                            child.NickName = row["ChildNickName"].ToString();
                            child.LastName = row["ChildLastName"].ToString();
                            child.Email = row["ChildEmail"].ToString();
                            child.GivingId = row["ChildGivingId"].ToString();
                            child.Birthdate = row["ChildBirthDate"] as DateTime?;
                            child.Age = Person.GetAge( child.Birthdate );
                            result.Child = child;
                        }

                        allResults.Add( result );
                    }

                    ti.end = DateTime.Now;

                } ) );
            }

            // Wait for all the queries to finish
            Task.WaitAll( qryTasks.ToArray() );

            if ( !showNonAttenders )
            {
                var attendees = allAttendeeVisits.AsQueryable();

                // Force filter application
                allAttendeeVisits = attendees.ToDictionary( k => k.Key, v => v.Value );

                // Add the First Visit information
                //foreach ( DataRow row in dtAttendeeFirstDates.Rows )
                //{
                //    int personId = ( int ) row["PersonId"];
                //    if ( allAttendeeVisits.ContainsKey( personId ) )
                //    {
                //        allAttendeeVisits[personId].FirstVisits.Add( ( DateTime ) row["StartDate"] );
                //    }
                //}

                // Add the Last Attended information
                if ( dtAttendeeLastAttendance != null )
                {
                    foreach ( DataRow row in dtAttendeeLastAttendance.Rows )
                    {
                        int personId = ( int ) row["PersonId"];
                        if ( allAttendeeVisits.ContainsKey( personId ) )
                        {
                            var result = allAttendeeVisits[personId];
                            if ( result.LastVisit == null )
                            {
                                var lastAttendance = new PersonLastAttendance();
                                lastAttendance.CampusId = row["CampusId"] as int?;
                                lastAttendance.GroupId = row["GroupId"] as int?;
                                lastAttendance.GroupName = row["GroupName"].ToString();
                                lastAttendance.RoleName = row["RoleName"].ToString();
                                lastAttendance.InGroup = !string.IsNullOrWhiteSpace( lastAttendance.RoleName );
                                lastAttendance.ScheduleId = row["ScheduleId"] as int?;
                                lastAttendance.StartDateTime = ( DateTime ) row["StartDateTime"];
                                lastAttendance.LocationId = row["LocationId"] as int?;
                                lastAttendance.LocationName = row["LocationName"].ToString();
                                result.LastVisit = lastAttendance;
                            }
                        }
                    }
                }

                // Add the Demographic information
                if ( dtAttendees != null )
                {
                    var newResults = new Dictionary<int, AttendeeResult>();

                    foreach ( DataRow row in dtAttendees.Rows )
                    {
                        int personId = ( int ) row["Id"];
                        if ( allAttendeeVisits.ContainsKey( personId ) )
                        {
                            var result = new AttendeeResult( allAttendeeVisits[personId] );

                            var person = new PersonInfo();
                            person.NickName = row["NickName"].ToString();
                            person.LastName = row["LastName"].ToString();
                            person.Gender = row["Gender"].ToString().ConvertToEnum<Gender>();
                            person.Email = row["Email"].ToString();
                            person.GivingId = row["GivingId"].ToString();
                            person.Birthdate = row["BirthDate"] as DateTime?;
                            person.Age = Person.GetAge( person.Birthdate );
                            person.ConnectionStatusValueId = row["ConnectionStatusValueId"] as int?;
                            result.Person = person;

                            if ( includeParents )
                            {
                                result.ParentId = ( int ) row["ParentId"];
                                var parent = new PersonInfo();
                                parent.NickName = row["ParentNickName"].ToString();
                                parent.LastName = row["ParentLastName"].ToString();
                                parent.Email = row["ParentEmail"].ToString();
                                parent.GivingId = row["ParentGivingId"].ToString();
                                parent.Birthdate = row["ParentBirthDate"] as DateTime?;
                                parent.Age = Person.GetAge( parent.Birthdate );
                                result.Parent = parent;
                            }

                            if ( includeChildren )
                            {
                                var child = new PersonInfo();
                                result.ChildId = ( int ) row["ChildId"];
                                child.NickName = row["ChildNickName"].ToString();
                                child.LastName = row["ChildLastName"].ToString();
                                child.Email = row["ChildEmail"].ToString();
                                child.GivingId = row["ChildGivingId"].ToString();
                                child.Birthdate = row["ChildBirthDate"] as DateTime?;
                                child.Age = Person.GetAge( child.Birthdate );
                                result.Child = child;
                            }

                            allResults.Add( result );
                        }
                    }
                }
            }
            else
            {
                // Add the first visit dates for people
                //foreach ( DataRow row in dtAttendeeFirstDates.Rows )
                //{
                //    int personId = ( int ) row["PersonId"];
                //    foreach ( var result in allResults.Where( r => r.PersonId == personId ) )
                //    {
                //        result.FirstVisits.Add( ( DateTime ) row["StartDate"] );
                //    }
                //}

                // Add the Last Attended information
                if ( dtAttendeeLastAttendance != null )
                {
                    foreach ( DataRow row in dtAttendeeLastAttendance.Rows )
                    {
                        int personId = ( int ) row["PersonId"];
                        foreach ( var result in allResults.Where( r => r.PersonId == personId ) )
                        {
                            if ( result.LastVisit == null )
                            {
                                var lastAttendance = new PersonLastAttendance();
                                lastAttendance.CampusId = row["CampusId"] as int?;
                                lastAttendance.GroupId = row["GroupId"] as int?;
                                lastAttendance.GroupName = row["GroupName"].ToString();
                                lastAttendance.RoleName = row["RoleName"].ToString();
                                lastAttendance.InGroup = !string.IsNullOrWhiteSpace( lastAttendance.RoleName );
                                lastAttendance.ScheduleId = row["ScheduleId"] as int?;
                                lastAttendance.StartDateTime = ( DateTime ) row["StartDateTime"];
                                lastAttendance.LocationId = row["LocationId"] as int?;
                                lastAttendance.LocationName = row["LocationName"].ToString();
                                result.LastVisit = lastAttendance;
                            }
                        }
                    }
                }
            }

            var combinedResults = new List<AttendeeResult>();
            using ( var rockContext = new RockContext() )
            {
                var attendanceService = new AttendanceService( rockContext );
                var attendanceDates = attendanceService
                    .Queryable().AsNoTracking().Where( a => a.Occurrence.GroupId == _group.Id && a.StartDateTime >= start && a.StartDateTime <= end );

                var peopleService = new PersonService( rockContext );
                var peopleInfo = peopleService.Queryable().AsNoTracking().Where( p => attendanceDates.Select( a => a.PersonAlias.PersonId ).Contains( p.Id ) );

                combinedResults = peopleInfo.Select(
                    p => new AttendeeResult
                    {
                        Person = new PersonInfo
                        {
                            NickName = p.NickName,
                            LastName = p.LastName
                        },
                        PersonId = p.Id,
                        AttendanceSummary = attendanceDates.Where( a => a.PersonAlias.PersonId == p.Id && a.DidAttend == true ).Select( a => a.StartDateTime ).ToList(),
                        LastVisit = attendanceDates.Where( a => a.PersonAlias.PersonId == p.Id && a.DidAttend == true )
                            .Select( a => new PersonLastAttendance
                            {
                                CampusId = a.CampusId,
                                GroupId = a.Occurrence.GroupId,
                                GroupName = a.Occurrence.Group.Name,
                                RoleName = a.Occurrence.Group.Members.Where( gm => gm.PersonId == p.Id && gm.GroupMemberStatus != GroupMemberStatus.Inactive ).Select( gm => gm.GroupRole.Name ).FirstOrDefault(),
                                InGroup = a.Occurrence.Group.Members.Any( gm => gm.PersonId == p.Id && gm.GroupMemberStatus != GroupMemberStatus.Inactive ),
                                ScheduleId = a.Occurrence.ScheduleId,
                                StartDateTime = a.StartDateTime,
                                LocationId = a.Occurrence.LocationId,
                                LocationName = a.Occurrence.Location.Name
                            } ).OrderByDescending( lv => lv.StartDateTime ).FirstOrDefault()
                    } ).ToList();

                _possibleAttendances = attendanceDates.GroupBy( a => a.StartDateTime ).Select( a => a.Key ).ToList();
            }

            // Begin formatting the columns
            //var qryResult = allResults.AsQueryable();
            var qryResult = combinedResults.AsQueryable();

            var personUrlFormatString = ( ( RockPage ) this.Page ).ResolveRockUrl( "~/Person/{0}" );

            var personHyperLinkField = gAttendeesAttendance.Columns.OfType<HyperLinkField>().FirstOrDefault( a => a.HeaderText == "Name" );
            if ( personHyperLinkField != null )
            {
                personHyperLinkField.DataNavigateUrlFormatString = personUrlFormatString;
            }

            var parentHyperLinkField = gAttendeesAttendance.Columns.OfType<HyperLinkField>().FirstOrDefault( a => a.HeaderText == "Parent" );
            if ( parentHyperLinkField != null )
            {
                parentHyperLinkField.Visible = includeParents;
                parentHyperLinkField.DataNavigateUrlFormatString = personUrlFormatString;
            }

            var parentField = gAttendeesAttendance.Columns.OfType<RockBoundField>().FirstOrDefault( a => a.HeaderText == "Parent" );
            if ( parentField != null )
            {
                parentField.ExcelExportBehavior = includeParents ? ExcelExportBehavior.AlwaysInclude : ExcelExportBehavior.NeverInclude;
            }

            var parentEmailField = gAttendeesAttendance.Columns.OfType<RockBoundField>().FirstOrDefault( a => a.HeaderText == "Parent Email" );
            if ( parentEmailField != null )
            {
                parentEmailField.ExcelExportBehavior = includeParents ? ExcelExportBehavior.AlwaysInclude : ExcelExportBehavior.NeverInclude;
            }

            var parentGivingId = gAttendeesAttendance.Columns.OfType<RockBoundField>().FirstOrDefault( a => a.HeaderText == "Parent GivingId" );
            if ( parentGivingId != null )
            {
                parentGivingId.ExcelExportBehavior = includeParents ? ExcelExportBehavior.AlwaysInclude : ExcelExportBehavior.NeverInclude;
            }

            var childHyperLinkField = gAttendeesAttendance.Columns.OfType<HyperLinkField>().FirstOrDefault( a => a.HeaderText == "Child" );
            if ( childHyperLinkField != null )
            {
                childHyperLinkField.Visible = includeChildren;
                childHyperLinkField.DataNavigateUrlFormatString = personUrlFormatString;
            }

            var childfield = gAttendeesAttendance.Columns.OfType<RockBoundField>().FirstOrDefault( a => a.HeaderText == "Child" );
            if ( childfield != null )
            {
                childfield.ExcelExportBehavior = includeChildren ? ExcelExportBehavior.AlwaysInclude : ExcelExportBehavior.NeverInclude;
            }

            var childEmailField = gAttendeesAttendance.Columns.OfType<RockBoundField>().FirstOrDefault( a => a.HeaderText == "Child Email" );
            if ( childEmailField != null )
            {
                childEmailField.ExcelExportBehavior = includeChildren ? ExcelExportBehavior.AlwaysInclude : ExcelExportBehavior.NeverInclude;
            }

            var childAgeField = gAttendeesAttendance.Columns.OfType<RockBoundField>().FirstOrDefault( a => a.HeaderText == "Child Age" );
            if ( childAgeField != null )
            {
                childAgeField.ExcelExportBehavior = includeChildren ? ExcelExportBehavior.AlwaysInclude : ExcelExportBehavior.NeverInclude;
            }

            var childGivingId = gAttendeesAttendance.Columns.OfType<RockBoundField>().FirstOrDefault( a => a.HeaderText == "Child GivingId" );
            if ( childGivingId != null )
            {
                childGivingId.ExcelExportBehavior = includeChildren ? ExcelExportBehavior.AlwaysInclude : ExcelExportBehavior.NeverInclude;
            }

            SortProperty sortProperty = gAttendeesAttendance.SortProperty;

            if ( sortProperty != null )
            {
                if ( sortProperty.Property == "AttendanceSummary.Count" )
                {
                    if ( sortProperty.Direction == SortDirection.Descending )
                    {
                        qryResult = qryResult.OrderByDescending( a => a.AttendanceSummary.Count() );
                    }
                    else
                    {
                        qryResult = qryResult.OrderBy( a => a.AttendanceSummary.Count() );
                    }
                }
                else if ( sortProperty.Property == "FirstVisit.StartDateTime" )
                {
                    if ( sortProperty.Direction == SortDirection.Descending )
                    {
                        qryResult = qryResult.OrderByDescending( a => a.FirstVisits.Min() );
                    }
                    else
                    {
                        qryResult = qryResult.OrderBy( a => a.FirstVisits.Min() );
                    }
                }
                else
                {
                    qryResult = qryResult.Sort( sortProperty );
                }
            }
            else
            {
                qryResult = qryResult.OrderBy( a => a.Person.LastName ).ThenBy( a => a.Person.NickName );
            }

            //var attendancePercentField = gAttendeesAttendance.Columns.OfType<RockTemplateField>().First( a => a.HeaderText.EndsWith( "Attendance %" ) );
            //attendancePercentField.HeaderText = string.Format( "{0}ly Attendance %", groupBy.ConvertToString() );

            // Calculate all the possible attendance summary dates
            //UpdatePossibleAttendances( dateRange, groupBy );

            // pre-load the schedule names since FriendlyScheduleText requires building the ICal object, etc
            _scheduleNameLookup = new ScheduleService( _rockContext ).Queryable()
                .ToList()
                .ToDictionary( k => k.Id, v => v.FriendlyScheduleText );

            if ( includeParents )
            {
                gAttendeesAttendance.PersonIdField = "ParentId";
                gAttendeesAttendance.DataKeyNames = new string[] { "ParentId", "PersonId" };
            }
            else if ( includeChildren )
            {
                gAttendeesAttendance.PersonIdField = "ChildId";
                gAttendeesAttendance.DataKeyNames = new string[] { "ChildId", "PersonId" };
            }
            else
            {
                gAttendeesAttendance.PersonIdField = "PersonId";
                gAttendeesAttendance.DataKeyNames = new string[] { "PersonId" };
            }

            // Create the dynamic attendance grid columns as needed
            CreateDynamicAttendanceGridColumns();

            try
            {
                nbAttendeesError.Visible = false;

                gAttendeesAttendance.SetLinqDataSource( qryResult );
                var currentPageItems = gAttendeesAttendance.DataSource as List<AttendeeResult>;
                if ( currentPageItems != null )
                {
                    var currentPagePersonIds = new List<int>();
                    if ( includeParents )
                    {
                        currentPagePersonIds = currentPageItems.Select( i => i.ParentId ).ToList();
                        gAttendeesAttendance.PersonIdField = "ParentId";
                        gAttendeesAttendance.DataKeyNames = new string[] { "ParentId", "PersonId" };
                    }
                    else if ( includeChildren )
                    {
                        currentPagePersonIds = currentPageItems.Select( i => i.ChildId ).ToList();
                        gAttendeesAttendance.PersonIdField = "ChildId";
                        gAttendeesAttendance.DataKeyNames = new string[] { "ChildId", "PersonId" };
                    }
                    else
                    {
                        currentPagePersonIds = currentPageItems.Select( i => i.PersonId ).ToList();
                        gAttendeesAttendance.PersonIdField = "PersonId";
                        gAttendeesAttendance.DataKeyNames = new string[] { "PersonId" };
                    }
                }

                _currentlyExporting = isExporting;
                gAttendeesAttendance.DataBind();
                _currentlyExporting = false;
            }
            catch ( Exception exception )
            {
                LogAndShowException( exception );
            }
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
        /// Logs and shows the exception.
        /// </summary>
        /// <param name="exception">The exception.</param>
        private void LogAndShowException( Exception exception )
        {
            LogException( exception );
            string errorMessage = null;
            string stackTrace = string.Empty;
            while ( exception != null )
            {
                errorMessage = exception.Message;
                stackTrace += exception.StackTrace;
                if ( exception is System.Data.SqlClient.SqlException )
                {
                    // if there was a SQL Server Timeout, have the warning be a friendly message about that.
                    if ( ( exception as System.Data.SqlClient.SqlException ).Number == -2 )
                    {
                        errorMessage = "The attendee report did not complete in a timely manner. Try again using a smaller date range and fewer campuses and groups.";
                        break;
                    }
                    else
                    {
                        exception = exception.InnerException;
                    }
                }
                else
                {
                    exception = exception.InnerException;
                }
            }

            nbAttendeesError.Text = errorMessage;
            nbAttendeesError.Details = stackTrace;
            nbAttendeesError.Visible = true;
        }

        /// <summary>
        /// Creates the dynamic attendance grid columns.
        /// </summary>
        /// <param name="groupBy">The group by.</param>
        private void CreateDynamicAttendanceGridColumns()
        {
            //ChartGroupBy groupBy = hfGroupBy.Value.ConvertToEnumOrNull<ChartGroupBy>() ?? ChartGroupBy.Week;
            ChartGroupBy groupBy = ChartGroupBy.Week;

            // Ensure the columns for the Attendance Checkmarks are there
            var attendanceSummaryFields = gAttendeesAttendance.Columns.OfType<BoolFromArrayField<DateTime>>().Where( a => a.DataField == "AttendanceSummary" ).ToList();
            var existingSummaryDates = attendanceSummaryFields.Select( a => a.ArrayKey ).ToList();

            if ( existingSummaryDates.Any( a => !_possibleAttendances.Contains( a ) ) || _possibleAttendances.Any( a => !existingSummaryDates.Contains( a ) ) )
            {
                foreach ( var oldField in attendanceSummaryFields.Reverse<BoolFromArrayField<DateTime>>() )
                {
                    // remove all these fields if they have changed
                    gAttendeesAttendance.Columns.Remove( oldField );
                }

                // limit to 520 checkmark columns so that we don't blow up the server (just in case they select every week for the last 100 years or something).
                var maxColumns = 520;
                foreach ( var summaryDate in _possibleAttendances.Take( maxColumns ) )
                {
                    var boolFromArrayField = new BoolFromArrayField<DateTime>();

                    boolFromArrayField.ArrayKey = summaryDate;
                    boolFromArrayField.DataField = "AttendanceSummary";
                    switch ( groupBy )
                    {
                        case ChartGroupBy.Year:
                            boolFromArrayField.HeaderText = summaryDate.ToString( "yyyy" );
                            break;

                        case ChartGroupBy.Month:
                            boolFromArrayField.HeaderText = summaryDate.ToString( "MMM yyyy" );
                            break;

                        case ChartGroupBy.Week:
                            boolFromArrayField.HeaderText = summaryDate.ToString( "M/d/yy" );
                            break;

                        default:
                            // shouldn't happen
                            boolFromArrayField.HeaderText = summaryDate.ToString();
                            break;
                    }

                    gAttendeesAttendance.Columns.Add( boolFromArrayField );
                }
            }
        }

        /// <summary>
        /// Updates the possible attendance summary dates
        /// </summary>
        /// <param name="dateRange">The date range.</param>
        /// <param name="attendanceGroupBy">The attendance group by.</param>
        public void UpdatePossibleAttendances( DateRange dateRange, ChartGroupBy attendanceGroupBy )
        {
            _possibleAttendances = GetPossibleAttendancesForDateRange( dateRange, attendanceGroupBy );
        }

        /// <summary>
        /// Gets the possible attendances for the date range.
        /// </summary>
        /// <param name="dateRange">The date range.</param>
        /// <param name="attendanceGroupBy">The attendance group by type.</param>
        /// <returns></returns>
        public List<DateTime> GetPossibleAttendancesForDateRange( DateRange dateRange, ChartGroupBy attendanceGroupBy )
        {
            var result = new List<DateTime>();

            // Attendance is grouped by Sunday dates between the start/end dates.
            // The possible dates (columns) should be calculated the same way.
            var startSunday = dateRange.Start.Value.SundayDate();
            var endDate = dateRange.End.Value;
            var endSunday = endDate.SundayDate();
            if ( endSunday > endDate )
            {
                endSunday = endSunday.AddDays( -7 );
            }

            if ( attendanceGroupBy == ChartGroupBy.Week )
            {
                var weekEndDate = startSunday;
                while ( weekEndDate <= endSunday )
                {
                    // Weeks are summarized as the last day of the "Rock" week (Sunday)
                    result.Add( weekEndDate );
                    weekEndDate = weekEndDate.AddDays( 7 );
                }
            }
            else if ( attendanceGroupBy == ChartGroupBy.Month )
            {
                var endOfFirstMonth = startSunday.AddDays( -( startSunday.Day - 1 ) ).AddMonths( 1 ).AddDays( -1 );
                var endOfLastMonth = endSunday.AddDays( -( endSunday.Day - 1 ) ).AddMonths( 1 ).AddDays( -1 );

                //// Months are summarized as the First Day of the month: For example, 5/1/2015 would include everything from 5/1/2015 - 5/31/2015 (inclusive)
                var monthStartDate = new DateTime( endOfFirstMonth.Year, endOfFirstMonth.Month, 1 );
                while ( monthStartDate <= endOfLastMonth )
                {
                    result.Add( monthStartDate );
                    monthStartDate = monthStartDate.AddMonths( 1 );
                }
            }
            else if ( attendanceGroupBy == ChartGroupBy.Year )
            {
                var endOfFirstYear = new DateTime( startSunday.Year, 1, 1 ).AddYears( 1 ).AddDays( -1 );
                var endOfLastYear = new DateTime( endSunday.Year, 1, 1 ).AddYears( 1 ).AddDays( -1 );

                //// Years are summarized as the First Day of the year: For example, 1/1/2015 would include everything from 1/1/2015 - 12/31/2015 (inclusive)
                var yearStartDate = new DateTime( endOfFirstYear.Year, 1, 1 );
                while ( yearStartDate <= endOfLastYear )
                {
                    result.Add( yearStartDate );
                    yearStartDate = yearStartDate.AddYears( 1 );
                }
            }

            // only include current and previous dates
            var currentDateTime = RockDateTime.Now;
            result = result.Where( a => a <= currentDateTime.Date ).ToList();

            return result;
        }
        #endregion
    }

    public class AttendanceListOccurrence
    {
        public int Id { get; set; }
        public DateTime OccurrenceDate { get; set; }
        public int? LocationId { get; set; }
        public string LocationName { get; set; }
        public int? ParentLocationId { get; set; }
        public string ParentLocationPath { get; set; }
        public int? ScheduleId { get; set; }
        public string ScheduleName { get; set; }
        public TimeSpan StartTime { get; set; }
        public int? CampusId { get; set; }
        public bool AttendanceEntered { get; set; }
        public bool DidNotOccur { get; set; }
        public int DidAttendCount { get; set; }
        public double AttendanceRate { get; set; }
        public bool CanDelete { get; set; }

        public AttendanceListOccurrence ( AttendanceOccurrence occurrence )
        {
            Id = occurrence.Id;
            OccurrenceDate = occurrence.OccurrenceDate;
            LocationId = occurrence.LocationId;

            if ( occurrence.Location != null )
            {
                if ( occurrence.Location.Name.IsNotNullOrWhiteSpace() )
                {
                    LocationName = occurrence.Location.Name;
                }
                else
                {
                    LocationName = occurrence.Location.ToString();
                }
            }

            LocationName = occurrence.Location != null ? occurrence.Location.Name : string.Empty;
            ParentLocationId = occurrence.Location != null ? occurrence.Location.ParentLocationId : (int?)null;
            ScheduleId = occurrence.ScheduleId;

            if ( occurrence.Schedule != null )
            {
                if ( occurrence.Schedule.Name.IsNotNullOrWhiteSpace() )
                {
                    ScheduleName = occurrence.Schedule.Name;
                }
                else
                {
                    ScheduleName = occurrence.Schedule.ToString();
                }
            }

            StartTime = occurrence.Schedule != null ? occurrence.Schedule.StartTimeOfDay : new TimeSpan();
            AttendanceEntered = occurrence.AttendanceEntered;
            DidNotOccur = occurrence.DidNotOccur ?? false;
            DidAttendCount = occurrence.DidAttendCount;
            AttendanceRate = occurrence.AttendanceRate;
        }
    }

    /// <summary>
    /// Attendee object that adds PersonInfo to the AttendeeVisit
    /// </summary>
    /// <seealso cref="RockWeb.Blocks.CheckIn.AttendanceAnalytics.AttendeeVisits" />
    public class AttendeeResult : AttendeeVisits
    {
        public PersonInfo Person { get; set; }

        public int ParentId { get; set; }

        public PersonInfo Parent { get; set; }

        public int ChildId { get; set; }

        public PersonInfo Child { get; set; }

        public AttendeeResult()
            : base()
        {
        }

        public AttendeeResult( AttendeeVisits attendeeDates )
        {
            this.PersonId = attendeeDates.PersonId;
            this.FirstVisits = attendeeDates.FirstVisits;
            this.LastVisit = attendeeDates.LastVisit;
            this.AttendanceSummary = attendeeDates.AttendanceSummary;
        }
    }

    /// <summary>
    /// List of person visits
    /// </summary>
    public class AttendeeVisits
    {
        public int PersonId { get; set; }

        public List<DateTime> FirstVisits { get; set; }

        public PersonLastAttendance LastVisit { get; set; }

        public List<DateTime> AttendanceSummary { get; set; }

        public AttendeeVisits()
        {
            FirstVisits = new List<DateTime>();
            AttendanceSummary = new List<DateTime>();
        }
    }

    /// <summary>
    /// Lightweight Rock Person object
    /// </summary>
    public class PersonInfo
    {
        public string NickName { get; set; }

        public string LastName { get; set; }

        public string Email { get; set; }

        public Gender Gender { get; set; }

        public int? Age { get; set; }

        public string GivingId { get; set; }

        public DateTime? Birthdate { get; set; }

        public int? ConnectionStatusValueId { get; set; }

        public override string ToString()
        {
            return NickName + " " + LastName;
        }
    }

    /// <summary>
    /// All visit information from the most recent attendance
    /// </summary>
    public class PersonLastAttendance
    {
        public int? CampusId { get; set; }

        public int? GroupId { get; set; }

        public string GroupName { get; set; }

        public bool InGroup { get; set; }

        public string RoleName { get; set; }

        public int? ScheduleId { get; set; }

        public DateTime StartDateTime { get; set; }

        public int? LocationId { get; set; }

        public string LocationName { get; set; }
    }

    public class TaskInfo
    {
        public string name { get; set; }
        public DateTime start { get; set; }
        public DateTime end { get; set; }
        public TimeSpan duration
        {
            get
            {
                return end.Subtract( start );
            }
        }

        public override string ToString()
        {
            return string.Format( "{0}: {1:c}", name, duration );
        }
    }

}