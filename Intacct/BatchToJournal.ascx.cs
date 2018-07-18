using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using com.kfs.Intacct;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Security;
using Rock.Web.UI;

namespace RockWeb.Plugins.com_kfs.Intacct
{
    [DisplayName( "Intacct Batch to Journal" )]
    [Category( "com_kfs > Intacct" )]
    [Description( "Block used to create Journal Entries in Intacct from a Rock Financial Batch." )]
    [TextField( "Journal Id", "The Intacct Symbol of the Journal that the Entry should be posted to. For example: GJ", true, "", "", 0 )]
    [TextField( "Button Text", "The text to use in the Export Button.", false, "Export to Intacct", "", 1 )]
    [BooleanField( "Close Batch", "Flag indicating if the Financial Batch be closed in Rock when successfully posted to Intacct.", true, "", 2 )]
    [BooleanField( "Log Response", "Flag indicating if the Intacct Response should be logged to the Batch Audit Log", true, "", 3 )]
    [EncryptedTextField( "Sender Id", "The Intacct Sender Id", true, "", "Configuration", 0 )]
    [EncryptedTextField( "Sender Password", "The Intacct Sender Password", true, "", "Configuration", 1 )]
    [EncryptedTextField( "Company Id", "The Intacct Sender Id", true, "", "Configuration", 2 )]
    [EncryptedTextField( "User Id", "The Intacct Sender Id", true, "", "Configuration", 3 )]
    [EncryptedTextField( "User Password", "The Intacct Sender Id", true, "", "Configuration", 4 )]
    [EncryptedTextField( "Location Id", "The Intacct Sender Id", false, "", "Configuration", 5 )]
    public partial class BatchToJournal : RockBlock
    {
        private int _batchId = 0;
        private FinancialBatch _financialBatch = null;

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

            _financialBatch = new FinancialBatchService( rockContext ).Get( _batchId );
            DateTime? dateExported = null;

            decimal variance = 0;

            if ( _financialBatch != null )
            {
                var financialTransactionDetailService = new FinancialTransactionDetailService( rockContext );
                var qryTransactionDetails = financialTransactionDetailService.Queryable().Where( a => a.Transaction.BatchId == _financialBatch.Id );
                decimal txnTotal = qryTransactionDetails.Select( a => ( decimal? ) a.Amount ).Sum() ?? 0;
                variance = txnTotal - _financialBatch.ControlAmount;

                _financialBatch.LoadAttributes();

                dateExported = ( DateTime? ) _financialBatch.AttributeValues["com.kfs.Intacct.DateExported"].ValueAsType;

                if ( dateExported != null && dateExported > DateTime.MinValue )
                {
                    isExported = true;
                }
            }

            if ( ValidSettings() && !isExported )
            {
                btnExportToIntacct.Text = GetAttributeValue( "ButtonText" );
                btnExportToIntacct.Visible = true;
                if ( variance == 0 )
                {
                    btnExportToIntacct.Enabled = true;
                }
                else
                {
                    btnExportToIntacct.Enabled = false;
                }
            }
            else if ( isExported )
            {
                litDateExported.Text = string.Format( "<div class=\"small\">Exported: {0}</div>", dateExported.ToRelativeDateString() );
                litDateExported.Visible = true;

                if ( UserCanEdit )
                {
                    btnRemoveDate.Visible = true;
                }
            }
        }

        protected void btnExportToIntacct_Click( object sender, EventArgs e )
        {
            if ( _financialBatch != null )
            {
                //
                // Get Intacct Auth
                //

                var intacctAuth = new IntacctAuth()
                {
                    SenderId = Encryption.DecryptString( GetAttributeValue( "SenderId" ) ),
                    SenderPassword = Encryption.DecryptString( GetAttributeValue( "SenderPassword" ) ),
                    CompanyId = Encryption.DecryptString( GetAttributeValue( "CompanyId" ) ),
                    UserId = Encryption.DecryptString( GetAttributeValue( "UserId" ) ),
                    UserPassword = Encryption.DecryptString( GetAttributeValue( "UserPassword" ) ),
                    LocationId = Encryption.DecryptString( GetAttributeValue( "LocationId" ) )
                };

                //
                // Create Intacct Journal XML and Post to Intacct
                //

                var journal = new IntacctJournal();
                var endpoint = new IntacctEndpoint();

                var postXml = journal.CreateJournalEntryXML( intacctAuth, _financialBatch.Id, GetAttributeValue( "JournalId" ) );
                var resultXml = endpoint.PostToIntacct( postXml );
                var success = endpoint.ParseEndpointResponse( resultXml, _financialBatch.Id, GetAttributeValue( "LogResponse" ).AsBoolean() );

                if ( success )
                {
                    var rockContext = new RockContext();
                    var financialBatch = new FinancialBatchService( rockContext ).Get( _batchId );

                    //
                    // Close Batch
                    //

                    var changes = new List<string>();

                    if ( GetAttributeValue( "CloseBatch" ).AsBoolean() )
                    {
                        History.EvaluateChange( changes, "Status", financialBatch.Status, BatchStatus.Closed );
                        financialBatch.Status = BatchStatus.Closed;
                    }

                    //
                    // Set Date Exported
                    //

                    financialBatch.LoadAttributes();

                    var oldDate = financialBatch.GetAttributeValue( "com.kfs.Intacct.DateExported" );
                    var newDate = RockDateTime.Now;

                    History.EvaluateChange( changes, "Date Exported", oldDate, newDate.ToString() );

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

                    financialBatch.SetAttributeValue( "com.kfs.Intacct.DateExported", newDate );
                    financialBatch.SaveAttributeValue( "com.kfs.Intacct.DateExported", rockContext );
                }
            }

            Response.Redirect( Request.RawUrl );
        }

        protected void btnRemoveDateExported_Click( object sender, EventArgs e )
        {
            if ( _financialBatch != null )
            {
                _financialBatch.LoadAttributes();

                var changes = new List<string>();
                var oldDate = _financialBatch.GetAttributeValue( "com.kfs.Intacct.DateExported" ).AsDateTime().ToString();
                var newDate = string.Empty;

                History.EvaluateChange( changes, "Date Exported", oldDate, newDate );

                var rockContext = new RockContext();
                rockContext.WrapTransaction( () =>
                {
                    if ( changes.Any() )
                    {
                        HistoryService.SaveChanges(
                            rockContext,
                            typeof( FinancialBatch ),
                            Rock.SystemGuid.Category.HISTORY_FINANCIAL_BATCH.AsGuid(),
                            _financialBatch.Id,
                            changes );
                    }
                } );

                _financialBatch.SetAttributeValue( "com.kfs.Intacct.DateExported", newDate );
                _financialBatch.SaveAttributeValue( "com.kfs.Intacct.DateExported", rockContext );
            }

            Response.Redirect( Request.RawUrl );
        }

        protected bool ValidSettings()
        {
            var settings = false;

            if (
                _batchId > 0 &&
                (
                !string.IsNullOrWhiteSpace( Encryption.DecryptString( GetAttributeValue( "SenderId" ) ) ) &&
                !string.IsNullOrWhiteSpace( Encryption.DecryptString( GetAttributeValue( "SenderPassword" ) ) ) &&
                !string.IsNullOrWhiteSpace( Encryption.DecryptString( GetAttributeValue( "CompanyId" ) ) ) &&
                !string.IsNullOrWhiteSpace( Encryption.DecryptString( GetAttributeValue( "UserId" ) ) ) &&
                !string.IsNullOrWhiteSpace( Encryption.DecryptString( GetAttributeValue( "UserPassword" ) ) ) &&
                !string.IsNullOrWhiteSpace( GetAttributeValue( "JournalId" ) )
                )
             )
            {
                settings = true;
            }

            return settings;
        }

        #endregion Methods
    }
}