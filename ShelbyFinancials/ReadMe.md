![Kingdom First Solutions](../.screenshots/KFSBanner.jpg)


# Shelby Financials Export
*Tested/Supported in Rock version:  8.0-14.0*   
*Created:  10/29/2018*  
*Updated:  5/11/2023*   
*Rock Shop Plugin: https://www.rockrms.com/Plugin/97*

## Summary



Quick Links:
- [What's New](#whats-new)
- [Configuration](#configuration)
- [Exporting to Shelby Financials](#exporting-to-shelby-financials)



## What's New

- The following new goodness will be added to your Rock install with this plugin:
  - **New Block**: Shelby Financials Batch to Journal (added to the Batch Detail page on install) 
  - **New Block**:  Shelby Financials Batches to Journal (added to the Shelby GL Batch Export page on install) 
  - **New Account Attributes**: There are a number of new Account attributes that control where transactions are posted in Shelby Financials
  - **New Transaction Attribute**: Transaction Project
  - **New Page**: Projects (Finance > Administration > Projects)
  - **New Page**: Shelby GL Export (Finance > Functions > Shelby GL Export)  
  - **New Defined Type**: Financial Projects stores the Defined Values that designate what Project a transaction or batch should be associated with  
  - **New Batch Attribute**: Date Exported  

<div style="page-break-after: always;"></div>

## Configuration

#### Batch to Journal Block

After install, the Shelby Financials Batch to Journal block was added to your Batch Details page. 

![](../.screenshots/ShelbyFinancials/BatchToJournal.png)

![](../.screenshots/ShelbyFinancials/BatchToJournalProperties.png)

<div style="page-break-after: always;"></div>

| | |
| --- | ---- |
| <span style="width: 3em; height: 3em; line-height: 3em; background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">1</span> | **Button Text** Customize the text on the export button |
| <span style="width: 3em; height: 3em; line-height: 3em; background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">2</span> | **Close Batch** Control whether the batch gets marked as closed in Rock after export |
| <span style="width: 3em; height: 3em; line-height: 3em; background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">3</span> | **GL Account Grouping** Determines if debit and/or credit lines should be grouped and summed as follows in the export file:<ul><li>**Debit Accounts**: Company, Region, Super Fund, Cost Center Debit Number, Debit Account, Debit Account Sub, Fund Number, Project, Transaction Fee Account, and Location</li><li>**Credit Accounts**: Company, Region, Department, Super Fund, Cost Center Credit Number, Revenue Account, Revenue Account Sub Account, Fund Number, Project, and Location</li><li>**Financial Account Grouping**: Both credit and debit lines are grouped by Rock Financial Account, Project, and Transaction Fee Account.</li></ul> |
| <span style="width: 3em; height: 3em; line-height: 3em; background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">4</span> | **Journal Description Lava** Lava for the journal description column per line. Default: Batch.Id: Batch.Name |
| <span style="width: 3em; height: 3em; line-height: 3em; background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">5</span> | **Enable Debug** Outputs the object graph to help create your Lava syntax |

#### Shelby Financials Batches to Journal Block

![](../.screenshots/ShelbyFinancials/BatchesToJournalProperties.png)

<div style="page-break-after: always;"></div>

| | |
| --- | ---- |
| <span style="width: 3em; height: 3em; line-height: 3em; background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">1</span> | **Detail Page** Link to the Financial Batch Details page |
| <span style="width: 3em; height: 3em; line-height: 3em; background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">2</span> | **Button Text** Customize the text for the export button |
| <span style="width: 3em; height: 3em; line-height: 3em; background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">3</span> | **Months Back** Number of months back that batches should be loaded. This is helpful to prevent database timeouts if there are years of historical batches |
| <span style="width: 3em; height: 3em; line-height: 3em; background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">3</span> | **GL Account Grouping** Determines if debit and/or credit lines should be grouped and summed as follows in the export file:<ul><li>**Debit Accounts**: Company, Region, Super Fund, Cost Center Debit Number, Debit Account, Debit Account Sub, Fund Number, Project, Transaction Fee Account, and Location</li><li>**Credit Accounts**: Company, Region, Department, Super Fund, Cost Center Credit Number, Revenue Account, Revenue Account Sub Account, Fund Number, Project, and Location</li><li>**Financial Account Grouping**: Both credit and debit lines are grouped by Rock Financial Account, Project, and Transaction Fee Account.</li></ul> |
| <span style="width: 3em; height: 3em; line-height: 3em; background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">5</span> | **Journal Description Lava** Lava for the journal description column per line. Default: Batch.Id: Batch.Name |
| <span style="width: 3em; height: 3em; line-height: 3em; background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">6</span> | **Enable Debug** Outputs the object graph to help create your Lava syntax |

<div style="page-break-after: always;"></div>

#### Account Attributes

The export will always create (at a minimum) two lines for a Journal - a debit and a credit line. The Credit and Debit Account attributes are how this is defined. Each of these attributes will need to be set to the Id of the option in your Shelby Financials GL. Only the Attributes that are filled in will be exported.

![](../.screenshots/ShelbyFinancials/AccountAttributes.png)
<div style="page-break-after: always;"></div>

| | |
| --- | ---- |
| <span style="width: 3em; height: 3em; line-height: 3em; background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">1</span> | **Default Project** Designates the project at the financial account level |
| <span style="width: 3em; height: 3em; line-height: 3em; background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">2</span> | **Company** Designates the company |
| <span style="width: 3em; height: 3em; line-height: 3em; background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">3</span> | **Fund** Designates the fund |
| <span style="width: 3em; height: 3em; line-height: 3em; background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">4</span> | **Debit Account** Account number to be used for the debit column |
| <span style="width: 3em; height: 3em; line-height: 3em; background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">5</span> | **Debit Account Sub** Designates the Debit Account sub |
| <span style="width: 3em; height: 3em; line-height: 3em; background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">6</span> | **Revenue Department** Designates the department |
| <span style="width: 3em; height: 3em; line-height: 3em; background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">7</span> | **Revenue Account** Account number to be used for the credit column |
| <span style="width: 3em; height: 3em; line-height: 3em; background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">8</span> | **Revenue Account Sub** Designates the Revenue Account sub |
| <span style="width: 3em; height: 3em; line-height: 3em; background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">9</span> | **Region** Designates the region |
| <span style="width: 3em; height: 3em; line-height: 3em; background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">10</span> | **Super Fund** Designates the super fund |
| <span style="width: 3em; height: 3em; line-height: 3em; background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">11</span> | **Location** Designates the location |
| <span style="width: 3em; height: 3em; line-height: 3em; background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">12</span> | **Cost Center Default/Debit** Cost Center Default will be used on both Credit/Debit lines if Cost Center Credit does not contain a value |
| <span style="width: 3em; height: 3em; line-height: 3em; background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">13</span> | **Cost Center Credit** Designates the cost center for credits |
| <span style="width: 3em; height: 3em; line-height: 3em; background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">14</span> | **Transaction Fee Account** Expense account number for gateway transaction fees |

<div style="page-break-after: always;"></div>

#### Financial Gateway Attributes

If your financial gateway reports transaction fees to Rock in their transaction download, you may want to configure these Financial Gateway attributes to choose how those fees are handled in your export file.

![](../.screenshots/ShelbyFinancials/GatewayAttributes.png)

| | |
| --- | ---- |
| <span style="width: 3em; height: 3em; line-height: 3em; background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">1</span> | **Gateway Fee Processing** How should the Intacct Export plugin process transaction fees?<ul><li>**Default**: No special handling of transaction fees will be performed.</li><li>**Net Debit**: Add credit entries for any transaction fees and use net amount (amount - transaction fees) for debit account entries.</li><li>**GrossDebit**: Debit account entries are left untouched (gross) and new debit and credit entries will be added for any transaction fees.NOTE: Both Net Debit and Gross Debit require a Fee Account attribute be set on either the financial gateway or financial account.</li></ul> |
| <span style="width: 3em; height: 3em; line-height: 3em; background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">2</span> | **Default Fee Account** The default account number for transaction fees. |

#### Projects Defined Type

You may want to define the values for the Financial Projects defined type so the export knows what GL Project to associate accounts or transactions to in Shelby Financials. We have added a new Projects page under Finance > Administration. This page allows you to manage  Projects defined values without needing the RSR-Rock Admin security role.

On the Projects page, add a value for each of your organization's Projects. The Value must be the Id from Shelby Financials. Description will be a friendly name for the Project.

![](/../.screenshots/ShelbyFinancials/Projects.png)

<div style="page-break-after: always;"></div>

## Exporting to Shelby Financials

#### Assigning Projects

You can assign Projects to a financial account, a transaction or to a specific amount in the transaction.

**To assign a Project to an account**, you will set an [account attribute](#account-attributes).

**To assign a Project to an entire transaction**, select the Transaction Project from the dropdown list when you create the transaction. To assign a project to an existing transaction, edit the transaction and choose a Project from the dropdown list.

![](/../.screenshots/ShelbyFinancials/TransactionAttribute.png)

**To assign a Project to part of a transaction**, as you add the accounts and amounts to the transaction, select the Project from the dropdown list. You can also a project by editing the accounts on an existing transaction.

![1543597303519](../.screenshots/ShelbyFinancials/TransactionDetailAttribute.png)

<div style="page-break-after: always;"></div>

#### Exporting Single Batches

On the Batch Detail page, select the Journal Type and enter an Accounting Period for the batch then click the Create Shelby Export button. You will not be able to export a batch if the variance amount is not $0.

![1543597518859](../.screenshots/ShelbyFinancials/ExportBatch.png)

<div style="page-break-after: always;"></div>

#### Exporting Multiple Batches

To export multiple batches, go to the Shelby GL Export page (Finance > Functions > Shelby GL Export). Select the batches you wish to export, select a Journal Type, enter an Accounting Period and click the Create Shelby Export button.

![1543598924073](../.screenshots/ShelbyFinancials/ExportMultipleBatches.png)


<style>
  table {
    background-color: rgba(220, 220, 220, 0.4);
  }
  th {
    display: none;
  }
</style>