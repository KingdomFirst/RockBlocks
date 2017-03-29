using System;
using System.ComponentModel;
using System.Linq;

using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Web.UI;

namespace RockWeb.Plugins.com_kfs.Finance
{
    [DisplayName( "Batch Export Details" )]
    [Category( "KFS > Finance" )]
    [Description( "Shows date exported and allows for quick access to Export Page" )]

    [BooleanField( "Show Remove Date Exported Button", "Option to display the 'Remove Date Exported' button.", false, "", 1 )]
    [BooleanField( "Confirm Remove Date Exported", "Option to display confirmation when 'Remove Date Exported' button is clicked.", true, "", 2 )]
    [LinkedPage( "Export Page", "Page where export block is located. If not set, export shortcut will not be displayed.", false )]

    public partial class BatchExportDetails : Rock.Web.UI.RockBlock, ISecondaryBlock
    {
        private DateTime exportDate = DateTime.MinValue;
        private FinancialBatch batch = null;
        private int batchId = 0;

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            this.BlockUpdated += Block_BlockUpdated;
            this.AddConfigurationUpdateTrigger( upnlBatchExportDetails );

            batchId = PageParameter( "batchId" ).AsInteger();

            if ( batchId > 0 )
            {
                batch = GetBatch( batchId );
                batch.LoadAttributes();

                var attValue = batch.GetAttributeValues( "GLExport_BatchExported" ).FirstOrDefault();

                if ( attValue != null )
                {
                    exportDate = (DateTime)attValue.AsDateTime();
                }
            }

            if ( GetAttributeValue( "ConfirmRemoveDateExported" ).AsBoolean() )
            {
                btnRemoveDateExported.OnClientClick = "return confirm('Are you sure you want to delete the Date Exported value?');";
            }
        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );

            ShowPanels();
        }

        /// <summary>
        /// Handles the BlockUpdated event of the control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void Block_BlockUpdated( object sender, EventArgs e )
        {
            ShowPanels();
        }

        /// <summary>
        /// Gets the batch.
        /// </summary>
        /// <param name="batchId">The batch identifier.</param>
        /// <param name="rockContext">The rock context.</param>
        /// <returns></returns>
        private FinancialBatch GetBatch( int batchId, RockContext rockContext = null )
        {
            rockContext = rockContext ?? new RockContext();
            var batch = new FinancialBatchService( rockContext )
                .Queryable()
                .Where( b => b.Id == batchId )
                .FirstOrDefault();
            return batch;
        }

        private void ShowPanels()
        {
            if ( !String.IsNullOrWhiteSpace( GetAttributeValue( "ExportPage" ) ) && exportDate == DateTime.MinValue )
            {
                pnlExportBatchButton.Visible = true;
                pnlDateExported.Visible = false;
            }
            else if ( exportDate != DateTime.MinValue )
            {
                pnlDateExported.Visible = true;
                pnlExportBatchButton.Visible = false;
                litDateExported.Text = string.Format( "Exported: {0}", exportDate );

                if ( GetAttributeValue( "ShowRemoveDateExportedButton" ).AsBoolean() )
                {
                    btnRemoveDateExported.Visible = true;
                    btnRemoveDateExported.Text = "Remove Date Exported";
                }
                else
                {
                    btnRemoveDateExported.Visible = false;
                }
            }
            else
            {
                pnlExportBatchButton.Visible = false;
                pnlDateExported.Visible = false;
            }
        }

        protected void btnRemoveDateExported_Click( object sender, EventArgs e )
        {
            batch.LoadAttributes();

            if ( batch.AttributeValues.ContainsKey( "GLExport_BatchExported" ) )
            {
                batch.SetAttributeValue( "GLExport_BatchExported", string.Empty );
                batch.SaveAttributeValues();
            }

            exportDate = DateTime.MinValue;

            ShowPanels();
        }

        protected void lbExportBatchButton_Click( object sender, EventArgs e )
        {
            NavigateToLinkedPage( "ExportPage", "batchId", batch.Id );
        }

        /// <summary>
        /// Hook so that other blocks can set the visibility of all ISecondaryBlocks on it's page
        /// </summary>
        /// <param name="visible">if set to <c>true</c> [visible].</param>
        public void SetVisible( bool visible )
        {
            pnlBatchExportDetails.Visible = visible;
        }
    }
}