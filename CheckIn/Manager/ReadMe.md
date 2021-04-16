![Kingdom First Solutions](https://user-images.githubusercontent.com/81330042/115041371-09b4e600-9e98-11eb-90bf-c119cc4d5a18.png)


# Advanced Check-In Monitor
_Tested/Supported in Rock Version:  8.0-12.0_    
_Released:  12/31/2018_   
_Updated:  3/16/2020_   

## Summary

This block is an advanced version of the core Check-in Manager block. It adds the following functions:

- Ability to view and reprint labels
- Ability to move a person who is checked in to a new location or group
- Ability to check-out a person
- Ability to move all people in a location to another location or group



Quick Links:

- [What's New](#whats-new)
- [Using the Block](#using-the-block)
- [Block Properties](#block-properties)


## What's New

The following new goodness will be added to your Rock install with this plugin:

- **New Block**: *Check-in Manager Locations*



## Using the Block

**You will need to swap out the core Locations block on the Check-in Manager page for the KFS Locations block.**

On the check-in manager page, choose the check-in area, group and location that you would like to manage. When you get to the list of people who are checked in, you will notice a few differences. 



![Images/PersonOptions](https://user-images.githubusercontent.com/81330042/115042860-7a103700-9e99-11eb-8105-48df431b7246.png)


> <span style="padding-left: 30px; margin-right: 10px; width: .8em;background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">&nbsp;&nbsp;1</span> **Move All** Allows you to move everyone checked into this location to a new location and/or group. Imagine you have a spill on the carpet in one of your rooms. You want to move all the kids from one room to another so you can clean it up, the move all button gives you an easy way to do that. Your leaders will still have an accurate count and list of who should be in which room. 
>
> <span style="padding-left: 30px; margin-right: 10px; width: .8em;background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">&nbsp;&nbsp;2</span>**View Labels** Allows you to view all the labels that printed when someone checked it. It also gives you the ability to reprint their label with the same security code. This is very helpful when a child rips or spills on their label. You can print another label with the same security code so it will still match the parent's label at pickup. Printing relies on the device having a default printer. 
>
> <span style="padding-left: 30px; margin-right: 10px; width: .8em;background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">&nbsp;&nbsp;3</span>**Move** Allows you to move just one person to a new location and/or group. 
>
> <span style="padding-left: 30px; margin-right: 10px; width: .8em;background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">&nbsp;&nbsp;4</span>**Checkout** Allows you to checkout the person. 



**Reprinting Labels**



![LabelReprint](https://user-images.githubusercontent.com/81330042/115043176-d410fc80-9e99-11eb-863e-eb52e64563f7.png)

> <span style="padding-left: 30px; margin-right: 10px; width: .8em;background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">&nbsp;&nbsp;1</span> **Select Label** Select the label you would like to view and reprint from the dropdown menu
>
> <span style="padding-left: 30px; margin-right: 10px; width: .8em;background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">&nbsp;&nbsp;2</span> **Advanced Print Options** The settings in this panel only apply if your label ZPL does not include print density and label size. The core Rock labels include these values. This panel is hidden by default in the block settings.
>
> <span style="padding-left: 30px; margin-right: 10px; width: .8em;background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">&nbsp;&nbsp;3</span> **Print** If you would like to reprint the label, use this button. It will print to your devices default printer.



## Block Properties

![BlockPropertiesGeneral](https://user-images.githubusercontent.com/81330042/115043300-fc98f680-9e99-11eb-98d5-e98bd757d51e.png)

> <span style="padding-left: 30px; margin-right: 10px; width: .8em;background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">&nbsp;&nbsp;1</span> **Name** Name for your block instance
>
> <span style="padding-left: 30px; margin-right: 10px; width: .8em;background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">&nbsp;&nbsp;2</span>**Navigation Mode** Navigation and attendance counts can be grouped and displayed by either Group Type or Location. Select whichever makes the most sense for your organization.
>
> <span style="padding-left: 30px; margin-right: 10px; width: .8em;background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">&nbsp;&nbsp;3</span>**Check-in Type** The Check-in Area to display. This value can be overridden by a URL query string.
>
> <span style="padding-left: 30px; margin-right: 10px; width: .8em;background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">&nbsp;&nbsp;4</span>**Person Page** The page used to display the selected person's details.
>
> <span style="padding-left: 30px; margin-right: 10px; width: .8em;background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">&nbsp;&nbsp;5</span>**Area Select Page** The page to redirect the user to if a check-in area has not been configured or selected.
>
> <span style="padding-left: 30px; margin-right: 10px; width: .8em;background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">&nbsp;&nbsp;6</span>**Chart Style** Choose your preferred chart style
>
> <span style="padding-left: 30px; margin-right: 10px; width: .8em;background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">&nbsp;&nbsp;7</span>**Search By Code** If enabled, you may enter a security code in the search box and find the corresponding person
>
> <span style="padding-left: 30px; margin-right: 10px; width: .8em;background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">&nbsp;&nbsp;8</span>**Lookback Minutes** The number of minutes the chart will lookback
>
> <span style="padding-left: 30px; margin-right: 10px; width: .8em;background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">&nbsp;&nbsp;9</span>**Location Active** If enabled, only locations with currently active schedules will be displayed
>
> <span style="padding-left: 30px; margin-right: 10px; width: .8em;background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">&nbsp;&nbsp;10</span>**Close Occurrence** If KFS Load Balance Locations is being used in the check-in workflow, this will close the occurrence instead of the location



![BlockPropertiesAttendance](https://user-images.githubusercontent.com/81330042/115043390-15091100-9e9a-11eb-832d-ed4034639765.png)


> <span style="padding-left: 30px; margin-right: 10px; width: .8em;background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">&nbsp;&nbsp;11</span>**Show Delete** Indicates if the Delete button should be shown. Attendance records delete from this screen are permanently deleted from the database.
>
> <span style="padding-left: 30px; margin-right: 10px; width: .8em;background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">&nbsp;&nbsp;12</span>**Show Checkout** Indicates if the Checkout button should be shown. People checked out will still show as having attended but no longer count toward room totals in check-in manager.
>
> <span style="padding-left: 30px; margin-right: 10px; width: .8em;background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">&nbsp;&nbsp;13</span>**Show Move** Indicates if the Move buttons should be shown. This setting controls both the individual move button and the move all button.
>
> <span style="padding-left: 30px; margin-right: 10px; width: .8em;background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">&nbsp;&nbsp;14</span>**Include Group Move** Controls if the option to move people to a new group is displayed in the move popup. If set to no, you will only be able to move people to new locations.
>
> <span style="padding-left: 30px; margin-right: 10px; width: .8em;background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">&nbsp;&nbsp;15</span>**Show Print Label** Indicates if the View Label button is available to view and reprint labels



**Print Actions** settings will only apply if your label ZPL <u>does not</u> include print density, label width or label height. The core Rock labels <u>do</u> include those settings in the ZPL.

![BlockPropertiesPrint](https://user-images.githubusercontent.com/81330042/115043507-2fdb8580-9e9a-11eb-9199-001c2a5a25a8.png)

> <span style="padding-left: 30px; margin-right: 10px; width: .8em;background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">&nbsp;&nbsp;16</span> **Show Advanced Print Options** Indicates if the advanced print options should be displayed on the view labels popup
>
> <span style="padding-left: 30px; margin-right: 10px; width: .8em;background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">&nbsp;&nbsp;17</span>**Print Density** Default print density for a label reprint.
>
> <span style="padding-left: 30px; margin-right: 10px; width: .8em;background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">&nbsp;&nbsp;18</span>**Label Width** Default label width for a label reprint. 
>
> <span style="padding-left: 30px; margin-right: 10px; width: .8em;background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">&nbsp;&nbsp;19</span>**Label Height** Default label height for a label reprint. 

