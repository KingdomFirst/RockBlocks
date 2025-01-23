![Kingdom First Solutions](../.screenshots/KFSBanner.jpg)

# Advanced Group Finder
*Tested/Supported in Rock version:  8.0-16.1*   
*Created:  6/8/2021*   
*Updated: 1/17/2024*   
*Rock Shop Plugin: https://www.rockrms.com/Plugin/161*

## Quick Links
- [Summary](#summary)
- [What's New](#whats-new)
- [Group Finder Configuration](#Group-Finder-Configuration)
- [Block Properties](#block-properties)

## Summary

Our Group finder is a significantly modified version of the core Rock version. Including but not limited to these features:

- Added ability to set default location so when address is enabled a campus can be selected and results auto load.
- Added single select campus filter setting so that the campus multiselect filter will be a drop down instead.
- Added ability to set filters by url parameter.
- Added an override setting for PersonGuid mode that enables search options.
- Added postal code search capability.
- Added Collapsible filters.
- Added Custom Sorting based on Attribute Filter.
- Added ability to hide attribute values from the search panel.
- Added Custom Schedule Support to Day of Week Filters.
- Added Keyword search to search name or description of groups.
- Added an additional setting to include Pending members in Over Capacity checking.
- Added a setting to override groups Is Public setting to show on the finder.
- Added ability to display Over Capacity groups with a filter.
- Added Auto Load Filter capability on value selection.
- Added ability to sort how filters are displayed.
- Added ability to load Group/Sign Up Opportunities into finder.

<div style="page-break-after: always;"></div>

**Collapsible Filters Screenshots**

![Filters 1](../.screenshots/GroupFinder/ColllapsibleFilters_1.png)  
![Filters 2](../.screenshots/GroupFinder/ColllapsibleFilters_2.png)  
![Filters 3](../.screenshots/GroupFinder/ColllapsibleFilters_3.png)
<div style="page-break-after: always;"></div>

## What's New

The following new goodness will be added to your Rock install with this plugin:

- **New Block** Group Finder KFS

**Note**: This block will not be added automatically to a page. You will need to create a new page or add this block to an existing page.

## Group Finder Configuration
![Filter Settings](../.screenshots/GroupFinder/Filter_Settings.jpg)
<div style="page-break-after: always;"></div>

| | |
| --- | ---- |
| <span style="width: 3em; height: 3em; line-height: 3em; background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">1</span> | **Group Type** The type of groups to look for. |
| <span style="width: 3em; height: 3em; line-height: 3em; background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">2</span> | **Overcapacity Groups Handling** When set to Hide, groups that are at capacity or whose default GroupTypeRole are at capacity are hidden. If Display as Filter is chosen a toggle will show up to show/hide groups that are at capacity. |
| <span style="width: 3em; height: 3em; line-height: 3em; background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">3</span> | **Geofence Group Type** An optional group type that contains groups with geographic boundary (fence). If specified, user will be prompted for their address, and only groups that are located in the same geographic boundary ( as defined by one or more groups of this type ) will be displayed. |
| <span style="width: 3em; height: 3em; line-height: 3em; background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">4</span> | **Location Types** The text above the day of the week filter. |
| <span style="width: 3em; height: 3em; line-height: 3em; background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">5</span> | **Day of the Week Filter Label** The text above the day of the week filter. |
| <span style="width: 3em; height: 3em; line-height: 3em; background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">6</span> | **Time of Day Filter Label** The text above the time of day filter. |
| <span style="width: 3em; height: 3em; line-height: 3em; background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">7</span> | **Campus Filter Label** The text above the campus filter. |
| <span style="width: 3em; height: 3em; line-height: 3em; background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">8</span> | **Postal Code Label** The text above the postal code filter. |
| <span style="width: 3em; height: 3em; line-height: 3em; background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">9</span> | **Keyword Label** The text above the keyword filter. |
| <span style="width: 3em; height: 3em; line-height: 3em; background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">10</span> | **Filter Button Text** When using collapsible filters, what the dropdown button says on it. |
| <span style="width: 3em; height: 3em; line-height: 3em; background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">11</span> | **More Filters Button Text** When using Hide Filters on Initial Load, what the dropdown button says on it. |
| <span style="width: 3em; height: 3em; line-height: 3em; background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">12</span> | **Show Full Groups Label** The text above the Show Full Groups filter. |
| <span style="width: 3em; height: 3em; line-height: 3em; background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">13</span> | **Campus Types** The campus types to filter the list of campuses on. |
| <span style="width: 3em; height: 3em; line-height: 3em; background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">14</span> | **Campus Statuses** The campus statuses to filter the list of campuses on. |
| <span style="width: 3em; height: 3em; line-height: 3em; background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">15</span> | **Display Day of Week Filter** Flag indicating if and how the Day of the Week filter should be displayed to filter groups with 'Weekly' schedules. |
| <span style="width: 3em; height: 3em; line-height: 3em; background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">16</span> | **Display Time of Day Filter** Display a Time of Day filter to filter groups with 'Weekly' schedules. |
| <span style="width: 3em; height: 3em; line-height: 3em; background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">17</span> | **Display Campus Filter** Display the campus filter. |
| <span style="width: 3em; height: 3em; line-height: 3em; background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">18</span> | **Enable Campus Context** If the page has a campus context its value will be used as a filter. |
| <span style="width: 3em; height: 3em; line-height: 3em; background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">19</span> | **Enable Postal Code Search** Set to yes to enable simple Postal code search instead of full address. |
| <span style="width: 3em; height: 3em; line-height: 3em; background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">20</span> | **Require Postal Code** If Postal Code search is enabled, do you want to require a value to search (can be autofilled by default with a default location or logged in person's location). |
| <span style="width: 3em; height: 3em; line-height: 3em; background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">21</span> | **Display Keyword Filter** Display the Keyword filter. |
| <span style="width: 3em; height: 3em; line-height: 3em; background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">22</span> | **Display Attribute Filters** The group attributes that should be available for user to filter results by. |
| <span style="width: 3em; height: 3em; line-height: 3em; background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">23</span> | **Collapse Filters on Initial Load** Hide these filter controls under a collapsible panel for user on first load. |
| <span style="width: 3em; height: 3em; line-height: 3em; background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">24</span> | **Custom Sort from Attribute** Select an attribute to sort by if a group contains multiple of the selected attribute filter options. |
| <span style="width: 3em; height: 3em; line-height: 3em; background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">25</span> | **Hide Attribute Filter Values** The group attribute values that you would like to hide from the filter options. This could be used to hide internal attribute values used for reporting. |
| <span style="width: 3em; height: 3em; line-height: 3em; background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">26</span> | **Attributes in Keyword Search** The text-based group attributes that should be included in keyword search. |
| <span style="width: 3em; height: 3em; line-height: 3em; background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">27</span> | **Sort Filters** List of filters to display filters in the set order. If using 'Collapse Filters on Initial Load' the sort order is within each individual area. |

(See the [Rock Your Groups Manual](https://community.rockrms.com/documentation/bookcontent/7/217#groupfinder) for the rest of the setting descriptions.)
<div style="page-break-after: always;"></div>

## Block Properties

![Block Properties](../.screenshots/GroupFinder/Block_Properties.jpg)
<div style="page-break-after: always;"></div>

| | |
| --- | ---- |
| <span style="width: 3em; height: 3em; line-height: 3em; background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">1</span> | **Name** The name of the block. |
| <span style="width: 3em; height: 3em; line-height: 3em; background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">2</span> | **Add Group Opportunities** Add the merge field GroupOpportunities to the lava result with a custom object for Sign-up Opportunities. See [below](#custom-lava-properties) for the field properties. |
| <span style="width: 3em; height: 3em; line-height: 3em; background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">3</span> | **Allow Search in PersonGuid Mode** When set to yes, PersonGuid mode will allow you to change filters and search in that mode for that person.  Generally used on an internal group finder page. The PersonGuid must be passed as a URL parameter. |
| <span style="width: 3em; height: 3em; line-height: 3em; background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">4</span> | **Auto Filter Enabled** When set to yes, the various filters will automatically filter the results, whether it is on checkbox checked, selection made, text changed, etc. |
| <span style="width: 3em; height: 3em; line-height: 3em; background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">5</span> | **Auto Load** When set to yes, all results will be loaded to begin. |
| <span style="width: 3em; height: 3em; line-height: 3em; background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">6</span> | **Campus Statuses** Allows selecting which campus statuses to filter campuses by. |
| <span style="width: 3em; height: 3em; line-height: 3em; background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">7</span> | **Campus Types** Allows selecting which campus types to filter campuses by. |
| <span style="width: 3em; height: 3em; line-height: 3em; background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">8</span> | **Collapse Filters on Search** When set to yes, all filters will be collapsed into a single 'Filters' dropdown. |
| <span style="width: 3em; height: 3em; line-height: 3em; background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">9</span> | **Default Location** The campus address that should be used as fallback for the search criteria. |
| <span style="width: 3em; height: 3em; line-height: 3em; background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">10</span> | **Formatted Output Enabled Lava Commands** Choose what commands to enable in formatted output lava. |
| <span style="width: 3em; height: 3em; line-height: 3em; background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">11</span> | **Overcapacity Groups include Pending** When set to yes, the Hide Overcapacity Groups setting also takes into account pending members. |
| <span style="width: 3em; height: 3em; line-height: 3em; background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">12</span> | **Show All Groups** When set to yes, all groups will show including those where Is Public is set to false.  This is most often used on a staff internal page. |
| <span style="width: 3em; height: 3em; line-height: 3em; background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">13</span> | **Single Select Campus Filter** When set to yes, the campus filter will be a drop down instead of checkbox. |


## Advanced


**How to Set Attribute Filters via URL Parameters**

Attribute filters can be filled out using URL Parameters. If used in combination with the *Auto Load* setting, a search can be run automatically. To use this capability the parameters are in the format `filter_<attributeKey>_<fieldTypeId>=<value(s)>` (i.e. /page/236?filter_GroupCategory_16=609)


### Custom Lava Properties

**Group Opportunities**

When "Add Group Opportunities" has been selected in the Block Properties above, GroupOpportunities properties can be accessed by `{% for groupopportunity in GroupOpportunities %}{{ groupopportunity.[PropertyKey] }}{% endfor %}`.

| | |
| --- | ---- |
| **Group** | The Rock.Model.Group attached to the opportunity. |
| **Project** | A custom [Project](#project) model attached to the opportunity. |
| **Location** | The Rock.Model.Location attached to the opportunity. |
| **Schedule** | The Rock.Model.Schedule attached to the opportunity. |
| **NextStartDateTime** | Gets the next start time based on Rock.RockDateTime.Now. |
| **ScheduleName** | The Name of the Schedule. |
| **SlotsMin** | The Minimum Attendance from the sign up opportunity. |
| **SlotsDesired** | The Desired Attendance from the sign up opportunity. |
| **SlotsMax** | The Maximum Attendance from the sign up opportunity. |
| **ParticipantCount** | The total participants signed up to this opportunity. |
| **GeoPoint** | The GeoPoint (GeoLocation) for the location. |
| **ProjectName** | The Project Name. |
| **Description** | The Project Description. |
| **ScheduleHasFutureStartDateTime** | Boolean value to determine if the schedule has a future start date time. |
| **FriendlySchedule** | "No Upcoming Occurrences" or the date format: "dddd, MMM d h:mm tt" with optional year if it is displaying an opportunity for next year. |
| **SlotsAvailable** | A calculated value of SlotsMax-ParticipantCount for quicker use. |

<a name="project">**Project**</a>

Sub-properties of the Group Opportunity above under the `Project` property.

| | |
| --- | ---- |
| **Group** | The Rock.Model.Group attached to the opportunity. |
| **Name** | The Project Name. |
| **Description** | The Project Description. |
| **ScheduleName** | The Schedule Name. |
| **FriendlySchedule** | "No Upcoming Occurrences" or the date format: "dddd, MMM d h:mm tt" with optional year if it is displaying an opportunity for next year. |
| **AvailableSpots** | A calculated value of Available Spots for this project. |
| **ShowRegisterButton** | Boolean value if the project has a future start date time and available spots. |
| **MapCenter** | Latitude Longitude of the Location for centering the map or the street address of the Location if not geolocated. |
| **GroupId** | The Group Id. |
| **LocationId** | The Location Id. |
| **ScheduleId** | The Schedule Id. |
| **GroupIdKey** | The Group Hashed Id. |
| **LocationIdKey** | The Location Hashed Id. |
| **ScheduleIdKey** | The Schedule Hashed Id. |

<style>
  table {
    background-color: rgba(220, 220, 220, 0.4);
  }
  th {
    display: none;
  }
</style>