![KFSBanner](https://user-images.githubusercontent.com/81330042/121249398-2bb06080-c86a-11eb-9795-8c0dfb0a7693.jpg)

# Advanced Group Finder
*Tested/Supported in Rock version:  8.0-12.0*   
*Created:  6/8/2021*  
*Rock Shop Plugin: https://www.rockrms.com/Plugin/161*

## Summary

Our Group finder is a significantly modified version of the core Rock version. Including but not limited to these features:

- Added ability to set default location so when address is enabled a campus can be selected and results auto load.
- Added single select setting so that multiselect filters will be a drop down.
- Added ability to set filters by url parameter.
- Added an override setting for PersonGuid mode that enables search options.
- Added postal code search capability.
- Added Collapsible filters.
- Added Custom Sorting based on Attribute Filter.
- Added ability to hide attribute values from the search panel.
- Added Custom Schedule Support to Day of Week Filters.
- Added Keyword search to search name or description of groups.
- Added an additional setting to include Pending members in Over Capacity checking.

**Collapsible Filters Screenshots**

<img width="439" alt="Filters 1" src="https://user-images.githubusercontent.com/81330042/122945404-2c52f780-d33e-11eb-8ca5-b9882b3dbbe5.png">
<img width="442" alt="Filters 2" src="https://user-images.githubusercontent.com/81330042/122945462-37a62300-d33e-11eb-8f3a-7047732f5746.png">
<img width="515" alt="Filters 3" src="https://user-images.githubusercontent.com/81330042/122945514-3f65c780-d33e-11eb-9f7a-c3a4d1be0fdd.png">



Quick Links:

- [What's New](#whats-new)
- [Group Finder Configuration](#group-finder-configuration)
- [Block Properties](#block-properties)



## What's New

The following new goodness will be added to your Rock install with this plugin:

- **New Block** Group Finder KFS

**Note**: This block will not be added automatically to a page. You will need to create a new page or add this block to an existing page.



## Group Finder Configuration



![Filter_Settings](https://user-images.githubusercontent.com/81330042/121250202-10922080-c86b-11eb-97d0-36489ee90f9b.png)

> <span style="padding-left: 30px; margin-right: 10px; width: .8em;background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">&nbsp;&nbsp;1&nbsp;&nbsp;</span> **Group Type** The type of groups to look for.
>
> <span style="padding-left: 30px; margin-right: 10px; width: .8em;background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">&nbsp;&nbsp;2&nbsp;&nbsp;</span> **Geofence Group Type** An optional group type that contains groups with geographic boundary (fence).  If specified, user will be prompted for their address and only groups that are located in the same geographic boundary (as defined by one or more groups of this type) will be displayed.
>
> <span style="padding-left: 30px; margin-right: 10px; width: .8em;background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">&nbsp;&nbsp;3&nbsp;&nbsp;</span> **Day of the Week Filter Label** The text above the day of the week filter
>
> <span style="padding-left: 30px; margin-right: 10px; width: .8em;background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">&nbsp;&nbsp;4&nbsp;&nbsp;</span> **Time of Day Filter Label** The text above the time of day filter
>
> <span style="padding-left: 30px; margin-right: 10px; width: .8em;background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">&nbsp;&nbsp;5&nbsp;&nbsp;</span> **Campus Filter Label** The text above the campus filter
>
> <span style="padding-left: 30px; margin-right: 10px; width: .8em;background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">&nbsp;&nbsp;6&nbsp;&nbsp;</span>**Postal Code Label** The text above the postal code filter
>
> <span style="padding-left: 30px; margin-right: 10px; width: .8em;background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">&nbsp;&nbsp;7&nbsp;&nbsp;</span>**Keyword Label** The text above the keyword filter
>
> <span style="padding-left: 30px; margin-right: 10px; width: .8em;background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">&nbsp;&nbsp;8&nbsp;&nbsp;</span>**Filter Button Text** When using collapsible filters, what the dropdown button says on it.
>
> <span style="padding-left: 30px; margin-right: 10px; width: .8em;background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">&nbsp;&nbsp;9&nbsp;&nbsp;</span>**More Filters Button Text** When using Hide Filters on Initial Load, what the dropdown button says on it.
>
> <span style="padding-left: 30px; margin-right: 10px; width: .8em;background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">&nbsp;&nbsp;10&nbsp;&nbsp;</span>**Display Day of Week Filter** Flag indicating if and how the Day of the Week filter should be displayed to filter groups with 'Weekly' schedules.
>
> <span style="padding-left: 30px; margin-right: 10px; width: .8em;background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">&nbsp;&nbsp;11&nbsp;&nbsp;</span>**Display Time of Day Filter** Display a Time of Day filter to filter groups with 'Weekly' schedules.
>
> <span style="padding-left: 30px; margin-right: 10px; width: .8em;background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">&nbsp;&nbsp;12&nbsp;&nbsp;</span>**Display Campus Filter** Display the campus filter
>
> <span style="padding-left: 30px; margin-right: 10px; width: .8em;background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">&nbsp;&nbsp;13&nbsp;&nbsp;</span>**Enable Campus Context** If the page has a campus context its value will be used as a filter
>
> <span style="padding-left: 30px; margin-right: 10px; width: .8em;background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">&nbsp;&nbsp;14&nbsp;&nbsp;</span>**Enable Postal Code Search** Set to yes to enable simple Postal code search instead of full address.
>
> <span style="padding-left: 30px; margin-right: 10px; width: .8em;background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">&nbsp;&nbsp;15&nbsp;&nbsp;</span>**Display Keyword Filter** Display the Keyword filter
>
> <span style="padding-left: 30px; margin-right: 10px; width: .8em;background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">&nbsp;&nbsp;16&nbsp;&nbsp;</span>**Display Attribute Filters** The group attributes that should be available for user to filter results by.
>
> <span style="padding-left: 30px; margin-right: 10px; width: .8em;background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">&nbsp;&nbsp;17&nbsp;&nbsp;</span>**Hide Filters on Initial Load** Hide these filter controls under a collapsible panel for user on first load.
>
> <span style="padding-left: 30px; margin-right: 10px; width: .8em;background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">&nbsp;&nbsp;18&nbsp;&nbsp;</span>**Custom Sort from Attribute** Select an attribute to sort by if a group contains multiple of the selected attribute filter options.
>
> <span style="padding-left: 30px; margin-right: 10px; width: .8em;background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">&nbsp;&nbsp;19&nbsp;&nbsp;</span>**Hide Attribute Filter Values** The group attribute values that you would like to hide from the filter options. This could be used to hide internal attributes used for reporting.



## Block Properties



![Block_Properties](https://user-images.githubusercontent.com/81330042/121250608-8a2a0e80-c86b-11eb-8ad0-87623234f8b7.png)

> <span style="padding-left: 30px; margin-right: 10px; width: .8em;background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">&nbsp;&nbsp;1&nbsp;&nbsp;</span> **Name** Name of Block
>
> <span style="padding-left: 30px; margin-right: 10px; width: .8em;background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">&nbsp;&nbsp;2&nbsp;&nbsp;</span>**Allow Search in PersonGuid Mode** When set to yes, PersonGuid mode will allow you to change filters and search in that mode for that person.  Generally used on an internal group finder page. The PersonGuid must be passed as a URL parameter.
>
> <span style="padding-left: 30px; margin-right: 10px; width: .8em;background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">&nbsp;&nbsp;3&nbsp;&nbsp;</span>**Auto Load** When set to yes, all results will be loaded to begin.
>
> <span style="padding-left: 30px; margin-right: 10px; width: .8em;background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">&nbsp;&nbsp;4&nbsp;&nbsp;</span>**Collapse Filters on Search** When set to yes, all filters will be collapsed into a single 'Filters' dropdown.
>
> <span style="padding-left: 30px; margin-right: 10px; width: .8em;background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">&nbsp;&nbsp;5&nbsp;&nbsp;</span>**Default Location** The campus address that should be used as fallback for the search criteria.
>
> <span style="padding-left: 30px; margin-right: 10px; width: .8em;background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">&nbsp;&nbsp;6&nbsp;&nbsp;</span>**Hide Overcapacity Groups** When set to yes, groups that are at capacity or whose default GroupTypeRole are at capacity are hidden.
>
> <span style="padding-left: 30px; margin-right: 10px; width: .8em;background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">&nbsp;&nbsp;7&nbsp;&nbsp;</span>**Overcapacity Groups include Pending** When set to yes, the Hide Overcapacity Groups setting also takes into account pending members.
>
> <span style="padding-left: 30px; margin-right: 10px; width: .8em;background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">&nbsp;&nbsp;8&nbsp;&nbsp;</span>**Show All Groups** When set to yes, all groups will show including those where Is Public is set to false.  This is most often used on a staff internal page.
>
> <span style="padding-left: 30px; margin-right: 10px; width: .8em;background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">&nbsp;&nbsp;9&nbsp;&nbsp;</span>**Single Select Campus Filter** When set to yes, the campus filter will be a drop down instead of checkbox.
