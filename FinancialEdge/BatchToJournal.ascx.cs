using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Web.UI;
using com.kfs.FinancialEdge;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Security;
using Rock.Web.UI;

namespace RockWeb.Plugins.com_kfs.FinancialEdge
{
    [DisplayName( "Financial Edge Batch to Journal" )]
    [Category( "com_kfs > Financial Edge" )]
    [Description( "Block used to create Journal Entries in Intacct from a Rock Financial Batch." )]
    [TextField( "Button Text", "The text to use in the Export Button.", false, "Create FE CSV", "", 0 )]
    [TextField( "Journal Type", "The Financial Edge Journal to post in. For example: JE", true, "", "", 1 )]
    [CustomDropdownListField( "Journal Reference Style", "Option to indicate how the Journal Reference text should be created.", "0^Use Batch Name,1^Use Account Name", true, "0", "", 2 )]
    [BooleanField( "Close Batch", "Flag indicating if the Financial Batch be closed in Rock when successfully posted to Intacct.", true, "", 3 )]
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

                dateExported = ( DateTime? ) _financialBatch.AttributeValues["com.kfs.FinancialEdge.DateExported"].ValueAsType;

                if ( dateExported != null && dateExported > DateTime.MinValue )
                {
                    isExported = true;
                }
            }

            if ( ValidSettings() && !isExported )
            {
                btnExportToFinancialEdge.Text = GetAttributeValue( "ButtonText" );
                btnExportToFinancialEdge.Visible = true;
                if ( variance == 0 )
                {
                    btnExportToFinancialEdge.Enabled = true;
                }
                else
                {
                    btnExportToFinancialEdge.Enabled = false;
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

        protected void btnExportToFinancialEdge_Click( object sender, EventArgs e )
        {
            if ( _financialBatch != null )
            {
                var rockContext = new RockContext();
                var feJournal = new FEJournal();

                List<JournalEntryLine> items = feJournal.GetGlEntries( rockContext, _financialBatch, GetAttributeValue( "JournalType" ), ( ReferenceStyle ) GetAttributeValue( "JournalReferenceStyle" ).AsInteger() );

                if ( items.Count > 0 )
                {
                    //
                    // Set session for export file
                    //
                    feJournal.SetFinancialEdgeSessions( items, _financialBatch.Id.ToString() );
                    
                    //
                    // vars we need now
                    //
                    var financialBatch = new FinancialBatchService( rockContext ).Get( _batchId );
                    var changes = new List<string>();

                    //
                    // Close Batch if we're supposed to
                    //
                    if ( GetAttributeValue( "CloseBatch" ).AsBoolean() )
                    {
                        History.EvaluateChange( changes, "Status", financialBatch.Status, BatchStatus.Closed );
                        financialBatch.Status = BatchStatus.Closed;
                    }

                    //
                    // Set Date Exported
                    //
                    financialBatch.LoadAttributes();
                    var oldDate = financialBatch.GetAttributeValue( "com.kfs.FinancialEdge.DateExported" );
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

                    financialBatch.SetAttributeValue( "com.kfs.FinancialEdge.DateExported", newDate );
                    financialBatch.SaveAttributeValue( "com.kfs.FinancialEdge.DateExported", rockContext );
                }
            }

            Response.Redirect( Request.RawUrl );
        }

        protected void btnRemoveDateExported_Click( object sender, EventArgs e )
        {
            if ( _financialBatch != null )
            {
                var rockContext = new RockContext();
                var financialBatch = new FinancialBatchService( rockContext ).Get( _batchId );
                var changes = new List<string>();

                //
                // Open Batch is we Closed it
                //
                if ( GetAttributeValue( "CloseBatch" ).AsBoolean() )
                {
                    History.EvaluateChange( changes, "Status", financialBatch.Status, BatchStatus.Open );
                    financialBatch.Status = BatchStatus.Open;
                }

                //
                // Remove Date Exported
                //
                financialBatch.LoadAttributes();
                var oldDate = financialBatch.GetAttributeValue( "com.kfs.FinancialEdge.DateExported" ).AsDateTime().ToString();
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

                financialBatch.SetAttributeValue( "com.kfs.FinancialEdge.DateExported", newDate );
                financialBatch.SaveAttributeValue( "com.kfs.FinancialEdge.DateExported", rockContext );
            }

            Response.Redirect( Request.RawUrl );
        }

        protected bool ValidSettings()
        {
            var settings = false;

            if ( _batchId > 0 && !string.IsNullOrWhiteSpace( GetAttributeValue( "JournalType" ) ) )
            {
                settings = true;
            }

            return settings;
        }

        #endregion Methods
    }
}