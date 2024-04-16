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
using System.ComponentModel;
using System.Linq;

using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Web.UI;

using rocks.kfs.FinancialEdge;

namespace RockWeb.Plugins.rocks_kfs.FinancialEdge
{
    #region Block Attributes

    [DisplayName( "Financial Edge Batch to Journal" )]
    [Category( "KFS > Financial Edge" )]
    [Description( "Block used to create a CSV file that can be imported to Financial Edge from a Rock Financial Batch." )]

    [ContextAware]

    #endregion

    #region Block Settings

    [TextField( "Button Text", "The text to use in the Export Button.", false, "Create FE CSV", "", 0 )]
    [TextField( "Journal Type", "The Financial Edge Journal to post in. For example: JE", true, "", "", 1 )]
    [BooleanField( "Close Batch", "Flag indicating if the Financial Batch be closed in Rock when a CSV file is created.", true, "", 3 )]

    #endregion

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
            base.OnInit( e );

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
            try
            {
                var contextEntity = this.ContextEntity();
                if ( contextEntity != null && contextEntity is FinancialBatch )
                {
                    _financialBatch = contextEntity as FinancialBatch;
                }
            }
            catch { }
            ShowDetail();
        }

        #endregion Control Methods

        #region Methods

        protected void ShowDetail()
        {
            var rockContext = new RockContext();
            var isExported = false;

            if ( _financialBatch == null )
            {
                _financialBatch = new FinancialBatchService( rockContext ).Get( _batchId );
            }
            DateTime? dateExported = null;

            decimal variance = 0;

            if ( _financialBatch != null )
            {
                var financialTransactionDetailService = new FinancialTransactionDetailService( rockContext );
                var qryTransactionDetails = financialTransactionDetailService.Queryable().Where( a => a.Transaction.BatchId == _financialBatch.Id );
                decimal txnTotal = qryTransactionDetails.Select( a => ( decimal? ) a.Amount ).Sum() ?? 0;
                variance = txnTotal - _financialBatch.ControlAmount;

                _financialBatch.LoadAttributes();

                dateExported = ( DateTime? ) _financialBatch.AttributeValues["rocks.kfs.FinancialEdge.DateExported"].ValueAsType;

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
                // Reload the Financial Batch with the ID so we can update properties and Save Changes with this Context.
                var rockContext = new RockContext();
                var financialBatch = new FinancialBatchService( rockContext ).Get( _financialBatch.Id );
                var feJournal = new FEJournal();

                var items = feJournal.GetGlEntries( rockContext, financialBatch, GetAttributeValue( "JournalType" ) );

                if ( items.Count > 0 )
                {
                    //
                    // Set session for export file
                    //
                    feJournal.SetFinancialEdgeSessions( items, financialBatch.Id.ToString() );

                    //
                    // vars we need now
                    //
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
                    var oldDate = financialBatch.GetAttributeValue( "rocks.kfs.FinancialEdge.DateExported" );
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

                        rockContext.SaveChanges(); // Don't just rely on SaveChanges within the SaveAttributeValues method.

                    } );

                    financialBatch.SetAttributeValue( "rocks.kfs.FinancialEdge.DateExported", newDate );
                    financialBatch.SaveAttributeValue( "rocks.kfs.FinancialEdge.DateExported", rockContext );
                }
            }

            Response.Redirect( Request.RawUrl );
        }

        protected void btnRemoveDateExported_Click( object sender, EventArgs e )
        {
            if ( _financialBatch != null )
            {
                // Reload the Financial Batch with the ID so we can update properties and Save Changes with this Context.
                var rockContext = new RockContext();
                var financialBatch = new FinancialBatchService( rockContext ).Get( _financialBatch.Id );
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
                var oldDate = financialBatch.GetAttributeValue( "rocks.kfs.FinancialEdge.DateExported" ).AsDateTime().ToString();
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

                    rockContext.SaveChanges(); // Don't just rely on SaveChanges within the SaveAttributeValues method.
                } );

                financialBatch.SetAttributeValue( "rocks.kfs.FinancialEdge.DateExported", newDate );
                financialBatch.SaveAttributeValue( "rocks.kfs.FinancialEdge.DateExported", rockContext );
            }

            Response.Redirect( Request.RawUrl );
        }

        protected bool ValidSettings()
        {
            var settings = false;

            if ( _financialBatch != null && !string.IsNullOrWhiteSpace( GetAttributeValue( "JournalType" ) ) )
            {
                settings = true;
            }

            return settings;
        }

        #endregion Methods
    }
}
