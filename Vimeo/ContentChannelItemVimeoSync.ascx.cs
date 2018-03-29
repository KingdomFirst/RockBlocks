using System;
using System.ComponentModel;
using System.Linq;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Security;
using com.kfs.Vimeo;
using VimeoDotNet;
using System.Web.UI.WebControls;

namespace RockWeb.Plugins.com_kfs.Vimeo
{
    #region Block Attributes

    [DisplayName( "Content Channel Item Vimeo Sync" )]
    [Category( "KFS > Vimeo" )]
    [Description( "Syncs Vimeo data into Content Channel Item Attributes." )]

    [TextField( "Vimeo Id Key", "The attribute key containing the Vimeo Id", true, "", "", 0 )]
    [EncryptedTextField( "Access Token", "The authentication token for Vimeo.", true, "", "", 1 )]
    [BooleanField( "Preview", "Flag indicating if a preview of the Vimeo video should displayed.", true, "", 2 )]
    [TextField( "Preview Width", "The bootstrap column width to display the preview in. Default: col-sm-3", false, "col-sm-3", "", 3 )]
    [BooleanField( "Sync Name", "Flag indicating if video name should be stored.", true, "", 4 )]
    [BooleanField( "Sync Description", "Flag indicating if video description should be stored.", true, "", 5 )]
    [TextField( "Image Attribute Key", "The Image Attribute Key that the Vimeo Image URL should be stored in. Leave blank to never sync.", false, "", "", 6 )]
    [IntegerField( "Image Width", "The desired width of image to store link to.", false, 1920, "", 7 )]
    [TextField( "Duration Attribute Key", "The Duration Attribute Key that the Vimeo Duration should be stored in. Leave blank to never sync.", false, "", "", 8 )]
    [TextField( "HD Video Attribute Key", "The HD Video Attribute Key that the HD Video should be stored in. Leave blank to never sync.", false, "", "", 9 )]
    [TextField( "SD Video Attribute Key", "The SD Video Attribute Key that the SD Video should be stored in. Leave blank to never sync.", false, "", "", 10 )]
    [TextField( "HLS Video Attribute Key", "The HLS Video Attribute Key that the HLS Video should be stored in. Leave blank to never sync.", false, "", "", 11 )]

    #endregion

    public partial class VimeoSync : Rock.Web.UI.RockBlock
    {
        #region Fields

        private int _vimeoId = 0;
        //ContentChannelItem _contentItem = null;
        private int _contentItemId = 0;
        private string _accessToken = string.Empty;
        private string _imageAttributeKey = string.Empty;
        private string _durationAttributeKey = string.Empty;
        private string _hdVideoAttributeKey = string.Empty;
        private string _sdVideoAttributeKey = string.Empty;
        private string _hlsVideoAttributeKey = string.Empty;

        #endregion

        #region Control Methods

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            _accessToken = Encryption.DecryptString( GetAttributeValue( "AccessToken" ) );
            _contentItemId = PageParameter( "contentItemId" ).AsInteger();
        }

        /// <summary>
        /// Handles the BlockUpdated event of the Block control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void Block_BlockUpdated( object sender, EventArgs e )
        {
            ShowDetail( _contentItemId );
        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            ShowDetail( _contentItemId );
        }

        #endregion

        #region Methods

        protected void ShowDetail( int contentItemId )
        {
            var rockContext = new RockContext();
            var contentItem = new ContentChannelItem();

            if ( contentItemId != 0 )
            {
                contentItem = new ContentChannelItemService( rockContext )
                    .Queryable( "ContentChannel,ContentChannelType" )
                    .FirstOrDefault( t => t.Id == contentItemId );
            }
            if ( contentItem == null || contentItem.Id == 0 )
            {
                pnlVimeoSync.Visible = false;
            }
            else
            {
                if ( contentItem.Attributes == null )
                {
                    contentItem.LoadAttributes();
                }
                _vimeoId = contentItem.GetAttributeValue( GetAttributeValue( "VimeoIdKey" ) ).AsInteger();
                if ( _vimeoId == 0 )
                {
                    pnlVimeoSync.Visible = false;
                }
                else
                {
                    cblSyncOptions.Items.Clear();

                    _imageAttributeKey = GetAttributeValue( "ImageAttributeKey" );
                    _durationAttributeKey = GetAttributeValue( "DurationAttributeKey" );
                    _hdVideoAttributeKey = GetAttributeValue( "HDVideoAttributeKey" );
                    _sdVideoAttributeKey = GetAttributeValue( "SDVideoAttributeKey" );
                    _hlsVideoAttributeKey = GetAttributeValue( "HLSVideoAttributeKey" );

                    if ( GetAttributeValue( "Preview" ).AsBoolean() )
                    {
                        litPreview.Text = string.Format( "<div class=\"{0}\"><div class=\"embed-responsive embed-responsive-16by9\"><iframe class=\"embed-responsive-item\" src=\"https://player.vimeo.com/video/{1}\"></iframe></div></div>", GetAttributeValue( "PreviewWidth" ), _vimeoId );
                        litPreview.Visible = true;
                    }
                    else
                    {
                        litPreview.Visible = false;
                    }

                    if ( GetAttributeValue( "SyncName" ).AsBoolean() )
                    {
                        var item = new ListItem();
                        item.Text = "Name";
                        item.Selected = true;
                        cblSyncOptions.Items.Add( item );
                    }

                    if ( GetAttributeValue( "SyncDescription" ).AsBoolean() )
                    {
                        var item = new ListItem();
                        item.Text = "Description";
                        item.Selected = true;
                        cblSyncOptions.Items.Add( item );
                    }

                    if ( !string.IsNullOrWhiteSpace( _imageAttributeKey ) )
                    {
                        var item = new ListItem();
                        item.Text = "Image";
                        item.Selected = true;
                        cblSyncOptions.Items.Add( item );
                    }
                    if ( !string.IsNullOrWhiteSpace( _durationAttributeKey ) )
                    {
                        var item = new ListItem();
                        item.Text = "Duration";
                        item.Selected = true;
                        cblSyncOptions.Items.Add( item );
                    }
                    if ( !string.IsNullOrWhiteSpace( _hdVideoAttributeKey ) )
                    {
                        var item = new ListItem();
                        item.Text = "HD Video";
                        item.Selected = true;
                        cblSyncOptions.Items.Add( item );
                    }
                    if ( !string.IsNullOrWhiteSpace( _sdVideoAttributeKey ) )
                    {
                        var item = new ListItem();
                        item.Text = "SD Video";
                        item.Selected = true;
                        cblSyncOptions.Items.Add( item );
                    }
                    if ( !string.IsNullOrWhiteSpace( _hlsVideoAttributeKey ) )
                    {
                        var item = new ListItem();
                        item.Text = "HLS Video";
                        item.Selected = true;
                        cblSyncOptions.Items.Add( item );
                    }

                    if ( cblSyncOptions.Items.Count > 0 )
                    {
                        pnlVimeoSync.Visible = true;
                    }
                    else
                    {
                        pnlVimeoSync.Visible = false;
                    }
                }
            }
        }

        protected void btnVimeoSync_Click( object sender, EventArgs e )
        {
            using ( var rockContext = new RockContext() )
            {
                SyncVimeo( rockContext );
            }
            Response.Redirect( Request.RawUrl );
        }

        private void SyncVimeo( RockContext rockContext )
        {
            var contentItem = new ContentChannelItemService( rockContext )
                    .Queryable( "ContentChannel,ContentChannelType" )
                    .FirstOrDefault( t => t.Id == _contentItemId );

            if ( contentItem.Attributes == null )
            {
                contentItem.LoadAttributes();
            }

            long videoId = _vimeoId;
            var client = new VimeoClient( _accessToken );
            var vimeo = new Video();
            var width = GetAttributeValue( "ImageWidth" ).AsInteger();
            var video = vimeo.GetVideoInfo( client, videoId, width );

            var cbName = cblSyncOptions.Items.FindByValue( "Name" );
            if ( cbName != null && cbName.Selected == true )
            {
                contentItem.Title = video.name;
            }

            var cbDescription = cblSyncOptions.Items.FindByValue( "Description" );
            if ( cbDescription != null && cbDescription.Selected == true )
            {
                contentItem.Content = video.description;
            }

            var cbImage = cblSyncOptions.Items.FindByValue( "Image" );
            if ( cbImage != null && cbImage.Selected == true )
            {
                contentItem.AttributeValues[_imageAttributeKey].Value = video.imageUrl;
            }

            var cbDuration = cblSyncOptions.Items.FindByValue( "Duration" );
            if ( cbDuration != null && cbDuration.Selected == true )
            {
                contentItem.AttributeValues[_durationAttributeKey].Value = video.duration.ToString();
            }

            var cbHDVideo = cblSyncOptions.Items.FindByValue( "HD Video" );
            if ( cbHDVideo != null && cbHDVideo.Selected == true && !string.IsNullOrWhiteSpace( video.hdLink ) )
            {
                contentItem.AttributeValues[_hdVideoAttributeKey].Value = video.hdLink;
            }

            var cbSDVideo = cblSyncOptions.Items.FindByValue( "SD Video" );
            if ( cbSDVideo != null && cbSDVideo.Selected == true && !string.IsNullOrWhiteSpace( video.sdLink ) )
            {
                contentItem.AttributeValues[_sdVideoAttributeKey].Value = video.sdLink;
            }

            var cbHLSVideo = cblSyncOptions.Items.FindByValue( "HLS Video" );
            if ( cbHLSVideo != null && cbHLSVideo.Selected == true && !string.IsNullOrWhiteSpace( video.hlsLink ) )
            {
                contentItem.AttributeValues[_hlsVideoAttributeKey].Value = video.hlsLink;
            }

            // Save Everything
            rockContext.WrapTransaction( () =>
            {
                rockContext.SaveChanges();
                contentItem.SaveAttributeValues( rockContext );
            } );
        }

        #endregion

    }
}