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

                gOccurrences.DataKeyNames = new string[] { "Id", "OccurrenceDate", "ScheduleId", "LocationId" };
                gOccurrences.GridRebind += gOccurrences_GridRebind;
                gOccurrences.RowDataBound += gOccurrences_RowDataBound;

                gOccurrences.Actions.ShowAdd = false;
                gOccurrences.Actions.ShowMergeTemplate = false;
                gOccurrences.IsDeleteEnabled = false;
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
                BindGrid();
            }
        }

        #endregion

        #region Events

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
        /// Handles the GridRebind event of the gOccurrences control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        protected void gOccurrences_GridRebind( object sender, EventArgs e )
        {
            BindGrid();
        }

        #endregion

        #region Internal Methods

        /// <summary>
        /// Binds the group members grid.
        /// </summary>
        protected void BindGrid()
        {
            if ( _group != null )
            {
                lHeading.Text = _group.Name;

                DateTime? fromDateTime = DateTime.Now.AddDays(-35);
                DateTime? toDateTime = DateTime.Now;
                List<int> locationIds = new List<int>();
                List<int> scheduleIds = new List<int>();

                // Get all the occurrences for this group for the selected dates, location and schedule
                var occurrences = new AttendanceOccurrenceService( _rockContext )
                    .GetGroupOccurrences( _group, fromDateTime, toDateTime, locationIds, scheduleIds )
                    .Select( o => new AttendanceListOccurrence( o ) )
                    .ToList();

                var locationService = new LocationService( _rockContext );

                // Update the Parent Location path 
                foreach ( int parentLocationId in occurrences
                    .Where( o => o.ParentLocationId.HasValue )
                    .Select( o => o.ParentLocationId.Value )
                    .Distinct() )
                {
                    string parentLocationPath = locationService.GetPath( parentLocationId );
                    foreach ( var occ in occurrences
                        .Where( o => 
                            o.ParentLocationId.HasValue && 
                            o.ParentLocationId.Value == parentLocationId ) )
                    {
                        occ.ParentLocationPath = parentLocationPath;
                    }
                }

                // Sort the occurrences
                SortProperty sortProperty = gOccurrences.SortProperty;
                List<AttendanceListOccurrence> sortedOccurrences = null;
                if ( sortProperty != null )
                {
                    if ( sortProperty.Property == "LocationPath,LocationName" )
                    {
                        if ( sortProperty.Direction == SortDirection.Ascending )
                        {
                            sortedOccurrences = occurrences.OrderBy( o => o.ParentLocationPath ).ThenBy( o => o.LocationName ).ToList();
                        }
                        else
                        {
                            sortedOccurrences = occurrences.OrderByDescending( o => o.ParentLocationPath ).ThenByDescending( o => o.LocationName ).ToList();
                        }
                    }
                    else
                    {
                        sortedOccurrences = occurrences.AsQueryable().Sort( sortProperty ).ToList();
                    }
                }
                else
                {
                    sortedOccurrences = occurrences.OrderByDescending( a => a.OccurrenceDate ).ThenByDescending( a => a.StartTime ).ToList();
                }

                gOccurrences.DataSource = sortedOccurrences;
                gOccurrences.DataBind();
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


}