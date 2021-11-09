// <copyright>
// Copyright 2019 by Kingdom First Solutions
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
using System.Linq;
using System.Web.UI.WebControls;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Security;
using Rock.Web.UI;

using rocks.kfs.Intacct;
using rocks.kfs.Intacct.Enums;

namespace RockWeb.Plugins.rocks_kfs.Intacct
{
    #region Block Attributes

    [DisplayName( "Intacct Batch to Journal" )]
    [Category( "KFS > Intacct" )]
    [Description( "Block used to create Journal Entries in Intacct from a Rock Financial Batch." )]

    #endregion

    #region Block Settings

    [TextField(
        "Journal Id",
        Description = "The Intacct Symbol of the Journal that the Entry should be posted to. For example: GJ",
        IsRequired = true,
        DefaultValue = "",
        Order = 0,
        Key = AttributeKey.JournalId )]

    [TextField(
        "Button Text",
        Description = "The text to use in the Export Button.",
        IsRequired = false,
        DefaultValue = "Export to Intacct",
        Order = 1,
        Key = AttributeKey.ButtonText )]

    [BooleanField(
        "Close Batch",
        Description = "Flag indicating if the Financial Batch be closed in Rock when successfully posted to Intacct.",
        DefaultBooleanValue = true,
        Order = 2,
        Key = AttributeKey.CloseBatch )]

    [BooleanField(
        "Log Response",
        Description = "Flag indicating if the Intacct Response should be logged to the Batch Audit Log",
        DefaultBooleanValue = true,
        Order = 3,
        Key = AttributeKey.LogResponse )]

    [EncryptedTextField(
        "Sender Id",
        Description = "The permanent Web Services sender Id.",
        IsRequired = true,
        DefaultValue = "",
        Category = "Configuration",
        Order = 0,
        Key = AttributeKey.SenderId )]

    [EncryptedTextField(
        "Sender Password",
        Description = "The permanent Web Services sender password.",
        IsRequired = true,
        DefaultValue = "",
        Category = "Configuration",
        Order = 1,
        Key = AttributeKey.SenderPassword )]

    [EncryptedTextField(
        "Company Id",
        Description = "The Intacct Company Id. This is the same information you use when you log into the Sage Intacct UI.",
        IsRequired = true,
        DefaultValue = "",
        Category = "Configuration",
        Order = 2,
        Key = AttributeKey.CompanyId )]

    [EncryptedTextField(
        "User Id",
        Description = "The Intacct User Id. This is the same information you use when you log into the Sage Intacct UI.",
        IsRequired = true,
        DefaultValue = "",
        Category = "Configuration",
        Order = 3,
        Key = AttributeKey.UserId )]

    [EncryptedTextField(
        "User Password",
        Description = "The Intacct User Password. This is the same information you use when you log into the Sage Intacct UI.",
        IsRequired = true,
        DefaultValue = "",
        Category = "Configuration",
        Order = 4,
        Key = AttributeKey.UserPassword )]

    [EncryptedTextField(
        "Location Id",
        Description ="The optional Intacct Location Id. Add a location ID to log into a multi-entity shared company. Entities are typically different locations of a single company.",
        IsRequired = false,
        DefaultValue = "",
        Category = "Configuration",
        Order = 5,
        Key = AttributeKey.LocationId )]

    [CustomDropdownListField(
        "Export Mode",
        Description = "Determines the type of object to create in Intacct. Selecting \"JournalEntry\" will result in creating journal entries of the type set in the \"Journal Id\" setting. Selecting \"OtherReceipt\" will result in creating Other Receipts in the Cash Management area of Intacct.",
        ListSource = "JournalEntry,OtherReceipt",
        DefaultValue = "JournalEntry",
        Category = "Configuration",
        Order = 6,
        Key = AttributeKey.ExportMode )]

    [TextField(
        "Undeposited Funds Account",
        Description = "The GL AccountId to use when Other Receipt mode is being used with Undeposited Funds option selected.",
        IsRequired = false,
        DefaultValue = "",
        Order = 7,
        Key = AttributeKey.UndepositedFundsAccount )]

    [LavaField(
        "Journal Memo Lava",
        Description = "Lava for the journal memo per line. Default: Batch.Id: Batch.Name",
        IsRequired = true,
        DefaultValue = "{{ Batch.Id }}: {{ Batch.Name }}",
        Order = 4,
        Key = AttributeKey.JournalMemoLava )]

    [BooleanField(
        "Enable Debug",
        Description = "Outputs the object graph to help create your Lava syntax. (Debug data will show after clicking export.)",
        DefaultBooleanValue = false,
        Order = 5,
        Key = AttributeKey.EnableDebug )]

    #endregion

    public partial class BatchToJournal : RockBlock
    {
        #region Keys

        /// <summary>
        /// Attribute Keys
        /// </summary>
        private static class AttributeKey
        {
            public const string JournalId = "JournalId";
            public const string ButtonText = "ButtonText";
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

        /// <summary>
        /// User Preference Key
        /// </summary>
        private static class UserPreferenceKey
        {
            public const string StartDate = "Start Date";
            public const string EndDate = "End Date";
            public const string FirstName = "First Name";
            public const string LastName = "Last Name";
            public const string SubmittedBy = "Submitted By";
            public const string Category = "Category";
            public const string Status = "Status";
            public const string Campus = "Campus";
            public const string AssignedToMe = "Assigned to Me";
            public const string StartDateFollowUp = "FollowUp Start Date";
            public const string EndDateFollowUp = "FollowUp End Date";
            public const string FirstNameFollowUp = "FollowUp First Name";
            public const string LastNameFollowUp = "FollowUp Last Name";
            public const string SubmittedByFollowUp = "FollowUp Submitted By";
            public const string CategoryFollowUp = "FollowUp Category";
            public const string StatusFollowUp = "FollowUp Status";
            public const string CampusFollowUp = "FollowUp Campus";
            public const string AssignedToMeFollowUp = "FollowUp Assigned to Me";
        }

        #endregion Keys

        private int _batchId = 0;
        private decimal _variance = 0;
        private FinancialBatch _financialBatch = null;
        private IntacctAuth _intacctAuth = null;

        #region Control Methods

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            _batchId = PageParameter( "batchId" ).AsInteger();
        }

        /// <summary>
        /// Handles the BlockUpdated event of the Block control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void Block_BlockUpdated( object sender, EventArgs e )
        {
            ShowDetail();
        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            ShowDetail();
        }

        #endregion Control Methods

        #region Methods

        protected void ShowDetail()
        {
            var rockContext = new RockContext();
            var isExported = false;
            var debugEnabled = GetAttributeValue( AttributeKey.EnableDebug ).AsBoolean();
            var exportMode = GetAttributeValue( AttributeKey.ExportMode );

            _financialBatch = new FinancialBatchService( rockContext ).Get( _batchId );
            DateTime? dateExported = null;

            _variance = 0;

            if ( _financialBatch != null )
            {
                var financialTransactionDetailService = new FinancialTransactionDetailService( rockContext );
                var qryTransactionDetails = financialTransactionDetailService.Queryable().Where( a => a.Transaction.BatchId == _financialBatch.Id );
                decimal txnTotal = qryTransactionDetails.Select( a => ( decimal? ) a.Amount ).Sum() ?? 0;
                _variance = txnTotal - _financialBatch.ControlAmount;

                _financialBatch.LoadAttributes();

                dateExported = ( DateTime? ) _financialBatch.AttributeValues["rocks.kfs.Intacct.DateExported"].ValueAsType;

                if ( dateExported != null && dateExported > DateTime.MinValue )
                {
                    isExported = true;
                }

                if ( debugEnabled )
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

            if ( ValidSettings() && !isExported )
            {
                btnExportToIntacct.Text = GetAttributeValue( AttributeKey.ButtonText );
                btnExportToIntacct.Visible = true;
                if ( exportMode == "JournalEntry" )
                {
                    pnlOtherReceipt.Visible = false;
                }
                else
                {
                    SetupOtherReceipts();
                }
                SetExportButtonVisibility();
            }
            else if ( isExported )
            {
                litDateExported.Text = string.Format( "<div class=\"small\">Exported: {0}</div>", dateExported.ToRelativeDateString() );
                litDateExported.Visible = true;

                if ( UserCanEdit )
                {
                    pnlExportedDetails.Visible = true;
                    btnRemoveDate.Visible = true;
                }
            }
        }

        private void SetExportButtonVisibility()
        {
            if ( _variance == 0 )
            {
                btnExportToIntacct.Enabled = true;
            }
            else
            {
                btnExportToIntacct.Enabled = false;
            }
        }

        private void SetupOtherReceipts()
        {
            pnlOtherReceipt.Visible = true;
            if ( ddlPaymentMethods.Items.Count == 0 )
            {
                foreach ( PaymentMethod pm in Enum.GetValues( typeof( PaymentMethod ) ) )
                {
                    var listItem = new ListItem
                    {
                        Value = ( ( int ) pm ).ToString(),
                        Text = pm.GetDescription()
                    };
                    ddlPaymentMethods.Items.Add( listItem );
                }
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
                ddlBankAccounts.Visible = true;
            }
            else
            {
                ddlBankAccounts.Visible = false;
            }
        }

        private List<CheckingAccount> GetIntacctBankAccountIds()
        {
            if ( _intacctAuth == null )
            {
                _intacctAuth = GetIntactAuth();
            }
            var debugLava = GetAttributeValue( AttributeKey.EnableDebug );
            var checkingAccountList = new IntacctCheckingAccountList();
            var accountFields = new List<string>();
            accountFields.Add( "BANKACCOUNTID" );
            var postXml = checkingAccountList.GetBankAccountsXML( _intacctAuth, _financialBatch.Id, ref debugLava, GetAttributeValue( AttributeKey.JournalMemoLava ) );

            var endpoint = new IntacctEndpoint();
            var resultXml = endpoint.PostToIntacct( postXml );
            return endpoint.ParseListCheckingAccountsResponse( resultXml, _financialBatch.Id, GetAttributeValue( AttributeKey.LogResponse ).AsBoolean() );

        }

        protected void btnExportToIntacct_Click( object sender, EventArgs e )
        {
            if ( _financialBatch != null )
            {
                if ( _intacctAuth == null )
                {
                    _intacctAuth = GetIntactAuth();
                }

                //
                // Create Intacct Journal XML and Post to Intacct
                //

                var endpoint = new IntacctEndpoint();
                var debugLava = GetAttributeValue( AttributeKey.EnableDebug );
                var postXml = new System.Xml.XmlDocument();

                if ( GetAttributeValue( AttributeKey.ExportMode ) == "JournalEntry" )
                {
                    var journal = new IntacctJournal();
                    postXml = journal.CreateJournalEntryXML( _intacctAuth, _financialBatch.Id, GetAttributeValue( AttributeKey.JournalId ), ref debugLava, GetAttributeValue( AttributeKey.JournalMemoLava ) );
                }
                else   // Export Mode is Other Receipt
                {
                    var otherReceipt = new IntacctOtherReceipt();
                    postXml = otherReceipt.CreateOtherReceiptXML( _intacctAuth, _financialBatch.Id, ref debugLava, ( PaymentMethod ) ddlPaymentMethods.SelectedValue.AsInteger(), ddlBankAccounts.SelectedValue, GetAttributeValue( AttributeKey.UndepositedFundsAccount ), GetAttributeValue( AttributeKey.JournalMemoLava ) );
                }

                var resultXml = endpoint.PostToIntacct( postXml );
                var success = endpoint.ParseEndpointResponse( resultXml, _financialBatch.Id, GetAttributeValue( AttributeKey.LogResponse ).AsBoolean() );

                if ( success )
                {
                    var rockContext = new RockContext();
                    var financialBatch = new FinancialBatchService( rockContext ).Get( _batchId );
                    var changes = new History.HistoryChangeList();

                    Session["IntacctDebugLava"] = debugLava;

                    //
                    // Close Batch if we're supposed to
                    //
                    if ( GetAttributeValue( AttributeKey.CloseBatch ).AsBoolean() )
                    {
                        History.EvaluateChange( changes, "Status", financialBatch.Status, BatchStatus.Closed );
                        financialBatch.Status = BatchStatus.Closed;
                    }

                    //
                    // Set Date Exported
                    //
                    financialBatch.LoadAttributes();
                    var oldDate = financialBatch.GetAttributeValue( "rocks.kfs.Intacct.DateExported" );
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
                                financialBatch.Id,
                                changes );
                        }
                    } );

                    financialBatch.SetAttributeValue( "rocks.kfs.Intacct.DateExported", newDate );
                    financialBatch.SaveAttributeValue( "rocks.kfs.Intacct.DateExported", rockContext );
                }
            }

            Response.Redirect( Request.RawUrl );
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

        protected void btnRemoveDateExported_Click( object sender, EventArgs e )
        {
            if ( _financialBatch != null )
            {
                var rockContext = new RockContext();
                var financialBatch = new FinancialBatchService( rockContext ).Get( _batchId );
                var changes = new History.HistoryChangeList();

                //
                // Open Batch is we Closed it
                //
                if ( GetAttributeValue( AttributeKey.CloseBatch ).AsBoolean() )
                {
                    History.EvaluateChange( changes, "Status", financialBatch.Status, BatchStatus.Open );
                    financialBatch.Status = BatchStatus.Open;
                }

                //
                // Remove Date Exported
                //
                financialBatch.LoadAttributes();
                var oldDate = financialBatch.GetAttributeValue( "rocks.kfs.Intacct.DateExported" ).AsDateTime().ToString();
                var newDate = string.Empty;
                History.EvaluateChange( changes, "Date Exported", oldDate, newDate );

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
                            financialBatch.Id,
                            changes );
                    }
                } );

                financialBatch.SetAttributeValue( "rocks.kfs.Intacct.DateExported", newDate );
                financialBatch.SaveAttributeValue( "rocks.kfs.Intacct.DateExported", rockContext );
            }

            Response.Redirect( Request.RawUrl );
        }

        protected bool ValidSettings()
        {
            var settings = false;

            if (
                _batchId > 0 &&
                (
                !string.IsNullOrWhiteSpace( Encryption.DecryptString( GetAttributeValue( AttributeKey.SenderId ) ) ) &&
                !string.IsNullOrWhiteSpace( Encryption.DecryptString( GetAttributeValue( AttributeKey.SenderPassword ) ) ) &&
                !string.IsNullOrWhiteSpace( Encryption.DecryptString( GetAttributeValue( AttributeKey.CompanyId ) ) ) &&
                !string.IsNullOrWhiteSpace( Encryption.DecryptString( GetAttributeValue( AttributeKey.UserId ) ) ) &&
                !string.IsNullOrWhiteSpace( Encryption.DecryptString( GetAttributeValue( AttributeKey.UserPassword ) ) ) &&
                !string.IsNullOrWhiteSpace( GetAttributeValue( AttributeKey.JournalId ) ) &&
                !string.IsNullOrWhiteSpace( GetAttributeValue( AttributeKey.ExportMode ) )
                )
             )
            {
                settings = true;
            }

            return settings;
        }

        #endregion Methods

        protected void ddlReceiptAccountType_SelectedIndexChanged( object sender, EventArgs e )
        {
            SetupOtherReceipts();
            SetExportButtonVisibility();
        }
    }
}
