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
    [DisplayName( "Group Attendance Summary" )]
    [Category( "KFS > Groups" )]
    [Description( "Lists weekly attendance summary of the selected group." )]
    [IntegerField( "Weeks of Attendance", "Number of weeks of attendance you would like displayed on the attendance summary.", true, 5 )]
    [BooleanField( "Admin Mode", "Enable the block to be used internally, displays person link and can display any group.", false )]
    public partial class GroupAttendanceSummary : RockBlock, ICustomGridColumns
    {
        #region Private Variables

        private RockContext _rockContext = null;
        private Group _group = null;
        private bool _canView = false;
        private int _numberOfWeeks = 5;
        private bool _adminMode = false;

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

            _adminMode = GetAttributeValue( "AdminMode" ).AsBoolean();

            if ( _group != null && ( _group.IsAuthorized( Authorization.VIEW, CurrentPerson ) || _adminMode ) )
            {
                _group.LoadAttributes( _rockContext );
                _canView = true;
            }

            // this event gets fired after block settings are updated. it's nice to repaint the screen if these settings would alter it
            this.BlockUpdated += Block_BlockUpdated;

            gAttendeesAttendance.GridRebind += gAttendeesAttendance_GridRebind;
            gAttendeesAttendance.EntityTypeId = EntityTypeCache.Get<Rock.Model.Person>().Id;
        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );

            pnlContent.Visible = _canView;

            _numberOfWeeks = GetAttributeValue( "WeeksofAttendance" ).AsInteger();

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

                gAttendeesAttendance.Actions.ShowBulkUpdate = _adminMode;
                gAttendeesAttendance.Actions.ShowCommunicate = _adminMode;
                gAttendeesAttendance.Actions.ShowMergePerson = _adminMode;
                gAttendeesAttendance.Actions.ShowMergeTemplate = _adminMode;
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
            int? groupId = PageParameter( "GroupId" ).AsIntegerOrNull();

            // Get the daterange filter
            var dateRange = new DateRange();
            var weeksToDays = ( _numberOfWeeks ) * 7;
            var start = RockDateTime.Now.AddDays( -weeksToDays );
            var end = RockDateTime.Now;
            dateRange.Start = start;
            dateRange.End = end;

            // Attendance results
            var allResults = new List<AttendeeResult>();

            using ( var rockContext = new RockContext() )
            {
                var attendanceService = new AttendanceService( rockContext );
                var attendanceDates = attendanceService
                    .Queryable().AsNoTracking().Where( a => groupId.HasValue && a.Occurrence.GroupId == groupId && a.StartDateTime >= start && a.StartDateTime <= end );

                var peopleService = new PersonService( rockContext );
                var peopleInfo = peopleService.Queryable().AsNoTracking().Where( p => attendanceDates.Select( a => a.PersonAlias.PersonId ).Contains( p.Id ) );

                allResults = peopleInfo.Select(
                    p => new AttendeeResult
                    {
                        Person = new PersonInfo
                        {
                            NickName = p.NickName,
                            LastName = p.LastName,
                            Gender = p.Gender,
                            Email = p.Email,
                            GivingId = p.GivingId,
                            Birthdate = p.BirthDate,
                            ConnectionStatusValueId = p.ConnectionStatusValueId
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
            if ( isExporting )
            {
                // Null check all results on Export due to an object reference error if they have never visited the group.
                var tempResults = new List<AttendeeResult>();
                foreach ( var result in allResults )
                {
                    if ( result.LastVisit.IsNull() )
                    {
                        result.LastVisit = new PersonLastAttendance
                        {
                            StartDateTime = new DateTime()
                        };
                    }
                    tempResults.Add( result );
                }
                allResults = tempResults;
            }


            // Begin formatting the columns
            var qryResult = allResults.AsQueryable();

            var personUrlFormatString = ( ( RockPage ) this.Page ).ResolveRockUrl( "~/Person/{0}" );

            var personHyperLinkField = gAttendeesAttendance.Columns.OfType<HyperLinkField>().FirstOrDefault( a => a.HeaderText == "Name" );
            if ( personHyperLinkField != null && _adminMode )
            {
                personHyperLinkField.DataNavigateUrlFormatString = personUrlFormatString;
                personHyperLinkField.Visible = true;
                var personField = gAttendeesAttendance.Columns.OfType<RockBoundField>().FirstOrDefault( a => a.HeaderText == "Name" );
                personField.Visible = false;
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
                else if ( sortProperty.Property == "LastVisit.StartDateTime" )
                {
                    if ( sortProperty.Direction == SortDirection.Descending )
                    {
                        qryResult = qryResult.OrderByDescending( a => ( a.LastVisit.IsNotNull() ) ? a.LastVisit.StartDateTime : new DateTime() );
                    }
                    else
                    {
                        qryResult = qryResult.OrderBy( a => ( a.LastVisit.IsNotNull() ) ? a.LastVisit.StartDateTime : new DateTime() );
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
            UpdatePossibleAttendances( dateRange, ChartGroupBy.Week );

            // pre-load the schedule names since FriendlyScheduleText requires building the ICal object, etc
            _scheduleNameLookup = new ScheduleService( _rockContext ).Queryable()
                .ToList()
                .ToDictionary( k => k.Id, v => v.FriendlyScheduleText );

            gAttendeesAttendance.PersonIdField = "PersonId";
            gAttendeesAttendance.DataKeyNames = new string[] { "PersonId" };

            // Create the dynamic attendance grid columns as needed
            CreateDynamicAttendanceGridColumns();

            try
            {
                nbAttendeesError.Visible = false;

                gAttendeesAttendance.SetLinqDataSource( qryResult );
                var currentPageItems = gAttendeesAttendance.DataSource as List<AttendeeResult>;
                if ( currentPageItems != null )
                {
                    var currentPagePersonIds = currentPageItems.Select( i => i.PersonId ).ToList();
                    gAttendeesAttendance.PersonIdField = "PersonId";
                    gAttendeesAttendance.DataKeyNames = new string[] { "PersonId" };
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
            result.AddRange( _possibleAttendances );

            var startDay = dateRange.Start.Value;
            var firstAttendanceDate = startDay;
            if ( _possibleAttendances.Any() )
            {
                firstAttendanceDate = _possibleAttendances.FirstOrDefault();
                startDay = dateRange.Start.Value.StartOfWeek( firstAttendanceDate.DayOfWeek );
                startDay = new DateTime( startDay.Year, startDay.Month, startDay.Day, firstAttendanceDate.Hour, firstAttendanceDate.Minute, firstAttendanceDate.Second );
            }
            var endDate = dateRange.End.Value;
            var endDay = endDate.StartOfWeek( firstAttendanceDate.DayOfWeek );
            endDay = new DateTime( endDay.Year, endDay.Month, endDay.Day, firstAttendanceDate.Hour, firstAttendanceDate.Minute, firstAttendanceDate.Second );
            if ( endDay > endDate )
            {
                endDay = endDay.AddDays( -7 );
            }
            else
            {
                startDay = startDay.AddDays( 7 );
            }

            if ( attendanceGroupBy == ChartGroupBy.Week )
            {
                var weekEndDate = startDay;
                while ( weekEndDate <= endDay )
                {
                    if ( !result.Contains( weekEndDate ) )
                    {
                        result.Add( weekEndDate );
                    }
                    weekEndDate = weekEndDate.AddDays( 7 );
                }
            }
            else if ( attendanceGroupBy == ChartGroupBy.Month )
            {
                var endOfFirstMonth = startDay.AddDays( -( startDay.Day - 1 ) ).AddMonths( 1 ).AddDays( -1 );
                var endOfLastMonth = endDay.AddDays( -( endDay.Day - 1 ) ).AddMonths( 1 ).AddDays( -1 );

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
                var endOfFirstYear = new DateTime( startDay.Year, 1, 1 ).AddYears( 1 ).AddDays( -1 );
                var endOfLastYear = new DateTime( endDay.Year, 1, 1 ).AddYears( 1 ).AddDays( -1 );

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
            result = result.Where( a => a <= currentDateTime.Date ).OrderBy(a => a).ToList();

            return result;
        }
        #endregion
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

        public int? Age
        {
            get
            {
                return Person.GetAge( Birthdate );
            }
        }

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