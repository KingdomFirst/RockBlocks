// KFS Event Item List

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Web.UI;
using Rock.Web.UI.Controls;

namespace RockWeb.Blocks.Event
{
    /// <summary>
    /// Renders a particular calendar using Lava.
    /// </summary>

    #region Block Attributes

    [DisplayName( "Calendar Item List Lava KFS" )]
    [Category( "KFS > Event" )]
    [Description( "Renders calendar items using Lava and pulls audience." )]

    [EventCalendarField( "Event Calendar", "The event calendar to be displayed", true, "1", order: 0 )]
    [CampusesField( "Campuses", "List of which campuses to show occurrences for. This setting will be ignored in the 'Use Campus Context' is enabled.", required: false, order: 1, includeInactive: true )]
    [BooleanField( "Use Campus Context", "Determine if the campus should be read from the campus context of the page.", order: 2 )]
    [LinkedPage( "Details Page", "Detail page for events", order: 3 )]
    [SlidingDateRangeField( "Date Range", "Optional date range to filter the items on. (defaults to next 1000 days)", false, order: 4 )]
    [IntegerField( "Max Occurrences", "The maximum number of occurrences to show.", false, 100, order: 5 )]
    [CodeEditorField( "Lava Template", "The lava template to use for the results", CodeEditorMode.Lava, CodeEditorTheme.Rock, defaultValue: "{% include '~~/Assets/Lava/EventItemList.lava' %}", order: 6 )]
    [ContentChannelField( "Channel for Lava", "If set lava will generate objects for each item in the selected Content Channel", false, "", "", 7 )]

    #endregion

    public partial class EventItemListLava : RockBlock
    {
        #region Fields

        private readonly string ITEM_TYPE_NAME = "Rock.Model.ContentChannelItem";
        private readonly string CONTENT_CACHE_KEY = "Content";
        private readonly string TEMPLATE_CACHE_KEY = "Template";

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the calendar event dates.
        /// </summary>
        /// <value>
        /// The calendar event dates.
        /// </value>
        private List<DateTime> CalendarEventDates { get; set; }

        #endregion

        #region Base Control Methods

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            // this event gets fired after block settings are updated. it's nice to repaint the screen if these settings would alter it
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

            if ( !Page.IsPostBack )
            {
                LoadContent();
            }
        }

        /// <summary>
        /// Handles the BlockUpdated event of the control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void Block_BlockUpdated( object sender, EventArgs e )
        {
            LoadContent();
        }

        #endregion

        #region Methods

        /// <summary>
        /// Loads the content.
        /// </summary>
        private void LoadContent()
        {
            var rockContext = new RockContext();
            var eventCalendarGuid = GetAttributeValue( "EventCalendar" ).AsGuid();
            var eventCalendar = new EventCalendarService( rockContext ).Get( eventCalendarGuid );

            if ( eventCalendar == null )
            {
                lMessages.Text = "<div class='alert alert-warning'>No event calendar is configured for this block.</div>";
                lContent.Text = string.Empty;
                return;
            }
            else
            {
                lMessages.Text = string.Empty;
            }

            var eventItemOccurrenceService = new EventItemOccurrenceService( rockContext );

            // Grab events
            // NOTE: Do not use AsNoTracking() so that things can be lazy loaded if needed
            var qry = eventItemOccurrenceService
                    .Queryable( "EventItem, EventItem.EventItemAudiences,Schedule" )
                    .Where( m =>
                        m.EventItem.EventCalendarItems.Any( i => i.EventCalendarId == eventCalendar.Id ) &&
                        m.EventItem.IsActive );

            // Filter by campus (always include the "All Campuses" events)
            if ( GetAttributeValue( "UseCampusContext" ).AsBoolean() )
            {
                var campusEntityType = EntityTypeCache.Get<Campus>();
                var contextCampus = RockPage.GetCurrentContext( campusEntityType ) as Campus;

                if ( contextCampus != null )
                {
                    qry = qry.Where( e => e.CampusId == contextCampus.Id || !e.CampusId.HasValue );
                }
            }
            else
            {
                var campusGuidList = GetAttributeValue( "Campuses" ).Split( ',' ).AsGuidList();
                if ( campusGuidList.Any() )
                {
                    qry = qry.Where( e => !e.CampusId.HasValue || campusGuidList.Contains( e.Campus.Guid ) );
                }
            }

            // make sure they have a date range
            var dateRange = SlidingDateRangePicker.CalculateDateRangeFromDelimitedValues( this.GetAttributeValue( "DateRange" ) );
            var today = RockDateTime.Today;
            dateRange.Start = dateRange.Start ?? today;
            if ( dateRange.End == null )
            {
                dateRange.End = dateRange.Start.Value.AddDays( 1000 );
            }

            // Get the occurrences
            var occurrences = qry.ToList();
            var occurrencesWithDates = occurrences
                .Select( o => new EventOccurrenceDate
                {
                    EventItemOccurrence = o,
                    Dates = o.GetStartTimes( dateRange.Start.Value, dateRange.End.Value ).ToList()
                } )
                .Where( d => d.Dates.Any() )
                .ToList();

            CalendarEventDates = new List<DateTime>();

            var eventOccurrenceSummaries = new List<EventOccurrenceSummaryKFS>();
            foreach ( var occurrenceDates in occurrencesWithDates )
            {
                var eventItemOccurrence = occurrenceDates.EventItemOccurrence;
                foreach ( var datetime in occurrenceDates.Dates )
                {
                    CalendarEventDates.Add( datetime.Date );

                    if ( datetime >= dateRange.Start.Value && datetime < dateRange.End.Value )
                    {
                        var eventAudiences = eventItemOccurrence.EventItem.EventItemAudiences;
                        eventOccurrenceSummaries.Add( new EventOccurrenceSummaryKFS
                        {
                            EventItemOccurrence = eventItemOccurrence,
                            EventItem = eventItemOccurrence.EventItem,
                            EventItemAudiences = eventAudiences.Select( o => DefinedValueCache.Get( o.DefinedValueId ).Value ).ToList(),
                            Name = eventItemOccurrence.EventItem.Name,
                            DateTime = datetime,
                            Date = datetime.ToShortDateString(),
                            Time = datetime.ToShortTimeString(),
                            Location = eventItemOccurrence.Campus != null ? eventItemOccurrence.Campus.Name : "All Campuses",
                            Description = eventItemOccurrence.EventItem.Description,
                            Summary = eventItemOccurrence.EventItem.Summary,
                            DetailPage = string.IsNullOrWhiteSpace( eventItemOccurrence.EventItem.DetailsUrl ) ? null : eventItemOccurrence.EventItem.DetailsUrl
                        } );
                    }
                }
            }

            eventOccurrenceSummaries = eventOccurrenceSummaries
                .OrderBy( e => e.DateTime )
                .ThenBy( e => e.Name )
                .ToList();

            // limit results
            int? maxItems = GetAttributeValue( "MaxOccurrences" ).AsIntegerOrNull();
            if ( maxItems.HasValue )
            {
                eventOccurrenceSummaries = eventOccurrenceSummaries.Take( maxItems.Value ).ToList();
            }

            var mergeFields = Rock.Lava.LavaHelper.GetCommonMergeFields( this.RockPage, this.CurrentPerson );
            mergeFields.Add( "DetailsPage", LinkedPageUrl( "DetailsPage", null ) );
            mergeFields.Add( "EventOccurrenceSummaries", eventOccurrenceSummaries );

            //KFS Custom code to link Channels together
            var items = GetCacheItem( CONTENT_CACHE_KEY ) as List<ContentChannelItem>;
            var errorMessages = new List<string>();

            Guid? channelGuid = GetAttributeValue( "ChannelforLava" ).AsGuidOrNull();
            if ( channelGuid.HasValue )
            {
                //var rockContext = new RockContext();
                var service = new ContentChannelItemService( rockContext );
                var itemType = typeof( Rock.Model.ContentChannelItem );

                ParameterExpression paramExpression = service.ParameterExpression;

                var contentChannel = new ContentChannelService( rockContext ).Get( channelGuid.Value );

                if ( contentChannel != null )
                {
                    var entityFields = HackEntityFields( contentChannel, rockContext );

                    if ( items == null )
                    {
                        items = new List<ContentChannelItem>();

                        var qryChannel = service.Queryable( "ContentChannel,ContentChannelType" );

                        int? itemId = PageParameter( "Item" ).AsIntegerOrNull();
                        {
                            qryChannel = qryChannel.Where( i => i.ContentChannelId == contentChannel.Id );

                            if ( contentChannel.RequiresApproval )
                            {
                                // Check for the configured status and limit query to those
                                var statuses = new List<ContentChannelItemStatus>();

                                foreach ( string statusVal in ( GetAttributeValue( "Status" ) ?? "2" ).Split( new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries ) )
                                {
                                    var status = statusVal.ConvertToEnumOrNull<ContentChannelItemStatus>();
                                    if ( status != null )
                                    {
                                        statuses.Add( status.Value );
                                    }
                                }
                                if ( statuses.Any() )
                                {
                                    qryChannel = qryChannel.Where( i => statuses.Contains( i.Status ) );
                                }
                            }

                            int? dataFilterId = GetAttributeValue( "FilterId" ).AsIntegerOrNull();
                            if ( dataFilterId.HasValue )
                            {
                                var dataFilterService = new DataViewFilterService( rockContext );
                                var dataFilter = dataFilterService.Queryable( "ChildFilters" ).FirstOrDefault( a => a.Id == dataFilterId.Value );
                                Expression whereExpression = dataFilter != null ? dataFilter.GetExpression( itemType, service, paramExpression, errorMessages ) : null;

                                qryChannel = qryChannel.Where( paramExpression, whereExpression, null );
                            }
                        }

                        // All filtering has been added, now run query and load attributes
                        foreach ( var item in qryChannel.ToList() )
                        {
                            item.LoadAttributes( rockContext );
                            items.Add( item );
                        }

                        // Order the items
                        SortProperty sortProperty = null;

                        string orderBy = GetAttributeValue( "Order" );
                        if ( !string.IsNullOrWhiteSpace( orderBy ) )
                        {
                            var fieldDirection = new List<string>();
                            foreach ( var itemPair in orderBy.Split( new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries ).Select( a => a.Split( '^' ) ) )
                            {
                                if ( itemPair.Length == 2 && !string.IsNullOrWhiteSpace( itemPair[0] ) )
                                {
                                    var sortDirection = SortDirection.Ascending;
                                    if ( !string.IsNullOrWhiteSpace( itemPair[1] ) )
                                    {
                                        sortDirection = itemPair[1].ConvertToEnum<SortDirection>( SortDirection.Ascending );
                                    }
                                    fieldDirection.Add( itemPair[0] + ( sortDirection == SortDirection.Descending ? " desc" : "" ) );
                                }
                            }

                            sortProperty = new SortProperty();
                            sortProperty.Direction = SortDirection.Ascending;
                            sortProperty.Property = fieldDirection.AsDelimited( "," );

                            string[] columns = sortProperty.Property.Split( new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries );

                            var itemQry = items.AsQueryable();
                            IOrderedQueryable<ContentChannelItem> orderedQry = null;

                            for ( int columnIndex = 0; columnIndex < columns.Length; columnIndex++ )
                            {
                                string column = columns[columnIndex].Trim();

                                var direction = sortProperty.Direction;
                                if ( column.ToLower().EndsWith( " desc" ) )
                                {
                                    column = column.Left( column.Length - 5 );
                                    direction = sortProperty.Direction == SortDirection.Ascending ? SortDirection.Descending : SortDirection.Ascending;
                                }

                                try
                                {
                                    if ( column.StartsWith( "Attribute:" ) )
                                    {
                                        string attributeKey = column.Substring( 10 );

                                        if ( direction == SortDirection.Ascending )
                                        {
                                            orderedQry = ( columnIndex == 0 ) ?
                                                itemQry.OrderBy( i => i.AttributeValues.Where( v => v.Key == attributeKey ).FirstOrDefault().Value.SortValue ) :
                                                orderedQry.ThenBy( i => i.AttributeValues.Where( v => v.Key == attributeKey ).FirstOrDefault().Value.SortValue );
                                        }
                                        else
                                        {
                                            orderedQry = ( columnIndex == 0 ) ?
                                                itemQry.OrderByDescending( i => i.AttributeValues.Where( v => v.Key == attributeKey ).FirstOrDefault().Value.SortValue ) :
                                                orderedQry.ThenByDescending( i => i.AttributeValues.Where( v => v.Key == attributeKey ).FirstOrDefault().Value.SortValue );
                                        }
                                    }
                                    else
                                    {
                                        if ( direction == SortDirection.Ascending )
                                        {
                                            orderedQry = ( columnIndex == 0 ) ? itemQry.OrderBy( column ) : orderedQry.ThenBy( column );
                                        }
                                        else
                                        {
                                            orderedQry = ( columnIndex == 0 ) ? itemQry.OrderByDescending( column ) : orderedQry.ThenByDescending( column );
                                        }
                                    }
                                }
                                catch { }
                            }

                            try
                            {
                                if ( orderedQry != null )
                                {
                                    items = orderedQry.ToList();
                                }
                            }
                            catch { }
                        }

                        int? cacheDuration = GetAttributeValue( "CacheDuration" ).AsInteger();
                        if ( cacheDuration > 0 )
                        {
                            AddCacheItem( CONTENT_CACHE_KEY, items, cacheDuration.Value );
                        }
                    }
                }

                if ( items != null )
                {
                    mergeFields.Add( "ContentChannelItems", items );
                }
            }

            lContent.Text = GetAttributeValue( "LavaTemplate" ).ResolveMergeFields( mergeFields );
        }

        /// <summary>
        /// The PropertyFilter checks for it's property/attribute list in a cached items object before recreating
        /// them using reflection and loading of generic attributes. Because of this, we're going to load them here
        /// and exclude some properties and add additional attributes specific to the channel type, and then save
        /// list to same cached object so that property filter lists our collection of properties/attributes
        /// instead.
        /// </summary>
        private List<Rock.Reporting.EntityField> HackEntityFields( ContentChannel channel, RockContext rockContext )
        {
            if ( channel != null )
            {
                var entityTypeCache = EntityTypeCache.Get( ITEM_TYPE_NAME );
                if ( entityTypeCache != null )
                {
                    var entityType = entityTypeCache.GetEntityType();

                    HttpContext.Current.Items.Remove( string.Format( "EntityHelper:GetEntityFields:{0}", entityType.FullName ) );
                    var entityFields = Rock.Reporting.EntityHelper.GetEntityFields( entityType );
                    foreach ( var entityField in entityFields
                        .Where( f =>
                            f.FieldKind == Rock.Reporting.FieldKind.Attribute &&
                            f.AttributeGuid.HasValue )
                        .ToList() )
                    {
                        var attribute = AttributeCache.Get( entityField.AttributeGuid.Value );
                        if ( attribute != null &&
                            attribute.EntityTypeQualifierColumn == "ContentChannelTypeId" &&
                            attribute.EntityTypeQualifierValue.AsInteger() != channel.ContentChannelTypeId )
                        {
                            entityFields.Remove( entityField );
                        }
                    }

                    if ( entityFields != null )
                    {
                        // Remove the status field
                        var ignoreFields = new List<string>();
                        ignoreFields.Add( "ContentChannelId" );
                        ignoreFields.Add( "Status" );

                        entityFields = entityFields.Where( f => !ignoreFields.Contains( f.Name ) ).ToList();

                        // Add any additional attributes that are specific to channel/type
                        var item = new ContentChannelItem();
                        item.ContentChannel = channel;
                        item.ContentChannelId = channel.Id;
                        item.ContentChannelType = channel.ContentChannelType;
                        item.ContentChannelTypeId = channel.ContentChannelTypeId;
                        item.LoadAttributes( rockContext );
                        foreach ( var attribute in item.Attributes
                            .Where( a =>
                                a.Value.EntityTypeQualifierColumn != "" &&
                                a.Value.EntityTypeQualifierValue != "" )
                            .Select( a => a.Value ) )
                        {
                            if ( !entityFields.Any( f => f.AttributeGuid.Equals( attribute.Guid ) ) )
                            {
                                Rock.Reporting.EntityHelper.AddEntityFieldForAttribute( entityFields, attribute );
                            }
                        }

                        // Re-sort fields
                        int index = 0;
                        var sortedFields = new List<Rock.Reporting.EntityField>();
                        foreach ( var entityProperty in entityFields.OrderBy( p => p.Title ).ThenBy( p => p.Name ) )
                        {
                            entityProperty.Index = index;
                            index++;
                            sortedFields.Add( entityProperty );
                        }

                        // Save new fields to cache ( which report field will use instead of reading them again )
                        HttpContext.Current.Items[string.Format( "EntityHelper:GetEntityFields:{0}", entityType.FullName )] = sortedFields;
                    }

                    return entityFields;
                }
            }

            return null;
        }

        #endregion

        #region Helper Classes

        /// <summary>
        /// A class to store event item occurrence data for liquid
        /// </summary>
        [DotLiquid.LiquidType( "EventItem", "EventItemAudiences", "EventItemOccurrence", "DateTime", "Name", "Date", "Time", "Location", "Description", "Summary", "DetailPage" )]
        public class EventOccurrenceSummaryKFS
        {
            /// <summary>
            /// Gets or sets the event item.
            /// </summary>
            /// <value>
            /// The event item.
            /// </value>
            public EventItem EventItem { get; set; }

            /// <summary>
            /// Gets or sets the event item.
            /// </summary>
            /// <value>
            /// The event item.
            /// </value>
            public ICollection<string> EventItemAudiences { get; set; }

            /// <summary>
            /// Gets or sets the event item occurrence.
            /// </summary>
            /// <value>
            /// The event item occurrence.
            /// </value>
            public EventItemOccurrence EventItemOccurrence { get; set; }

            /// <summary>
            /// Gets or sets the date time.
            /// </summary>
            /// <value>
            /// The date time.
            /// </value>
            public DateTime DateTime { get; set; }

            /// <summary>
            /// Gets or sets the name.
            /// </summary>
            /// <value>
            /// The name.
            /// </value>
            public string Name { get; set; }

            /// <summary>
            /// Gets or sets the date.
            /// </summary>
            /// <value>
            /// The date.
            /// </value>
            public string Date { get; set; }

            /// <summary>
            /// Gets or sets the time.
            /// </summary>
            /// <value>
            /// The time.
            /// </value>
            public string Time { get; set; }

            /// <summary>
            /// Gets or sets the location.
            /// </summary>
            /// <value>
            /// The location.
            /// </value>
            public string Location { get; set; }

            /// <summary>
            /// Gets or sets the summary.
            /// </summary>
            /// <value>
            /// The summary.
            /// </value>
            public string Summary { get; set; }

            /// <summary>
            /// Gets or sets the description.
            /// </summary>
            /// <value>
            /// The description.
            /// </value>
            public string Description { get; set; }

            /// <summary>
            /// Gets or sets the detail page.
            /// </summary>
            /// <value>
            /// The detail page.
            /// </value>
            public string DetailPage { get; set; }
        }

        /// <summary>
        /// A class to store the event item occurrences dates
        /// </summary>
        public class EventOccurrenceDate
        {
            /// <summary>
            /// Gets or sets the event item occurrence.
            /// </summary>
            /// <value>
            /// The event item occurrence.
            /// </value>
            public EventItemOccurrence EventItemOccurrence { get; set; }

            /// <summary>
            /// Gets or sets the dates.
            /// </summary>
            /// <value>
            /// The dates.
            /// </value>
            public List<DateTime> Dates { get; set; }
        }

        #endregion
    }
}
