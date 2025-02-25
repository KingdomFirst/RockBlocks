// <copyright>
// Copyright 2023 by Kingdom First Solutions
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

using rocks.kfs.ShelbyFinancials;

namespace RockWeb.Plugins.rocks_kfs.ShelbyFinancials
{
    #region Block Attributes

    [DisplayName( "Shelby Financials Batch to Journal" )]
    [Category( "KFS > Shelby Financials" )]
    [Description( "Block used to create Journal Entries in Shelby Financials from a Rock Financial Batch." )]

    #endregion

    #region Block Settings
    [ContextAware]
    [TextField(
        "Button Text",
        Description = "The text to use in the Export Button.",
        IsRequired = false,
        DefaultValue = "Create Shelby Export",
        Order = 0,
        Key = AttributeKey.ButtonText )]

    [BooleanField(
        "Close Batch",
        Description = "Flag indicating if the Financial Batch be closed in Rock when successfully posted to Intacct.",
        IsRequired = false,
        DefaultBooleanValue = true,
        Order = 1,
        Key = AttributeKey.CloseBatch )]

    [EnumField(
        "GL Account Grouping",
        Description = "Determines if debit and/or credit lines should be grouped and summed by GL account in the export file. NOTE: Unique Projects, Regions, Funds, etc. may result in multiple lines even if account is grouped.",
        IsRequired = true,
        EnumSourceType = typeof( GLEntryGroupingMode ),
        DefaultEnumValue = ( int ) GLEntryGroupingMode.DebitAndCreditByFinancialAccount,
        Order = 2,
        Key = AttributeKey.AccountGroupingMode )]

    [EnumField(
        "Project Handling Mode",
        Description = "Determines if Project should only be applied to debit lines, credit lines, or both debit and credit lines.",
        IsRequired = true,
        EnumSourceType = typeof( GLEntryProjectMode ),
        DefaultEnumValue = ( int ) GLEntryProjectMode.DebitAndCreditLines,
        Order = 3,
        Key = AttributeKey.ProjectHandlingMode )]

    [LavaField(
        "Journal Description Lava",
        Description = "Lava for the journal description column per line. Default: Batch.Id: Batch.Name",
        IsRequired = true,
        DefaultValue = "{{ Batch.Id }}: {{ Batch.Name }}",
        Order = 4,
        Key = AttributeKey.JournalMemoLava )]

    [BooleanField(
        "Enable Debug",
        Description = "Outputs the object graph to help create your Lava syntax.",
        DefaultBooleanValue = false,
        Order = 5,
        Key = AttributeKey.EnableDebug )]

    #endregion

    public partial class BatchToJournal : Rock.Web.UI.RockBlock, ISecondaryBlock
    {
        #region Keys

        /// <summary>
        /// Attribute Keys
        /// </summary>
        private static class AttributeKey
        {
            public const string ButtonText = "ButtonText";
            public const string CloseBatch = "CloseBatch";
            public const string AccountGroupingMode = "AccountGroupingMode";
            public const string JournalMemoLava = "JournalDescriptionLava";
            public const string ProjectHandlingMode = "ProjectHandlingMode";
            public const string EnableDebug = "EnableDebug";
        }

        #endregion Keys

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
            base.OnLoad( e );

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
            var debugEnabled = GetAttributeValue( AttributeKey.EnableDebug ).AsBoolean();

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

                dateExported = ( DateTime? ) _financialBatch.AttributeValues["rocks.kfs.ShelbyFinancials.DateExported"].ValueAsType;

                if ( dateExported != null && dateExported > DateTime.MinValue )
                {
                    isExported = true;
                }
            }

            if ( !isExported )
            {
                btnExportToShelbyFinancials.Text = GetAttributeValue( AttributeKey.ButtonText );
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

            if ( debugEnabled )
            {
                var debugLava = Session["ShelbyFinancialsDebugLava"].ToStringSafe();
                if ( !string.IsNullOrWhiteSpace( debugLava ) )
                {
                    lDebug.Visible = true;
                    lDebug.Text += debugLava;
                    Session["ShelbyFinancialsDebugLava"] = string.Empty;
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

                var debugLava = GetAttributeValue( AttributeKey.EnableDebug );
                sfJournal.GroupingMode = ( GLEntryGroupingMode ) GetAttributeValue( AttributeKey.AccountGroupingMode ).AsInteger();
                sfJournal.ProjectMode = ( GLEntryProjectMode ) GetAttributeValue( AttributeKey.ProjectHandlingMode ).AsInteger();
                sfJournal.JournalMemoLava = GetAttributeValue( AttributeKey.JournalMemoLava );

                var items = sfJournal.GetGLExcelLines( rockContext, _financialBatch, journalCode, period, ref debugLava );

                if ( items.Count > 0 )
                {
                    var excel = sfJournal.GLExcelExport( items );

                    Session["ShelbyFinancialsExcelExport"] = excel;
                    Session["ShelbyFinancialsFileId"] = _financialBatch.Id.ToString();
                    Session["ShelbyFinancialsDebugLava"] = debugLava;

                    // Reload the Financial Batch with the ID so we can update properties and Save Changes with this Context.
                    var financialBatch = new FinancialBatchService( rockContext ).Get( _financialBatch.Id );

                    //
                    // vars we need to know
                    //
                    var changes = new History.HistoryChangeList();

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
                    var oldDate = financialBatch.GetAttributeValue( "rocks.kfs.ShelbyFinancials.DateExported" );
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

                    financialBatch.SetAttributeValue( "rocks.kfs.ShelbyFinancials.DateExported", newDate );
                    financialBatch.SaveAttributeValue( "rocks.kfs.ShelbyFinancials.DateExported", rockContext );
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
                if ( GetAttributeValue( AttributeKey.CloseBatch ).AsBoolean() )
                {
                    History.EvaluateChange( changes, "Status", financialBatch.Status, BatchStatus.Open );
                    financialBatch.Status = BatchStatus.Open;
                }

                //
                // Remove Date Exported
                //
                financialBatch.LoadAttributes();
                var oldDate = financialBatch.GetAttributeValue( "rocks.kfs.ShelbyFinancials.DateExported" ).AsDateTime().ToString();
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

                financialBatch.SetAttributeValue( "rocks.kfs.ShelbyFinancials.DateExported", newDate );
                financialBatch.SaveAttributeValue( "rocks.kfs.ShelbyFinancials.DateExported", rockContext );
            }

            Response.Redirect( Request.RawUrl );
        }

        /// <summary>
        /// Hook so that other blocks can set the visibility of all ISecondaryBlocks on it's page
        /// </summary>
        /// <param name="visible">if set to <c>true</c> [visible].</param>
        public void SetVisible( bool visible )
        {
            upnlSync.Visible = visible;
        }
        #endregion Methods
    }
}
