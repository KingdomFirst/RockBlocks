using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Web.UI;
using OfficeOpenXml;
using com.kfs.ShelbyBatchExport;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Security;
using Rock.Utility;
using Rock.Web.UI;

namespace RockWeb.Plugins.com_kfs.ShelbyFinancials
{
    [DisplayName( "Shelby Financials Batch to Journal" )]
    [Category( "com_kfs > Shelby Financials" )]
    [Description( "Block used to create Journal Entries in Intacct from a Rock Financial Batch." )]
    [TextField( "Button Text", "The text to use in the Export Button.", false, "Create Shelby Export", "", 0 )]
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

            object journalType = Session["JournalType"];
            if ( journalType != null )
            {
                ddlJournalType.SetValue( journalType.ToString() );
            }
            object accountingPeriod = Session["AccountingPeriod"];
            if ( accountingPeriod != null )
            {
                tbAccountingPeriod.Text = accountingPeriod.ToString();
            }

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

                dateExported = ( DateTime? ) _financialBatch.AttributeValues["com.kfs.ShelbyFinancials.DateExported"].ValueAsType;

                if ( dateExported != null && dateExported > DateTime.MinValue )
                {
                    isExported = true;
                }
            }

            if ( !isExported )
            {
                btnExportToShelbyFinancials.Text = GetAttributeValue( "ButtonText" );
                btnExportToShelbyFinancials.Visible = true;
                ddlJournalType.Visible = true;
                tbAccountingPeriod.Visible = true;
                if ( variance == 0 )
                {
                    btnExportToShelbyFinancials.Enabled = true;
                }
                else
                {
                    btnExportToShelbyFinancials.Enabled = false;
                }
            }
            else
            {
                pnlExportedDetails.Visible = true;

                litDateExported.Text = string.Format( "<div class=\"small\">Exported: {0}</div>", dateExported.ToRelativeDateString() );
                litDateExported.Visible = true;

                if ( UserCanEdit )
                {
                    btnRemoveDate.Visible = true;
                }
            }
        }

        protected void btnExportToShelbyFinancials_Click( object sender, EventArgs e )
        {
            Session["JournalType"] = ddlJournalType.SelectedValue;
            Session["AccountingPeriod"] = tbAccountingPeriod.Text;

            if ( _financialBatch != null )
            {
                var rockContext = new RockContext();
                
                var sfJournal = new SFJournal();
                var journalCode = ddlJournalType.SelectedValue;
                var period = tbAccountingPeriod.Text.AsInteger();

                var items = sfJournal.GetGLExcelLines( rockContext, _financialBatch, journalCode, period );

                if ( items.Count > 0 )
                {
                    var excel = sfJournal.GLExcelExport( items );

                    Session["ShelbyFinancialsExcelExport"] = excel;
                    Session["ShelbyFinancialsFileId"] = _financialBatch.Id.ToString();

                    //
                    // vars we need to know
                    //
                    var financialBatch = new FinancialBatchService( rockContext ).Get( _batchId );
                    var changes = new History.HistoryChangeList();

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
                    var oldDate = financialBatch.GetAttributeValue( "com.kfs.ShelbyFinancials.DateExported" );
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

                    financialBatch.SetAttributeValue( "com.kfs.ShelbyFinancials.DateExported", newDate );
                    financialBatch.SaveAttributeValue( "com.kfs.ShelbyFinancials.DateExported", rockContext );
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
                var changes = new History.HistoryChangeList();

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
                var oldDate = financialBatch.GetAttributeValue( "com.kfs.ShelbyFinancials.DateExported" ).AsDateTime().ToString();
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

                financialBatch.SetAttributeValue( "com.kfs.ShelbyFinancials.DateExported", newDate );
                financialBatch.SaveAttributeValue( "com.kfs.ShelbyFinancials.DateExported", rockContext );
            }

            Response.Redirect( Request.RawUrl );
        }

        #endregion Methods
    }
}