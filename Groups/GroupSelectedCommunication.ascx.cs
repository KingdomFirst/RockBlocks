using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Entity;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;

using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Security;
using Rock.Web.Cache;
using Rock.Web.UI.Controls;

namespace RockWeb.Plugins.com_kfs.Groups
{
    #region Block Attributes

    [DisplayName( "Group Member Selected Communication" )]
    [Category( "KFS > Groups" )]
    [Description( "Presents a button when combined with the group detail lava block with checkbox html inserted into the lava." )]

    [LinkedPage( "Communication Page", "The communication page to use for sending emails to the group members.", true, "", "", 1 )]
    [LinkedPage( "Alternate Communication Page", "The communication page to use for sending an alternate communication to the group members.", false, "", "", 2 )]

    #endregion

    public partial class GroupSelectedCommunication : Rock.Web.UI.RockBlock
    {
        #region Fields

        // used for private variables
        private int _groupId = 0;

        #endregion

        #region Properties

        #endregion

        #region Base Control Methods

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            // this event gets fired after block settings are updated. it's nice to repaint the screen if these settings would alter it
            this.BlockUpdated += Block_BlockUpdated;
            this.AddConfigurationUpdateTrigger( upnlContent );

            // get the group id
            if ( !string.IsNullOrWhiteSpace( PageParameter( "GroupId" ) ) )
            {
                _groupId = Convert.ToInt32( PageParameter( "GroupId" ) );
            }
        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );

            if ( !IsPostBack )
            {
                BlockSetup();
            }

        }

        #endregion

        #region Events

        // handlers called by the controls on your block

        /// <summary>
        /// Handles the BlockUpdated event of the control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void Block_BlockUpdated( object sender, EventArgs e )
        {
            BlockSetup();
        }

        /// <summary>
        /// Handles the Click event of the btnEmailSelected control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void btnEmailGroup_Click( object sender, EventArgs e )
        {
            SendCommunication();
        }

        /// <summary>
        /// Handles the Click event of the btnCommunicateSelected control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void btnAlternateGroup_Click( object sender, EventArgs e )
        {
            SendAlternateCommunication();
        }

        #endregion

        #region Methods

        /// <summary>
        /// setup block
        /// </summary>
        private void BlockSetup()
        {
            btnCommunicateSelected.Visible = !string.IsNullOrWhiteSpace( GetAttributeValue( "AlternateCommunicationPage" ) );
        }

        private Dictionary<string, string> CreateCommunication()
        {
            var rockContext = new RockContext();
            var service = new Rock.Model.CommunicationService( rockContext );
            var communication = new Rock.Model.Communication();
            communication.IsBulkCommunication = false;
            communication.Status = Rock.Model.CommunicationStatus.Transient;

            communication.SenderPersonAliasId = this.CurrentPersonAliasId;

            service.Add( communication );

            var personAliasIds = new GroupMemberService( rockContext ).Queryable()
                                .Where( m => m.GroupId == _groupId && m.GroupMemberStatus != GroupMemberStatus.Inactive )
                                .ToList()
                                .Select( m => m.Person.PrimaryAliasId )
                                .ToList();

            // Get the primary aliases
            foreach ( int personAlias in personAliasIds )
            {
                var recipient = new Rock.Model.CommunicationRecipient();
                recipient.PersonAliasId = personAlias;
                communication.Recipients.Add( recipient );
            }

            rockContext.SaveChanges();

            Dictionary<string, string> queryParameters = new Dictionary<string, string>();
            queryParameters.Add( "CommunicationId", communication.Id.ToString() );

            return queryParameters;
        }
        
        /// <summary>
        /// Sends the communication.
        /// </summary>
        private void SendCommunication()
        {
            // create communication
            if ( this.CurrentPerson != null && _groupId != -1 && !string.IsNullOrWhiteSpace( GetAttributeValue( "CommunicationPage" ) ) )
            {
                var queryParameters = CreateCommunication();
                NavigateToLinkedPage( "CommunicationPage", queryParameters );
            }
        }

        /// <summary>
        /// Sends the communication.
        /// </summary>
        private void SendAlternateCommunication()
        {
            // create communication
            if ( this.CurrentPerson != null && _groupId != -1 && !string.IsNullOrWhiteSpace( GetAttributeValue( "AlternateCommunicationPage" ) ) )
            {
                var queryParameters = CreateCommunication();
                NavigateToLinkedPage( "AlternateCommunicationPage", queryParameters );
            }
        }

        #endregion
    }
}
