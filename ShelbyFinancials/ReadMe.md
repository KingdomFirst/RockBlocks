![Kingdom First Solutions](https://user-images.githubusercontent.com/81330042/113191442-2d82f580-9223-11eb-9e65-81ae5bc740f6.png)


# Shelby Financials Export
*Tested/Supported in Rock version:  8.0-12.0*   
*Created:  10/29/2018*  
*Updated:  11/20/2019*
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



## Configuration

#### Batch to Journal Block

After install, the Shelby Financials Batch to Journal block was added to your Batch Details page. 

![](https://user-images.githubusercontent.com/81330042/113193210-53a99500-9225-11eb-9066-85ebd714e6ae.png)








![](Images/BatchToJournalProperties.png)

> <span style="padding-left: 30px; margin-right: 10px; width: .8em;background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">&nbsp;&nbsp;1&nbsp;&nbsp;</span>**Name** Block name
>
> <span style="padding-left: 30px; margin-right: 10px; width: .8em;background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">&nbsp;&nbsp;2&nbsp;&nbsp;</span>**Button Text** Customize the text on the export button
>
> <span style="padding-left: 30px; margin-right: 10px; width: .8em;background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">&nbsp;&nbsp;3&nbsp;&nbsp;</span>**Close Batch** Control whether the batch gets marked as closed in Rock after export



#### Shelby Financials Batches to Journal Block

![](https://user-images.githubusercontent.com/81330042/113193298-6fad3680-9225-11eb-95ef-f61edf9b87a7.png)


> <span style="padding-left: 30px; margin-right: 10px; width: .8em;background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">&nbsp;&nbsp;1&nbsp;&nbsp;</span>**Name** Block name
>
> <span style="padding-left: 30px; margin-right: 10px; width: .8em;background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">&nbsp;&nbsp;2</span>**Detail Page** Link to the Financial Batch Details page
>
> <span style="padding-left: 30px; margin-right: 10px; width: .8em;background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">&nbsp;&nbsp;3&nbsp;&nbsp;</span>**Button Text** Customize the text for the export button
>
> <span style="padding-left: 30px; margin-right: 10px; width: .8em;background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">&nbsp;&nbsp;4&nbsp;&nbsp;</span>**Months Back** Number of months back that batches should be loaded. This is helpful to prevent database timeouts if there are years of historical batches



#### Account Attributes

The export will always create (at a minimum) two lines for a Journal - a debit and a credit line. The Credit and Debit Account attributes are how this is defined. Each of these attributes will need to be set to the Id of the option in your Shelby Financials GL. Only the Attributes that are filled in will be exported.

![](https://user-images.githubusercontent.com/81330042/113193420-8fdcf580-9225-11eb-8b9b-0fa3d2f90a12.png)

> <span style="padding-left: 30px; margin-right: 10px; width: .8em;background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">&nbsp;&nbsp;1&nbsp;&nbsp;</span>**Default Project** Designates the project at the financial account level
>
> <span style="padding-left: 30px; margin-right: 10px; width: .8em;background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">&nbsp;&nbsp;2&nbsp;&nbsp;</span>**Company** Designates the company
>
> <span style="padding-left: 30px; margin-right: 10px; width: .8em;background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">&nbsp;&nbsp;3&nbsp;&nbsp;</span>**Fund** Designates the fund
>
> <span style="padding-left: 30px; margin-right: 10px; width: .8em;background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">&nbsp;&nbsp;4&nbsp;&nbsp;</span>**Debit Account** Account number to be used for the debit column
>
> <span style="padding-left: 30px; margin-right: 10px; width: .8em;background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">&nbsp;&nbsp;5&nbsp;&nbsp;</span>**Dapartment** Designates the department
>
> <span style="padding-left: 30px; margin-right: 10px; width: .8em;background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">&nbsp;&nbsp;6&nbsp;&nbsp;</span>**Credit Account** Account number to be used for the credit column
>
> <span style="padding-left: 30px; margin-right: 10px; width: .8em;background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">&nbsp;&nbsp;7&nbsp;&nbsp;</span>**Region** Designates the region
>
> <span style="padding-left: 30px; margin-right: 10px; width: .8em;background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">&nbsp;&nbsp;8&nbsp;&nbsp;</span>**Super Fund** Designates the super fund
>
> <span style="padding-left: 30px; margin-right: 10px; width: .8em;background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">&nbsp;&nbsp;9&nbsp;&nbsp;</span>**Location** Designates the location
>
> <span style="padding-left: 30px; margin-right: 10px; width: .8em;background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">&nbsp;&nbsp;10&nbsp;&nbsp;</span>**Cost Center** Designates the cost center
>
> <span style="padding-left: 30px; margin-right: 10px; width: .8em;background: #d21919; border-radius: 100%; color: white; text-align: center; display: inline-block;">&nbsp;&nbsp;11&nbsp;&nbsp;</span>**Account Sub** Designates the account sub



#### Projects Defined Type

You may want to define the values for the Financial Projects defined type so the export knows what GL Project to associate accounts or transactions to in Shelby Financials. We have added a new Projects page under Finance > Administration. This page allows you to manage  Projects defined values without needing the RSR-Rock Admin security role.

On the Projects page, add a value for each of your organization's Projects. The Value must be the Id from Shelby Financials. Description will be a friendly name for the Project.

![](https://user-images.githubusercontent.com/81330042/113193523-aaaf6a00-9225-11eb-9443-646bbb45c913.png)



## Exporting to Shelby Financials

#### Assigning Projects

You can assign Projects to a financial account, a transaction or to a specific amount in the transaction.

**To assign a Project to an account**, you will set an [account attribute](#account-attributes).

**To assign a Project to an entire transaction**, select the Transaction Project from the dropdown list when you create the transaction. To assign a project to an existing transaction, edit the transaction and choose a Project from the dropdown list.

![](/Images/TransactionAttribute.png)

**To assign a Project to part of a transaction**, as you add the accounts and amounts to the transaction, select the Project from the dropdown list. You can also assing a project by editing the accounts on an existing transaction.

![](https://user-images.githubusercontent.com/81330042/113193574-bb5fe000-9225-11eb-9319-f69fb0af4a53.png)



#### Exporting Single Batches

On the Batch Detail page, select the Journal Type and enter an Accounting Period for the batch then click the Create Shelby Export button. You will not be able to export a batch if the variance amount is not $0.

![](https://user-images.githubusercontent.com/81330042/113193871-11348800-9226-11eb-9a88-091d43a0ad4f.png)



#### Exporting Multiple Batches

To export multiple batches, go to the Shelby GL Export page (Finance > Functions > Shelby GL Export). Select the batches you wish to export, select a Journal Type, enter an Accounting Period and click the Create Shelby Export button.

![](https://user-images.githubusercontent.com/81330042/113193895-1b568680-9226-11eb-9df7-78b8290b4cd9.png)
