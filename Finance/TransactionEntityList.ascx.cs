using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Entity;
using System.Linq;

using Rock;
using Rock.Data;
using Rock.Model;
using Rock.Web.UI;
using Rock.Web.UI.Controls;

namespace RockWeb.Plugins.com_kfs.Finance
{
    [DisplayName( "Transaction Entity List" )]
    [Category( "KFS > Finance" )]
    [Description( "List of a transaction's related entities for management." )]
    public partial class TransactionEntityList : RockBlock, ISecondaryBlock, ICustomGridColumns
    {
        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            gTransactionEntities.DataKeyNames = new[] { "Id" };
            gTransactionEntities.ShowActionRow = false;
            gTransactionEntities.GridRebind += gTransactionEntities_GridRebind;

            // only show delete if user can edit block
            if ( UserCanEdit )
            {
                gTransactionEntities.Columns[4].Visible = true;
            }
            else
            {
                gTransactionEntities.Columns[4].Visible = false;
            }
        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );

            if ( !Page.IsPostBack )
            {
                BindGrid();
            }
        }

        /// <summary>
        /// Handles the GridRebind event of the gTransactionEntities control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void gTransactionEntities_GridRebind( object sender, EventArgs e )
        {
            BindGrid();
        }

        /// <summary>
        /// Handles the Delete event of the gTransactionEntities control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RowEventArgs"/> instance containing the event data.</param>
        protected void gTransactionEntities_Delete( object sender, RowEventArgs e )
        {
            var rockContext = new RockContext();
            var service = new FinancialTransactionDetailService( rockContext );
            var detail = service.Get( e.RowKeyId );

            if ( detail == null )
            {
                return;
            }
            else
            {
                var changes = new List<string>();
                var typeName = detail.EntityType.FriendlyName;
                var name = GetEntityName( detail.EntityTypeId, detail.EntityId, rockContext );

                History.EvaluateChange( changes, "Entity Type Id", detail.EntityTypeId, null );
                History.EvaluateChange( changes, "Entity Id", detail.EntityId, null );

                detail.EntityTypeId = null;
                detail.EntityId = null;

                changes.Add( string.Format( "Removed transaction detail association to {0} {1}.", typeName, name ) );

                HistoryService.SaveChanges(
                    rockContext,
                    typeof( FinancialTransaction ),
                    Rock.SystemGuid.Category.HISTORY_FINANCIAL_TRANSACTION.AsGuid(),
                    detail.TransactionId,
                    changes
                );

                rockContext.SaveChanges();
            }

            BindGrid();
        }

        /// <summary>
        /// Binds the grid.
        /// </summary>
        private void BindGrid()
        {
            var rockContext = new RockContext();

            var txnId = PageParameter( "transactionId" ).AsInteger();

            gTransactionEntities.DataSource = new FinancialTransactionDetailService( rockContext )
                .Queryable().AsNoTracking()
                .Where( d =>
                     d.TransactionId == txnId &&
                     d.EntityId != null )
                .OrderBy( d => d.Account.Name )
                .ToList()
                .Select( d => new
                {
                    d.Id,
                    AccountName = d.Account.Name,
                    EntityTypeName = d.EntityType.FriendlyName,
                    Entity = GetEntityName( d.EntityTypeId, d.EntityId, rockContext ),
                    d.Amount
                } )
                .ToList();
            gTransactionEntities.DataBind();

            if ( gTransactionEntities.Rows.Count > 0 )
            {
                pnlTransactionEntities.Visible = true;
            }
            else
            {
                pnlTransactionEntities.Visible = false;
            }
        }

        /// <summary>
        /// Gets the entity name
        /// </summary>
        /// <param name="entityTypeId">The Id of the Entity Type.</param>
        /// <param name="entityId">The Id of the Entity.</param>
        private string GetEntityName( int? entityTypeId, int? entityId, RockContext rockContext )
        {
            var retVal = string.Empty;

            if ( entityTypeId.HasValue && entityId.HasValue )
            {
                var service = new EntityTypeService( rockContext );
                var entity = service.GetEntity( ( int ) entityTypeId, ( int ) entityId );
                if ( entity is Group )
                {
                    retVal = ( ( Group ) entity ).Name;
                }
                else if ( entity is GroupMember )
                {
                    retVal = ( ( GroupMember ) entity ).Person.FullName;
                }
                else if ( entity is DefinedValue )
                {
                    retVal = ( ( DefinedValue ) entity ).Value;
                }
                else if ( entity is RegistrationRegistrant )
                {
                    retVal = ( ( RegistrationRegistrant ) entity ).Person.FullName;
                }
            }

            return retVal;
        }

        /// <summary>
        /// Hook so that other blocks can set the visibility of all ISecondaryBlocks on its page
        /// </summary>
        /// <param name="visible">if set to <c>true</c> [visible].</param>
        public void SetVisible( bool visible )
        {
            pnlTransactionEntities.Visible = visible;
        }
    }
}
