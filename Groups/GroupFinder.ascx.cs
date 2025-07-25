﻿// <copyright>
// Copyright by the Spark Development Network
//
// Licensed under the Rock Community License (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.rockrms.com/license
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>
//
// <notice>
// This file contains modifications by Kingdom First Solutions
// and is a derivative work.
//
// Modification (including but not limited to):
// * Added ability to set default location so when address is enabled a campus can be selected and results auto load.
// * Added single select setting so that multiselect campus filter can be a drop down.
// * Added ability to set filters by url param
// * Added an override setting for PersonGuid mode that enables search options
// * Added postal code search capability
// * Added Collapsible filters
// * Added Custom Sorting based on Attribute Filter
// * Added ability to hide attribute values from the search panel
// * Added Custom Schedule Support to DOW Filters
// * Added Keyword search to search name or description of groups
// * Added an additional setting to include Pending members in Over Capacity checking
// * Added a setting to override groups Is Public setting to show on the finder
// * Added ability to display Over Capacity groups with a filter
// * Added Auto Load Filter capability on value selection
// * Added ability to sort how filters are displayed
// * Added ability to load Group/Sign Up Opportunities into finder
// * Added a setting to use Abbreviated Attribute Name in the filter control
// Package Version 1.8.4
// </notice>
//
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Entity;
using System.Data.Entity.Spatial;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

using DotLiquid;
using RestSharp.Extensions;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Field.Types;
using Rock.Lava;
using Rock.Model;
using Rock.Reporting;
using Rock.Security;
using Rock.Utility;
using Rock.Web;
using Rock.Web.Cache;
using Rock.Web.UI;
using Rock.Web.UI.Controls;

namespace RockWeb.Plugins.rocks_kfs.Groups
{
    /// <summary>
    /// Block for people to find a group that matches their search parameters.
    /// </summary>

    #region Block Attributes

    [DisplayName( "Group Finder KFS" )]
    [Category( "KFS > Groups" )]
    [Description( "Block for people to find a group that matches their search parameters." )]

    #endregion Block Attributes

    #region Block Settings

    //Advanced/not custom settings
    [BooleanField( "Auto Load",
        Description = "When set to true, all results will be loaded to begin.",
        DefaultBooleanValue = false,
        Key = AttributeKey.AutoLoad )]
    [CampusField( "Default Location",
        "The campus address that should be used as fallback for the search criteria.",
        false,
        "",
        "",
        Key = AttributeKey.DefaultLocation )]
    [BooleanField( "Single Select Campus Filter",
        Description = "When set to true, the campus filter will be a drop down instead of checkbox.",
        DefaultBooleanValue = false,
        Key = AttributeKey.SingleSelectFilters )]
    [BooleanField( "Allow Search in PersonGuid Mode",
        Description = "When set to true PersonGuid mode will allow you to change filters and search in that mode for that person.",
        DefaultBooleanValue = false,
        Key = AttributeKey.AllowSearchPersonGuid )]
    [CustomDropdownListField( "Collapse Filters on Search",
        Description = "When set to yes, all filters will be collapsed into a single 'Filters' dropdown. If set to 'Same as Initial Load' it will behave the same way as the initial load after search. Default: No",
        ListSource = "False^No,True^Yes,InitialLoad^Same as Initial Load",
        IsRequired = false,
        DefaultValue = "False",
        Key = AttributeKey.CollapseFiltersonSearch )]
    [BooleanField( "Show All Groups",
        Description = "When set to true, all groups will show including those without Is Public being set to true.",
        DefaultBooleanValue = false,
        Key = AttributeKey.ShowAllGroups )]
    [BooleanField( "Overcapacity Groups include Pending",
        Description = "When set to true, the Hide Overcapacity Groups setting also takes into account pending members.",
        DefaultBooleanValue = false,
        Key = AttributeKey.OvercapacityGroupsincludePending )]
    [BooleanField( "Auto Filter Enabled",
        Description = "When set to yes, the various filters will automatically filter the results, whether it is on checkbox checked, selection made, text changed, etc.",
        DefaultBooleanValue = false,
        Key = AttributeKey.AutoFilterEnabled )]
    [LavaCommandsField( "Formatted Output Enabled Lava Commands",
        Description = "Choose what commands to enable in formatted output lava.",
        IsRequired = false,
        Key = AttributeKey.FormattedOutputEnabledLavaCommands )]
    [BooleanField( "Add Group Opportunities",
        Description = "Add the merge field GroupOpportunities to the lava result with a custom object for Sign-up Opportunities. See documentation for field properties.",
        DefaultBooleanValue = false,
        Key = AttributeKey.AddGroupOpportunities )]
    [BooleanField( "Use Abbreviated Attribute Name",
        Description = "Use the Abbreviated Attribute name when displaying the filter control.",
        DefaultBooleanValue = false,
        Key = AttributeKey.UseAbbreviatedAttributeName )]

    // Linked Pages
    [LinkedPage( "Group Detail Page",
        Description = "The page to navigate to for group details.",
        IsRequired = false,
        Category = AttributeCategory.CustomSetting,
        Key = AttributeKey.GroupDetailPage )]
    [LinkedPage( "Register Page",
        Description = "The page to navigate to when registering for a group.",
        IsRequired = false,
        Category = AttributeCategory.CustomSetting,
        Key = AttributeKey.RegisterPage )]

    // Filter Settings
    [GroupTypeField( "Group Type",
        IsRequired = true,
        Category = AttributeCategory.CustomSetting,
        Key = AttributeKey.GroupType )]
    [GroupTypeField( "Geofenced Group Type",
        IsRequired = false,
        Category = AttributeCategory.CustomSetting,
        Key = AttributeKey.GeofencedGroupType )]
    [TextField( "CampusLabel",
        IsRequired = true,
        DefaultValue = "Campuses",
        Category = AttributeCategory.CustomSetting,
        Key = AttributeKey.CampusLabel )]
    [DefinedValueField(
        "Campus Types",
        Key = AttributeKey.CampusTypes,
        Description = "Allows selecting which campus types to filter campuses by.",
        IsRequired = false,
        DefinedTypeGuid = Rock.SystemGuid.DefinedType.CAMPUS_TYPE,
        AllowMultiple = true )]
    [DefinedValueField(
        "Campus Statuses",
        Key = AttributeKey.CampusStatuses,
        Description = "This allows selecting which campus statuses to filter campuses by.",
        IsRequired = false,
        DefinedTypeGuid = Rock.SystemGuid.DefinedType.CAMPUS_STATUS,
        AllowMultiple = true )]
    [TextField( "TimeOfDayLabel",
        IsRequired = true,
        DefaultValue = "Time of Day",
        Category = AttributeCategory.CustomSetting,
        Key = AttributeKey.TimeOfDayLabel )]
    [TextField( "DayOfWeekLabel",
        IsRequired = true,
        DefaultValue = "Day of Week",
        Category = AttributeCategory.CustomSetting,
        Key = AttributeKey.DayOfWeekLabel )]
    [TextField( "PostalCodeLabel",
        IsRequired = true,
        DefaultValue = "Zip Code",
        Category = AttributeCategory.CustomSetting,
        Key = AttributeKey.PostalCodeLabel )]
    [TextField( "KeywordLabel",
        IsRequired = true,
        DefaultValue = "Keyword",
        Category = AttributeCategory.CustomSetting,
        Key = AttributeKey.KeywordLabel )]
    [TextField( "FilterLabel", IsRequired = true,
        DefaultValue = "Filter",
        Category = AttributeCategory.CustomSetting,
        Key = AttributeKey.FilterLabel )]
    [TextField( "MoreFiltersLabel", IsRequired = true,
        DefaultValue = "More Filters", Category = AttributeCategory.CustomSetting,
        Key = AttributeKey.MoreFiltersLabel )]
    [TextField( "Show Full Groups Label",
        IsRequired = true,
        DefaultValue = "Show Full Groups",
        Category = AttributeCategory.CustomSetting,
        Key = AttributeKey.ShowFullGroupsLabel )]
    [TextField( "ScheduleFilters",
        IsRequired = false,
        Category = AttributeCategory.CustomSetting,
        Key = AttributeKey.ScheduleFilters )]
    [BooleanField( "Display Campus Filter",
        IsRequired = false,
        Category = AttributeCategory.CustomSetting,
        Key = AttributeKey.DisplayCampusFilter )]
    [BooleanField( "Display Keyword Search",
        DefaultBooleanValue = false,
        Category = AttributeCategory.CustomSetting,
        Key = AttributeKey.DisplayKeywordSearch )]
    [BooleanField( "Enable Campus Context",
        IsRequired = false,
        Category = AttributeCategory.CustomSetting,
        Key = AttributeKey.EnableCampusContext )]
    [CustomDropdownListField( "Hide Overcapacity Groups",
        Description = "When set to true, groups that are at capacity or whose default GroupTypeRole are at capacity are hidden.",
        DefaultValue = "True",
        ListSource = "False^Don't Hide,True^Hide,Filter^Display as Filter",
        Category = AttributeCategory.CustomSetting,
        Key = AttributeKey.HideOvercapacityGroups )]
    [AttributeField( "Attribute Filters",
        EntityTypeGuid = Rock.SystemGuid.EntityType.GROUP,
        IsRequired = false,
        AllowMultiple = true,
        Category = AttributeCategory.CustomSetting,
        Key = AttributeKey.AttributeFilters )]
    [AttributeField( "Attribute Custom Sort",
        EntityTypeGuid = Rock.SystemGuid.EntityType.GROUP,
        Description = "Select an attribute to sort by if a group contains multiple of the selected filter options.",
        IsRequired = false,
        AllowMultiple = false,
        Category = AttributeCategory.CustomSetting,
        Key = AttributeKey.AttributeCustomSort )]
    [BooleanField( "Enable Postal Code Search",
        DefaultBooleanValue = false,
        Category = AttributeCategory.CustomSetting,
        Key = AttributeKey.EnablePostalCodeSearch )]
    [BooleanField( "Require Postal Code",
        DefaultBooleanValue = true,
        Category = AttributeCategory.CustomSetting,
        Key = AttributeKey.RequirePostalCode )]
    [CustomCheckboxListField( "Collapse Filters on Initial Load",
        Description = "Collapse/Hide these filter controls under a collapsible panel for user on first load. Note: Selected filters may impact Filter Display Order.",
        ListSource = "SELECT REPLACE(item,'filter_','') as Text, LOWER(item) as Value FROM string_split('filter_DayofWeek,filter_Time,filter_Campus,filter_Address,filter_PostalCode,filter_ShowFullGroups') UNION ALL SELECT a.Name as Text, a.Id as Value FROM [Attribute] a JOIN [EntityType] et ON et.Id = a.EntityTypeId WHERE et.[Guid] = '9BBFDA11-0D22-40D5-902F-60ADFBC88987'",
        IsRequired = false,
        Category = AttributeCategory.CustomSetting,
        Key = AttributeKey.HideFiltersInitialLoad )]
    [CustomCheckboxListField( "Exclude Attribute Values from Filter",
        Description = "Use this setting to hide attribute values from the available options in the search filter. Some field types are not supported (such as Enhanced For Long List attributes).",
        ListSource = "SELECT a.Name as Text, CONCAT(a.[Key],'_',a.FieldTypeId) as Value FROM [Attribute] a JOIN [EntityType] et ON et.Id = a.EntityTypeId WHERE et.[Guid] = '9BBFDA11-0D22-40D5-902F-60ADFBC88987'",
        IsRequired = false, Category = AttributeCategory.CustomSetting,
        Key = AttributeKey.HideAttributeValues )]
    [AttributeField( "Attributes included in Keyword Search",
        EntityTypeGuid = Rock.SystemGuid.EntityType.GROUP,
        IsRequired = false,
        AllowMultiple = true,
        Category = AttributeCategory.CustomSetting,
        Key = AttributeKey.AttributesInKeywords )]
    [KeyValueListField( "Filter Display Order",
        Description = "Key Value list of filters to display filters in the set order. If using 'Hide Selected Filters on Initial Load' the sort order is within each individual area.",
        KeyPrompt = "Order",
        ValuePrompt = "Filter Id",
        Category = AttributeCategory.CustomSetting,
        Key = AttributeKey.FilterOrder )]

    // Map Settings
    [BooleanField( "Show Map",
        DefaultBooleanValue = false,
        Category = AttributeCategory.CustomSetting,
        Key = AttributeKey.ShowMap )]
    [DefinedValueField( "Map Style",
        DefinedTypeGuid = Rock.SystemGuid.DefinedType.MAP_STYLES,
        IsRequired = true,
        AllowMultiple = false,
        DefaultValue = Rock.SystemGuid.DefinedValue.MAP_STYLE_GOOGLE,
        Category = AttributeCategory.CustomSetting,
        Key = AttributeKey.MapStyle )]
    [IntegerField( "Map Height",
        IsRequired = false,
        DefaultIntegerValue = 600,
        Category = AttributeCategory.CustomSetting,
        Key = AttributeKey.MapHeight )]
    [BooleanField( "Show Fence",
        DefaultBooleanValue = false,
        Category = AttributeCategory.CustomSetting,
        Key = AttributeKey.ShowFence )]
    [ValueListField( "Polygon Colors",
        IsRequired = false,
        DefaultValue = "#f37833|#446f7a|#afd074|#649dac|#f8eba2|#92d0df|#eaf7fc",
        ValuePrompt = "#ffffff",
        Category = AttributeCategory.CustomSetting,
        Key = AttributeKey.PolygonColors
        )]
    [CodeEditorField( "Map Info",
        EditorMode = CodeEditorMode.Lava,
        EditorTheme = CodeEditorTheme.Rock,
        EditorHeight = 200,
        IsRequired = false,
        DefaultValue = AttributeDefaultLava.MapInfo,
        Category = AttributeCategory.CustomSetting,
        Key = AttributeKey.MapInfo )]

    // Lava Output Settings
    [BooleanField( "Show Lava Output",
        DefaultBooleanValue = false,
        Category = AttributeCategory.CustomSetting,
        Key = AttributeKey.ShowLavaOutput )]
    [CodeEditorField( "Lava Output",
        EditorMode = CodeEditorMode.Lava,
        EditorTheme = CodeEditorTheme.Rock,
        EditorHeight = 200,
        IsRequired = false,
        DefaultValue = "",
        Category = AttributeCategory.CustomSetting,
        Key = AttributeKey.LavaOutput )]

    // Grid Settings
    [BooleanField( "Show Grid",
        DefaultBooleanValue = false,
        Category = AttributeCategory.CustomSetting,
        Key = AttributeKey.ShowGrid )]
    [BooleanField( "Show Schedule",
        DefaultBooleanValue = false,
        Category = AttributeCategory.CustomSetting,
        Key = AttributeKey.ShowSchedule )]
    [BooleanField( "Show Proximity",
        DefaultBooleanValue = false,
        Category = AttributeCategory.CustomSetting,
        Key = AttributeKey.ShowProximity )]
    [BooleanField( "Show Campus",
        DefaultBooleanValue = false,
        Category = AttributeCategory.CustomSetting,
        Key = AttributeKey.ShowCampus )]
    [BooleanField( "Show Count",
        DefaultBooleanValue = false,
        Category = AttributeCategory.CustomSetting,
        Key = AttributeKey.ShowCount )]
    [BooleanField( "Show Age",
        DefaultBooleanValue = false,
        Category = AttributeCategory.CustomSetting,
        Key = AttributeKey.ShowAge )]
    [BooleanField( "Show Description",
        DefaultBooleanValue = true,
        Category = AttributeCategory.CustomSetting,
        Key = AttributeKey.ShowDescription )]
    [AttributeField( "Attribute Columns",
        Category = AttributeCategory.CustomSetting,
        EntityTypeGuid = Rock.SystemGuid.EntityType.GROUP,
        IsRequired = false,
        AllowMultiple = true,
        Key = AttributeKey.AttributeColumns )]
    [BooleanField( "Sort By Distance",
        DefaultBooleanValue = false,
        Category = AttributeCategory.CustomSetting,
        Key = AttributeKey.SortByDistance )]
    [TextField( "Page Sizes",
        Description = "To show a dropdown of page sizes, enter a comma delimited list of page sizes. For example: 10,20 will present a drop down with 10,20,All as options with the default as 10",
        IsRequired = false,
        Category = AttributeCategory.CustomSetting,
        Key = AttributeKey.PageSizes )]
    [BooleanField( "Include Pending",
        DefaultBooleanValue = true,
        Category = AttributeCategory.CustomSetting,
        Key = AttributeKey.IncludePending )]
    // Due to wanting to replace our AutoLoad attribute with the new block setting the core version added in 12.5, we will update the value behind the scenes until the next release. Currently visible = false in the panel for now.
    [BooleanField( "Load Initial Results",
        Key = AttributeKey.LoadInitialResults,
        Category = AttributeCategory.CustomSetting )]
    [TextField( "Group Type Locations",
        Key = AttributeKey.GroupTypeLocations,
        Category = AttributeCategory.CustomSetting )]
    [IntegerField( "Maximum Zoom Level",
        Key = AttributeKey.MaximumZoomLevel,
        Category = AttributeCategory.CustomSetting )]
    [IntegerField( "Minimum Zoom Level",
        Key = AttributeKey.MinimumZoomLevel,
        Category = AttributeCategory.CustomSetting )]
    [IntegerField( "Initial Zoom Level",
        Key = AttributeKey.InitialZoomLevel,
        Category = AttributeCategory.CustomSetting )]
    [IntegerField( "Marker Zoom Level",
        Key = AttributeKey.MarkerZoomLevel,
        Category = AttributeCategory.CustomSetting )]
    [IntegerField( "Marker Zoom Amount",
        DefaultIntegerValue = 1,
        Key = AttributeKey.MarkerZoomAmount,
        Category = AttributeCategory.CustomSetting )]
    [TextField( "Location Precision Level",
        DefaultValue = "Precise",
        Key = AttributeKey.LocationPrecisionLevel,
        Category = AttributeCategory.CustomSetting )]
    [TextField( "Map Marker",
        Key = AttributeKey.MapMarker,
        Category = AttributeCategory.CustomSetting )]
    [TextField( "Marker Color",
        Key = AttributeKey.MarkerColor,
        Category = AttributeCategory.CustomSetting )]

    #endregion Block Settings

    public partial class GroupFinder : RockBlockCustomSettings
    {
        private static class AttributeKey
        {
            public const string GroupDetailPage = "GroupDetailPage";
            public const string RegisterPage = "RegisterPage";
            public const string GroupType = "GroupType";
            public const string GeofencedGroupType = "GeofencedGroupType";
            public const string CampusLabel = "CampusLabel";
            public const string CampusTypes = "CampusTypes";
            public const string CampusStatuses = "CampusStatuses";
            public const string TimeOfDayLabel = "TimeOfDayLabel";
            public const string DayOfWeekLabel = "DayOfWeekLabel";
            public const string ScheduleFilters = "ScheduleFilters";
            public const string DisplayCampusFilter = "DisplayCampusFilter";
            public const string EnableCampusContext = "EnableCampusContext";
            public const string HideOvercapacityGroups = "HideOvercapacityGroups";
            public const string AttributeFilters = "AttributeFilters";
            public const string ShowMap = "ShowMap";
            public const string MapStyle = "MapStyle";
            public const string MapHeight = "MapHeight";
            public const string ShowFence = "ShowFence";
            public const string PolygonColors = "PolygonColors";
            public const string MapInfo = "MapInfo";
            public const string ShowLavaOutput = "ShowLavaOutput";
            public const string LavaOutput = "LavaOutput";
            public const string ShowGrid = "ShowGrid";
            public const string ShowSchedule = "ShowSchedule";
            public const string ShowProximity = "ShowProximity";
            public const string ShowCampus = "ShowCampus";
            public const string ShowCount = "ShowCount";
            public const string ShowAge = "ShowAge";
            public const string ShowDescription = "ShowDescription";
            public const string AttributeColumns = "AttributeColumns";
            public const string SortByDistance = "SortByDistance";
            public const string PageSizes = "PageSizes";
            public const string IncludePending = "IncludePending";
            public const string LoadInitialResults = "LoadInitialResults";
            public const string GroupTypeLocations = "GroupTypeLocations";
            public const string MaximumZoomLevel = "MaximumZoomLevel";
            public const string MinimumZoomLevel = "MinimumZoomLevel";
            public const string InitialZoomLevel = "InitialZoomLevel";
            public const string MarkerZoomLevel = "MarkerZoomLevel";
            public const string MarkerZoomAmount = "MarkerZoomAmount";
            public const string LocationPrecisionLevel = "LocationPrecisionLevel";
            public const string MapMarker = "MapMarker";
            public const string MarkerColor = "MarkerColor";
            public const string PostalCodeLabel = "PostalCodeLabel";
            public const string KeywordLabel = "KeywordLabel";
            public const string FilterLabel = "FilterLabel";
            public const string MoreFiltersLabel = "MoreFiltersLabel";
            public const string DisplayKeywordSearch = "DisplayKeywordSearch";
            public const string OvercapacityGroupsincludePending = "OvercapacityGroupsincludePending";
            public const string AttributeCustomSort = "AttributeCustomSort";
            public const string EnablePostalCodeSearch = "EnablePostalCodeSearch";
            public const string HideFiltersInitialLoad = "HideFiltersInitialLoad";
            public const string HideAttributeValues = "HideAttributeValues";
            public const string AutoLoad = "AutoLoad";
            public const string DefaultLocation = "DefaultLocation";
            public const string SingleSelectFilters = "SingleSelectFilters";
            public const string AllowSearchPersonGuid = "AllowSearchPersonGuid";
            public const string CollapseFiltersonSearch = "CollapseFiltersonSearch";
            public const string ShowAllGroups = "ShowAllGroups";
            public const string AutoFilterEnabled = "AutoFilterEnabled";
            public const string RequirePostalCode = "RequirePostalCode";
            public const string ShowFullGroupsLabel = "ShowFullGroupsLabel";
            public const string AttributesInKeywords = "AttributesInKeywords";
            public const string FormattedOutputEnabledLavaCommands = "FormattedOutputEnabledLavaCommands";
            public const string FilterOrder = "FilterOrder";
            public const string AddGroupOpportunities = "AddGroupOpportunities";
            public const string UseAbbreviatedAttributeName = "UseAbbreviatedAttributeName";
        }

        private static class AttributeDefaultLava
        {
            public const string MapInfo = @"
<h4 class='margin-t-none'>{{ Group.Name }}</h4>

<div class='margin-b-sm'>
{% for attribute in Group.AttributeValues %}
    <strong>{{ attribute.AttributeName }}:</strong> {{ attribute.ValueFormatted }} <br />
{% endfor %}
</div>

<div class='margin-v-sm'>
{% if Location.FormattedHtmlAddress and Location.FormattedHtmlAddress != '' %}
	{{ Location.FormattedHtmlAddress }}
{% endif %}
</div>

{% if LinkedPages.GroupDetailPage and LinkedPages.GroupDetailPage != '' %}
    <a class='btn btn-xs btn-action margin-r-sm' href='{{ LinkedPages.GroupDetailPage }}?GroupId={{ Group.Id }}'>View {{ Group.GroupType.GroupTerm }}</a>
{% endif %}

{% if LinkedPages.RegisterPage and LinkedPages.RegisterPage != '' %}
    {% if LinkedPages.RegisterPage contains '?' %}
        <a class='btn btn-xs btn-action' href='{{ LinkedPages.RegisterPage }}&GroupGuid={{ Group.Guid }}'>Register</a>
    {% else %}
        <a class='btn btn-xs btn-action' href='{{ LinkedPages.RegisterPage }}?GroupGuid={{ Group.Guid }}'>Register</a>
    {% endif %}
{% endif %}
";
        }

        private static class AttributeCategory
        {
            public const string CustomSetting = "CustomSetting";
        }

        private static class ViewStateKey
        {
            public const string AttributeFilters = "AttributeFilters";
            public const string AttributeColumns = "AttributeColumns";
            public const string GroupTypeLocations = "GroupTypeLocations";
            public const string GroupTypeAttributeNames = "GroupTypeAttributeNames";
            public const string KFSGroupFinderFilterOrder = "KFSGroupFinderFilterOrder";
        }

        #region Private Variables

        private Guid _targetPersonGuid = Guid.Empty;
        private Dictionary<string, string> _urlParms = new Dictionary<string, string>();
        private bool _autoLoad = false;
        private bool _ssFilters = false;
        private bool _allowSearch = false;
        private bool _autoPostback = false;
        private string _collapseFilters = "false";
        private List<string> _hideFilters = new List<string>();
        private Dictionary<string, string> _filterValues = new Dictionary<string, string>();
        private Dictionary<string, string> _groupTypeAttributeNames = new Dictionary<string, string>();
        private Dictionary<int, string> _filterOrder = new Dictionary<int, string>();

        private Dictionary<decimal, Control> _collapsedControls = new Dictionary<decimal, Control>();
        private Dictionary<decimal, Control> _filterControls = new Dictionary<decimal, Control>();

        private const string DEFINED_TYPE_KEY = "definedtype";
        private const string ALLOW_MULTIPLE_KEY = "allowmultiple";
        private const string DISPLAY_DESCRIPTION = "displaydescription";
        private const string INCLUDE_INACTIVE_KEY = "includeInactive";
        private const string VALUES_KEY = "values";
        private const string REPEAT_COLUMNS = "repeatColumns";

        private AddressControl filter_acAddress = null;
        private RockTextBox filter_tbPostalCode = null;
        private RegularExpressionValidator revPostalCode = null;
        private RockCheckBoxList filter_cblCampus = null;
        private RockDropDownList filter_ddlCampus = null;

        #endregion Private Variables

        #region Properties

        /// <summary>
        /// Gets the settings tool tip.
        /// </summary>
        /// <value>
        /// The settings tool tip.
        /// </value>
        public override string SettingsToolTip
        {
            get
            {
                return "Edit Settings";
            }
        }

        /// <summary>
        /// Gets or sets the attribute filters.
        /// </summary>
        /// <value>
        /// The attribute filters.
        /// </value>
        public List<AttributeCache> AttributeFilters { get; set; }

        /// <summary>
        /// Gets or sets the _ attribute columns.
        /// </summary>
        /// <value>
        /// The _ attribute columns.
        /// </value>
        public List<AttributeCache> AttributeColumns { get; set; }

        /// <summary>
        /// Gets or sets the Filter Order to the Dictionary.
        /// </summary>
        /// <value>
        /// The Filter Order Dictionary.
        /// </value>
        private Dictionary<int, string> FilterOrder { get; set; }

        #endregion Properties

        #region Base Control Methods

        /// <summary>
        /// Restores the view-state information from a previous user control request that was saved by the <see cref="M:System.Web.UI.UserControl.SaveViewState" /> method.
        /// </summary>
        /// <param name="savedState">An <see cref="T:System.Object" /> that represents the user control state to be restored.</param>
        protected override void LoadViewState( object savedState )
        {
            base.LoadViewState( savedState );

            AttributeFilters = ViewState[ViewStateKey.AttributeFilters] as List<AttributeCache>;
            AttributeColumns = ViewState[ViewStateKey.AttributeColumns] as List<AttributeCache>;
            GroupTypeLocations = ViewState[ViewStateKey.GroupTypeLocations] as Dictionary<int, int>;
            if ( GroupTypeLocations == null )
            {
                GroupTypeLocations = new Dictionary<int, int>();
            }
            _groupTypeAttributeNames = ViewState[ViewStateKey.GroupTypeAttributeNames] as Dictionary<string, string>;
            if ( _groupTypeAttributeNames == null )
            {
                _groupTypeAttributeNames = new Dictionary<string, string>();
            }
            FilterOrder = ViewState[ViewStateKey.KFSGroupFinderFilterOrder] as Dictionary<int, string>;
            if ( FilterOrder == null )
            {
                FilterOrder = new Dictionary<int, string>();
            }

            BuildDynamicControls( true );
        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            _autoLoad = GetAttributeValue( AttributeKey.AutoLoad ).AsBoolean();
            // Due to wanting to replace our AutoLoad attribute with the new block setting the core version added in 12.5, we will update the value behind the scenes until the next release.
            var loadInitialResults = GetAttributeValue( AttributeKey.LoadInitialResults ).AsBoolean();
            if ( loadInitialResults != _autoLoad )
            {
                SetAttributeValue( AttributeKey.LoadInitialResults, _autoLoad.ToString() );
            }
            _ssFilters = GetAttributeValue( AttributeKey.SingleSelectFilters ).AsBoolean();
            _allowSearch = GetAttributeValue( AttributeKey.AllowSearchPersonGuid ).AsBoolean();
            _autoPostback = GetAttributeValue( AttributeKey.AutoFilterEnabled ).AsBoolean();
            _collapseFilters = GetAttributeValue( AttributeKey.CollapseFiltersonSearch );

            base.OnInit( e );

            GroupTypeLocations = GetAttributeValue( AttributeKey.GroupTypeLocations ).FromJsonOrNull<Dictionary<int, int>>();
            var mapMarkerDefinedType = DefinedTypeCache.Get( Rock.SystemGuid.DefinedType.MAP_MARKERS.AsGuid() );
            ddlMapMarker.DefinedTypeId = mapMarkerDefinedType.Id;

            dvpCampusTypes.DefinedTypeId = DefinedTypeCache.Get( new Guid( Rock.SystemGuid.DefinedType.CAMPUS_TYPE ) ).Id;
            dvpCampusStatuses.DefinedTypeId = DefinedTypeCache.Get( new Guid( Rock.SystemGuid.DefinedType.CAMPUS_STATUS ) ).Id;

            gGroups.DataKeyNames = new string[] { "Id" };
            gGroups.Actions.ShowAdd = false;
            gGroups.GridRebind += gGroups_GridRebind;
            gGroups.ShowActionRow = false;
            gGroups.AllowPaging = false;

            this.BlockUpdated += Block_Updated;
            this.AddConfigurationUpdateTrigger( upnlContent );

            this.LoadGoogleMapsApi();

            if ( GroupTypeLocations == null )
            {
                GroupTypeLocations = new Dictionary<int, int>();
            }
            Page.LoadComplete += Page_LoadComplete;
        }

        /// <summary>
        /// Handles the LoadComplete event of the Page control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void Page_LoadComplete( object sender, EventArgs e )
        {
            // Add postback controls
            if ( Page.IsPostBack )
            {
                var control = Page.FindControl( Request.Form["__EVENTTARGET"] );
                if ( control != null && ( control.UniqueID.Contains( "attribute_field_" ) || control.UniqueID.Contains( "filter_" ) ) )
                {
                    btnSearch_Click( null, null );
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

            nbNotice.Visible = false;

            if ( Request["PersonGuid"] != null )
            {
                Guid.TryParse( Request["PersonGuid"].ToString(), out _targetPersonGuid );
                _urlParms.Add( "PersonGuid", _targetPersonGuid.ToString() );
            }

            if ( !Page.IsPostBack )
            {
                BindAttributes();
                BuildDynamicControls();

                var campusPageParam = PageParameter( "filter_campus" );
                var dowPageParam = PageParameter( "filter_dow" );
                var timePageParam = PageParameter( "filter_time" );
                var postalcodePageParam = PageParameter( "postalcode" );

                if ( GetAttributeValue( AttributeKey.EnableCampusContext ).AsBoolean() && GetAttributeValue( AttributeKey.DisplayCampusFilter ).AsBoolean() )
                {
                    // Default campus filter selection to campus context (user can override filter).
                    var campusEntityType = EntityTypeCache.Get( "Rock.Model.Campus" );
                    var contextCampus = RockPage.GetCurrentContext( campusEntityType ) as Campus;

                    if ( contextCampus != null )
                    {
                        if ( filter_cblCampus != null )
                        {
                            filter_cblCampus.SetValue( contextCampus.Id.ToString() );
                        }
                        if ( filter_ddlCampus != null )
                        {
                            filter_ddlCampus.SetValue( contextCampus.Id.ToString() );
                        }
                    }
                }
                else if ( !string.IsNullOrWhiteSpace( campusPageParam ) )
                {
                    var pageParamList = campusPageParam.Split( new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries ).ToList();
                    if ( filter_cblCampus != null )
                    {
                        filter_cblCampus.SetValues( pageParamList );
                    }
                    if ( filter_ddlCampus != null )
                    {
                        filter_ddlCampus.SetValue( campusPageParam );
                    }
                }
                if ( !string.IsNullOrWhiteSpace( dowPageParam ) )
                {
                    var pageParamList = dowPageParam.Split( new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries ).ToList();
                    var dowsFilterControl = phFilterControls.FindControl( "filter_dows" ) as RockCheckBoxList;
                    if ( dowsFilterControl != null )
                    {
                        dowsFilterControl.SetValues( pageParamList );
                    }

                    var dowFilterControl = phFilterControls.FindControl( "filter_dow" );
                    if ( dowFilterControl != null )
                    {
                        var field = FieldTypeCache.Get( Rock.SystemGuid.FieldType.DAY_OF_WEEK ).Field;
                        field.SetFilterValues( dowFilterControl, null, pageParamList );
                    }
                }
                if ( !string.IsNullOrWhiteSpace( timePageParam ) )
                {
                    var pageParamList = timePageParam.Split( new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries ).ToList();
                    var timeFilterControl = phFilterControls.FindControl( "filter_time" );
                    if ( timeFilterControl != null )
                    {
                        var field = FieldTypeCache.Get( Rock.SystemGuid.FieldType.TIME ).Field;
                        field.SetFilterValues( timeFilterControl, null, pageParamList );
                    }
                }

                if ( AttributeFilters != null && AttributeFilters.Any() )
                {
                    foreach ( var attribute in AttributeFilters )
                    {
                        var filterId = $"filter_{attribute.Key}_{attribute.FieldType.Id}";
                        var filterControl = phFilterControls.FindControl( filterId );
                        var filterPageParam = PageParameter( filterId );
                        if ( !string.IsNullOrWhiteSpace( filterPageParam ) )
                        {
                            var filterParamList = filterPageParam.Split( new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries ).ToList();
                            if ( attribute.FieldType.Field is DefinedValueFieldType &&
                                 filterControl != null &&
                                 filterControl.Controls != null &&
                                 filterControl.Controls.Count != 0 &&
                                 filterControl.Controls[0].Controls != null &&
                                 filterControl.Controls[0].Controls.Count != 0 &&
                                 filterControl.Controls.Count > 1 &&
                                 filterControl.Controls[1].Controls != null &&
                                 filterControl.Controls[1].Controls.Count != 0 )
                            {
                                var definedValuePicker = filterControl.Controls[1].Controls[0] as DefinedValuesPicker;
                                var definedValuePickerEnhanced = filterControl.Controls[1].Controls[0] as DefinedValuesPickerEnhanced;
                                if ( definedValuePicker != null )
                                {
                                    definedValuePicker.SetValues( filterParamList );
                                }
                                if ( definedValuePickerEnhanced != null )
                                {
                                    definedValuePickerEnhanced.SetValues( filterParamList );
                                }
                            }
                            else
                            {
                                attribute.FieldType.Field.SetFilterValues( filterControl, attribute.QualifierValues, new List<string> { filterPageParam } );
                            }
                        }
                    }
                }

                if ( !string.IsNullOrWhiteSpace( postalcodePageParam ) )
                {
                    filter_tbPostalCode.Text = postalcodePageParam;
                }

                if ( _targetPersonGuid != Guid.Empty )
                {
                    ShowViewForPerson( _targetPersonGuid );
                }
                else
                {
                    ShowView();
                }
            }
        }

        /// <summary>
        /// Saves any user control view-state changes that have occurred since the last page postback.
        /// </summary>
        /// <returns>
        /// Returns the user control's current view state. If there is no view state associated with the control, it returns null.
        /// </returns>
        protected override object SaveViewState()
        {
            ViewState[ViewStateKey.AttributeFilters] = AttributeFilters;
            ViewState[ViewStateKey.AttributeColumns] = AttributeColumns;
            ViewState[ViewStateKey.GroupTypeLocations] = GroupTypeLocations;
            ViewState[ViewStateKey.GroupTypeAttributeNames] = _groupTypeAttributeNames;
            ViewState[ViewStateKey.KFSGroupFinderFilterOrder] = FilterOrder;

            return base.SaveViewState();
        }

        #endregion Base Control Methods

        #region Events

        /// <summary>
        /// Handles the BlockUpdated event of the ContentDynamic control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void Block_Updated( object sender, EventArgs e )
        {
            ShowView();
        }

        /// <summary>
        /// Handles the SelectedIndexChanged event of the gtpGroupType control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void gtpGroupType_SelectedIndexChanged( object sender, EventArgs e )
        {
            SetGroupTypeOptions();
            SelectGroupTypeOptions();
        }

        /// <summary>
        /// Handles the Click event of the lbSave control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void lbSave_Click( object sender, EventArgs e )
        {
            if ( !Page.IsValid )
            {
                return;
            }

            SetAttributeValue( AttributeKey.GroupType, gtpGroupType.SelectedValuesAsInt.ToJson() );
            SetAttributeValue( AttributeKey.GeofencedGroupType, gtpGeofenceGroupType.SelectedGroupTypeId.ToString() );

            SetAttributeValue( AttributeKey.DayOfWeekLabel, tbDayOfWeekLabel.Text );
            SetAttributeValue( AttributeKey.TimeOfDayLabel, tbTimeOfDayLabel.Text );
            SetAttributeValue( AttributeKey.CampusLabel, tbCampusLabel.Text );
            SetAttributeValue( AttributeKey.PostalCodeLabel, tbPostalCodeLabel.Text );
            SetAttributeValue( AttributeKey.KeywordLabel, tbKeywordLabel.Text );
            SetAttributeValue( AttributeKey.ShowFullGroupsLabel, tbShowFullGroupsLabel.Text );
            SetAttributeValue( AttributeKey.EnablePostalCodeSearch, cbPostalCode.Checked.ToString() );
            SetAttributeValue( AttributeKey.RequirePostalCode, cbRequirePostalCode.Checked.ToString() );
            SetAttributeValue( AttributeKey.DisplayKeywordSearch, cbKeyword.Checked.ToString() );
            SetAttributeValue( AttributeKey.FilterLabel, tbFilterLabel.Text );
            SetAttributeValue( AttributeKey.MoreFiltersLabel, tbMoreFiltersLabel.Text );

            var schFilters = new List<string>();
            if ( rblFilterDOW.Visible )
            {
                schFilters.Add( rblFilterDOW.SelectedValue );
                schFilters.Add( cbFilterTimeOfDay.Checked ? "Time" : string.Empty );
            }

            SetAttributeValue( AttributeKey.ScheduleFilters, schFilters.Where( f => f != string.Empty ).ToList().AsDelimited( "," ) );

            SetAttributeValue( AttributeKey.DisplayCampusFilter, cbFilterCampus.Checked.ToString() );
            SetAttributeValue( AttributeKey.EnableCampusContext, cbCampusContext.Checked.ToString() );
            SetAttributeValue( AttributeKey.HideOvercapacityGroups, ddlHideOvercapacityGroups.SelectedValue );
            SetAttributeValue( AttributeKey.LoadInitialResults, cbLoadInitialResults.Checked.ToString() );
            SetAttributeValue( AttributeKey.GroupTypeLocations, GroupTypeLocations.ToJson() );
            SetAttributeValue( AttributeKey.MaximumZoomLevel, ddlMaxZoomLevel.SelectedValue );
            SetAttributeValue( AttributeKey.MinimumZoomLevel, ddlMinZoomLevel.SelectedValue );
            SetAttributeValue( AttributeKey.InitialZoomLevel, ddlInitialZoomLevel.SelectedValue );
            SetAttributeValue( AttributeKey.MarkerZoomLevel, ddlMarkerZoomLevel.SelectedValue );
            SetAttributeValue( AttributeKey.MarkerZoomAmount, nbMarkerAutoScaleAmount.Text );
            SetAttributeValue( AttributeKey.LocationPrecisionLevel, ddlLocationPrecisionLevel.SelectedValue );
            SetAttributeValue( AttributeKey.MapMarker, ddlMapMarker.SelectedValue );
            SetAttributeValue( AttributeKey.MarkerColor, cpMarkerColor.Text );

            SetAttributeValue( AttributeKey.AttributeFilters, cblAttributes.Items.Cast<ListItem>().Where( i => i.Selected ).Select( i => i.Value ).ToList().AsDelimited( "," ) );
            SetAttributeValue( AttributeKey.HideFiltersInitialLoad, cblInitialLoadFilters.Items.Cast<ListItem>().Where( i => i.Selected ).Select( i => i.Value ).ToList().AsDelimited( "," ) );
            SetAttributeValue( AttributeKey.AttributeCustomSort, ddlAttributeSort.Items.Cast<ListItem>().Where( i => i.Selected ).Select( i => i.Value ).ToList().AsDelimited( "," ) );
            SetAttributeValue( AttributeKey.HideAttributeValues, rlbAttributeHiddenOptions.SelectedValues.AsDelimited( "," ) );
            SetAttributeValue( AttributeKey.AttributesInKeywords, cblAttributesInKeywords.Items.Cast<ListItem>().Where( i => i.Selected ).Select( i => i.Value ).ToList().AsDelimited( "," ) );
            SetAttributeValue( AttributeKey.FilterOrder, ToKVPString( FilterOrder ) );

            SetAttributeValue( AttributeKey.ShowMap, cbShowMap.Checked.ToString() );
            SetAttributeValue( AttributeKey.MapStyle, dvpMapStyle.SelectedValue );
            SetAttributeValue( AttributeKey.MapHeight, nbMapHeight.Text );
            SetAttributeValue( AttributeKey.ShowFence, cbShowFence.Checked.ToString() );
            SetAttributeValue( AttributeKey.PolygonColors, vlPolygonColors.Value );
            SetAttributeValue( AttributeKey.MapInfo, ceMapInfo.Text );

            SetAttributeValue( AttributeKey.ShowLavaOutput, cbShowLavaOutput.Checked.ToString() );
            SetAttributeValue( AttributeKey.LavaOutput, ceLavaOutput.Text );

            SetAttributeValue( AttributeKey.ShowGrid, cbShowGrid.Checked.ToString() );
            SetAttributeValue( AttributeKey.ShowSchedule, cbShowSchedule.Checked.ToString() );
            SetAttributeValue( AttributeKey.ShowDescription, cbShowDescription.Checked.ToString() );
            SetAttributeValue( AttributeKey.ShowCampus, cbShowCampus.Checked.ToString() );

            // Convert the select campus type defined value ids to defined value guids to make them compatible with the Defined Values field type
            var selectedCampusTypes = dvpCampusTypes.SelectedValuesAsInt
                  .Select( a => DefinedValueCache.Get( a ) )
                  .Where( a => a != null )
                  .Select( a => a.Guid )
                  .ToList()
                  .AsDelimited( "," );

            SetAttributeValue( AttributeKey.CampusTypes, selectedCampusTypes );

            var selectedCampusStatuses = dvpCampusStatuses.SelectedValuesAsInt
                  .Select( a => DefinedValueCache.Get( a ) )
                  .Where( a => a != null )
                  .Select( a => a.Guid )
                  .ToList()
                  .AsDelimited( "," );

            SetAttributeValue( AttributeKey.CampusStatuses, selectedCampusStatuses );

            SetAttributeValue( AttributeKey.ShowProximity, cbProximity.Checked.ToString() );
            SetAttributeValue( AttributeKey.SortByDistance, cbSortByDistance.Checked.ToString() );
            SetAttributeValue( AttributeKey.PageSizes, tbPageSizes.Text );
            SetAttributeValue( AttributeKey.ShowCount, cbShowCount.Checked.ToString() );
            SetAttributeValue( AttributeKey.ShowAge, cbShowAge.Checked.ToString() );
            SetAttributeValue( AttributeKey.AttributeColumns, cblGridAttributes.Items.Cast<ListItem>().Where( i => i.Selected ).Select( i => i.Value ).ToList().AsDelimited( "," ) );
            SetAttributeValue( AttributeKey.IncludePending, cbIncludePending.Checked.ToString() );

            var ppFieldType = new PageReferenceFieldType();
            SetAttributeValue( AttributeKey.GroupDetailPage, ppFieldType.GetEditValue( ppGroupDetailPage, null ) );
            SetAttributeValue( AttributeKey.RegisterPage, ppFieldType.GetEditValue( ppRegisterPage, null ) );

            SaveAttributeValues();

            mdEdit.Hide();
            pnlEditModal.Visible = false;
            upnlContent.Update();

            BindAttributes();
            BuildDynamicControls();
            ShowView();
        }

        /// <summary>
        /// Handles the Click event of the btnSearch control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void btnSearch_Click( object sender, EventArgs e )
        {
            pnlBtnFilterControls.Visible = phFilterControlsCollapsed.Controls.Count > 0;
            ShowResults();
        }

        /// <summary>
        /// Handles the Click event of the btnClear control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void btnClear_Click( object sender, EventArgs e )
        {
            filter_acAddress.SetValues( null );
            filter_tbPostalCode.Text = "";
            BuildDynamicControls();

            pnlSearch.CssClass = "";
            pnlBtnFilter.Visible = false;

            ShowResults();
        }

        /// <summary>
        /// Handles the RowSelected event of the gGroups control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RowEventArgs"/> instance containing the event data.</param>
        protected void gGroups_RowSelected( object sender, RowEventArgs e )
        {
            if ( !NavigateToLinkedPage( AttributeKey.GroupDetailPage, "GroupId", e.RowKeyId ) )
            {
                ShowResults();
                ScriptManager.RegisterStartupScript( pnlMap, pnlMap.GetType(), "group-finder-row-selected", "openInfoWindowById(" + e.RowKeyId + ");", true );
            }
        }

        /// <summary>
        /// Handles the Click event of the registerColumn control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RowEventArgs"/> instance containing the event data.</param>
        protected void registerColumn_Click( object sender, RowEventArgs e )
        {
            using ( var rockContext = new RockContext() )
            {
                var group = new GroupService( rockContext ).Get( e.RowKeyId );
                if ( group != null )
                {
                    _urlParms.AddOrReplace( "GroupGuid", group.Guid.ToString() );
                    if ( !NavigateToLinkedPage( AttributeKey.RegisterPage, _urlParms ) )
                    {
                        ShowResults();
                    }
                }
                else
                {
                    ShowResults();
                }
            }
        }

        /// <summary>
        /// Handles the GridRebind event of the gGroups control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void gGroups_GridRebind( object sender, EventArgs e )
        {
            ShowResults();
        }

        /// <summary>
        /// Handles the SelectItem event from an ItemPicker (fake event to register the postback)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ItemPicker_SelectItem( object sender, EventArgs e )
        {
            // Hopefully an xhr happens here
        }

        #endregion Events

        #region Internal Methods

        /// <summary>
        /// Shows the settings.
        /// </summary>
        protected override void ShowSettings()
        {
            pnlEditModal.Visible = true;
            upnlContent.Update();
            mdEdit.Show();

            BindGroupTypeLocationGrid();

            var rockContext = new RockContext();
            var groupTypes = new GroupTypeService( rockContext )
                .Queryable().AsNoTracking().OrderBy( t => t.Order ).ToList();

            BindGroupType( gtpGroupType, groupTypes );
            BindGroupType( gtpGeofenceGroupType, groupTypes, AttributeKey.GeofencedGroupType );
            tbCampusLabel.Text = GetAttributeValue( AttributeKey.CampusLabel );
            tbDayOfWeekLabel.Text = GetAttributeValue( AttributeKey.DayOfWeekLabel );
            tbTimeOfDayLabel.Text = GetAttributeValue( AttributeKey.TimeOfDayLabel );
            tbPostalCodeLabel.Text = GetAttributeValue( AttributeKey.PostalCodeLabel );
            tbKeywordLabel.Text = GetAttributeValue( AttributeKey.KeywordLabel );
            tbFilterLabel.Text = GetAttributeValue( AttributeKey.FilterLabel );
            tbMoreFiltersLabel.Text = GetAttributeValue( AttributeKey.MoreFiltersLabel );
            tbShowFullGroupsLabel.Text = GetAttributeValue( AttributeKey.ShowFullGroupsLabel );

            var scheduleFilters = GetAttributeValue( AttributeKey.ScheduleFilters ).SplitDelimitedValues( false ).ToList();
            if ( scheduleFilters.Contains( "Day" ) )
            {
                rblFilterDOW.SetValue( "Day" );
            }
            else if ( scheduleFilters.Contains( "Days" ) )
            {
                rblFilterDOW.SetValue( "Days" );
            }
            else
            {
                rblFilterDOW.SelectedIndex = 0;
            }

            cbFilterTimeOfDay.Checked = scheduleFilters.Contains( "Time" );

            SetGroupTypeOptions();
            SelectGroupTypeOptions();

            ddlMaxZoomLevel.SelectedValue = GetAttributeValue( AttributeKey.MaximumZoomLevel );
            ddlMinZoomLevel.SelectedValue = GetAttributeValue( AttributeKey.MinimumZoomLevel );
            ddlInitialZoomLevel.SelectedValue = GetAttributeValue( AttributeKey.InitialZoomLevel );
            ddlMarkerZoomLevel.SelectedValue = GetAttributeValue( AttributeKey.MarkerZoomLevel );
            nbMarkerAutoScaleAmount.Text = GetAttributeValue( AttributeKey.MarkerZoomAmount );
            ddlLocationPrecisionLevel.SelectedValue = GetAttributeValue( AttributeKey.LocationPrecisionLevel );
            ddlMapMarker.SetValue( GetAttributeValue( AttributeKey.MapMarker ) );
            cpMarkerColor.Text = GetAttributeValue( AttributeKey.MarkerColor );
            ddlHideOvercapacityGroups.SelectedValue = GetAttributeValue( AttributeKey.HideOvercapacityGroups );

            cbFilterCampus.Checked = GetAttributeValue( AttributeKey.DisplayCampusFilter ).AsBoolean();
            cbCampusContext.Checked = GetAttributeValue( AttributeKey.EnableCampusContext ).AsBoolean();
            cbPostalCode.Checked = GetAttributeValue( AttributeKey.EnablePostalCodeSearch ).AsBoolean();
            cbRequirePostalCode.Checked = GetAttributeValue( AttributeKey.RequirePostalCode ).AsBoolean();
            cbKeyword.Checked = GetAttributeValue( AttributeKey.DisplayKeywordSearch ).AsBoolean();

            cbShowMap.Checked = GetAttributeValue( AttributeKey.ShowMap ).AsBoolean();
            dvpMapStyle.DefinedTypeId = DefinedTypeCache.Get( Rock.SystemGuid.DefinedType.MAP_STYLES.AsGuid() ).Id;
            dvpMapStyle.SetValue( GetAttributeValue( AttributeKey.MapStyle ) );
            nbMapHeight.Text = GetAttributeValue( AttributeKey.MapHeight );
            cbShowFence.Checked = GetAttributeValue( AttributeKey.ShowFence ).AsBoolean();
            vlPolygonColors.Value = GetAttributeValue( AttributeKey.PolygonColors );
            ceMapInfo.Text = GetAttributeValue( AttributeKey.MapInfo );

            cbShowLavaOutput.Checked = GetAttributeValue( AttributeKey.ShowLavaOutput ).AsBoolean();
            ceLavaOutput.Text = GetAttributeValue( AttributeKey.LavaOutput );

            cbShowGrid.Checked = GetAttributeValue( AttributeKey.ShowGrid ).AsBoolean();
            cbShowSchedule.Checked = GetAttributeValue( AttributeKey.ShowSchedule ).AsBoolean();
            cbShowDescription.Checked = GetAttributeValue( AttributeKey.ShowDescription ).AsBoolean();
            cbShowCampus.Checked = GetAttributeValue( AttributeKey.ShowCampus ).AsBoolean();

            cbProximity.Checked = GetAttributeValue( AttributeKey.ShowProximity ).AsBoolean();
            cbSortByDistance.Checked = GetAttributeValue( AttributeKey.SortByDistance ).AsBoolean();
            tbPageSizes.Text = GetAttributeValue( AttributeKey.PageSizes );
            cbShowCount.Checked = GetAttributeValue( AttributeKey.ShowCount ).AsBoolean();
            cbShowAge.Checked = GetAttributeValue( AttributeKey.ShowAge ).AsBoolean();
            cbIncludePending.Checked = GetAttributeValue( AttributeKey.IncludePending ).AsBoolean();

            var ppFieldType = new PageReferenceFieldType();
            ppFieldType.SetEditValue( ppGroupDetailPage, null, GetAttributeValue( AttributeKey.GroupDetailPage ) );
            ppFieldType.SetEditValue( ppRegisterPage, null, GetAttributeValue( AttributeKey.RegisterPage ) );

            upnlContent.Update();
        }

        /// <summary>
        /// Binds the group attribute list.
        /// </summary>
        private void SetGroupTypeOptions()
        {
            rblFilterDOW.Visible = false;
            cbFilterTimeOfDay.Visible = false;

            // Rebuild the checkbox list settings for both the filter and display in grid attribute lists
            cblAttributes.Items.Clear();
            cblGridAttributes.Items.Clear();
            cblInitialLoadFilters.Items.Clear();
            ddlAttributeSort.Items.Clear();
            rlbAttributeHiddenOptions.Items.Clear();
            cblAttributesInKeywords.Items.Clear();

            FilterOrder = new Dictionary<int, string>();

            if ( gtpGroupType.SelectedValuesAsInt != null )
            {
                var groupTypes = gtpGroupType.SelectedValuesAsInt.Select( id => GroupTypeCache.Get( id ) );

                _groupTypeAttributeNames.AddOrIgnore( "filter_dow", "Day of Week" );
                _groupTypeAttributeNames.AddOrIgnore( "filter_time", "Time of Day" );
                _groupTypeAttributeNames.AddOrIgnore( "filter_campus", "Campus" );
                _groupTypeAttributeNames.AddOrIgnore( "filter_address", "Address" );
                _groupTypeAttributeNames.AddOrIgnore( "filter_postalcode", "Postal Code" );
                _groupTypeAttributeNames.AddOrIgnore( "filter_keyword", "Keyword" );
                _groupTypeAttributeNames.AddOrIgnore( "filter_showfullgroups", "Show Full Groups" );

                var filterOrderIndex = 1;
                foreach ( var filter in _groupTypeAttributeNames.Where( f => f.Key.Contains( "filter_" ) ) )
                {
                    AddToFilterOrder( filterOrderIndex, filter.Key );
                    cblInitialLoadFilters.Items.Add( new ListItem( filter.Value, filter.Key ) );
                    filterOrderIndex++;
                }

                cblInitialLoadFilters.Items.Add( new ListItem( "Search Button", "btnSearch" ) );
                cblInitialLoadFilters.Items.Add( new ListItem( "Clear Button", "btnClear" ) );

                foreach ( var groupType in groupTypes )
                {
                    if ( groupType != null )
                    {
                        bool hasWeeklyschedule = ( groupType.AllowedScheduleTypes & ScheduleType.Weekly ) == ScheduleType.Weekly || ( groupType.AllowedScheduleTypes & ScheduleType.Custom ) == ScheduleType.Custom;
                        rblFilterDOW.Visible = hasWeeklyschedule;
                        cbFilterTimeOfDay.Visible = hasWeeklyschedule;

                        var group = new Group();
                        group.GroupTypeId = groupType.Id;
                        group.LoadAttributes();
                        ddlAttributeSort.Items.Add( new ListItem() );
                        foreach ( var attribute in group.Attributes )
                        {
                            if ( attribute.Value.FieldType.Field.HasFilterControl() )
                            {
                                cblAttributes.Items.Add( new ListItem( attribute.Value.Name + string.Format( " ({0})", groupType.Name ), attribute.Value.Guid.ToString() ) );
                                cblInitialLoadFilters.Items.Add( new ListItem( attribute.Value.Name + string.Format( " ({0})", groupType.Name ), attribute.Value.Guid.ToString() ) );
                                ddlAttributeSort.Items.Add( new ListItem( attribute.Value.Name + string.Format( " ({0})", groupType.Name ), attribute.Value.Guid.ToString() ) );

                                AddToFilterOrder( filterOrderIndex, attribute.Value.Guid.ToString() );
                                _groupTypeAttributeNames.AddOrIgnore( attribute.Value.Guid.ToString(), attribute.Value.Name + string.Format( " ({0})", groupType.Name ) );
                                filterOrderIndex++;

                                var configurationValues = attribute.Value.QualifierValues;
                                bool useDescription = configurationValues != null && configurationValues.ContainsKey( DISPLAY_DESCRIPTION ) && configurationValues[DISPLAY_DESCRIPTION].Value.AsBoolean();
                                int? definedTypeId = configurationValues != null && configurationValues.ContainsKey( DEFINED_TYPE_KEY ) ? configurationValues[DEFINED_TYPE_KEY].Value.AsIntegerOrNull() : null;

                                if ( definedTypeId.HasValue )
                                {
                                    var definedType = DefinedTypeCache.Get( definedTypeId.Value );
                                    foreach ( var val in definedType.DefinedValues )
                                    {
                                        rlbAttributeHiddenOptions.Items.Add( new ListItem( string.Format( "{0} [{1} ({2})]", ( useDescription ) ? val.Description : val.Value, attribute.Value.Name, groupType.Name ), string.Format( "filter_{0}_{1}^{2}", attribute.Value.Key.ToString(), attribute.Value.FieldTypeId, val.Id.ToString() ) ) );
                                    }
                                }
                                else
                                {
                                    foreach ( var keyVal in Rock.Field.Helper.GetConfiguredValues( configurationValues ) )
                                    {
                                        rlbAttributeHiddenOptions.Items.Add( new ListItem( string.Format( "{0} [{1} ({2})]", keyVal.Value, attribute.Value.Name, groupType.Name ), string.Format( "filter_{0}_{1}^{2}", attribute.Value.Key.ToString(), attribute.Value.FieldTypeId, keyVal.Key ) ) );
                                    }
                                }
                            }

                            cblAttributesInKeywords.Items.Add( new ListItem( attribute.Value.Name + string.Format( " ({0})", groupType.Name ), attribute.Value.Guid.ToString() ) );

                            cblGridAttributes.Items.Add( new ListItem( attribute.Value.Name + string.Format( " ({0})", groupType.Name ), attribute.Value.Guid.ToString() ) );
                        }
                    }
                }
            }

            // Remove location data for any group type that has it but isn't selected.
            if ( GroupTypeLocations != null )
            {
                var groupTypeIdWithLocations = GroupTypeLocations.Keys.ToList();
                foreach ( var groupTypeId in groupTypeIdWithLocations )
                {
                    if ( !gtpGroupType.SelectedValuesAsInt.Contains( groupTypeId ) )
                    {
                        GroupTypeLocations.Remove( groupTypeId );
                    }
                }
            }

            cblAttributes.Visible = cblAttributes.Items.Count > 0;
            cblGridAttributes.Visible = cblAttributes.Items.Count > 0;
            rlbAttributeHiddenOptions.Visible = rlbAttributeHiddenOptions.Items.Count > 0;
            ddlAttributeSort.Visible = ddlAttributeSort.Items.Count > 0;
            cblAttributesInKeywords.Visible = cblAttributesInKeywords.Items.Count > 0;

            BindGroupTypeLocationGrid();
            BindFilterOrder();
        }

        private void SelectGroupTypeOptions()
        {
            foreach ( string attr in GetAttributeValue( AttributeKey.AttributeFilters ).SplitDelimitedValues() )
            {
                var li = cblAttributes.Items.FindByValue( attr );
                if ( li != null )
                {
                    li.Selected = true;
                }
            }
            foreach ( string attr in GetAttributeValue( AttributeKey.HideFiltersInitialLoad ).SplitDelimitedValues() )
            {
                var li = cblInitialLoadFilters.Items.FindByValue( attr );
                if ( li != null )
                {
                    li.Selected = true;
                }
            }
            foreach ( string attr in GetAttributeValue( AttributeKey.AttributeCustomSort ).SplitDelimitedValues() )
            {
                var li = ddlAttributeSort.Items.FindByValue( attr );
                if ( li != null )
                {
                    li.Selected = true;
                }
            }
            foreach ( string attr in GetAttributeValue( AttributeKey.HideAttributeValues ).Split( new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries ) )
            {
                var li = rlbAttributeHiddenOptions.Items.FindByValue( attr.Replace( "||", "^" ) );
                if ( li != null )
                {
                    li.Selected = true;
                }
            }
            foreach ( string attr in GetAttributeValue( AttributeKey.AttributesInKeywords ).SplitDelimitedValues() )
            {
                var li = cblAttributesInKeywords.Items.FindByValue( attr );
                if ( li != null )
                {
                    li.Selected = true;
                }
            }

            // Convert the defined value guids to ints as required by the control
            var selectedCampusTypes = GetAttributeValue( AttributeKey.CampusTypes )
                        .Split( ',' )
                        .AsGuidList()
                        .Select( a => DefinedValueCache.Get( a ) )
                        .Select( a => a.Id )
                        .ToList();

            dvpCampusTypes.SetValues( selectedCampusTypes );

            var selectedCampusStatuses = GetAttributeValue( AttributeKey.CampusStatuses )
                        .Split( ',' )
                        .AsGuidList()
                        .Select( a => DefinedValueCache.Get( a ) )
                        .Select( a => a.Id )
                        .ToList();

            dvpCampusStatuses.SetValues( selectedCampusStatuses );

            foreach ( string attr in GetAttributeValue( AttributeKey.AttributeColumns ).SplitDelimitedValues() )
            {
                var li = cblGridAttributes.Items.FindByValue( attr );
                if ( li != null )
                {
                    li.Selected = true;
                }
            }
        }

        private void AddToFilterOrder( int defaultkey, string filterId )
        {
            var filterAddKey = _filterOrder.FirstOrDefault( f => f.Value == filterId ).Key;
            var checkKey = ( filterAddKey != 0 ) ? filterAddKey : defaultkey;
            var maxKey1 = _filterOrder.OrderByDescending( f => f.Key ).FirstOrDefault().Key;
            var maxKey2 = FilterOrder.OrderByDescending( f => f.Key ).FirstOrDefault().Key;
            if ( !FilterOrder.ContainsKey( checkKey ) )
            {
                FilterOrder.Add( checkKey, filterId );
            }
            else
            {
                FilterOrder.AddOrIgnore( ( ( maxKey2 > maxKey1 ) ? maxKey2 : maxKey1 ) + 1, filterId );
            }
        }

        private void ShowViewForPerson( Guid targetPersonGuid )
        {
            // check for a specific person in the query string
            Person targetPerson = null;
            Location targetPersonLocation = null;

            targetPerson = new PersonService( new RockContext() ).Queryable().Where( p => p.Guid == targetPersonGuid ).FirstOrDefault();
            targetPersonLocation = targetPerson.GetHomeLocation();

            if ( targetPerson != null )
            {
                lTitle.Text = string.Format( "<h4 class='margin-t-none'>Groups for {0}</h4>", targetPerson.FullName );
                filter_acAddress.SetValues( targetPersonLocation );
                filter_acAddress.Visible = false;
                phFilterControls.Visible = _allowSearch;
                if ( _ssFilters )
                {
                    filter_ddlCampus.Visible = _allowSearch;
                }
                else
                {
                    filter_cblCampus.Visible = _allowSearch;
                }
                btnSearch.Visible = _allowSearch;
                btnClear.Visible = _allowSearch;

                if ( targetPersonLocation != null && targetPersonLocation.GeoPoint != null )
                {
                    lTitle.Text += string.Format( "<p>Search based on: {0}</p>", targetPersonLocation.ToString() );

                    ShowResults();
                }
                else if ( targetPersonLocation != null )
                {
                    lTitle.Text += string.Format( "<p>The position of the address on file ({0}) could not be determined.</p>", targetPersonLocation.ToString() );
                }
                else
                {
                    lTitle.Text += string.Format( "<p>The person does not have an address on file.</p>" );
                }
            }
        }

        /// <summary>
        /// Shows the view.
        /// </summary>
        private void ShowView()
        {
            // If the groups should be limited by geofence, or the distance should be displayed,
            // then we need to capture the person's address
            Guid? fenceTypeGuid = GetAttributeValue( AttributeKey.GeofencedGroupType ).AsGuidOrNull();
            if ( fenceTypeGuid.HasValue || GetAttributeValue( AttributeKey.ShowProximity ).AsBoolean() )
            {
                var enablePostalCode = GetAttributeValue( AttributeKey.EnablePostalCodeSearch ).AsBoolean();
                filter_acAddress.Visible = !enablePostalCode;
                filter_tbPostalCode.Visible = enablePostalCode;
                revPostalCode.Enabled = enablePostalCode;

                if ( CurrentPerson != null )
                {
                    var currentPersonLocation = CurrentPerson.GetHomeLocation();
                    filter_acAddress.SetValues( currentPersonLocation );
                    if ( currentPersonLocation != null )
                    {
                        filter_tbPostalCode.Text = currentPersonLocation.PostalCode;
                    }
                }

                phFilterControls.Visible = true;
                btnSearch.Visible = true;
            }
            else
            {
                filter_acAddress.Visible = false;
                filter_tbPostalCode.Visible = false;
                revPostalCode.Visible = false;

                // Check to see if there's any filters
                string scheduleFilters = GetAttributeValue( AttributeKey.ScheduleFilters );
                if ( !string.IsNullOrWhiteSpace( scheduleFilters ) || AttributeFilters.Any() )
                {
                    phFilterControls.Visible = true;
                    btnSearch.Visible = true;
                }
                else
                {
                    // Hide the search button and show the results immediately since there is
                    // no filter criteria to be entered
                    phFilterControls.Visible = false;
                    btnSearch.Visible = GetAttributeValue( AttributeKey.DisplayCampusFilter ).AsBoolean();
                    pnlResults.Visible = true;
                }
            }

            if ( !pnlResults.Visible && GetAttributeValue( AttributeKey.LoadInitialResults ).AsBoolean() )
            {
                pnlResults.Visible = true;
            }

            btnClear.Visible = btnSearch.Visible;

            // If we've already displayed results, then re-display them
            if ( pnlResults.Visible || _autoLoad )
            {
                ShowResults();
            }
        }

        /// <summary>
        /// Adds the attribute filters.
        /// </summary>
        private void BindAttributes()
        {
            // Parse the attribute filters
            AttributeFilters = new List<AttributeCache>();
            foreach ( string attr in GetAttributeValue( AttributeKey.AttributeFilters ).SplitDelimitedValues() )
            {
                Guid? attributeGuid = attr.AsGuidOrNull();
                if ( attributeGuid.HasValue )
                {
                    var attribute = AttributeCache.Get( attributeGuid.Value );
                    if ( attribute != null && attribute.FieldType.Field.HasFilterControl() )
                    {
                        AttributeFilters.Add( attribute );
                    }
                }
            }

            // Parse the attribute filters
            AttributeColumns = new List<AttributeCache>();
            foreach ( string attr in GetAttributeValue( AttributeKey.AttributeColumns ).SplitDelimitedValues() )
            {
                Guid? attributeGuid = attr.AsGuidOrNull();
                if ( attributeGuid.HasValue )
                {
                    var attribute = AttributeCache.Get( attributeGuid.Value );
                    if ( attribute != null )
                    {
                        AttributeColumns.Add( attribute );
                    }
                }
            }
        }

        /// <summary>
        /// Builds the dynamic controls.
        /// </summary>
        private void BuildDynamicControls( bool clearHideFilters = false, bool clearControls = true )
        {
            _hideFilters = GetAttributeValues( AttributeKey.HideFiltersInitialLoad );
            _filterOrder = loadFilterOrder();

            if ( clearControls )
            {
                _filterControls = new Dictionary<decimal, Control>();
                _collapsedControls = new Dictionary<decimal, Control>();
                // Clear attribute filter controls and recreate
                phFilterControls.Controls.Clear();
                phFilterControlsCollapsed.Controls.Clear();
            }

            if ( filter_acAddress == null )
            {
                filter_acAddress = new AddressControl();
                filter_acAddress.ID = "filter_acAddress";
                filter_acAddress.Required = true;
                filter_acAddress.RequiredErrorMessage = "Your Address is Required";
            }
            AddFilterControl( filter_acAddress, "", "", "filter_address" );

            if ( _autoPostback )
            {
                if ( filter_acAddress.Visible )
                {
                    foreach ( var ctrl in filter_acAddress.ControlsOfTypeRecursive<RockTextBox>() )
                    {
                        ctrl.AutoPostBack = true;
                    }
                }
            }

            if ( filter_tbPostalCode == null )
            {
                filter_tbPostalCode = new RockTextBox();
                filter_tbPostalCode.ID = "filter_tbPostalCode";
                filter_tbPostalCode.Required = GetAttributeValue( AttributeKey.RequirePostalCode ).AsBoolean();
                filter_tbPostalCode.RequiredErrorMessage = string.Format( "Your {0} is Required", GetAttributeValue( AttributeKey.PostalCodeLabel ) );
                filter_tbPostalCode.CssClass = "form-control js-postal-code js-postcode js-address-field";
                filter_tbPostalCode.Label = GetAttributeValue( AttributeKey.PostalCodeLabel );
                filter_tbPostalCode.AutoPostBack = _autoPostback;

                revPostalCode = new RegularExpressionValidator();
                revPostalCode.ID = "revPostalCode";
                revPostalCode.ControlToValidate = "filter_tbPostalCode";
                revPostalCode.ValidationExpression = "[0-9]*\\-*[0-9]*";
                revPostalCode.Text = "-";
                revPostalCode.CssClass = "hidden hide";
                revPostalCode.ErrorMessage = string.Format( "Your {0} is an invalid format, 12345 or 12345-6789 only.", GetAttributeValue( AttributeKey.PostalCodeLabel ) );
            }
            AddFilterControl( filter_tbPostalCode, GetAttributeValue( AttributeKey.PostalCodeLabel ), "", "filter_postalcode" );
            AddFilterControl( revPostalCode, "", "", "filter_postalcode" );

            var scheduleFilters = GetAttributeValue( AttributeKey.ScheduleFilters ).SplitDelimitedValues().ToList();
            if ( scheduleFilters.Contains( "Days" ) )
            {
                var dowsFilterControl = new RockCheckBoxList();
                dowsFilterControl.ID = "filter_dows";
                dowsFilterControl.Label = GetAttributeValue( AttributeKey.DayOfWeekLabel );
                dowsFilterControl.BindToEnum<DayOfWeek>();
                dowsFilterControl.RepeatDirection = RepeatDirection.Horizontal;
                dowsFilterControl.AutoPostBack = _autoPostback;
                if ( _autoPostback )
                {
                    dowsFilterControl.SelectedIndexChanged += btnSearch_Click;
                }

                AddFilterControl( dowsFilterControl, "Days of Week", "", "filter_dow" );
            }

            if ( scheduleFilters.Contains( "Day" ) )
            {
                var control = FieldTypeCache.Get( Rock.SystemGuid.FieldType.DAY_OF_WEEK ).Field.FilterControl( null, "filter_dow", false, Rock.Reporting.FilterMode.SimpleFilter );
                string dayOfWeekLabel = GetAttributeValue( AttributeKey.DayOfWeekLabel );
                if ( _autoPostback )
                {
                    AddAutoPostback( control );
                }
                AddFilterControl( control, dayOfWeekLabel, "", "filter_dow" );
            }

            if ( scheduleFilters.Contains( "Time" ) )
            {
                var control = FieldTypeCache.Get( Rock.SystemGuid.FieldType.TIME ).Field.FilterControl( null, "filter_time", false, Rock.Reporting.FilterMode.SimpleFilter );
                string timeOfDayLabel = GetAttributeValue( AttributeKey.TimeOfDayLabel );
                if ( _autoPostback )
                {
                    AddAutoPostback( control );
                }
                AddFilterControl( control, timeOfDayLabel, "", "filter_time" );
            }

            if ( GetAttributeValue( AttributeKey.DisplayCampusFilter ).AsBoolean() )
            {
                var filterCampusTypeIds = GetAttributeValue( AttributeKey.CampusTypes )
                  .SplitDelimitedValues( true )
                  .AsGuidList()
                  .Select( a => DefinedValueCache.Get( a ) )
                  .Where( a => a != null )
                  .Select( a => a.Id )
                  .ToList();

                var filterCampusStatusIds = GetAttributeValue( AttributeKey.CampusStatuses )
                    .SplitDelimitedValues( true )
                    .AsGuidList()
                    .Select( a => DefinedValueCache.Get( a ) )
                    .Where( a => a != null )
                    .Select( a => a.Id )
                    .ToList();

                var campuses = CampusCache.All( includeInactive: false );

                if ( filterCampusTypeIds.Any() )
                {
                    campuses = campuses.Where( c => c.CampusTypeValueId != null && filterCampusTypeIds.Contains( c.CampusTypeValueId.Value ) ).ToList();
                }

                if ( filterCampusStatusIds.Any() )
                {
                    campuses = campuses.Where( c => c.CampusStatusValueId != null && filterCampusStatusIds.Contains( c.CampusStatusValueId.Value ) ).ToList();
                }

                if ( _ssFilters )
                {
                    filter_ddlCampus = new RockDropDownList();
                    filter_ddlCampus.ID = "filter_ddlCampus";
                    filter_ddlCampus.DataTextField = "Name";
                    filter_ddlCampus.DataValueField = "Id";
                    filter_ddlCampus.AutoPostBack = _autoPostback;
                    filter_ddlCampus.Items.Add( new ListItem( string.Empty, string.Empty ) );
                    foreach ( var campus in campuses.OrderBy( c => c.Order ) )
                    {
                        ListItem li = new ListItem( campus.Name, campus.Id.ToString() );
                        filter_ddlCampus.Items.Add( li );
                    }

                    AddFilterControl( filter_ddlCampus, GetAttributeValue( AttributeKey.CampusLabel ), "", "filter_campus" );
                }
                else
                {
                    filter_cblCampus = new RockCheckBoxList();
                    filter_cblCampus.ID = "filter_cblCampus";
                    filter_cblCampus.DataTextField = "Name";
                    filter_cblCampus.DataValueField = "Id";
                    filter_cblCampus.RepeatDirection = RepeatDirection.Horizontal;
                    filter_cblCampus.AutoPostBack = _autoPostback;
                    filter_cblCampus.DataSource = campuses.OrderBy( c => c.Order );
                    filter_cblCampus.DataBind();
                    AddFilterControl( filter_cblCampus, GetAttributeValue( AttributeKey.CampusLabel ), "", "filter_campus" );
                }
            }

            if ( GetAttributeValue( AttributeKey.HideOvercapacityGroups ) == "Filter" )
            {
                var filter_tglShowFullGroups = new Toggle();
                filter_tglShowFullGroups.ID = "filter_tglShowFullGroups";
                if ( _autoPostback )
                {
                    filter_tglShowFullGroups.CheckedChanged -= btnSearch_Click;
                    filter_tglShowFullGroups.CheckedChanged += btnSearch_Click;
                }
                AddFilterControl( filter_tglShowFullGroups, GetAttributeValue( AttributeKey.ShowFullGroupsLabel ), "", "filter_showfullgroups" );
            }

            btnFilter.InnerHtml = btnFilter.InnerHtml.Replace( "[Filter] ", GetAttributeValue( AttributeKey.FilterLabel ) + " " );
            btnFilterControls.InnerHtml = btnFilterControls.InnerHtml.Replace( "[More Filters] ", GetAttributeValue( AttributeKey.MoreFiltersLabel ) + " " );

            if ( AttributeFilters != null )
            {
                hideAttributeValues();
                var existingFilters = new HashSet<string>();
                var useAbbreviatedName = GetAttributeValue( AttributeKey.UseAbbreviatedAttributeName ).AsBoolean();
                foreach ( var attribute in AttributeFilters )
                {
                    var filterId = $"filter_{attribute.Key}_{attribute.FieldType.Id}";
                    if ( existingFilters.Contains( filterId ) )
                    {
                        continue;
                    }
                    existingFilters.Add( filterId );
                    var control = attribute.FieldType.Field.FilterControl( attribute.QualifierValues, filterId, false, Rock.Reporting.FilterMode.SimpleFilter );
                    if ( control != null )
                    {
                        if ( _autoPostback )
                        {
                            AddAutoPostback( control );
                        }
                        foreach ( var ctrl in control.ControlsOfTypeRecursive<DefinedValuesPickerEnhanced>() )
                        {
                            ctrl.Attributes.Add( "data-placeholder", $"Select {attribute.Name}" );
                        }
                        foreach ( var ctrl in control.ControlsOfTypeRecursive<RockListBox>() )
                        {
                            ctrl.Attributes.Add( "data-placeholder", $"Select {attribute.Name}" );
                        }

                        AddFilterControl( control, ( useAbbreviatedName ) ? attribute.AbbreviatedName : attribute.Name, attribute.Description, attribute.Guid.ToString() );
                    }
                }
            }

            if ( GetAttributeValue( AttributeKey.DisplayKeywordSearch ).AsBoolean() )
            {
                var tbKeyword = new RockTextBox();
                tbKeyword.Label = GetAttributeValue( AttributeKey.KeywordLabel );
                tbKeyword.ID = "filter_keyword";
                tbKeyword.AutoPostBack = _autoPostback;
                AddFilterControl( tbKeyword, GetAttributeValue( AttributeKey.KeywordLabel ), "", tbKeyword.ID );
            }

            foreach ( var ctrl in _filterControls.OrderBy( c => c.Key ) )
            {
                phFilterControls.Controls.Add( ctrl.Value );
            }

            foreach ( var ctrl in _collapsedControls.OrderBy( c => c.Key ) )
            {
                if ( clearHideFilters && _collapseFilters != "InitialLoad" )
                {
                    phFilterControls.Controls.Add( ctrl.Value );
                }
                else
                {
                    phFilterControlsCollapsed.Controls.Add( ctrl.Value );
                }
            }

            btnFilterControls.Attributes["data-target"] = string.Format( "#{0}", pnlHiddenFilterControls.ClientID );
            btnFilterControls.Attributes["aria-controls"] = pnlHiddenFilterControls.ClientID;
            pnlBtnFilterControls.Visible = phFilterControlsCollapsed.Controls.Count > 0;

            if ( _hideFilters.Contains( "btnSearch" ) )
            {
                pnlSearch.Controls.Remove( btnSearch );
                phFilterControlsCollapsed.Controls.Add( btnSearch );
            }
            if ( _hideFilters.Contains( "btnClear" ) )
            {
                pnlSearch.Controls.Remove( btnClear );
                phFilterControlsCollapsed.Controls.Add( btnClear );
            }

            // Build attribute columns
            foreach ( var column in gGroups.Columns.OfType<AttributeField>().ToList() )
            {
                gGroups.Columns.Remove( column );
            }

            if ( AttributeColumns != null )
            {
                foreach ( var attribute in AttributeColumns )
                {
                    string dataFieldExpression = attribute.Key;
                    bool columnExists = gGroups.Columns.OfType<AttributeField>().FirstOrDefault( a => a.DataField.Equals( dataFieldExpression ) ) != null;
                    if ( !columnExists )
                    {
                        AttributeField boundField = new AttributeField();
                        boundField.DataField = dataFieldExpression;
                        boundField.AttributeId = attribute.Id;
                        boundField.HeaderText = attribute.Name;

                        var attributeCache = AttributeCache.Get( attribute.Id );
                        if ( attributeCache != null )
                        {
                            boundField.ItemStyle.HorizontalAlign = attributeCache.FieldType.Field.AlignValue;
                        }

                        gGroups.Columns.Add( boundField );
                    }
                }
            }

            // Add Register Column
            foreach ( var column in gGroups.Columns.OfType<EditField>().ToList() )
            {
                gGroups.Columns.Remove( column );
            }

            var registerPage = new PageReference( GetAttributeValue( AttributeKey.RegisterPage ) );

            if ( _targetPersonGuid != Guid.Empty )
            {
                registerPage.Parameters = _urlParms;
            }

            if ( registerPage.PageId > 0 )
            {
                var registerColumn = new EditField();
                registerColumn.ToolTip = "Register";
                registerColumn.HeaderText = "Register";
                registerColumn.Click += registerColumn_Click;
                gGroups.Columns.Add( registerColumn );
            }

            var pageSizes = new List<int>();
            if ( !string.IsNullOrWhiteSpace( GetAttributeValue( AttributeKey.PageSizes ) ) )
            {
                pageSizes = GetAttributeValue( AttributeKey.PageSizes ).Split( ',' ).AsIntegerList();
            }

            ddlPageSize.Items.Clear();
            ddlPageSize.Items.AddRange( pageSizes.Select( a => new ListItem( a.ToString(), a.ToString() ) ).ToArray() );
            ddlPageSize.Items.Add( new ListItem( "All", "0" ) );

            if ( pageSizes.Any() )
            {
                // set default PageSize to whatever is first in the PageSize setting
                ddlPageSize.Visible = true;
                ddlPageSize.SelectedValue = pageSizes[0].ToString();
            }
            else
            {
                ddlPageSize.Visible = false;
            }

            // if the SortByDistance is enabled, prevent them from sorting by ColumnClick
            if ( GetAttributeValue( AttributeKey.SortByDistance ).AsBoolean() )
            {
                gGroups.AllowSorting = false;
            }
        }

        private Dictionary<int, string> loadFilterOrder()
        {
            var filterOrder = new Dictionary<int, string>();
            var filterOrderKVP = GetAttributeValue( AttributeKey.FilterOrder ).ToKeyValuePairList();
            var defaultOrder = 0;
            foreach ( var kvp in filterOrderKVP )
            {
                filterOrder.AddOrIgnore( kvp.Key.ToIntSafe( defaultOrder ), kvp.Value.ToString() );
                defaultOrder++;
            }
            return filterOrder;
        }

        private void AddAutoPostback( Control control )
        {
            if ( control is ListControl )
            {
                var listControl = control as ListControl;
                listControl.AutoPostBack = true;
            }

            // Enable ItemPicker postback event
            if ( control is ItemPicker )
            {
                var itemPicker = control as ItemPicker;
                itemPicker.SelectItem += ItemPicker_SelectItem;
            }

            if ( control is RockDropDownList )
            {
                var ddl = control as RockDropDownList;
                ddl.AutoPostBack = true;
            }

            if ( control is RockCheckBoxList )
            {
                var cbl = control as RockCheckBoxList;
                cbl.AutoPostBack = true;
            }

            foreach ( var ctrl in control.ControlsOfTypeRecursive<TextBox>() )
            {
                ctrl.AutoPostBack = true;
            }
            foreach ( var ctrl in control.ControlsOfTypeRecursive<RockDropDownList>() )
            {
                ctrl.AutoPostBack = true;
            }
            foreach ( var ctrl in control.ControlsOfTypeRecursive<RockCheckBox>() )
            {
                ctrl.AutoPostBack = true;
            }
            foreach ( var ctrl in control.ControlsOfTypeRecursive<RockCheckBoxList>() )
            {
                ctrl.AutoPostBack = true;
            }
            foreach ( var ctrl in control.ControlsOfTypeRecursive<DefinedValuesPicker>() )
            {
                ctrl.AutoPostBack = true;
            }
            foreach ( var ctrl in control.ControlsOfTypeRecursive<DefinedValuesPickerEnhanced>() )
            {
                ctrl.AutoPostBack = true;
                ctrl.SelectedIndexChanged -= btnSearch_Click;
                ctrl.SelectedIndexChanged += btnSearch_Click;
            }
            foreach ( var ctrl in control.ControlsOfTypeRecursive<RockListBox>() )
            {
                ctrl.AutoPostBack = true;
                ctrl.SelectedIndexChanged -= btnSearch_Click;
                ctrl.SelectedIndexChanged += btnSearch_Click;
            }
        }

        private void hideAttributeValues()
        {
            var hiddenAttributeValues = GetAttributeValue( AttributeKey.HideAttributeValues );

            if ( hiddenAttributeValues.HasValue() )
            {
                var hiddenValues = new Dictionary<string, string>();
                var splitValues = hiddenAttributeValues.Split( new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries );
                foreach ( var val in splitValues )
                {
                    var dictSplit = val.Split( new string[] { "^", "||" }, StringSplitOptions.RemoveEmptyEntries );
                    if ( dictSplit.Count() > 1 )
                    {
                        if ( hiddenValues.ContainsKey( dictSplit[0] ) )
                        {
                            hiddenValues[dictSplit[0]] = string.Format( "{0},{1}", hiddenValues[dictSplit[0]], dictSplit[1] );
                        }
                        else
                        {
                            hiddenValues.Add( dictSplit[0], dictSplit[1] );
                        }
                    }
                }
                if ( hiddenValues.Count > 0 )
                {
                    var cssStyle = new StringBuilder();
                    foreach ( var hideFilter in hiddenValues )
                    {
                        var valSplit = hideFilter.Value.SplitDelimitedValues( false );
                        foreach ( var hideVal in valSplit )
                        {
                            cssStyle.AppendFormat( "[id*=\"{0}\"][value=\"{1}\"], [id*=\"{0}\"][value=\"{1}\"] + span, ", hideFilter.Key, hideVal.EscapeQuotes() );
                        }
                    }
                    cssStyle.Append( " .hideSpecificValue { visibility: hidden !important; display: none !important; }" );
                    cssStyle.AppendLine( ".field-criteria .in-columns, .field-criteria .checkbox-inline { margin-top: 0px; }" );
                    cssStyle.AppendLine( ".radio input[type=\"radio\"], .radio-inline input[type=\"radio\"], .checkbox input[type=\"checkbox\"], .checkbox-inline input[type=\"checkbox\"] { top: 50%; transform: translateY(-50%); }" );
                    cssStyle.AppendLine( ".in-columns .label-text { margin-top: 8px }" );

                    Page.Header.Controls.Add( new LiteralControl( string.Format( "<style>{0}</style>", cssStyle ) ) );
                }
            }
        }

        private void AddFilterControl( Control control, string name, string description, string filterShortCode )
        {
            var collapsible = _hideFilters.Contains( filterShortCode );
            var controlPosition = _filterOrder.FirstOrDefault( f => f.Value.Equals( filterShortCode ) );
            decimal controlKey = controlPosition.Key;
            if ( _filterControls.ContainsKey( controlKey ) || _collapsedControls.ContainsKey( controlKey ) )
            {
                controlKey = $"{controlKey}.{controlKey}".AsDecimal();
            }
            if ( control is IRockControl )
            {
                var rockControl = ( IRockControl ) control;
                rockControl.Label = name;
                rockControl.Help = description;
                if ( collapsible )
                {
                    //phFilterControlsCollapsed.Controls.Add( control );
                    _collapsedControls.AddOrIgnore( ( controlPosition.Value.IsNullOrWhiteSpace() ) ? _collapsedControls.Count : controlKey, control );
                }
                else
                {
                    //phFilterControls.Controls.Add( control );
                    _filterControls.AddOrIgnore( ( controlPosition.Value.IsNullOrWhiteSpace() ) ? _filterControls.Count : controlKey, control );
                }
            }
            else
            {
                var wrapper = new RockControlWrapper();
                wrapper.ID = control.ID + "_wrapper";
                wrapper.Label = name;
                wrapper.Controls.Add( control );
                if ( collapsible )
                {
                    //phFilterControlsCollapsed.Controls.Add( wrapper );
                    _collapsedControls.AddOrIgnore( ( controlPosition.Value.IsNullOrWhiteSpace() ) ? _collapsedControls.Count : controlKey, wrapper );
                }
                else
                {
                    //phFilterControls.Controls.Add( wrapper );
                    _filterControls.AddOrIgnore( ( controlPosition.Value.IsNullOrWhiteSpace() ) ? _filterControls.Count : controlKey, wrapper );
                }
            }
        }

        /// <summary>
        /// Binds the grid.
        /// </summary>
        private void ShowResults()
        {
            // Get the group types that we're interested in
            var groupTypeIds = GetGroupTypeIds();
            if ( groupTypeIds == null )
            {
                ShowError( "A valid Group Type is required." );
                return;
            }

            gGroups.Columns[1].Visible = GetAttributeValue( AttributeKey.ShowDescription ).AsBoolean();
            gGroups.Columns[2].Visible = GetAttributeValue( AttributeKey.ShowSchedule ).AsBoolean();
            gGroups.Columns[3].Visible = GetAttributeValue( AttributeKey.ShowCount ).AsBoolean();
            gGroups.Columns[4].Visible = GetAttributeValue( AttributeKey.ShowAge ).AsBoolean();

            var includePending = GetAttributeValue( AttributeKey.IncludePending ).AsBoolean();
            bool showProximity = GetAttributeValue( AttributeKey.ShowProximity ).AsBoolean();
            gGroups.Columns[6].Visible = showProximity;  // Distance

            if ( _collapseFilters == "True" )
            {
                pnlSearch.CssClass = "collapse";
                pnlBtnFilter.Visible = true;
                btnFilter.Attributes["data-target"] = string.Format( "#{0}", pnlSearch.ClientID );
                btnFilter.Attributes["aria-controls"] = pnlSearch.ClientID;
            }

            // Get query of groups of the selected group type
            var rockContext = new RockContext();
            var groupService = new GroupService( rockContext );
            var showAllGroups = GetAttributeValue( AttributeKey.ShowAllGroups ).AsBoolean();
            var addGroupOpportunities = GetAttributeValue( AttributeKey.AddGroupOpportunities ).AsBoolean();
            var groupQry = groupService
                .Queryable( "GroupLocations.Location" )
                .Where( g => g.IsActive
                        && groupTypeIds.Contains( g.GroupType.Id )
                        && ( showAllGroups || g.IsPublic ) );

            var groupParameterExpression = groupService.ParameterExpression;
            var schedulePropertyExpression = Expression.Property( groupParameterExpression, "Schedule" );

            var dowsFilterControl = phFilterControls.FindControl( "filter_dows" ) as RockCheckBoxList;
            if ( dowsFilterControl != null )
            {
                var dows = new List<DayOfWeek>();
                dowsFilterControl.SelectedValuesAsInt.ForEach( i => dows.Add( ( DayOfWeek ) i ) );

                var dowsStr = new List<string>();
                dowsFilterControl.SelectedNames.ForEach( s => dowsStr.Add( s.Left( 2 ).ToUpper() ) );

                if ( dows.Any() )
                {
                    if ( _filterValues.ContainsKey( "FilterDows" ) )
                    {
                        _filterValues["FilterDows"] = dowsFilterControl.SelectedValuesAsInt.AsDelimited( "^" );
                    }
                    else
                    {
                        _filterValues.AddOrReplace( "FilterDows", dowsFilterControl.SelectedValuesAsInt.AsDelimited( "^" ) );
                    }
                    groupQry = groupQry.Where( g =>
                        ( g.Schedule.WeeklyDayOfWeek.HasValue && dows.Contains( g.Schedule.WeeklyDayOfWeek.Value ) ) ||
                        dowsStr.Any( s => g.Schedule.iCalendarContent.Substring( g.Schedule.iCalendarContent.IndexOf( "BYDAY=" ), 20 ).Contains( s ) ) ||
                        ( g.Schedule.EffectiveStartDate.HasValue && dows.Contains( ( DayOfWeek ) System.Data.Entity.SqlServer.SqlFunctions.DatePart( "dw", g.Schedule.EffectiveStartDate.Value ) - 1 ) ) ||
                        g.GroupLocations.Any( gl =>
                            gl.Schedules.Any( gls => gls.WeeklyDayOfWeek.HasValue && dows.Contains( g.Schedule.WeeklyDayOfWeek.Value ) ||
                                ( gls.EffectiveStartDate.HasValue && dows.Contains( ( DayOfWeek ) System.Data.Entity.SqlServer.SqlFunctions.DatePart( "dw", gls.EffectiveStartDate.Value ) - 1 ) ) ||
                                dowsStr.Any( s => gls.iCalendarContent.Substring( gls.iCalendarContent.IndexOf( "BYDAY=" ), 20 ).Contains( s ) ) ) ) );
                }
            }

            var dowFilterControl = phFilterControls.FindControl( "filter_dow" );
            if ( dowFilterControl != null )
            {
                var field = FieldTypeCache.Get( Rock.SystemGuid.FieldType.DAY_OF_WEEK ).Field;

                var filterValues = field.GetFilterValues( dowFilterControl, null, Rock.Reporting.FilterMode.SimpleFilter );
                //var expression = field.PropertyFilterExpression( null, filterValues, schedulePropertyExpression, "WeeklyDayOfWeek", typeof( DayOfWeek? ) );
                //groupQry = groupQry.Where( groupParameterExpression, expression, null );
                //Commented out property filter to have a custom DOW filter for iCalendarContent search.
                _filterValues.AddOrReplace( "FilterDow", filterValues.AsDelimited( "^" ) );

                string formattedValue = string.Empty;
                string searchStr = string.Empty;
                if ( filterValues.Count > 1 )
                {
                    int? intValue = filterValues[1].AsIntegerOrNull();
                    if ( intValue.HasValue )
                    {
                        System.DayOfWeek dayOfWeek = ( System.DayOfWeek ) intValue.Value;
                        formattedValue = dayOfWeek.ConvertToString();
                        searchStr = formattedValue.Left( 2 ).ToUpper();
                        groupQry = groupQry.Where( g =>
                             ( g.Schedule.WeeklyDayOfWeek.HasValue && g.Schedule.WeeklyDayOfWeek.Value == dayOfWeek ) ||
                             g.Schedule.iCalendarContent.Substring( g.Schedule.iCalendarContent.IndexOf( "BYDAY=" ), 20 ).Contains( searchStr ) ||
                             ( g.Schedule.EffectiveStartDate.HasValue && ( DayOfWeek ) System.Data.Entity.SqlServer.SqlFunctions.DatePart( "dw", g.Schedule.EffectiveStartDate.Value ) == dayOfWeek ) ||
                             g.GroupLocations.Any( gl =>
                                gl.Schedules.Any( gls => ( gls.WeeklyDayOfWeek.HasValue && gls.WeeklyDayOfWeek.Value == dayOfWeek ) ||
                                    ( gls.EffectiveStartDate.HasValue && ( DayOfWeek ) System.Data.Entity.SqlServer.SqlFunctions.DatePart( "dw", gls.EffectiveStartDate.Value ) == dayOfWeek ) ||
                                    gls.iCalendarContent.Substring( gls.iCalendarContent.IndexOf( "BYDAY=" ), 20 ).Contains( searchStr ) ) ) );
                    }
                }
            }

            var timeFilterControl = phFilterControls.FindControl( "filter_time" );
            if ( timeFilterControl != null )
            {
                var field = FieldTypeCache.Get( Rock.SystemGuid.FieldType.TIME ).Field;

                var filterValues = field.GetFilterValues( timeFilterControl, null, Rock.Reporting.FilterMode.SimpleFilter );
                var expression = field.PropertyFilterExpression( null, filterValues, schedulePropertyExpression, "WeeklyTimeOfDay", typeof( TimeSpan? ) );
                if ( _filterValues.ContainsKey( "FilterTime" ) )
                {
                    _filterValues["FilterTime"] = filterValues.AsDelimited( "^" );
                }
                else
                {
                    _filterValues.Add( "FilterTime", filterValues.AsDelimited( "^" ) );
                }
                groupQry = groupQry.Where( groupParameterExpression, expression, null );
            }

            if ( GetAttributeValue( AttributeKey.DisplayCampusFilter ).AsBoolean() )
            {
                var searchCampuses = new List<int>();

                if ( _ssFilters )
                {
                    if ( !string.IsNullOrWhiteSpace( filter_ddlCampus.SelectedValue ) )
                    {
                        searchCampuses.Add( filter_ddlCampus.SelectedValue.AsInteger() );
                    }
                }
                else
                {
                    searchCampuses = filter_cblCampus.SelectedValuesAsInt;
                }
                if ( searchCampuses.Count > 0 )
                {
                    if ( _filterValues.ContainsKey( "FilterCampus" ) )
                    {
                        _filterValues["FilterCampus"] = searchCampuses.AsDelimited( "^" );
                    }
                    else
                    {
                        _filterValues.Add( "FilterCampus", searchCampuses.AsDelimited( "^" ) );
                    }
                    groupQry = groupQry.Where( c => searchCampuses.Contains( c.CampusId ?? -1 ) );
                }
            }
            else if ( GetAttributeValue( AttributeKey.EnableCampusContext ).AsBoolean() )
            {
                // if Campus Context is enabled and the filter is not shown, we need to filter campuses directly.
                var campusEntityType = EntityTypeCache.Get( "Rock.Model.Campus" );
                var contextCampus = RockPage.GetCurrentContext( campusEntityType ) as Campus;

                if ( contextCampus != null )
                {
                    groupQry = groupQry.Where( c => c.CampusId == contextCampus.Id );
                }
            }

            if ( GetAttributeValue( AttributeKey.DisplayKeywordSearch ).AsBoolean() )
            {
                var tbKeyword = ( RockTextBox ) phFilterControls.FindControl( "filter_keyword" ) ?? ( RockTextBox ) phFilterControlsCollapsed.FindControl( "filter_keyword" );
                if ( tbKeyword != null && tbKeyword.Text.IsNotNullOrWhiteSpace() )
                {
                    var keywordStr = tbKeyword.Text.Trim();
                    var originalQry = groupQry;
                    groupQry = groupQry.Where( g => g.Name.Contains( keywordStr ) || g.Description.Contains( keywordStr ) );

                    var attributesInKeywords = GetAttributeValue( AttributeKey.AttributesInKeywords ).SplitDelimitedValues();
                    if ( attributesInKeywords.Length > 0 )
                    {
                        var concatQueries = new List<IQueryable<Group>>();
                        foreach ( string attr in attributesInKeywords )
                        {
                            Guid? attributeGuid = attr.AsGuidOrNull();
                            if ( attributeGuid.HasValue )
                            {
                                concatQueries.Add( originalQry.WhereAttributeValue( rockContext, a => a.Attribute.Guid == attributeGuid && a.PersistedTextValue.Contains( keywordStr ) ) );
                            }
                        }
                        foreach ( var qry in concatQueries )
                        {
                            groupQry = groupQry.Concat( qry );
                        }

                        groupQry = groupQry.GroupBy( g => g.Id ).Select( gg => gg.FirstOrDefault() );
                    }

                }
            }

            // This hides the groups that are at or over capacity by doing two things:
            // 1) If the group has a GroupCapacity, check that we haven't met or exceeded that.
            // 2) When someone registers for a group on the front-end website, they automatically get added with the group's default
            //    GroupTypeRole. If that role exists and has a MaxCount, check that we haven't met or exceeded it yet.
            var tglShowFullGroups = ( Toggle ) phFilterControls.FindControl( "filter_tglShowFullGroups" ) ?? ( Toggle ) phFilterControlsCollapsed.FindControl( "filter_tglShowFullGroups" );
            if ( GetAttributeValue( AttributeKey.HideOvercapacityGroups ) == "True" || ( GetAttributeValue( AttributeKey.HideOvercapacityGroups ) == "Filter" && !tglShowFullGroups.Checked ) )
            {
                var includePendingInCapacity = GetAttributeValue( AttributeKey.OvercapacityGroupsincludePending ).AsBoolean();
                groupQry = groupQry.Where(
                    g => g.GroupCapacity == null ||
                    g.Members.Where( m => m.GroupMemberStatus == GroupMemberStatus.Active || ( includePendingInCapacity && m.GroupMemberStatus == GroupMemberStatus.Pending ) ).Count() < g.GroupCapacity );

                groupQry = groupQry.Where( g =>
                     g.GroupType == null ||
                     g.GroupType.DefaultGroupRole == null ||
                     g.GroupType.DefaultGroupRole.MaxCount == null ||
                     g.Members.Where( m => m.GroupRoleId == g.GroupType.DefaultGroupRole.Id ).Count() < g.GroupType.DefaultGroupRole.MaxCount );
            }

            Guid? attributeCustomSortGuid = GetAttributeValue( AttributeKey.AttributeCustomSort ).AsGuidOrNull();
            var attributeValList = new List<string>();
            var attributeValKey = string.Empty;

            // Filter query by any configured attribute filters
            if ( AttributeFilters != null && AttributeFilters.Any() )
            {
                var processedAttributes = new HashSet<string>();
                /*
                    07/01/2021 - MSB

                    This section of code creates an expression for each attribute in the search. The attributes from the same
                    Group Type get grouped and &&'d together. Then the grouped Expressions will get ||'d together so that results
                    will be returned across Group Types.

                    If we don't do this, when the Admin adds attributes from two different Group Types and then the user enters data
                    for both attributes they would get no results because Attribute A from Group Type A doesn't exists in Group Type B.

                    Reason: Queries across Group Types
                */
                var filters = new Dictionary<string, Expression>();
                var parameterExpression = groupService.ParameterExpression;

                foreach ( var attribute in AttributeFilters )
                {
                    var filterId = $"filter_{attribute.Key}_{attribute.FieldType.Id}";
                    var queryKey = $"{attribute.EntityTypeQualifierColumn}_{attribute.EntityTypeQualifierValue}";

                    var filterControl = phFilterControls.FindControl( filterId );

                    Expression leftExpression = null;
                    if ( filters.ContainsKey( queryKey ) )
                    {
                        leftExpression = filters[queryKey];
                    }

                    var expression = Rock.Utility.ExpressionHelper.BuildExpressionFromFieldType<Group>( attribute.FieldType.Field, filterControl, attribute, groupService, parameterExpression, FilterMode.SimpleFilter );
                    if ( expression != null )
                    {
                        if ( leftExpression == null )
                        {
                            filters[queryKey] = expression;
                        }
                        else
                        {
                            filters[queryKey] = Expression.And( leftExpression, expression );
                        }
                    }

                    if ( attributeCustomSortGuid != null && attribute.Guid == attributeCustomSortGuid )
                    {
                        attributeValList = attribute.FieldType.Field.GetFilterValues( filterControl, attribute.QualifierValues, Rock.Reporting.FilterMode.SimpleFilter );
                        attributeValKey = attribute.Key;
                    }
                }

                if ( filters.Count == 1 )
                {
                    groupQry = groupQry.Where( parameterExpression, filters.FirstOrDefault().Value );
                }
                else if ( filters.Count > 1 )
                {
                    var keys = filters.Keys.ToList();
                    var expression = filters[keys[0]];

                    for ( var i = 1; i < filters.Count; i++ )
                    {
                        expression = Expression.Or( expression, filters[keys[i]] );
                    }
                    groupQry = groupQry.Where( parameterExpression, expression );
                }
            }

            List<GroupLocation> fences = null;
            List<Group> groups = groupQry.ToList();

            groups = groups.Where( g => !GroupTypeLocations.ContainsKey( g.GroupTypeId ) || g.GroupLocations.Any( gl => gl.GroupLocationTypeValueId == GroupTypeLocations[g.GroupTypeId] ) ).ToList();

            // Run query to get list of matching groups
            SortProperty sortProperty = gGroups.SortProperty;
            if ( sortProperty != null )
            {
                groups = groups.AsQueryable().Sort( sortProperty ).ToList();
            }
            else
            {
                if ( attributeValList != null && attributeValList.Any() && ( ( attributeValList.Count >= 2 && !string.IsNullOrWhiteSpace( attributeValList[1] ) ) || !string.IsNullOrWhiteSpace( attributeValList[0] ) ) )
                {
                    groups = groupQry.ToList();

                    var attributeVal = ( attributeValList.Count >= 2 ) ? attributeValList[1] : attributeValList[0];
                    foreach ( var group in groups )
                    {
                        if ( group.Attributes == null )
                        {
                            group.LoadAttributes( rockContext );
                        }
                    }

                    groups.Sort( ( item1, item2 ) =>
                    {
                        var parseAttributeVal = attributeVal.Split( ',' );
                        var item1AttributeValues = item1.AttributeValues.Where( a => a.Key == attributeValKey ).FirstOrDefault().Value.Value.Split( ',' );
                        var compareIntersect1 = parseAttributeVal.Intersect( item1AttributeValues ).ToList();
                        var item2AttributeValues = item2.AttributeValues.Where( a => a.Key == attributeValKey ).FirstOrDefault().Value.Value.Split( ',' );
                        var compareIntersect2 = parseAttributeVal.Intersect( item2AttributeValues ).ToList();

                        if ( compareIntersect1.Count == compareIntersect2.Count )
                        {
                            return ( item1.Name as IComparable ).CompareTo( item2.Name as IComparable );
                        }
                        else
                        {
                            return ( compareIntersect2.Count as IComparable ).CompareTo( compareIntersect1.Count as IComparable );
                        }
                    } );
                }
                else
                {
                    groups = groups.OrderBy( g => g.Name ).ToList();
                }
            }

            gGroups.Columns[5].Visible = GetAttributeValue( AttributeKey.ShowCampus ).AsBoolean() && groups.Any( g => g.CampusId.HasValue );

            int? fenceGroupTypeId = GetGroupTypeId( GetAttributeValue( AttributeKey.GeofencedGroupType ).AsGuidOrNull() );
            bool showMap = GetAttributeValue( AttributeKey.ShowMap ).AsBoolean();
            bool showFences = showMap && GetAttributeValue( AttributeKey.ShowFence ).AsBoolean();

            var distances = new Dictionary<int, double>();
            FinderMapItem personMapItem = null;
            var fenceMapItems = new List<MapItem>();

            // If we care where these groups are located...
            if ( fenceGroupTypeId.HasValue || showMap || showProximity )
            {
                // Get the location for the address entered
                Location personLocation = null;
                MapCoordinate mapCoordinate = null;
                if ( fenceGroupTypeId.HasValue || showProximity || _autoLoad )
                {
                    if ( GetAttributeValue( AttributeKey.EnablePostalCodeSearch ).AsBoolean() )
                    {
                        if ( !string.IsNullOrWhiteSpace( filter_tbPostalCode.Text ) )
                        {
                            try
                            {
                                mapCoordinate = new LocationService( rockContext )
                                    .GetMapCoordinateFromPostalCode( filter_tbPostalCode.Text );
                            }
                            catch
                            {
                                ShowWarning( $"{filter_tbPostalCode.Text} is an invalid {filter_tbPostalCode.Label}. If {filter_tbPostalCode.Label} is required, it will be reset to the default location." );
                            } // handle invalid postal codes by leaving mapCoordinate null and reverting to default location.
                        }
                    }
                    else
                    {
                        personLocation = new LocationService( rockContext )
                        .Get( filter_acAddress.Street1, filter_acAddress.Street2, filter_acAddress.City,
                            filter_acAddress.State, filter_acAddress.PostalCode, filter_acAddress.Country, null, true, false );
                        if ( personLocation != null && personLocation.GeoPoint != null )
                            mapCoordinate = new MapCoordinate( personLocation.Latitude, personLocation.Longitude );
                    }

                    if ( mapCoordinate == null && personLocation == null || ( personLocation != null && personLocation.GeoPoint == null ) )
                    {
                        Guid? campusGuid = GetAttributeValue( AttributeKey.DefaultLocation ).AsGuidOrNull();
                        if ( campusGuid != null )
                        {
                            var campusLocation = CampusCache.Get( ( Guid ) campusGuid ).LocationId;
                            personLocation = new LocationService( rockContext ).Get( ( int ) campusLocation );
                            if ( !string.IsNullOrWhiteSpace( personLocation.PostalCode ) )
                            {
                                filter_tbPostalCode.Text = personLocation.PostalCode.Substring( 0, 5 );
                            }
                            if ( personLocation.GeoPoint != null )
                                mapCoordinate = new MapCoordinate( personLocation.Latitude, personLocation.Longitude );
                        }
                    }
                }

                // If showing a map, and person's location was found, save a map item for this location
                if ( showMap && personLocation != null && personLocation.GeoPoint != null )
                {
                    var infoWindow = string.Format(
                        @"
<div style='width:250px'>
    <div class='clearfix'>
		<strong>Your Location</strong>
        <br/>{0}
    </div>
</div>
",
                        personLocation.FormattedHtmlAddress );

                    personMapItem = new FinderMapItem( personLocation );
                    personMapItem.Name = "Your Location";
                    personMapItem.InfoWindow = HttpUtility.HtmlEncode( infoWindow.Replace( Environment.NewLine, string.Empty ).Replace( "\n", string.Empty ).Replace( "\t", string.Empty ) );
                }
                else if ( mapCoordinate != null && GetAttributeValue( AttributeKey.EnablePostalCodeSearch ).AsBoolean() )
                {
                    var infoWindow = string.Format(
                        @"
<div style='width:250px'>
    <div class='clearfix'>
		<strong>Your Location</strong>
        <br/>{0}
    </div>
</div>
",
                        filter_tbPostalCode.Text );

                    personMapItem = new FinderMapItem();
                    personMapItem.Name = "Your Location";
                    personMapItem.Point = mapCoordinate;
                    personMapItem.PolygonPoints = new List<MapCoordinate>();
                    personMapItem.PolygonPoints.Add( mapCoordinate );
                    personMapItem.InfoWindow = HttpUtility.HtmlEncode( infoWindow.Replace( Environment.NewLine, string.Empty ).Replace( "\n", string.Empty ).Replace( "\t", string.Empty ) );
                }

                // Get the locations, and optionally calculate the distance for each of the groups
                var groupLocations = new List<GroupLocation>();
                foreach ( var group in groups )
                {
                    foreach ( var groupLocation in group.GroupLocations )
                    {
                        groupLocations.Add( groupLocation );

                        if ( groupLocation.Location.GeoPoint != null && showProximity && ( ( personLocation != null && personLocation.GeoPoint != null ) || ( mapCoordinate != null ) ) )
                        {
                            string geoText = string.Format( "POINT({0} {1})", mapCoordinate.Longitude, mapCoordinate.Latitude );
                            DbGeography geoPoint = DbGeography.FromText( geoText );
                            double meters = groupLocation.Location.GeoPoint.Distance( geoPoint ) ?? 0.0D;
                            double miles = meters * Location.MilesPerMeter;

                            // If this group already has a distance calculated, see if this location is closer and if so, use it instead
                            if ( distances.ContainsKey( group.Id ) )
                            {
                                if ( distances[group.Id] < miles )
                                {
                                    distances[group.Id] = miles;
                                }
                            }
                            else
                            {
                                distances.Add( group.Id, miles );
                            }
                        }
                        else
                        {
                            distances.AddOrIgnore( group.Id, 9999 );
                        }
                    }
                }

                // If groups should be limited by a geofence
                if ( fenceGroupTypeId.HasValue )
                {
                    fences = new List<GroupLocation>();
                    if ( personLocation != null && personLocation.GeoPoint != null )
                    {
                        fences = new GroupLocationService( rockContext )
                            .Queryable( "Group,Location" )
                            .Where( gl => gl.Group.GroupTypeId == fenceGroupTypeId
                                && gl.Location.GeoFence != null
                                && personLocation.GeoPoint.Intersects( gl.Location.GeoFence ) )
                            .ToList();
                    }

                    // Limit the group locations to only those locations inside one of the fences
                    groupLocations = groupLocations
                        .Where( gl =>
                            fences.Any( f => gl.Location.GeoPoint.Intersects( f.Location.GeoFence ) )
                            )
                        .ToList();

                    // Limit the groups to the those that still contain a valid location
                    groups = groups
                        .Where( g =>
                            groupLocations.Any( gl => gl.GroupId == g.Id ) )
                        .ToList();

                    // If the map and fences should be displayed, create a map item for each fence
                    if ( showMap && showFences )
                    {
                        foreach ( var fence in fences )
                        {
                            var mapItem = new FinderMapItem( fence.Location );
                            mapItem.EntityTypeId = EntityTypeCache.Get( "Rock.Model.Group" ).Id;
                            mapItem.EntityId = fence.GroupId;
                            mapItem.Name = fence.Group.Name;
                            fenceMapItems.Add( mapItem );
                        }
                    }
                }

                // if not sorting by ColumnClick and SortByDistance, then sort the groups by distance
                if ( gGroups.SortProperty == null && showProximity && GetAttributeValue( AttributeKey.SortByDistance ).AsBoolean() )
                {
                    if ( distances.Count > 0 )
                    {
                        // only show groups with a known location, and sort those by distance
                        groups = groups.Where( a => distances.Select( b => b.Key ).Contains( a.Id ) ).ToList();
                    }

                    if ( attributeValList != null && attributeValList.Any() && ( ( attributeValList.Count >= 2 && !string.IsNullOrWhiteSpace( attributeValList[1] ) ) || !string.IsNullOrWhiteSpace( attributeValList[0] ) ) )
                    {
                        var attributeVal = ( attributeValList.Count >= 2 ) ? attributeValList[1] : attributeValList[0];
                        foreach ( var group in groups )
                        {
                            if ( group.Attributes == null )
                            {
                                group.LoadAttributes( rockContext );
                            }
                        }

                        groups.Sort( ( item1, item2 ) =>
                        {
                            var parseAttributeVal = attributeVal.Split( ',' );
                            var item1AttributeValues = item1.AttributeValues.Where( a => a.Key == attributeValKey ).FirstOrDefault().Value.Value.Split( ',' );
                            var compareIntersect1 = parseAttributeVal.Intersect( item1AttributeValues ).ToList();
                            var item2AttributeValues = item2.AttributeValues.Where( a => a.Key == attributeValKey ).FirstOrDefault().Value.Value.Split( ',' );
                            var compareIntersect2 = parseAttributeVal.Intersect( item2AttributeValues ).ToList();

                            if ( compareIntersect1.Count == compareIntersect2.Count )
                            {
                                if ( distances[item1.Id] == distances[item2.Id] )
                                {
                                    return ( item1.Name as IComparable ).CompareTo( item2.Name as IComparable );
                                }
                                else
                                {
                                    return ( distances[item1.Id] as IComparable ).CompareTo( distances[item2.Id] as IComparable );
                                }
                            }
                            else
                            {
                                return ( compareIntersect2.Count as IComparable ).CompareTo( compareIntersect1.Count as IComparable );
                            }
                        } );
                    }
                    else if ( distances.Count > 0 )
                    {
                        groups = groups.OrderBy( a => distances[a.Id] ).ThenBy( a => a.Name ).ToList();
                    }
                }

                // if limiting by PageSize, limit to the top X groups
                int? pageSize = ddlPageSize.SelectedValue.AsIntegerOrNull();
                if ( pageSize.HasValue && pageSize > 0 )
                {
                    groups = groups.Take( pageSize.Value ).ToList();
                }

                // If a map is to be shown
                if ( showMap && groups.Any() && !addGroupOpportunities )
                {
                    GenerateGroupMap( groups, personMapItem, groupLocations, fenceMapItems );
                }
                else
                {
                    pnlMap.Visible = false;
                }
            }
            else
            {
                pnlMap.Visible = false;
            }

            // Should a lava output be displayed
            if ( GetAttributeValue( AttributeKey.ShowLavaOutput ).AsBoolean() )
            {
                string template = GetAttributeValue( AttributeKey.LavaOutput );
                var enabledCommands = GetAttributeValue( AttributeKey.FormattedOutputEnabledLavaCommands );

                var mergeFields = new Dictionary<string, object>();
                if ( fences != null )
                {
                    mergeFields.Add( "Fences", fences.Select( f => f.Group ).ToList() );
                }
                else
                {
                    mergeFields.Add( "Fences", new Dictionary<string, object>() );
                }

                mergeFields.Add( "Groups", groups );

                mergeFields.Add( "GroupDistances", distances.Select( d => { return new { Id = d.Key, Distance = d.Value }; } ).ToList() );


                if ( addGroupOpportunities )
                {
                    var qryGroupLocationSchedules = groupQry.SelectMany( g => g.GroupLocations ).SelectMany( gl => gl.Schedules, ( gl, s ) => new
                    {
                        gl.Group,
                        gl.Location,
                        Schedule = s,
                        Config = gl.GroupLocationScheduleConfigs.FirstOrDefault( glsc => glsc.ScheduleId == s.Id )
                    } );

                    var participantCounts = new GroupMemberAssignmentService( rockContext )
                    .Queryable()
                    .AsNoTracking()
                    .Where( gma =>
                        !gma.GroupMember.Person.IsDeceased
                        && qryGroupLocationSchedules.Any( gls =>
                            gls.Group.Id == gma.GroupMember.GroupId
                            && gls.Location.Id == gma.LocationId
                            && gls.Schedule.Id == gma.ScheduleId
                        )
                    )
                    .GroupBy( gma => new
                    {
                        gma.GroupMember.GroupId,
                        gma.LocationId,
                        gma.ScheduleId
                    } )
                   .Select( g => new
                   {
                       g.Key.GroupId,
                       g.Key.LocationId,
                       g.Key.ScheduleId,
                       Count = g.Count()
                   } )
                   .ToList();

                    var opportunities = qryGroupLocationSchedules
                    .ToList() // Execute the query; we have additional filtering that needs to happen once we materialize these objects.
                    .Select( gls =>
                    {
                        var participantCount = participantCounts.FirstOrDefault( c =>
                            c.GroupId == gls.Group.Id
                            && c.LocationId == gls.Location.Id
                            && c.ScheduleId == gls.Schedule.Id
                        )?.Count ?? 0;

                        return new Opportunity
                        {
                            Group = gls.Group,
                            Location = gls.Location,
                            Schedule = gls.Schedule,
                            NextStartDateTime = gls.Schedule.NextStartDateTime,
                            ScheduleName = gls.Config?.ConfigurationName,
                            SlotsMin = gls.Config?.MinimumCapacity,
                            SlotsDesired = gls.Config?.DesiredCapacity,
                            SlotsMax = gls.Config?.MaximumCapacity,
                            ParticipantCount = participantCount,
                            GeoPoint = gls.Location.GeoPoint
                        };
                    } ).Where( o => o.NextStartDateTime.HasValue ).ToList();

                    if ( dowsFilterControl != null )
                    {
                        var dows = new List<DayOfWeek>();
                        dowsFilterControl.SelectedValuesAsInt.ForEach( i => dows.Add( ( DayOfWeek ) i ) );
                        if ( dows.Any() )
                        {
                            opportunities = opportunities.Where( o => dows.Contains( o.NextStartDateTime.Value.DayOfWeek ) ).ToList();
                        }
                    }

                    if ( dowFilterControl != null )
                    {
                        var field = FieldTypeCache.Get( Rock.SystemGuid.FieldType.DAY_OF_WEEK ).Field;
                        var filterValues = field.GetFilterValues( dowFilterControl, null, Rock.Reporting.FilterMode.SimpleFilter );
                        _filterValues.AddOrReplace( "FilterDow", filterValues.AsDelimited( "^" ) );

                        if ( filterValues.Count > 1 )
                        {
                            int? intValue = filterValues[1].AsIntegerOrNull();
                            if ( intValue.HasValue )
                            {
                                System.DayOfWeek dayOfWeek = ( System.DayOfWeek ) intValue.Value;
                                opportunities = opportunities.Where( o => dayOfWeek == o.NextStartDateTime.Value.DayOfWeek ).ToList();
                            }
                        }
                    }

                    opportunities = sortGroupOpportunities( rockContext, opportunities, distances, attributeValList, attributeValKey, showProximity );

                    if ( showMap && opportunities.Any() )
                    {
                        GenerateGroupMap( opportunities.Select( o => o.Group ).ToList(), personMapItem, opportunities.Select( o => o.Group.GroupLocations.FirstOrDefault( gl => gl.Location == o.Location ) ).ToList(), fenceMapItems, opportunities );
                    }
                    else
                    {
                        pnlMap.Visible = false;
                    }

                    mergeFields.Add( "GroupOpportunities", opportunities );
                }

                Dictionary<string, object> linkedPages = new Dictionary<string, object>();
                linkedPages.Add( AttributeKey.GroupDetailPage, LinkedPageRoute( AttributeKey.GroupDetailPage ) );

                if ( _targetPersonGuid != Guid.Empty )
                {
                    linkedPages.Add( AttributeKey.RegisterPage, LinkedPageUrl( AttributeKey.RegisterPage, _urlParms ) );
                }
                else
                {
                    linkedPages.Add( AttributeKey.RegisterPage, LinkedPageRoute( AttributeKey.RegisterPage ) );
                }

                mergeFields.Add( "LinkedPages", linkedPages );
                mergeFields.Add( "CampusContext", RockPage.GetCurrentContext( EntityTypeCache.Get( "Rock.Model.Campus" ) ) as Campus );
                mergeFields.Add( "FilterValues", _filterValues );
                foreach ( var filter in _filterValues )
                {
                    mergeFields.Add( filter.Key, filter.Value );
                }

                lLavaOverview.Text = template.ResolveMergeFields( mergeFields, enabledCommands );

                pnlLavaOutput.Visible = true;
            }
            else
            {
                pnlLavaOutput.Visible = false;
            }

            // Should a grid be displayed
            if ( GetAttributeValue( AttributeKey.ShowGrid ).AsBoolean() )
            {
                pnlGrid.Visible = true;

                // Save the groups into the grid's object list since it is not being bound to actual group objects
                gGroups.ObjectList = new Dictionary<string, object>();
                groups.ForEach( g => gGroups.ObjectList.Add( g.Id.ToString(), g ) );

                // Bind the grid
                gGroups.DataSource = groups.Select( g =>
                {
                    var qryMembers = new GroupMemberService( rockContext )
                        .Queryable()
                        .Where( a => a.GroupId == g.Id );

                    if ( includePending )
                    {
                        qryMembers = qryMembers.Where( m => m.GroupMemberStatus == GroupMemberStatus.Active
                            || m.GroupMemberStatus == GroupMemberStatus.Pending );
                    }
                    else
                    {
                        qryMembers = qryMembers.Where( m => m.GroupMemberStatus == GroupMemberStatus.Active );
                    }

                    var groupType = GroupTypeCache.Get( g.GroupTypeId );

                    return new
                    {
                        Id = g.Id,
                        Name = g.Name,
                        GroupTypeName = groupType.Name,
                        GroupOrder = g.Order,
                        GroupTypeOrder = groupType.Order,
                        Description = g.Description,
                        IsSystem = g.IsSystem,
                        IsActive = g.IsActive,
                        GroupRole = string.Empty,
                        DateAdded = DateTime.MinValue,
                        Schedule = g.Schedule,
                        MemberCount = qryMembers.Count(),
                        AverageAge = Math.Round( qryMembers.Select( m => new { m.Person.BirthDate, m.Person.DeceasedDate } ).ToList().Select( a => Person.GetAge( a.BirthDate, a.DeceasedDate ) ).Average() ?? 0.0D ),
                        Campus = g.Campus != null ? g.Campus.Name : string.Empty,
                        Distance = distances.Where( d => d.Key == g.Id )
                            .Select( d => d.Value ).FirstOrDefault()
                    };
                } ).ToList();
                gGroups.DataBind();
            }
            else
            {
                pnlGrid.Visible = false;
            }

            // Show the results
            pnlResults.Visible = true;
        }

        private void GenerateGroupMap( List<Group> groups, FinderMapItem personMapItem, List<GroupLocation> groupLocations, List<MapItem> fenceMapItems, List<Opportunity> opportunities = null )
        {
            Template template = null;
            ILavaTemplate lavaTemplate = null;

            if ( LavaService.RockLiquidIsEnabled )
            {
                template = Template.Parse( GetAttributeValue( AttributeKey.MapInfo ) );

                LavaHelper.VerifyParseTemplateForCurrentEngine( GetAttributeValue( AttributeKey.MapInfo ) );
            }
            else
            {
                var parseResult = LavaService.ParseTemplate( GetAttributeValue( AttributeKey.MapInfo ) );

                lavaTemplate = parseResult.Template;
            }


            // Add map items for all the remaining valid group locations
            var groupMapItems = new List<MapItem>();
            foreach ( var gl in groupLocations )
            {
                var group = groups.Where( g => g.Id == gl.GroupId ).FirstOrDefault();
                List<Opportunity> opportunitiesForGroup = null;
                if ( opportunities != null )
                {
                    opportunitiesForGroup = opportunities.Where( o => o.Group.Id == gl.GroupId && o.Location.Id == gl.LocationId ).ToList();
                }
                if ( group != null )
                {
                    // Resolve info window lava template
                    var linkedPageParams = new Dictionary<string, string> { { "GroupId", group.Id.ToString() } };
                    var mergeFields = new Dictionary<string, object>();
                    mergeFields.Add( "Group", gl.Group );
                    mergeFields.Add( "Location", gl.Location );
                    if ( opportunities != null )
                    {
                        mergeFields.Add( "Opportunities", opportunitiesForGroup );
                    }

                    Dictionary<string, object> linkedPages = new Dictionary<string, object>();
                    linkedPages.Add( AttributeKey.GroupDetailPage, LinkedPageRoute( AttributeKey.GroupDetailPage ) );

                    if ( _targetPersonGuid != Guid.Empty )
                    {
                        linkedPages.Add( AttributeKey.RegisterPage, LinkedPageUrl( AttributeKey.RegisterPage, _urlParms ) );
                    }
                    else
                    {
                        linkedPages.Add( AttributeKey.RegisterPage, LinkedPageRoute( AttributeKey.RegisterPage ) );
                    }

                    mergeFields.Add( "LinkedPages", linkedPages );
                    mergeFields.Add( "CampusContext", RockPage.GetCurrentContext( EntityTypeCache.Get( "Rock.Model.Campus" ) ) as Campus );

                    // add collection of allowed security actions
                    Dictionary<string, object> securityActions = new Dictionary<string, object>();
                    securityActions.Add( "View", group.IsAuthorized( Authorization.VIEW, CurrentPerson ) );
                    securityActions.Add( "Edit", group.IsAuthorized( Authorization.EDIT, CurrentPerson ) );
                    securityActions.Add( "Administrate", group.IsAuthorized( Authorization.ADMINISTRATE, CurrentPerson ) );
                    mergeFields.Add( "AllowedActions", securityActions );

                    string infoWindow;

                    if ( LavaService.RockLiquidIsEnabled )
                    {
                        infoWindow = template.Render( Hash.FromDictionary( mergeFields ) );
                    }
                    else
                    {
                        var result = LavaService.RenderTemplate( lavaTemplate, mergeFields );

                        infoWindow = result.Text;
                    }

                    // Add a map item for group
                    var mapItem = new FinderMapItem( gl.Location );
                    mapItem.EntityTypeId = EntityTypeCache.Get( "Rock.Model.Group" ).Id;
                    mapItem.EntityId = group.Id;
                    mapItem.Name = group.Name;

                    var markerColor = GetAttributeValue( AttributeKey.MarkerColor );
                    if ( markerColor.IsNullOrWhiteSpace() )
                    {
                        mapItem.Color = group.GroupType.GroupTypeColor;
                    }
                    else
                    {
                        mapItem.Color = markerColor;
                    }

                    if ( mapItem.Point != null )
                    {
                        var locationPrecisionLevel = GetAttributeValue( AttributeKey.LocationPrecisionLevel );
                        switch ( locationPrecisionLevel.ToLower() )
                        {
                            case "narrow":
                                mapItem.Point.Latitude = mapItem.Point.Latitude != null ? Convert.ToDouble( mapItem.Point.Latitude.Value.ToString( "#.###5" ) ) : ( double? ) null;
                                mapItem.Point.Longitude = mapItem.Point.Longitude != null ? Convert.ToDouble( mapItem.Point.Longitude.Value.ToString( "#.###5" ) ) : ( double? ) null;
                                break;

                            case "close":
                                mapItem.Point.Latitude = mapItem.Point.Latitude != null ? Convert.ToDouble( mapItem.Point.Latitude.Value.ToString( "#.###" ) ) : ( double? ) null;
                                mapItem.Point.Longitude = mapItem.Point.Longitude != null ? Convert.ToDouble( mapItem.Point.Longitude.Value.ToString( "#.###" ) ) : ( double? ) null;
                                break;

                            case "wide":
                                mapItem.Point.Latitude = mapItem.Point.Latitude != null ? Convert.ToDouble( mapItem.Point.Latitude.Value.ToString( "#.##" ) ) : ( double? ) null;
                                mapItem.Point.Longitude = mapItem.Point.Longitude != null ? Convert.ToDouble( mapItem.Point.Longitude.Value.ToString( "#.##" ) ) : ( double? ) null;
                                break;
                        }

                        mapItem.InfoWindow = HttpUtility.HtmlEncode( infoWindow.Replace( Environment.NewLine, string.Empty ).Replace( "\n", string.Empty ).Replace( "\t", string.Empty ) );
                        groupMapItems.Add( mapItem );
                    }
                }
            }

            // Show the map
            Map( personMapItem, fenceMapItems, groupMapItems );
            pnlMap.Visible = true;
        }

        private List<Opportunity> sortGroupOpportunities( RockContext rockContext, List<Opportunity> opportunities, Dictionary<int, double> distances, List<string> attributeValList, string attributeValKey, bool showProximity )
        {
            if ( showProximity && GetAttributeValue( AttributeKey.SortByDistance ).AsBoolean() )
            {
                if ( distances.Count > 0 )
                {
                    // only show groups with a known location, and sort those by distance
                    opportunities = opportunities.Where( a => distances.Select( b => b.Key ).Contains( a.Group.Id ) ).ToList();
                }

                if ( attributeValList != null && attributeValList.Any() && ( ( attributeValList.Count >= 2 && !string.IsNullOrWhiteSpace( attributeValList[1] ) ) || !string.IsNullOrWhiteSpace( attributeValList[0] ) ) )
                {
                    var attributeVal = ( attributeValList.Count >= 2 ) ? attributeValList[1] : attributeValList[0];
                    foreach ( var opportunity in opportunities )
                    {
                        if ( opportunity.Group.Attributes == null )
                        {
                            opportunity.Group.LoadAttributes( rockContext );
                        }
                    }

                    opportunities.Sort( ( item1, item2 ) =>
                    {
                        var parseAttributeVal = attributeVal.Split( ',' );
                        var item1AttributeValues = item1.Group.AttributeValues.Where( a => a.Key == attributeValKey ).FirstOrDefault().Value.Value.Split( ',' );
                        var compareIntersect1 = parseAttributeVal.Intersect( item1AttributeValues ).ToList();
                        var item2AttributeValues = item2.Group.AttributeValues.Where( a => a.Key == attributeValKey ).FirstOrDefault().Value.Value.Split( ',' );
                        var compareIntersect2 = parseAttributeVal.Intersect( item2AttributeValues ).ToList();

                        if ( compareIntersect1.Count == compareIntersect2.Count )
                        {
                            if ( distances[item1.Group.Id] == distances[item2.Group.Id] )
                            {
                                return ( item1.Group.Name as IComparable ).CompareTo( item2.Group.Name as IComparable );
                            }
                            else
                            {
                                return ( distances[item1.Group.Id] as IComparable ).CompareTo( distances[item2.Group.Id] as IComparable );
                            }
                        }
                        else
                        {
                            return ( compareIntersect2.Count as IComparable ).CompareTo( compareIntersect1.Count as IComparable );
                        }
                    } );
                }
                else if ( distances.Count > 0 )
                {
                    opportunities = opportunities.OrderBy( o => distances[o.Group.Id] ).ThenBy( o => o.NextStartDateTime ).ToList();
                }
            }
            else
            {
                if ( attributeValList != null && attributeValList.Any() && ( ( attributeValList.Count >= 2 && !string.IsNullOrWhiteSpace( attributeValList[1] ) ) || !string.IsNullOrWhiteSpace( attributeValList[0] ) ) )
                {
                    var attributeVal = ( attributeValList.Count >= 2 ) ? attributeValList[1] : attributeValList[0];
                    foreach ( var opportunity in opportunities )
                    {
                        if ( opportunity.Group.Attributes == null )
                        {
                            opportunity.Group.LoadAttributes( rockContext );
                        }
                    }

                    opportunities.Sort( ( item1, item2 ) =>
                    {
                        var parseAttributeVal = attributeVal.Split( ',' );
                        var item1AttributeValues = item1.Group.AttributeValues.Where( a => a.Key == attributeValKey ).FirstOrDefault().Value.Value.Split( ',' );
                        var compareIntersect1 = parseAttributeVal.Intersect( item1AttributeValues ).ToList();
                        var item2AttributeValues = item2.Group.AttributeValues.Where( a => a.Key == attributeValKey ).FirstOrDefault().Value.Value.Split( ',' );
                        var compareIntersect2 = parseAttributeVal.Intersect( item2AttributeValues ).ToList();

                        if ( compareIntersect1.Count == compareIntersect2.Count )
                        {
                            return ( item1.Group.Name as IComparable ).CompareTo( item2.Group.Name as IComparable );
                        }
                        else
                        {
                            return ( compareIntersect2.Count as IComparable ).CompareTo( compareIntersect1.Count as IComparable );
                        }
                    } );
                }
                else
                {
                    opportunities = opportunities.OrderBy( o => o.NextStartDateTime ).ToList();
                }
            }
            return opportunities;
        }

        /// <summary>
        /// Binds the type of the group.
        /// </summary>
        /// <param name="control">The control.</param>
        /// <param name="groupTypes">The group types.</param>
        /// <param name="attributeName">Name of the attribute.</param>
        private void BindGroupType( GroupTypePicker control, List<GroupType> groupTypes, string attributeName )
        {
            control.GroupTypes = groupTypes;

            var groupTypeId = GetAttributeValue( attributeName ).AsIntegerOrNull();
            if ( groupTypeId.HasValue )
            {
                var groupType = groupTypes.FirstOrDefault( g => g.Id.Equals( groupTypeId.Value ) );
                if ( groupType != null )
                {
                    control.SelectedGroupTypeId = groupType.Id;
                }
            }
        }

        private void BindGroupType( RockListBox control, List<GroupType> groupTypes )
        {
            control.DataSource = groupTypes.Select( t => new { value = t.Id, text = t.Name } );
            control.DataBind();

            var groupTypeIds = GetGroupTypeIds();

            if ( groupTypeIds != null )
            {
                var existingGroupTypeIds = groupTypes.Where( g => groupTypeIds.Any( gt => gt == g.Id ) );
                control.SetValues( groupTypeIds );
            }
        }

        private void BindGroupTypeLocationGrid()
        {
            var groupTypeIds = gtpGroupType.SelectedValuesAsInt;
            if ( groupTypeIds == null )
            {
                groupTypeIds = GetGroupTypeIds();
            }

            gGroupTypeLocation.DataSource = GroupTypeCache.All().Where( gt => groupTypeIds.Contains( gt.Id ) ).ToList();
            gGroupTypeLocation.DataBind();
        }

        private List<int> GetGroupTypeIds()
        {
            var groupTypeGuid = GetAttributeValue( AttributeKey.GroupType ).AsGuidOrNull();

            if ( groupTypeGuid == null )
            {
                return GetAttributeValue( AttributeKey.GroupType ).FromJsonOrNull<List<int>>();
            }

            return new List<int> { GroupTypeCache.Get( groupTypeGuid.Value ).Id };
        }

        private int? GetGroupTypeId( Guid? groupTypeGuid )
        {
            if ( groupTypeGuid.HasValue )
            {
                var groupType = GroupTypeCache.Get( groupTypeGuid.Value );
                if ( groupType != null )
                {
                    return groupType.Id;
                }
            }

            return null;
        }

        private void BindFilterOrder()
        {
            FilterOrder = FilterOrder.OrderBy( f => f.Key ).ToDictionary( k => k.Key, v => v.Value );
            gSortFilters.DataSource = FilterOrder.ToList();
            gSortFilters.DataBind();
        }

        /// <summary>
        /// Maps the specified location.
        /// </summary>
        /// <param name="location">The location.</param>
        /// <param name="fences">The fences.</param>
        /// <param name="groups">The groups.</param>
        private void Map( MapItem location, List<MapItem> fences, List<MapItem> groups )
        {
            pnlMap.Visible = true;

            string mapStylingFormat = @"
                        <style>
                            #map_wrapper {{
                                height: {0}px;
                            }}

                            #map_canvas {{
                                width: 100%;
                                height: 100%;
                                border-radius: var(--border-radius-base);
                            }}
                        </style>";
            lMapStyling.Text = string.Format( mapStylingFormat, GetAttributeValue( AttributeKey.MapHeight ) );

            // add styling to map
            string styleCode = "null";
            var markerColors = new List<string>();

            DefinedValueCache dvcMapStyle = DefinedValueCache.Get( GetAttributeValue( AttributeKey.MapStyle ).AsInteger() );
            if ( dvcMapStyle != null )
            {
                styleCode = dvcMapStyle.GetAttributeValue( "DynamicMapStyle" );
                var colorsSetting = dvcMapStyle.GetAttributeValue( "Colors" ) ?? string.Empty;
                markerColors = colorsSetting.Split( new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries )
                    .ToList();
                markerColors.ForEach( c => c = c.Replace( "#", string.Empty ) );
            }

            if ( !markerColors.Any() )
            {
                markerColors.Add( "FE7569" );
            }

            string locationColor = markerColors[0].Replace( "#", string.Empty );
            var polygonColorList = GetAttributeValue( AttributeKey.PolygonColors ).Split( new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries ).ToList();
            string polygonColors = "\"" + polygonColorList.AsDelimited( "\", \"" ) + "\"";
            string groupColor = ( markerColors.Count > 1 ? markerColors[1] : markerColors[0] ).Replace( "#", string.Empty );

            string latitude = "39.8282";
            string longitude = "-98.5795";
            var orgLocation = GlobalAttributesCache.Get().OrganizationLocation;
            if ( orgLocation != null && orgLocation.GeoPoint != null )
            {
                latitude = orgLocation.GeoPoint.Latitude.ToString();
                longitude = orgLocation.GeoPoint.Longitude.ToString();
            }

            var maxZoomLevel = GetAttributeValue( AttributeKey.MaximumZoomLevel );
            if ( maxZoomLevel.IsNullOrWhiteSpace() )
            {
                maxZoomLevel = "null";
            }
            var minZoomLevel = GetAttributeValue( AttributeKey.MinimumZoomLevel );
            if ( minZoomLevel.IsNullOrWhiteSpace() )
            {
                minZoomLevel = "null";
            }
            var zoom = GetAttributeValue( AttributeKey.InitialZoomLevel );
            if ( zoom.IsNullOrWhiteSpace() )
            {
                zoom = "null";
            }

            var zoomThreshold = GetAttributeValue( AttributeKey.MarkerZoomLevel );
            if ( zoomThreshold.IsNullOrWhiteSpace() )
            {
                zoomThreshold = "null";
            }

            var zoomAmount = GetAttributeValue( AttributeKey.MarkerZoomAmount );
            if ( zoomAmount.IsNullOrWhiteSpace() )
            {
                zoomAmount = "null";
            }

            // write script to page
            string mapScriptFormat = @"

        var locationData = {0};
        var fenceData = {1};
        var groupData = {2};
        var markerScale = 1;

        var allMarkers = [];

        var map;
        var bounds = new google.maps.LatLngBounds();
        var infoWindow = new google.maps.InfoWindow();

        var mapStyle = {3};

        var polygonColorIndex = 0;
        var polygonColors = [{5}];

        var min = .999999;
        var max = 1.000001;

        initializeMap();

        function initializeMap() {{
            // Set default map options
            var mapOptions = {{
                 mapTypeId: 'roadmap'
                ,styles: mapStyle
                ,center: new google.maps.LatLng({7}, {8})
                ,maxZoom: {11}
                ,minZoom: {12}
                ,zoom: {9}
            }};

            // Display a map on the page
            map = new google.maps.Map(document.getElementById('map_canvas'), mapOptions);
            google.maps.event.addDomListener(map, 'zoom_changed', function() {{
                var zoomThreshold = {13};
                var zoomAmount = {14};

                var zoom = map.getZoom();

                if(!zoomThreshold || !zoomAmount) {{
                    return;
                }}

                var scale = markerScale;
                if ( zoom >= zoomThreshold ) {{
                    let zoomScale = [ 0, 1, 1.00025, 1.0005, 1.001, 1.002, 1.004, 1.008, 1.016, 1.032, 1.064, 1.128, 1.256, 1.512, 2.024, 3.048, 5.096, 9.192, 17.384, 33.769, 66.536]
                    let zoomScaleLastIndex = zoomScale.length - 1;
                    if(zoom > zoomScaleLastIndex)
                    {{
                        zoom = zoomScaleLastIndex;
                    }}

                    scale = (zoomScale[zoom] * zoomAmount );
                }}

                var markerCount = allMarkers.length;
                var updatedMarkers = [];
                for (var i = 0; i < markerCount; i++) {{
                    var marker = allMarkers[i];

                    var pinImage = {{
                        path: marker.icon.path,
                        fillColor: marker.icon.fillColor,
                        fillOpacity: marker.icon.fillOpacity,
                        strokeColor: marker.icon.strokeColor,
                        strokeWeight: marker.icon.strokeWeight,
                        scale: scale,
                        labelOrigin: marker.icon.labelOrigin,
                        anchor: marker.icon.anchor,
                    }};

                    // Remove marker
  			        marker.setMap(null);

				    // Add marker back
                    var updatedMarker = new google.maps.Marker({{
                        position: marker.position,
                        icon: pinImage,
                        map: map,
                        id: marker.id,
                        title: marker.title,
                        info_window: marker.info_window,
                    }});

                if ( updatedMarker.info_window != null ) {{
                    google.maps.event.addListener(updatedMarker, 'click', (function (marker) {{
                        return function () {{
                            openInfoWindow(marker);
                        }}
                    }})(updatedMarker));
                }}

                if ( updatedMarker.id && updatedMarker.id > 0 ) {{
                    google.maps.event.addListener(updatedMarker, 'mouseover', (function (marker) {{
                        return function () {{
                            $(""tr[datakey='"" + marker.id + ""']"").addClass('row-highlight');
                        }}
                    }})(updatedMarker));

                    google.maps.event.addListener(updatedMarker, 'mouseout', (function (marker) {{
                        return function () {{
                            $(""tr[datakey='"" + marker.id + ""']"").removeClass('row-highlight');
                        }}
                    }})(updatedMarker));
                }}

                    updatedMarkers.push(updatedMarker);
                }}

                allMarkers = [...updatedMarkers];
	        }});

            map.setTilt(45);

            if ( locationData != null )
            {{
                var items = addMapItem(0, locationData, '{4}');
                for (var j = 0; j < items.length; j++) {{
                    items[j].setMap(map);
                }}
            }}

            if ( fenceData != null ) {{
                for (var i = 0; i < fenceData.length; i++) {{
                    var items = addMapItem(i, fenceData[i] );
                    for (var j = 0; j < items.length; j++) {{
                        items[j].setMap(map);
                    }}
                }}
            }}

            if ( groupData != null ) {{
                for (var i = 0; i < groupData.length; i++) {{
                    var items = addMapItem(i, groupData[i], groupData[i].Color ? groupData[i].Color : '{6}');
                    for (var j = 0; j < items.length; j++) {{
                        items[j].setMap(map);
                    }}
                }}
            }}

            // adjust any markers that may overlap
            adjustOverlappedMarkers();

            if (!bounds.isEmpty()) {{
                if(mapOptions.zoom || mapOptions.zoom === 0){{
                    map.setCenter(bounds.getCenter());
                }} else {{
                    map.fitBounds(bounds);
                }}
            }}
        }}

        function openInfoWindowById(id) {{
            marker = $.grep(allMarkers, function(m) {{ return m.id == id }})[0];
            openInfoWindow(marker);
        }}

        function openInfoWindow(marker) {{
            infoWindow.setContent( $('<div/>').html(marker.info_window).text() );
            infoWindow.open(map, marker);
        }}

        function addMapItem( i, mapItem, color ) {{
            var items = [];

            if (mapItem.Point) {{
                var position = new google.maps.LatLng(mapItem.Point.Latitude, mapItem.Point.Longitude);
                bounds.extend(position);

                if (!color) {{
                    color = '#FE7569';
                }}

                if ( color.length > 0 && color.toLowerCase().indexOf('rgb') < 0 && color[0] != '#' )
                {{
                    color = '#' + color;
                }}

                var pinImage = {{
                    path: '{10}',
                    fillColor: color,
                    fillOpacity: 1,
                    strokeColor: '#000',
                    strokeWeight: 0,
                    scale: markerScale,
                    labelOrigin: new google.maps.Point(0, 0),
                    anchor: new google.maps.Point(0, 0),
                }};

                marker = new google.maps.Marker({{
                    id: mapItem.EntityId,
                    position: position,
                    map: map,
                    title: htmlDecode(mapItem.Name),
                    icon: pinImage,
                    info_window: mapItem.InfoWindow,
                    label: String.fromCharCode(9679)
                }});

                items.push(marker);
                allMarkers.push(marker);

                if ( mapItem.InfoWindow != null ) {{
                    google.maps.event.addListener(marker, 'click', (function (marker, i) {{
                        return function () {{
                            openInfoWindow(marker);
                        }}
                    }})(marker, i));
                }}

                if ( mapItem.EntityId && mapItem.EntityId > 0 ) {{
                    google.maps.event.addListener(marker, 'mouseover', (function (marker, i) {{
                        return function () {{
                            $(""tr[datakey='"" + mapItem.EntityId + ""']"").addClass('row-highlight');
                        }}
                    }})(marker, i));

                    google.maps.event.addListener(marker, 'mouseout', (function (marker, i) {{
                        return function () {{
                            $(""tr[datakey='"" + mapItem.EntityId + ""']"").removeClass('row-highlight');
                        }}
                    }})(marker, i));
                }}
            }}

            if (typeof mapItem.PolygonPoints !== 'undefined' && mapItem.PolygonPoints.length > 0) {{
                var polygon;
                var polygonPoints = [];

                $.each(mapItem.PolygonPoints, function(j, point) {{
                    var position = new google.maps.LatLng(point.Latitude, point.Longitude);
                    bounds.extend(position);
                    polygonPoints.push(position);
                }});

                var polygonColor = getNextPolygonColor();

                polygon = new google.maps.Polygon({{
                    paths: polygonPoints,
                    map: map,
                    strokeColor: polygonColor,
                    fillColor: polygonColor
                }});

                items.push(polygon);

                // Get Center
                var polyBounds = new google.maps.LatLngBounds();
                for ( j = 0; j < polygonPoints.length; j++) {{
                    polyBounds.extend(polygonPoints[j]);
                }}

                if ( mapItem.InfoWindow != null ) {{
                    google.maps.event.addListener(polygon, 'click', (function (polygon, i) {{
                        return function () {{
                            infoWindow.setContent( $('<div/>').html(mapItem.InfoWindow).text() );
                            infoWindow.setPosition(polyBounds.getCenter());
                            infoWindow.open(map);
                        }}
                    }})(polygon, i));
                }}
            }}

            return items;
        }}

        function setAllMap(markers, map) {{
            for (var i = 0; i < markers.length; i++) {{
                markers[i].setMap(map);
            }}
        }}

        function htmlDecode(input) {{
            var e = document.createElement('div');
            e.innerHTML = input;
            return e.childNodes.length === 0 ? """" : e.childNodes[0].nodeValue;
        }}

        function getNextPolygonColor() {{
            var color = 'FE7569';
            if ( polygonColors.length > polygonColorIndex ) {{
                color = polygonColors[polygonColorIndex];
                polygonColorIndex++;
            }} else {{
                color = polygonColors[0];
                polygonColorIndex = 1;
            }}
            return color;
        }}

        function adjustOverlappedMarkers() {{
            if (allMarkers.length > 1) {{
                for(i=0; i < allMarkers.length-1; i++) {{
                    var marker1 = allMarkers[i];
                    var pos1 = marker1.getPosition();
                    for(j=i+1; j < allMarkers.length; j++) {{
                        var marker2 = allMarkers[j];
                        var pos2 = marker2.getPosition();
                        if (pos1.equals(pos2)) {{
                            var newLat = pos1.lat() * (Math.random() * (max - min) + min);
                            var newLng = pos1.lng() * (Math.random() * (max - min) + min);
                            marker1.setPosition( new google.maps.LatLng(newLat,newLng) );
                        }}
                    }}
                }}
            }}
        }}
";

            var locationJson = location != null ?
                string.Format( "JSON.parse('{0}')", location.ToJson().Replace( Environment.NewLine, string.Empty ).Replace( "\\", "\\\\" ).EscapeQuotes().Replace( "\x0A", string.Empty ) ) : "null";

            var fencesJson = fences != null && fences.Any() ?
                string.Format( "JSON.parse('{0}')", fences.ToJson().Replace( Environment.NewLine, string.Empty ).Replace( "\\", "\\\\" ).EscapeQuotes().Replace( "\x0A", string.Empty ) ) : "null";

            var groupsJson = groups != null && groups.Any() ?
                string.Format( "JSON.parse('{0}')", groups.ToJson().Replace( Environment.NewLine, string.Empty ).Replace( "\\", "\\\\" ).EscapeQuotes().Replace( "\x0A", string.Empty ) ) : "null";

            var markerDefinedValueId = GetAttributeValue( AttributeKey.MapMarker ).AsIntegerOrNull();
            var marker = "M 0,0 C -2,-20 -10,-22 -10,-30 A 10,10 0 1,1 10,-30 C 10,-22 2,-20 0,0 z";

            if ( markerDefinedValueId != null )
            {
                marker = DefinedValueCache.Get( markerDefinedValueId.Value ).Description;
            }

            string mapScript = string.Format(
                mapScriptFormat,
                locationJson,       // 0
                fencesJson,         // 1
                groupsJson,         // 2
                styleCode,          // 3
                locationColor,      // 4
                polygonColors,      // 5
                groupColor,         // 6
                latitude,           // 7
                longitude,          // 8
                zoom,               // 9
                marker,             // 10
                maxZoomLevel,       // 11
                minZoomLevel,       // 12
                zoomThreshold,      // 13
                zoomAmount );       // 14

            ScriptManager.RegisterStartupScript( pnlMap, pnlMap.GetType(), "group-finder-map-script", mapScript, true );
        }

        private void ShowError( string message )
        {
            nbNotice.Heading = "Error";
            nbNotice.NotificationBoxType = NotificationBoxType.Danger;
            ShowMessage( message );
        }

        private void ShowWarning( string message )
        {
            nbNotice.Heading = "Warning";
            nbNotice.NotificationBoxType = NotificationBoxType.Warning;
            ShowMessage( message );
        }

        private void ShowMessage( string message )
        {
            nbNotice.Text = string.Format( "<p>{0}</p>", message );
            nbNotice.Visible = true;
        }

        #endregion Internal Methods

        /// <summary>
        /// A map item class specific to group finder
        /// </summary>
        public class FinderMapItem : MapItem
        {
            /// <summary>
            /// Gets or sets the information window.
            /// </summary>
            /// <value>
            /// The information window.
            /// </value>
            public string InfoWindow { get; set; }

            public string Color { get; set; }

            /// <summary>
            /// Initializes a new instance of the <see cref="FinderMapItem"/> class.
            /// </summary>
            /// <param name="location">The location.</param>
            public FinderMapItem( Location location )
                : base( location )
            {
            }

            public FinderMapItem()
                : base()
            {
            }
        }

        /// <summary>
        /// Handles the SelectedIndexChanged event of the ddlPageSize control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void ddlPageSize_SelectedIndexChanged( object sender, EventArgs e )
        {
            ShowResults();
        }

        protected void gGroupTypeLocation_RowDataBound( object sender, GridViewRowEventArgs e )
        {
            var dropDownList = e.Row.ControlsOfTypeRecursive<RockDropDownList>().FirstOrDefault();
            var groupType = e.Row.DataItem as GroupTypeCache;

            if ( dropDownList == null || groupType == null )
            {
                return;
            }

            dropDownList.Attributes.Add( "group-type-id", groupType.Id.ToString() );
            var locationTypeValues = groupType.LocationTypeValues;
            if ( locationTypeValues != null )
            {
                dropDownList.Items.Add( new ListItem( "All", string.Empty ) );
                foreach ( var locationTypeValue in locationTypeValues )
                {
                    dropDownList.Items.Add( new ListItem( locationTypeValue.Value, locationTypeValue.Id.ToString() ) );
                }

                if ( GroupTypeLocations != null && GroupTypeLocations.ContainsKey( groupType.Id ) )
                {
                    dropDownList.SelectedValue = GroupTypeLocations[groupType.Id].ToString();
                }
            }
        }

        private Dictionary<int, int> GroupTypeLocations { get; set; }

        protected void lLocationList_SelectedIndexChanged( object sender, EventArgs e )
        {
            var dropDownList = sender as RockDropDownList;
            if ( dropDownList == null )
            {
                return;
            }

            var groupTypeId = dropDownList.Attributes["group-type-id"].AsIntegerOrNull();
            if ( groupTypeId == null )
            {
                return;
            }

            var groupTypeLocations = GroupTypeLocations;
            if ( groupTypeLocations == null )
            {
                groupTypeLocations = new Dictionary<int, int>();
            }
            var groupTypeLocationId = dropDownList.SelectedValue.AsIntegerOrNull();
            if ( groupTypeLocationId == null )
            {
                groupTypeLocations.Remove( groupTypeId.Value );
            }
            else
            {
                groupTypeLocations.AddOrReplace( groupTypeId.Value, groupTypeLocationId.Value );
            }

            GroupTypeLocations = groupTypeLocations;
        }

        protected void lbShowAdditionalMapSettings_Click( object sender, EventArgs e )
        {
            dMapAdditionalSettings.Visible = !dMapAdditionalSettings.Visible;
        }

        protected void gSortFilters_RowDataBound( object sender, GridViewRowEventArgs e )
        {
            if ( e.Row.RowType != DataControlRowType.Header )
            {
                var lName = ( Literal ) e.Row.FindControl( "lName" );

                if ( e.Row.DataItem == null || lName == null )
                {
                    return;
                }

                var currentRow = ( KeyValuePair<int, string> ) e.Row.DataItem;

                if ( _groupTypeAttributeNames.ContainsKey( currentRow.Value ) )
                {
                    lName.Text = _groupTypeAttributeNames[currentRow.Value];
                }
                else
                {
                    lName.Text = currentRow.Value;
                }
            }
        }

        protected void gSortFilters_GridReorder( object sender, GridReorderEventArgs e )
        {
            var newFilterOrderList = new Dictionary<int, string>();

            // ElementAt is 0 index based, use OldIndex to retrieve value
            var oldEle = FilterOrder.ElementAt( e.OldIndex );
            // NewIndex is 0 based, but our keys are 1 based, so increment by 1 for the loop.
            var newIndexStart = e.NewIndex + 1;
            var i = 1;
            foreach ( var filter in FilterOrder )
            {
                if ( filter.Key == newIndexStart )
                {
                    newFilterOrderList.Add( i, ( e.NewIndex > e.OldIndex ) ? filter.Value : oldEle.Value );
                    i++;
                    newFilterOrderList.Add( i, ( e.NewIndex > e.OldIndex ) ? oldEle.Value : filter.Value );
                    i++;
                }
                else if ( filter.Value != oldEle.Value )
                {
                    newFilterOrderList.Add( i, filter.Value );
                    i++;
                }
            }

            FilterOrder = newFilterOrderList;

            BindFilterOrder();

            upOrderFilters.Update();
        }

        private const string keyValuePairEntrySeparator = "|";
        private const string keyValuePairInternalSeparator = "^";

        /// <summary>
        /// Creates a formatted string representation of the provided dictionary where the keys and values are Uri-encoded.
        /// </summary>
        /// <returns></returns>
        public string ToKVPString( IDictionary<int, string> dictionary )
        {
            var sb = new StringBuilder();

            var isFirstItem = true;

            foreach ( var kvp in dictionary )
            {
                if ( isFirstItem )
                {
                    isFirstItem = false;
                }
                else
                {
                    sb.Append( keyValuePairEntrySeparator );
                }

                sb.Append( kvp.Key );
                sb.Append( keyValuePairInternalSeparator );
                sb.Append( kvp.Value.ToStringSafe() );
            }

            return sb.ToString();
        }

        #region Supporting Classes

        /// <summary>
        /// This POCO will be used to hold project opportunity instances (GroupLocationSchedules), against which we'll perform filtering.
        /// <para>
        /// It will also provide convenience properties for the final, Lava class, <see cref="SignUpFinder.Project"/> to easily pick from.
        /// </para>
        /// </summary>
        private class Opportunity : RockDynamic
        {
            public Rock.Model.Group Group { get; set; }

            public Project Project { get { return this.ToProject(); } }

            public Location Location { get; set; }

            public Schedule Schedule { get; set; }

            public DateTime? NextStartDateTime { get; set; }

            public string ScheduleName { get; set; }

            public int? SlotsMin { get; set; }

            public int? SlotsDesired { get; set; }

            public int? SlotsMax { get; set; }

            public int ParticipantCount { get; set; }

            public DbGeography GeoPoint { get; set; }

            public string ProjectName
            {
                get
                {
                    return this.Group?.Name;
                }
            }

            public string Description
            {
                get
                {
                    return this.Group?.Description;
                }
            }

            public bool ScheduleHasFutureStartDateTime
            {
                get
                {
                    return this.Schedule != null
                        && this.Schedule.NextStartDateTime.HasValue
                        && this.Schedule.NextStartDateTime.Value >= RockDateTime.Now;
                }
            }

            public string FriendlySchedule
            {
                get
                {
                    if ( !this.ScheduleHasFutureStartDateTime )
                    {
                        return "No upcoming occurrences.";
                    }

                    var friendlySchedule = this.NextStartDateTime.Value.ToString( "dddd, MMM d h:mm tt" );

                    if ( this.NextStartDateTime.Value.Year != RockDateTime.Now.Year )
                    {
                        friendlySchedule = $"{friendlySchedule} ({this.NextStartDateTime.Value.Year})";
                    }

                    return friendlySchedule;
                }
            }

            public int SlotsAvailable
            {
                get
                {
                    if ( !this.ScheduleHasFutureStartDateTime )
                    {
                        return 0;
                    }

                    // This more complex approach uses a dynamic/floating minuend:
                    // 1) If the max value is defined, use that;
                    // 2) Else, if the desired value is defined, use that;
                    // 3) Else, if the min value is defined, use that;
                    // 4) Else, use int.MaxValue (there is no limit to the slots available).
                    //var minuend = this.SlotsMax.GetValueOrDefault() > 0
                    //    ? this.SlotsMax.Value
                    //    : this.SlotsDesired.GetValueOrDefault() > 0
                    //        ? this.SlotsDesired.Value
                    //        : this.SlotsMin.GetValueOrDefault() > 0
                    //            ? this.SlotsMin.Value
                    //            : int.MaxValue;

                    // Simple approach:
                    // 1) If the max value is defined, subtract participant count from that;
                    // 2) Otherwise, use int.MaxValue (there is no limit to the slots available).
                    var available = int.MaxValue;
                    if ( this.SlotsMax.GetValueOrDefault() > 0 )
                    {
                        available = this.SlotsMax.Value - this.ParticipantCount;
                    }

                    return available < 0 ? 0 : available;
                }
            }

            /// <summary>
            /// Converts an <see cref="Opportunity"/> to a <see cref="SignUpFinder.Project"/> for display within the lava results template.
            /// </summary>
            /// <param name="projectDetailPageUrl">The project detail page URL for this <see cref="SignUpFinder.Project"/>.</param>
            /// <param name="registrationPageUrl">The registration page URL for this <see cref="SignUpFinder.Project"/>.</param>
            /// <returns>a <see cref="SignUpFinder.Project"/> instance for display within the lava results template.</returns>
            public Project ToProject()
            {
                int? availableSpots = null;
                if ( this.SlotsAvailable != int.MaxValue )
                {
                    availableSpots = this.SlotsAvailable;
                }

                var showRegisterButton = this.ScheduleHasFutureStartDateTime
                    &&
                    (
                        !availableSpots.HasValue
                        || availableSpots.Value > 0
                    );

                string mapCenter = null;
                if ( this.Location.Latitude.HasValue && this.Location.Longitude.HasValue )
                {
                    mapCenter = $"{this.Location.Latitude.Value},{this.Location.Longitude.Value}";
                }
                else
                {
                    var streetAddress = this.Location.GetFullStreetAddress();
                    if ( !string.IsNullOrWhiteSpace( streetAddress ) )
                    {
                        mapCenter = streetAddress;
                    }
                }

                return new Project
                {
                    Name = this.ProjectName,
                    Description = this.Description,
                    ScheduleName = this.ScheduleName,
                    FriendlySchedule = this.FriendlySchedule,
                    AvailableSpots = availableSpots,
                    ShowRegisterButton = showRegisterButton,
                    MapCenter = mapCenter,
                    GroupId = this.Group.Id,
                    LocationId = this.Location.Id,
                    ScheduleId = this.Schedule.Id,
                    GroupIdKey = this.Group.IdKey,
                    LocationIdKey = this.Location.IdKey,
                    ScheduleIdKey = this.Schedule.IdKey
                };
            }
        }

        /// <summary>
        /// This POCO will be passed to the results Lava template, one instance for each project opportunity (GroupLocationSchedule).
        /// </summary>
        /// <seealso cref="Rock.Utility.RockDynamic" />
        private class Project : RockDynamic
        {
            public string Name { get; set; }

            public string Description { get; set; }

            public string ScheduleName { get; set; }

            public string FriendlySchedule { get; set; }

            public int? AvailableSpots { get; set; }

            public bool ShowRegisterButton { get; set; }

            public string MapCenter { get; set; }

            public string ProjectDetailPageUrl { get; set; }

            public string RegisterPageUrl { get; set; }

            public int GroupId { get; set; }

            public int LocationId { get; set; }

            public int ScheduleId { get; set; }

            public string GroupIdKey { get; set; }

            public string LocationIdKey { get; set; }

            public string ScheduleIdKey { get; set; }
        }

        #endregion

    }
}
