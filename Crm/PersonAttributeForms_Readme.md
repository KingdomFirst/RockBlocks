![KFSBanner (4)](https://user-images.githubusercontent.com/81330042/118964662-5a9f7a80-b92d-11eb-88a2-aa5018457f7b.jpg)

# Person Attributes Form Advanced Block
*Tested/Supported in Rock version:  8.0-13.0*   
*Created:  11/20/2018*  
*Updated:  8/9/2022*   
*Rock Shop Plugin: https://www.rockrms.com/Plugin/101*

## Summary

This block is an advanced version of the core Rock Person Attributes Form block. It adds the following functions:

- Ability to use Person Fields in the forms
- Ability to pass a Connection Request to the workflow upon form completion
- Ability to add a Person to a Group upon form completion
- Ability to create a Connection Request upon form completion



Quick Links:

- [What's New](#whats-new)

- [Using the Block](#using-the-block)

- [Block Settings](#block-settings)

- [Block Properties](#block-properties)



## What's New

The following new goodness will be added to your Rock install with this plugin:

- Added ability to run this block in **Person Mode** which can be run as any person with guid in the url, family members, or only logged in users. See [Block Properties](#block-properties) for more information.
- Added an option to **Display Family Member Picker** to work in tandem with this new mode.



## Using the Block

**This block requires a person to be logged in for them to complete a form.**

Each form you create in the block will appear to the user as another form page. If you have 2 forms, they will complete the first form, hit next, complete the second form, and hit finish.

Any Person Attributes you wish to access with this block must be set up under Admin Tools > General Settings > Person Attributes. This block will allow people to edit their attributes regardless of the attribute permissions.

#### Groups

Imagine you have a youth group that will be going to an event in your community. There is no cost so Event Registration feels too complicated. You really just need some basic information and to add them to a group. 

You can set up this block to require them to update their contact information and any person attributes like emergency contact information or shirt size. With this block, you can use the group settings to allow you to pass a Group Guid or Group Id to the block in the URL. You can also specify a specific group in the block settings and not need to pass the information in the URL. After the user completes the form, their person fields and attributes will be updated and they will be added to the group. You can also launch a workflow using the new Group Member as the entity to send welcome emails, notify group leaders, etc.

#### Connections

Wanting to create a volunteer application? This block is your friend! There are 2 ways you can use this block with Connections. 

The first way is to pass the Connection Opportunity Id in URL to this block. Your new volunteer then fills out your form with your choice of person fields and attributes. Once they submit their form, their person record is updated and a connection request is created for them within the connection opportunity. You can launch a workflow with the newly created connection request as the entity to send follow up emails, launch other workflows, etc.

The second way to to pass a Connection Request Id in the URL to this block. Doing this works with the Workflow setting on the block and sets the entity of the workflow to that connection request. When a new connection request is created, email the volunteer a link to this form with the connection request id passed in the URL. The volunteer completes their application and the connection request is passed to the workflow selected in the block settings. This follow up workflow can be built do whatever is needed for your process. You may want to notify the connector that their application is complete, launch another workflow to contact their references and send a thank you email to the volunteer.

The possibilities are endless. Want to automate your process for Baptisms? Need a new process for managing applications for potential small group leaders? Gathering applications for a missions trip? Because this block ties in with groups, connections and workflows, you are limited only by your creativity.



## Block Settings

![EditBlock1 (1)](https://user-images.githubusercontent.com/81330042/118964726-6db24a80-b92d-11eb-8e4b-05b12a81410b.png)


> <span style="padding-left: 30px; margin-right: 10px; width: .8em;background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">&nbsp;&nbsp;1&nbsp;&nbsp;</span> **Display Progress Bar** Displays a progress bar to the user indicating how far along they are in filling out multiple forms. Not shown when there is only one form.
>
> <span style="padding-left: 30px; margin-right: 10px; width: .8em;background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">&nbsp;&nbsp;2&nbsp;&nbsp;</span>**Workflow** An optional workflow to launch after the person has filled out all forms.
>
> <span style="padding-left: 30px; margin-right: 10px; width: .8em;background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">&nbsp;&nbsp;3&nbsp;&nbsp;</span>**Done Page** An optional page to redirect users to after they have completed all the forms.
>
> <span style="padding-left: 30px; margin-right: 10px; width: .8em;background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">&nbsp;&nbsp;4&nbsp;&nbsp;</span>**Save Values** Determines if values should be save each time the user progresses to the next form or not saved until the end. An advantage of saving them on each form, saved values can then be used in the header and footer of following forms using Lava.
>
> <span style="padding-left: 30px; margin-right: 10px; width: .8em;background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">&nbsp;&nbsp;5&nbsp;&nbsp;</span>**Workflow Entity** The entity that should be used to initiate the workflow. Options are Person, Connection Request, or Group Member. See notes below for details on using Workflow Entities.
>
> <span style="padding-left: 30px; margin-right: 10px; width: .8em;background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">&nbsp;&nbsp;6&nbsp;&nbsp;</span>**Confirmation Text** The message to display after all forms are completed if Done Page is not set.
**Workflow Entities**

- If the block is passed a valid Connection Opportunity Id in the URL, a new Connection Request will be created in that opportunity when the form is completed. You can use that new Connection Request as the Workflow Entity.

- If the block is passed a valid Connection Request Id in the URL, that Connection Request can be used as the Workflow Entity.

- If the block is passed a valid Group Id or Guid in the URL, the person will be added to the group when the form is completed. That new Group Member can be used as the Workflow Entity.

- You can always use the Person as the Workflow Entity.



![EditBlock2 (1)](https://user-images.githubusercontent.com/81330042/118964779-815db100-b92d-11eb-9991-cd3e2f996a82.png)




> <span style="padding-left: 30px; margin-right: 10px; width: .8em;background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">&nbsp;&nbsp;7&nbsp;&nbsp;</span>**Form Title** Each form can have a unique title. {{ Lava }}
>
> <span style="padding-left: 30px; margin-right: 10px; width: .8em;background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">&nbsp;&nbsp;8&nbsp;&nbsp;</span>**Form Header** An optional HTML header that is unique to each form. {{ Lava }}
>
> <span style="padding-left: 30px; margin-right: 10px; width: .8em;background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">&nbsp;&nbsp;9&nbsp;&nbsp;</span>**Adding Fields** Each form may have as many fields as you like
>
> <span style="padding-left: 30px; margin-right: 10px; width: .8em;background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">&nbsp;&nbsp;10&nbsp;&nbsp;</span>**Form Footer** An optional HTML footer that is unique to each form. {{ Lava }}
>
> <span style="padding-left: 30px; margin-right: 10px; width: .8em;background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">&nbsp;&nbsp;11&nbsp;&nbsp;</span>**Fields** The label for each form field. If you would like to reorder field, use the 3 bar handle next to the field label.
>
> <span style="padding-left: 30px; margin-right: 10px; width: .8em;background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">&nbsp;&nbsp;12&nbsp;&nbsp;</span>**Source** Displays whether this is a Person Field or a Person Attribute
>
> <span style="padding-left: 30px; margin-right: 10px; width: .8em;background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">&nbsp;&nbsp;13&nbsp;&nbsp;</span>**Type** Displays the field type for the Person Field or Attribute. This cannot be changed from within this block but it will be helpful for knowing how the field will display on your form.
>
> <span style="padding-left: 30px; margin-right: 10px; width: .8em;background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">&nbsp;&nbsp;14&nbsp;&nbsp;</span>**Use Current Value** Shows if the field is set to use the Person's current value for this field.
>
> <span style="padding-left: 30px; margin-right: 10px; width: .8em;background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">&nbsp;&nbsp;15&nbsp;&nbsp;</span>**Required** Shows if the field is required for form submission.
>
> <span style="padding-left: 30px; margin-right: 10px; width: .8em;background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">&nbsp;&nbsp;16&nbsp;&nbsp;</span>**Add Form** You may add as many forms as needed to this block. Forms will display in the order shown.


## Block Properties

![BlockProperties](https://user-images.githubusercontent.com/2990519/183775785-c1d960b9-7a68-41dd-8f39-156d6022030e.jpg)

> <span style="padding-left: 30px; margin-right: 10px; width: .8em;background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">&nbsp;&nbsp;1&nbsp;&nbsp;</span>**Display Family Member Picker** Should we show the family member picker on the form? (Note: this will only display in Family Members or Anyone "Person Mode".)
>
> <span style="padding-left: 30px; margin-right: 10px; width: .8em;background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">&nbsp;&nbsp;2&nbsp;&nbsp;</span>**Display SMS Checkbox on Mobile Phone** Should we show the SMS checkbox when a mobile phone is displayed on the form? 
>
> <span style="padding-left: 30px; margin-right: 10px; width: .8em;background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">&nbsp;&nbsp;3&nbsp;&nbsp;</span>**Person Mode** You can use this block to edit other person's information by passing a Person Guid via a URL Parameter `?Person=<guid>`, this setting narrows the option down for security purposes, you can allow family members, any person guid, or the logged in user only. The default value is "Family Members" so you can use it with the "Display Family Member Picker" option above.
>
> <span style="padding-left: 30px; margin-right: 10px; width: .8em;background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">&nbsp;&nbsp;4&nbsp;&nbsp;</span>**Allow Connection Opportunity** Determines if a URL parameter of OpportunityId should be evaluated when complete. 
>
> <span style="padding-left: 30px; margin-right: 10px; width: .8em;background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">&nbsp;&nbsp;5&nbsp;&nbsp;</span>**Allow Group Membership** Determines if a URL parameter of GroupGuid or GroupId should be evaluated when complete
>
> <span style="padding-left: 30px; margin-right: 10px; width: .8em;background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">&nbsp;&nbsp;6&nbsp;&nbsp;</span>**Enable Passing Group Id** If enabled, allows the passing of the GroupId instead of the GroupGuid
>
> <span style="padding-left: 30px; margin-right: 10px; width: .8em;background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">&nbsp;&nbsp;7&nbsp;&nbsp;</span>**Allowed Group Types** This setting restricts which types of groups a person can be added to, however selecting a specific group via the Group setting will override this restriction.
>
> <span style="padding-left: 30px; margin-right: 10px; width: .8em;background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">&nbsp;&nbsp;8&nbsp;&nbsp;</span>**Group** Optional group to add the person to. If omitted, the group's Guid should be passed via the query string
>
> <span style="padding-left: 30px; margin-right: 10px; width: .8em;background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">&nbsp;&nbsp;9&nbsp;&nbsp;</span>**Group Member Status** The group member status to use when adding person to group
