// <copyright>
// Copyright 2022 by Kingdom First Solutions
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>
//
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Security;
using Rock.Web.Cache;
using Rock.Web.UI;
using Rock.Web.UI.Controls;
using rocks.kfs.Intacct;
using rocks.kfs.Intacct.Enums;

namespace RockWeb.Plugins.rocks_kfs.Intacct
{
    #region Block Attributes

    [DisplayName( "Intacct Batches to Journal" )]
    [Category( "KFS > Intacct" )]
    [Description( "Block used to create Journal Entries in Intacct from multiple Rock Financial Batches." )]

    #endregion

    #region Block Settings

    [LinkedPage(
        "Detail Page",
        Description = "The Financial Batch Detail Page",
        IsRequired = true,
        DefaultValue = "606BDA31-A8FE-473A-B3F8-A00ECF7E06EC",
        Category = "Batch List Settings",
        Order = 0,
        Key = AttributeKey.DetailPage )]

    [TextField(
        "Button Text",
        Description = "The text to use in the Export Button.",
        IsRequired = false,
        DefaultValue = "Export to Intacct",
        Category = "Batch List Settings",
        Order = 1,
        Key = AttributeKey.ButtonText )]

    [IntegerField(
        "Months Back",
        Description = "The number of months back that batches should be loaded. This is helpful to prevent database timeouts if there are years of historical batches.",
        IsRequired = true,
        DefaultIntegerValue = 2,
        Category = "Batch List Settings",
        Order = 2,
        Key = AttributeKey.MonthsBack )]

    [BooleanField(
        "Close Batch",
        Description = "Flag indicating if the Financial Batch(es) should be closed in Rock when successfully posted to Intacct.",
        DefaultBooleanValue = true,
        Category = "Batch List Settings",
        Order = 3,
        Key = AttributeKey.CloseBatch )]

    [BooleanField(
        "Log Response",
        Description = "Flag indicating if the Intacct Response should be logged to the Batch Audit Log",
        DefaultBooleanValue = true,
        Category = "Batch List Settings",
        Order = 4,
        Key = AttributeKey.LogResponse )]

    [BooleanField(
        "Enable Debug",
        Description = "Outputs the object graph to help create your Lava syntax.",
        DefaultBooleanValue = false,
        Category = "Batch List Settings",
        Order = 5,
        Key = AttributeKey.EnableDebug )]

    [TextField(
        "Journal Id",
        Description = "The Intacct Symbol of the Journal that the Entry should be posted to. For example: GJ",
        IsRequired = true,
        DefaultValue = "",
        Category = "Intacct Settings",
        Order = 6,
        Key = AttributeKey.JournalId )]

    [TextField(
        "Undeposited Funds Account",
        Description = "The GL AccountId to use when Other Receipt mode is being used with Undeposited Funds option selected.",
        IsRequired = false,
        DefaultValue = "",
        Category = "Intacct Settings",
        Order = 7,
        Key = AttributeKey.UndepositedFundsAccount )]

    [LavaField(
        "Journal Description Lava",
        Description = "Lava for the journal (or other receipt if configured) memo per line. Default: Batch.Id: Batch.Name",
        IsRequired = true,
        DefaultValue = "{{ Batch.Id }}: {{ Batch.Name }}",
        Category = "Intacct Settings",
        Order = 8,
        Key = AttributeKey.JournalMemoLava )]

    [EncryptedTextField(
        "Sender Id",
        Description = "The permanent Web Services sender Id.",
        IsRequired = true,
        DefaultValue = "",
        Category = "Intacct Settings",
        Order = 9,
        Key = AttributeKey.SenderId )]

    [EncryptedTextField(
        "Sender Password",
        Description = "The permanent Web Services sender password.",
        IsRequired = true,
        DefaultValue = "",
        Category = "Intacct Settings",
        Order = 10,
        Key = AttributeKey.SenderPassword )]

    [EncryptedTextField(
        "Company Id",
        Description = "The Intacct Company Id. This is the same information you use when you log into the Sage Intacct UI.",
        IsRequired = true,
        DefaultValue = "",
        Category = "Intacct Settings",
        Order = 11,
        Key = AttributeKey.CompanyId )]

    [EncryptedTextField(
        "User Id",
        Description = "The Intacct User Id. This is the same information you use when you log into the Sage Intacct UI.",
        IsRequired = true,
        DefaultValue = "",
        Category = "Intacct Settings",
        Order = 12,
        Key = AttributeKey.UserId )]

    [EncryptedTextField(
        "User Password",
        Description = "The Intacct User Password. This is the same information you use when you log into the Sage Intacct UI.",
        IsRequired = true,
        DefaultValue = "",
        Category = "Intacct Settings",
        Order = 13,
        Key = AttributeKey.UserPassword )]

    [EncryptedTextField(
        "Location Id",
        Description = "The optional Intacct Location Id. Add a location ID to log into a multi-entity shared company. Entities are typically different locations of a single company.",
        IsRequired = false,
        DefaultValue = "",
        Category = "Intacct Settings",
        Order = 14,
        Key = AttributeKey.LocationId )]

    [CustomDropdownListField(
        "Export Mode",
        Description = "Determines the type of object to create in Intacct. Selecting \"JournalEntry\" will result in creating journal entries of the type set in the \"Journal Id\" setting. Selecting \"OtherReceipt\" will result in creating Other Receipts in the Cash Management area of Intacct.",
        ListSource = "JournalEntry,OtherReceipt",
        DefaultValue = "JournalEntry",
        Category = "Intacct Settings",
        Order = 15,
        Key = AttributeKey.ExportMode )]

    #endregion

    public partial class BatchesToJournal : RockBlock, ICustomGridColumns
    {
        #region Keys

        /// <summary>
        /// Attribute Keys
        /// </summary>
        private static class AttributeKey
        {
            public const string DetailPage = "DetailPage";
            public const string ButtonText = "ButtonText";
            public const string MonthsBack = "MonthsBack";
            public const string JournalId = "JournalId";
            public const string CloseBatch = "CloseBatch";
            public const string LogResponse = "LogResponse";
            public const string SenderId = "SenderId";
            public const string SenderPassword = "SenderPassword";
            public const string CompanyId = "CompanyId";
            public const string UserId = "UserId";
            public const string UserPassword = "UserPassword";
            public const string LocationId = "LocationId";
            public const string JournalMemoLava = "JournalMemoLava";
            public const string EnableDebug = "EnableDebug";
            public const string ExportMode = "ExportMode";
            public const string UndepositedFundsAccount = "UndepositedFundsAccount";
        }

        #endregion Keys

        #region Global Variables

        //private int _batchId = 0;
        //private decimal _variance = 0;
        private string _selectedBankAccountId;
        private string _selectedPaymentMethod;
        private string _selectedReceiptAccountType;
        private string _exportMode;
        private int _monthsBack;
        private string _enableDebug;
        //private FinancialBatch _financialBatch = null;
        private IntacctAuth _intacctAuth = null;

        #endregion Global Variables
        #region Base Control Methods

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            gfBatchesToExportFilter.ApplyFilterClick += gfBatchesToExportFilter_ApplyFilterClick;
            gfBatchesToExportFilter.ClearFilterClick += gfBatchesToExportFilter_ClearFilterClick;
            gfBatchesToExportFilter.DisplayFilterValue += gfBatchesToExportFilter_DisplayFilterValue;

            gBatchesToExport.GridRebind += gBatchesToExport_GridRebind;

            this.BlockUpdated += Block_BlockUpdated;
            this.AddConfigurationUpdateTrigger( upnlContent );
        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            btnExportToIntacct.Text = GetAttributeValue( AttributeKey.ButtonText );
            _monthsBack = GetAttributeValue( AttributeKey.MonthsBack ).AsInteger() * -1;
            _enableDebug = GetAttributeValue( AttributeKey.EnableDebug );
            _exportMode = GetAttributeValue( AttributeKey.ExportMode );
            if ( !Page.IsPostBack )
            {
                if ( _exportMode == "JournalEntry" )
                {
                    pnlOtherReceipt.Visible = false;
                }
                else
                {
                    SetupOtherReceipts();
                }
                BindFilter();
                BindGrid();
            }

            if ( _enableDebug.AsBoolean() )
            {
                var debugLava = Session["IntacctDebugLava"].ToStringSafe();
                if ( !string.IsNullOrWhiteSpace( debugLava ) )
                {
                    lDebug.Visible = true;
                    lDebug.Text += debugLava;
                    Session["IntacctDebugLava"] = string.Empty;
                }
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Binds the filter.
        /// </summary>
        private void BindFilter()
        {
            ddlStatus.BindToEnum<BatchStatus>();
            ddlStatus.Items.Insert( 0, Rock.Constants.All.ListItem );
            string statusFilter = gfBatchesToExportFilter.GetUserPreference( "Status" );
            if ( string.IsNullOrWhiteSpace( statusFilter ) )
            {
                statusFilter = BatchStatus.Open.ConvertToInt().ToString();
            }

            ddlStatus.SetValue( statusFilter );

            drpBatchDate.DelimitedValues = gfBatchesToExportFilter.GetUserPreference( "Date Range" );
        }

        /// <summary>
        /// Binds the grid.
        /// </summary>
        private void BindGrid()
        {
            var firstBatchDate = RockDateTime.Now.AddMonths( _monthsBack );

            var batchIdList = new List<int>();
            var filteredBatchIdList = new List<int>();
            var batchesToExport = new List<BatchData>();

            using ( var rockContextBatches = new RockContext() )
            {
                var batchService = new FinancialBatchService( rockContextBatches );
                var qry = batchService
                    .Queryable().AsNoTracking()
                    .Where( b => b.BatchStartDateTime.HasValue )
                    .Where( b => b.BatchStartDateTime >= firstBatchDate )
                    .Where( b => b.ControlAmount == b.Transactions.Sum( t => t.TransactionDetails.Sum( d => d.Amount ) ) );

                string dateRangeValue = gfBatchesToExportFilter.GetUserPreference( "Date Range" );
                if ( !string.IsNullOrWhiteSpace( dateRangeValue ) )
                {
                    var drp = new DateRangePicker();
                    drp.DelimitedValues = dateRangeValue;
                    if ( drp.LowerValue.HasValue )
                    {
                        qry = qry.Where( b => b.BatchStartDateTime >= drp.LowerValue.Value );
                    }

                    if ( drp.UpperValue.HasValue )
                    {
                        var endOfDay = drp.UpperValue.Value.AddDays( 1 );
                        qry = qry.Where( b => b.BatchStartDateTime < endOfDay );
                    }
                }

                var status = gfBatchesToExportFilter.GetUserPreference( "Status" ).ConvertToEnumOrNull<BatchStatus>();
                if ( status.HasValue )
                {
                    qry = qry.Where( b => b.Status == status );
                }

                SortProperty sortProperty = gBatchesToExport.SortProperty;
                if ( sortProperty != null )
                {
                    qry = qry.Sort( sortProperty );
                }
                else
                {
                    qry = qry
                        .OrderByDescending( b => b.BatchStartDateTime )
                        .ThenBy( b => b.Name );
                }

                batchIdList = qry.Select( b => b.Id ).ToList();
            }

            using ( var rockContextAttributeValues = new RockContext() )
            {
                var dateExportedAttributeId = AttributeCache.Get( "1C85E090-3DAB-4929-957E-A6140633724A".AsGuid() ).Id;  //rocks.kfs.Intacct.DateExported

                foreach ( var batchId in batchIdList )
                {
                    var attributeValue = new AttributeValueService( rockContextAttributeValues ).GetByAttributeIdAndEntityId( dateExportedAttributeId, batchId );
                    if ( attributeValue == null || attributeValue.ValueAsDateTime == null )
                    {
                        filteredBatchIdList.Add( batchId );
                    }
                }
            }

            using ( var rockContextBatchData = new RockContext() )
            {
                foreach ( var batchId in filteredBatchIdList )
                {
                    var batch = new FinancialBatchService( rockContextBatchData ).Get( batchId );

                    batchesToExport.Add( new BatchData
                    {
                        Id = batch.Id,
                        BatchStartDateTime = batch.BatchStartDateTime,
                        Name = batch.Name,
                        Status = batch.Status.ToString(),
                        Note = batch.Note,
                        Transactions = batch.Transactions.Count,
                        Total = batch.GetTotalTransactionAmount( rockContextBatchData )
                    } );
                }
            }

            gBatchesToExport.DataSource = batchesToExport;
            gBatchesToExport.ObjectList = ( ( List<BatchData> ) gBatchesToExport.DataSource ).ToDictionary( b => b.Id.ToString(), v => v as object );
            gBatchesToExport.EntityTypeId = EntityTypeCache.Get<FinancialBatch>().Id;

            gBatchesToExport.DataBind();
        }

        private void SetupOtherReceipts( bool setSelectControlValues = true )
        {
            pnlOtherReceipt.Visible = true;
            if ( ddlPaymentMethods.Items.Count == 0 )
            {
                foreach ( PaymentMethod pm in Enum.GetValues( typeof( PaymentMethod ) ) )
                {
                    var listItem = new ListItem
                    {
                        Value = ( ( int ) pm ).ToString(),
                        Text = pm.GetAttribute<DisplayAttribute>().Name
                    };
                    ddlPaymentMethods.Items.Add( listItem );
                }
            }
            if ( setSelectControlValues )
            {
                ddlPaymentMethods.SetValue( _selectedPaymentMethod );
            }
            var undepFundAccountId = GetAttributeValue( AttributeKey.UndepositedFundsAccount );
            if ( string.IsNullOrWhiteSpace( undepFundAccountId ) )
            {
                ddlReceiptAccountType.Items[1].Enabled = false;
                ddlReceiptAccountType.Items[1].Text = "Undeposited Funds";
            }
            else
            {
                ddlReceiptAccountType.Items[1].Enabled = true;
                ddlReceiptAccountType.Items[1].Text = string.Format( "Undeposited Funds ({0})", undepFundAccountId );
            }
            if ( setSelectControlValues )
            {
                ddlReceiptAccountType.SetValue( _selectedReceiptAccountType );
            }
            if ( ddlReceiptAccountType.SelectedValue == "BankAccount" )
            {
                if ( ddlBankAccounts.Items.Count == 0 )
                {
                    var bankAccounts = GetIntacctBankAccountIds();
                    ddlBankAccounts.DataSource = bankAccounts;
                    ddlBankAccounts.DataTextField = "BankName";
                    ddlBankAccounts.DataValueField = "BankAccountId";
                    ddlBankAccounts.DataBind();
                }
                if ( setSelectControlValues )
                {
                    ddlBankAccounts.SetValue( _selectedBankAccountId );
                }
                pnlBankAccounts.Visible = true;
            }
            else
            {
                pnlBankAccounts.Visible = false;
            }
        }

        private List<CheckingAccount> GetIntacctBankAccountIds()
        {
            if ( _intacctAuth == null )
            {
                _intacctAuth = GetIntactAuth();
            }
            //var debugLava = GetAttributeValue( AttributeKey.EnableDebug );
            var checkingAccountList = new IntacctCheckingAccountList();
            var accountFields = new List<string>();
            accountFields.Add( "BANKACCOUNTID" );
            accountFields.Add( "BANKNAME" );
            var postXml = checkingAccountList.GetBankAccountsXML( _intacctAuth, this.BlockId, accountFields );

            var endpoint = new IntacctEndpoint();
            var resultXml = endpoint.PostToIntacct( postXml );
            return endpoint.ParseListCheckingAccountsResponse( resultXml, this.BlockId );

        }

        private IntacctAuth GetIntactAuth()
        {
            return new IntacctAuth()
            {
                SenderId = Encryption.DecryptString( GetAttributeValue( AttributeKey.SenderId ) ),
                SenderPassword = Encryption.DecryptString( GetAttributeValue( AttributeKey.SenderPassword ) ),
                CompanyId = Encryption.DecryptString( GetAttributeValue( AttributeKey.CompanyId ) ),
                UserId = Encryption.DecryptString( GetAttributeValue( AttributeKey.UserId ) ),
                UserPassword = Encryption.DecryptString( GetAttributeValue( AttributeKey.UserPassword ) ),
                LocationId = Encryption.DecryptString( GetAttributeValue( AttributeKey.LocationId ) )
            };
        }

        #endregion

        #region Events

        /// <summary>
        /// Handles the GridRebind event of the gBatchesToExport control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="GridRebindEventArgs"/> instance containing the event data.</param>
        private void gBatchesToExport_GridRebind( object sender, GridRebindEventArgs e )
        {
            BindGrid();
        }

        /// <summary>
        /// Handles the BlockUpdated event of the Block control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void Block_BlockUpdated( object sender, EventArgs e )
        {
            BindGrid();
        }

        /// <summary>
        /// Gfs the batches to export filter display filter value.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The e.</param>
        protected void gfBatchesToExportFilter_DisplayFilterValue( object sender, GridFilter.DisplayFilterValueArgs e )
        {
            switch ( e.Key )
            {
                case "Date Range":
                    {
                        e.Value = DateRangePicker.FormatDelimitedValues( e.Value );
                        break;
                    }

                case "Status":
                    {
                        var status = e.Value.ConvertToEnumOrNull<BatchStatus>();
                        if ( status.HasValue )
                        {
                            e.Value = status.ConvertToString();
                        }
                        else
                        {
                            e.Value = string.Empty;
                        }

                        break;
                    }
            }
        }

        /// <summary>
        /// Handles the ApplyFilterClick event of the gfBatchesToExportFilter control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void gfBatchesToExportFilter_ApplyFilterClick( object sender, EventArgs e )
        {
            gfBatchesToExportFilter.SaveUserPreference( "Status", ddlStatus.SelectedValue );
            gfBatchesToExportFilter.SaveUserPreference( "Date Range", drpBatchDate.DelimitedValues );

            BindGrid();
        }

        /// <summary>
        /// Handles the ClearFilterClick event of the gfBatchesToExportFilter control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void gfBatchesToExportFilter_ClearFilterClick( object sender, EventArgs e )
        {
            gfBatchesToExportFilter.DeleteUserPreferences();
            gfBatchesToExportFilter.SaveUserPreference( "Status", BatchStatus.Open.ConvertToInt().ToString() );
            BindFilter();
            BindGrid();
        }

        /// <summary>
        /// Handles the Click event of the gBatchesToExport control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RowEventArgs"/> instance containing the event data.</param>
        protected void gBatchesToExport_Click( object sender, RowEventArgs e )
        {
            NavigateToLinkedPage( "DetailPage", "batchId", e.RowKeyId );
        }

        /// <summary>
        /// Handles the Click event of the btnExportToIntacct control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void btnExportToIntacct_Click( object sender, EventArgs e )
        {
            var selectedBatches = new List<int>();

            gBatchesToExport.SelectedKeys.ToList().ForEach( b => selectedBatches.Add( b.ToString().AsInteger() ) );

            if ( selectedBatches.Any() )
            {
                var debugLava = GetAttributeValue( AttributeKey.EnableDebug );
                if ( _exportMode == "OtherReceipt" )
                {
                    //
                    // Capture ddl values as user preferences
                    //

                    SetBlockUserPreference( "ReceiptAccountType", ddlReceiptAccountType.SelectedValue ?? "" );
                    SetBlockUserPreference( "PaymentMethod", ddlPaymentMethods.SelectedValue ?? "" );
                }

                if ( _intacctAuth == null )
                {
                    _intacctAuth = GetIntactAuth();
                }

                var rockContext = new RockContext();
                var batchService = new FinancialBatchService( rockContext );
                var batchesToUpdate = new List<FinancialBatch>();

                var exportedBatches = batchService.Queryable()
                    .WhereAttributeValue( rockContext, a => a.Attribute.Key == "rocks.kfs.Intacct.DateExported" && ( a.Value != null && a.Value != "" ) )
                    .Select( b => b.Id );

                batchesToUpdate = batchService.Queryable()
                    .Where( b =>
                        selectedBatches.Contains( b.Id )
                        && !exportedBatches.Contains( b.Id )
                      ).ToList();

                foreach ( var batch in batchesToUpdate )
                {
                    var changes = new History.HistoryChangeList();
                    History.EvaluateChange( changes, "Status", batch.Status, BatchStatus.Closed );

                    string errorMessage;

                    if ( batch.IsAutomated && batch.Status == BatchStatus.Pending )
                    {
                        errorMessage = string.Format( "{0} is an automated batch and the status can not be modified when the status is pending. The system will automatically set this batch to OPEN when all transactions have been downloaded.", batch.Name );
                        maWarningDialog.Show( errorMessage, ModalAlertType.Warning );
                        return;
                    }

                    batch.Status = BatchStatus.Closed;

                    if ( !batch.IsValid )
                    {
                        string message = string.Format( "Unable to update status for the selected batches.<br/><br/>{0}", batch.ValidationResults.AsDelimited( "<br/>" ) );
                        maWarningDialog.Show( message, ModalAlertType.Warning );
                        return;
                    }

                    //
                    // Create Intacct Journal XML and Post to Intacct
                    //

                    var endpoint = new IntacctEndpoint();
                    var postXml = new System.Xml.XmlDocument();

                    if ( _exportMode == "JournalEntry" )
                    {
                        var journal = new IntacctJournal();
                        postXml = journal.CreateJournalEntryXML( _intacctAuth, batch.Id, GetAttributeValue( AttributeKey.JournalId ), ref debugLava, GetAttributeValue( AttributeKey.JournalMemoLava ) );
                    }
                    else   // Export Mode is Other Receipt
                    {
                        var otherReceipt = new IntacctOtherReceipt();
                        string bankAccountId = null;
                        string undepFundAccount = null;
                        if ( ddlReceiptAccountType.SelectedValue == "BankAccount" )
                        {
                            SetBlockUserPreference( "BankAccountId", ddlBankAccounts.SelectedValue ?? "" );
                            bankAccountId = ddlBankAccounts.SelectedValue;
                        }
                        else
                        {
                            undepFundAccount = GetAttributeValue( AttributeKey.UndepositedFundsAccount );
                        }
                        postXml = otherReceipt.CreateOtherReceiptXML( _intacctAuth, batch.Id, ref debugLava, ( PaymentMethod ) ddlPaymentMethods.SelectedValue.AsInteger(), bankAccountId, undepFundAccount, GetAttributeValue( AttributeKey.JournalMemoLava ) );
                    }

                    var resultXml = endpoint.PostToIntacct( postXml );
                    var success = endpoint.ParseEndpointResponse( resultXml, batch.Id, GetAttributeValue( AttributeKey.LogResponse ).AsBoolean() );

                    if ( success )
                    {

                        //
                        // Close Batch if we're supposed to
                        //
                        if ( GetAttributeValue( AttributeKey.CloseBatch ).AsBoolean() )
                        {
                            History.EvaluateChange( changes, "Status", batch.Status, BatchStatus.Closed );
                            batch.Status = BatchStatus.Closed;
                        }

                        //
                        // Set Date Exported
                        //
                        batch.LoadAttributes();
                        var oldDate = batch.GetAttributeValue( "rocks.kfs.Intacct.DateExported" );
                        var newDate = RockDateTime.Now;
                        History.EvaluateChange( changes, "Date Exported", oldDate, newDate.ToString() );

                        //
                        // Save the changes
                        //
                        rockContext.WrapTransaction( () =>
                        {
                            if ( changes.Any() )
                            {
                                HistoryService.SaveChanges(
                                    rockContext,
                                    typeof( FinancialBatch ),
                                    Rock.SystemGuid.Category.HISTORY_FINANCIAL_BATCH.AsGuid(),
                                    batch.Id,
                                    changes );
                            }
                        } );

                        batch.SetAttributeValue( "rocks.kfs.Intacct.DateExported", newDate );
                        batch.SaveAttributeValue( "rocks.kfs.Intacct.DateExported", rockContext );
                    }
                }

                rockContext.SaveChanges();
                Session["IntacctDebugLava"] = debugLava;

                NavigateToPage( this.RockPage.Guid, new Dictionary<string, string>() );
            }
            else
            {
                nbError.Text = string.Format( "There were not any batches selected." );
                nbError.NotificationBoxType = NotificationBoxType.Warning;
                nbError.Visible = true;
            }
        }

        protected void ddlReceiptAccountType_SelectedIndexChanged( object sender, EventArgs e )
        {
            _selectedReceiptAccountType = ddlReceiptAccountType.SelectedValue;
            _selectedPaymentMethod = ddlPaymentMethods.SelectedValue;
            if ( ddlReceiptAccountType.SelectedValue != "BankAccount" )
            {
                _selectedBankAccountId = ddlBankAccounts.SelectedValue;
            }
            SetupOtherReceipts();
        }

        #endregion

        #region Classes

        public class BatchData
        {
            public int Id { get; set; }
            public DateTime? BatchStartDateTime { get; set; }
            public string Name { get; set; }
            public string Status { get; set; }
            public string Note { get; set; }
            public int Transactions { get; set; }
            public decimal Total { get; set; }
        }

        #endregion
    }
}
