![Kingdom First Solutions](../.screenshots/KFSBanner.jpg)

# Intacct Export to Journal 
_Tested/Supported in Rock Version:  13.0-14.0_    
_Released:  10/9/2018_   
_Updated:  6/27/2023_   

## Summary 

This plug in will allow you to create journal entries in Intacct for batches with the click of a button.

Quick Links:
- [What's New](#whats-new)
- [Configuration](#configuration)
    - [Intacct Configuration](#intacct-configuration)
    - [Rock Configuration](#rock-configuration)
- [Advanced Configuration](#advanced-configuration)

## What's New

The following new goodness will be added to your Rock install with this plugin:

- **New Page**: Intacct Projects (Finance > Administration > Intacct Projects)  
- **New Page**: Intacct Export (Finance > Functions > Intacct Export)  
- **New Block**: Batch to Journal (added to the Batch Detail Page on install)  
- **New Block**: Batches to Journal (added to the new Intacct Export Page on install)  
- **New Account Attributes**: There are a number of new Account attributes that control where transactions are posted in Intacct  
- **New Defined Type**: Financial Projects stores the Defined Values that designate what Project a Transaction should be associated with  
- **New Batch Attribute**: Date Exported 
- **New Financial Gateway Attributes:** There are 2 new Financial Gateway attributes to control how transaction fees are handled

## Configuration

There is configuration needed in Intacct. You may need to contact the Intacct Administrator for your organization for help with these steps.

<div style="page-break-after: always;"></div>

### **Intacct Configuration** 

**Create a new Role**

In Intacct, go to Company > Roles

![](../.screenshots/BatchToIntacct/IntacctCreateRoleName.png)

```
    Name: API Journal
    Description: Used to make Journals via API
```
After you save the Role, the next screen will allow you to assign subscriptions to the Role

![](../.screenshots/BatchToIntacct/IntacctCreateRoleApplication.png)

```
    Application Module: General Ledger
    Click on Permissions
```
<div style="page-break-after: always;"></div>

In the permissions window, grant All permissions for the General Ledger to the Role

![](../.screenshots/BatchToIntacct/IntacctCreateRolePermissions.png)

```
    Select the All radio button
```
Save to close the window

Then save your changes on the Role Subscriptions page
<div style="page-break-after: always;"></div>

**Create a new User**

Note: Creating users can cost extra in Intacct. Only create a new user if there are not any existing API or generic users that you can use.

Go to Company > Admin > Users to add a user

![](../.screenshots/BatchToIntacct/IntacctCreateUser.png)

```
    User Id: RockAPI
    Last name: API
    First name: Rock
    Email address: A valid email where you can receive the password email
    Contact name: Leave this blank to create a new contact automatically
    User name: Rock API
    User Type: Business Account
    Admin Privileges: Full
```
<div style="page-break-after: always;"></div>

**Assign User Role**

Find your new or existing API user in the Users page

![](../.screenshots/BatchToIntacct/IntacctAssignRoleEdit.png)

```
    Click the edit link next to your API user then go to the Role Information tab
```

![](../.screenshots/BatchToIntacct/IntacctAssignRoleSelect.png)

```
    In the blank drop down, select your API Journal Role
```
Save your changes
<div style="page-break-after: always;"></div>

**Create a new Employee**

Go to Company > Setup > Employees

Add a new Employee

![](../.screenshots/BatchToIntacct/IntacctCreateEmployee.png)

```
    Primary contact name: RockAPI (or existing API user)
```


### **Rock Configuration** 

After install, the Batch to Intacct block was added to your Batch Details page to enable exporting individual batches to Intacct. Also, a new Intacct Export page containing the Batches to Intacct block added under your Finances directory to enable exporting multiple batches at once. The export button will only show up if the batch Transaction and Control amounts match.

**Batch to Journal Block**

Located on your Batch Details page, the export button will only show up if the batch Transaction and Control amounts match.

![](../.screenshots/BatchToIntacct/BatchToJournalBlock.png)
<div style="page-break-after: always;"></div>

When the block is in Other Receipts mode, you will have options for Deposit To, Payment Method and Bank Account next to the Export to Intacct button.

![](../.screenshots/BatchToIntacct/BatchToJournalBlockOtherReceipts.png)
<div style="page-break-after: always;"></div>

You will need to configure the Batch to Journal block settings.

![](../.screenshots/BatchToIntacct/BatchToJournalBlockSettings.png)
<div style="page-break-after: always;"></div>

| | |
| --- | ---- |
| <span style="width: 3em; height: 3em; line-height: 3em; background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">1</span> | **Name** Block name. |
| <span style="width: 3em; height: 3em; line-height: 3em; background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">2</span> | **Journal Id** The Intacct Symbol of the Journal that the Entry should be posted to. (example: GJ) |
| <span style="width: 3em; height: 3em; line-height: 3em; background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">3</span> | **Button Text** Customize the text on the export button. |
| <span style="width: 3em; height: 3em; line-height: 3em; background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">4</span> | **Close Batch** Flag indicating if the Financial Batch should be closed in Rock when successfully posted to Intacct. |
| <span style="width: 3em; height: 3em; line-height: 3em; background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">5</span> | **Log Response** Flag indicating if the Intacct Response should be logged to the Batch Audit Log. |
| <span style="width: 3em; height: 3em; line-height: 3em; background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">6</span> | **Journal Memo Lava** Allows you to use Lava to control what is saved in the memo column of the export. Default: {{ Batch.Id }}: {{ Batch.Name }} |
| <span style="width: 3em; height: 3em; line-height: 3em; background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">7</span> | **Enable Debug** Outputs the object graph to help create your Lava syntax. |
| <span style="width: 3em; height: 3em; line-height: 3em; background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">8</span> | **Undeposited Funds Account** The GL Account Id to use when Other Receipt mode is being used with Undeposited Funds option selected. |
| <span style="width: 3em; height: 3em; line-height: 3em; background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">9</span> | **Sender Id** The permanent Web Services sender Id. |
| <span style="width: 3em; height: 3em; line-height: 3em; background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">10</span> | **Sender Password** The permanent Web Services sender password. |
| <span style="width: 3em; height: 3em; line-height: 3em; background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">11</span> | **Company Id** The Intacct company Id. This is the same information you use when you log into the Sage Intacct UI. |
| <span style="width: 3em; height: 3em; line-height: 3em; background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">12</span> | **User Id** The Intacct API user Id. This is the same information you use when you log into the Sage Intacct UI. |
| <span style="width: 3em; height: 3em; line-height: 3em; background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">13</span> | **User Password** he Intacct API password. This is the same information you use when you log into the Sage UI. |
| <span style="width: 3em; height: 3em; line-height: 3em; background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">14</span> | **Location Id** The optional Intacct Location Id. Add a location ID to log into a multi-entity shared company. Entities are typically different locations of a single company. |
| <span style="width: 3em; height: 3em; line-height: 3em; background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">15</span> | **Export Mode** Determines the type of object to create in Intacct. Selecting Journal Entry will result in creating journal entries of the type set in the Journal Id setting. Selecting Other Receipts will result in creating Other Receipts in the Cash Management area of Intacct. |
| <span style="width: 3em; height: 3em; line-height: 3em; background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">16</span> | **GL Account Grouping** Determines if debit and/or credit lines should be grouped and summed as follows in the resulting Intacct Journal Entry or Other Receipt:<ul><li>**Debit Accounts**: Company, Region, Super Fund, Cost Center Debit Number, Debit Account, Debit Account Sub, Fund Number, Project, Transaction Fee Account, and Location</li><li>**Credit Accounts**: Company, Region, Department, Super Fund, Cost Center Credit Number, Revenue Account, Revenue Account Sub, Fund Number, Project, and Location</li><li>**Financial Account Grouping**: Both credit and debit lines are grouped by Rock Financial Account, Project, and Transaction Fee Account.</li></ul> |

<div style="page-break-after: always;"></div>

**Batches to Journal Block**

The Batches to Journal block was added to a new page under your Finances heading named Intacct Export. It consists of a specialized grid of exportable batches with an export button at the bottom.

![](../.screenshots/BatchToIntacct/BatchesToJournalBlock.png)

When the block is in Other Receipts mode, you will have options for Deposit To, Payment Method and Bank Account next to the Export to Intacct button.

![](../.screenshots/BatchToIntacct/BatchesToJournalBlockOtherReceipts.png)
<div style="page-break-after: always;"></div>

You will need to configure the Batches to Journal block settings.

![](../.screenshots/BatchToIntacct/BatchesToJournalBlockSettings.png)
<div style="page-break-after: always;"></div>

| | |
| --- | ---- |
| <span style="width: 3em; height: 3em; line-height: 3em; background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">1</span> | **Name** Block name. |
| <span style="width: 3em; height: 3em; line-height: 3em; background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">2</span> | **Detail Page** The Financial Batch Detail page. |
| <span style="width: 3em; height: 3em; line-height: 3em; background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">3</span> | **Button Text** Customize the text on the export button. |
| <span style="width: 3em; height: 3em; line-height: 3em; background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">4</span> | **Months Back** Number of months back that batches should be loaded. This is helpful to prevent database timeouts if there are years of historical batches. |
| <span style="width: 3em; height: 3em; line-height: 3em; background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">5</span> | **Close Batch** Flag indicating if the Financial Batch should be closed in Rock when successfully posted to Intacct. |
| <span style="width: 3em; height: 3em; line-height: 3em; background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">6</span> | **Log Response** Flag indicating if the Intacct Response should be logged to the Batch Audit Log. |
| <span style="width: 3em; height: 3em; line-height: 3em; background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">7</span> | **Enable Debug** Outputs the object graph to help create your Lava syntax. |
| <span style="width: 3em; height: 3em; line-height: 3em; background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">8</span> | **Journal Id** The Intacct Symbol of the Journal that the Entry should be posted to. (example: GJ) |
| <span style="width: 3em; height: 3em; line-height: 3em; background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">9</span> | **Undeposited Funds Account** The GL Account Id to use when Other Receipt mode is being used with Undeposited Funds option selected. |
| <span style="width: 3em; height: 3em; line-height: 3em; background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">10</span> | **Journal Description Lava** Allows you to use Lava to control what is saved in the memo column of the export. Default: {{ Batch.Id }}: {{ Batch.Name }} |
| <span style="width: 3em; height: 3em; line-height: 3em; background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">11</span> | **Sender Id** The permanent Web Services sender Id. |
| <span style="width: 3em; height: 3em; line-height: 3em; background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">12</span> | **Sender Password** The permanent Web Services sender password. |
| <span style="width: 3em; height: 3em; line-height: 3em; background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">13</span> | **Company Id** The Intacct company Id. This is the same information you use when you log into the Sage Intacct UI. |
| <span style="width: 3em; height: 3em; line-height: 3em; background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">14</span> | **User Id** The Intacct API user Id. This is the same information you use when you log into the Sage Intacct UI. |
| <span style="width: 3em; height: 3em; line-height: 3em; background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">15</span> | **User Password** he Intacct API password. This is the same information you use when you log into the Sage UI. |
| <span style="width: 3em; height: 3em; line-height: 3em; background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">16</span> | **Location Id** The optional Intacct Location Id. Add a location ID to log into a multi-entity shared company. Entities are typically different locations of a single company. |
| <span style="width: 3em; height: 3em; line-height: 3em; background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">17</span> | **Export Mode** Determines the type of object to create in Intacct. Selecting Journal Entry will result in creating journal entries of the type set in the Journal Id setting. Selecting Other Receipts will result in creating Other Receipts in the Cash Management area of Intacct. |
| <span style="width: 3em; height: 3em; line-height: 3em; background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">18</span> | **GL Account Grouping** Determines if debit and/or credit lines should be grouped and summed as follows in the resulting Intacct Journal Entry or Other Receipt:<ul><li>**Debit Accounts**: Company, Region, Super Fund, Cost Center Debit Number, Debit Account, Debit Account Sub, Fund Number, Project, Transaction Fee Account, and Location</li><li>**Credit Accounts**: Company, Region, Department, Super Fund, Cost Center Credit Number, Revenue Account, Revenue Account Sub, Fund Number, Project, and Location</li><li>**Financial Account Grouping**: Both credit and debit lines are grouped by Rock Financial Account, Project, and Transaction Fee Account.</li></ul> |
<div style="page-break-after: always;"></div>

**Financial Projects Defined Type**

You will need to define the values for the Financial Projects defined type so the export knows what GL Project to associate accounts or transactions to in Intacct. We have added a new Intacct Projects page under Finance > Administration. This page allows you to manage Intacct Projects defined values without needing the RSR-Rock Admin security role.

On the Intacct Projects page, add a value for each of your organization's Projects. The Value must be the Intacct Journal Id. Description will be a friendly name for the Project.

![](../.screenshots/BatchToIntacct/FinancialProjectsDefinedValues.png)

**Financial Gateway Attributes**

If your financial gateway reports transaction fees to Rock in their transaction download, you may want to configure these Financial Gateway attributes to choose how those fees are sent to Intacct.

![](../.screenshots/BatchToIntacct/GatewayAttributes.png)

| |
| --- |
| **Gateway Fee Processing** How should the Intacct Export plugin process transaction fees? DEFAULT: No special handling of transaction fees will be performed. NET DEBIT: Add credit entries for any transaction fees and use net amount (amount - transaction fees) for debit account entries. GROSS DEBIT: Debit account entries are left untouched (gross) and new debit and credit entries will be added for any transaction fees. NOTE: Both Net Debit and Gross Debit require a Fee Account attribute be set on either the financial gateway or financial account. |
| **Default Fee Account** Default account number for transaction fees. |
<div style="page-break-after: always;"></div>

**Account Attributes**

The export will always create (at a minimum) two lines for a Journal - a debit and a credit line. The Credit and Debit Account attributes are how this is defined.

In addition to the Intacct Dimensions included, custom Dimensions can also be added.

Most organizations will mark the GL Project designation by setting a default Project on an account in Rock. If more specific Project marking is needed, the export utility also created a Financial Transaction Detail Attribute that allows for designation at the gift level.

![](../.screenshots/BatchToIntacct/AccountAttributes.png)
<div style="page-break-after: always;"></div>

| | |
| --- | ---- |
| <span style="width: 3em; height: 3em; line-height: 3em; background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">1</span> | **Default Project** Designates the project at the financial account level. |
| <span style="width: 3em; height: 3em; line-height: 3em; background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">2</span> | **Credit Account** Account number to be used for the credit column. Required by Intacct. |
| <span style="width: 3em; height: 3em; line-height: 3em; background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">3</span> | **Debit Account** Account number to be used for the debit column. Required by Intacct. |
| <span style="width: 3em; height: 3em; line-height: 3em; background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">4</span> | **Transaction Fee Account** Expense account number for gateway transaction fees. |
| <span style="width: 3em; height: 3em; line-height: 3em; background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">5</span> | **Class** The Intacct dimension for Class Id. |
| <span style="width: 3em; height: 3em; line-height: 3em; background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">6</span> | **Department** The Intacct dimension for Department Id. |
| <span style="width: 3em; height: 3em; line-height: 3em; background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">7</span> | **Location** The Intacct dimension for Location Id. Required if multi-entity enabled. |

<div style="page-break-after: always;"></div>

## Advanced Configuration

### **Adding Custom Dimension Example**

- Go to Admin Tools > System Settings > Entity Attributes
- In the filter options, set the Entity Type to Financial Account
- Add a new Attribute

![](../.screenshots/BatchToIntacct/CustomDimension.png)

| | |
| --- | ---- |
| <span style="width: 3em; height: 3em; line-height: 3em; background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">1</span> | **Name** Restriction |
| <span style="width: 3em; height: 3em; line-height: 3em; background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">2</span> | **Categories** Intacct Export |
| <span style="width: 3em; height: 3em; line-height: 3em; background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">3</span> | **Key** GLDIMRESTRICTION |
| <span style="width: 3em; height: 3em; line-height: 3em; background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">4</span> | **Field Type** Text |

<div style="page-break-after: always;"></div>

**Important Information about Your Custom Dimension**
- The Name is for internal (Rock) purposes only
- The Category has to be set to the Intacct Export category in order to be included in the API post
- The Key is the actual Intacct specific name of the Custom Dimension in all caps, beginning with "GLDIM". Also, you'll notice that the core KFS Dimensions use the format `rocks.kfs.Intacct.CLASSID`. The custom key can be anything you'd like, so long as there is a period before the Dimension name. For example, `org.mychurch.Intacct.GLDIMMYCUSTOMDIM` is a valid Attribute Key.
- Text attributes are recommended for the Custom Dimensions. However, the value you enter in the attribute on the Financial Account must be the System Info > ID, not the text value. (i.e. Value: MT-Event-P1234, ID: 10005. You must enter 10005 in Rock.)

**Examples:**
- *Custom Dimension 1:*
  - Record Name: Restriction
  - Integration Name: restriction
  - Rock Key: GLDIMRESTRICTION
- *Custom Dimension 2:*
  - Record Name: Custom Project
  - Integration Name: custom_project
  - Rock Key: GLDIMCUSTOM_PROJECT
- *Custom Dimension 2 Value:*
  - Custom Project: MT - Event - P1234
  - ID: 10005
  - Rock Value: 10005

![IntacctCustomDimensionSystemInfoScreenshot](https://user-images.githubusercontent.com/2990519/174348414-42ead26f-0dd5-4c3c-b985-d1fb48141508.jpeg)


<style>
  table {
    background-color: rgba(220, 220, 220, 0.4);
  }
  th {
    display: none;
  }
</style>