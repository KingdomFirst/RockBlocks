using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Entity;
using System.Linq;
using System.Web.UI;

using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Security;
using Rock.Web.Cache;
using Rock.Web.UI;
using Rock.Web.UI.Controls;

namespace RockWeb.Plugins.com_kfs.CheckIn
{
    /// <summary>
    /// Block that adds a content channel item for person paging.
    /// </summary>
    [DisplayName( "Person Paging" )]
    [Category( "Check-in > Manager" )]
    [Description( "Block that adds a content channel item for person paging." )]
    [ContentChannelField( "Person Paging Content Channel", "The Content Channel that where new Content Channel Items will be created.", category: "Content Channel", order: 0 )]
    [TextField( "Person Attribute Key", "The Attribute Key of the Content Channel Item where the person is stored. Default: Person", true, "Person", category: "Content Channel", order: 1 )]
    [TextField( "Button Text", "The button text to display for adding new Content Channel Items.", false, "Page Person", "Text Options", order: 0 )]
    [CampusField( "Context Campus", "The campus context that this button should be displayed for. If no context is set, the button will alway appear.", false, "", "Context", 0 )]
    [ContextAware]

    public partial class PersonPaging : RockBlock
    {
        #region Fields

        // used for public / protected properties
        Guid? personGuid = null;

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
        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );

            nbWarning.Visible = false;

            var campusEntity = RockPage.GetCurrentContext( EntityTypeCache.Read( typeof( Campus ) ) );
            var campusGuid = GetAttributeValue( "ContextCampus" ).AsGuidOrNull();
            var campusMatch = true;
            if ( campusEntity != null && campusGuid != null )
            {
                var rockContext = new RockContext();
                var campus = new CampusService( rockContext ).Get( (Guid)campusGuid );
                if ( campusEntity.Id != campus.Id )
                {
                    campusMatch = false;
                }
            }

            if ( IsUserAuthorized( Authorization.VIEW ) && campusMatch )
            {
                personGuid = PageParameter( "Person" ).AsGuidOrNull();
                if ( personGuid.HasValue )
                {
                    lbPersonPaging.Text = GetAttributeValue( "ButtonText" );
                    lbPersonPaging.Visible = true;
                }
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

        }

        protected void lbPersonPaging_Click( object sender, EventArgs e )
        {
            var rockContext = new RockContext();
            var personService = new PersonService( rockContext );
            var person = personService.Queryable().FirstOrDefault( a => a.Guid == personGuid );
            var attributeKey = GetAttributeValue( "PersonAttributeKey" );
            var contentChannel = new ContentChannelService( rockContext ).Get( GetAttributeValue( "PersonPagingContentChannel" ).AsGuid() );
            var contentChannelItem = new ContentChannelItem
            {
                Title = string.Empty,
                Status = ContentChannelItemStatus.Approved,
                Content = string.Empty,
                ContentChannelId = contentChannel.Id,
                ContentChannelTypeId = contentChannel.ContentChannelTypeId,
                Priority = 0,
                StartDateTime = DateTime.Now,
                Order = 0,
                Guid = new Guid()
            };
            contentChannelItem.LoadAttributes();

            if ( !contentChannelItem.Attributes.ContainsKey( attributeKey ) )
            {
                nbWarning.Text = "The selected Content Channel is not configured with provided Attribute Key.";
                nbWarning.Visible = true;
            }
            else
            {
                var contentChannelItems = new ContentChannelItemService( rockContext ).Queryable().AsNoTracking().Where( i => i.ContentChannelId.Equals( contentChannel.Id ) ).ToList();
                var exists = false;
                foreach ( var item in contentChannelItems )
                {
                    item.LoadAttributes();
                    if ( item.AttributeValues[attributeKey].Value.Equals( person.PrimaryAlias.Guid.ToString() ) )
                    {
                        exists = true;
                    }
                }

                if ( exists )
                {
                    nbWarning.Text = string.Format( "{0} is already on the list.", person.FullName );
                    nbWarning.Visible = true;
                }
                else
                {
                    contentChannelItem.AttributeValues[attributeKey].Value = person.PrimaryAlias.Guid.ToString();

                    rockContext.WrapTransaction( () =>
                    {
                        rockContext.ContentChannelItems.Add( contentChannelItem );
                        rockContext.SaveChanges();
                        contentChannelItem.SaveAttributeValues( rockContext );
                    } );

                    nbWarning.Text = string.Format( "{0} added to the list.", person.FullName );
                    nbWarning.Visible = true;
                }
            }
        }

        #endregion
    }
}