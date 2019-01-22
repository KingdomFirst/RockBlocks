using System;
using System.ComponentModel;
using System.Text;
using System.Web.UI;
using System.Xml;
using System.Xml.XPath;
using System.Xml.Xsl;

using Rock;
using Rock.Attribute;
using Rock.Model;
using Rock.Web.UI;

namespace RockWeb.Plugins.com_kfs.Utility
{
    #region Block Attributes

    [DisplayName( "XSL Transform" )]
    [Category( "KFS > Utility" )]
    [Description( "This block applies a provided XSL Transform to a source XML File." )]

    [TextField( "XML File Path", "The path of the XML file. Example: ~/Plugins/com_kfs/XslTransform/sample/books.xml", true )]
    [TextField( "XSLT File Path", "The path of the XSLT file. Example: ~/Plugins/com_kfs/XslTransform/sample/output.xsl", true )]
    [BooleanField("Raw XML","Flag indicating if the output should be only the result of the XSL Transform.")]

    #endregion

    public partial class XsltFromXml : RockBlock
    {
        #region Fields

        private string _xml = string.Empty;
        private string _xslt = string.Empty;

        #endregion

        #region Base Control Methods

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            _xslt = GetAttributeValue( "XSLTFilePath" );
            if ( !_xslt.StartsWith("http") )
            {
                _xslt = ResolveRockUrlIncludeRoot( _xslt );
            }

            _xml = GetAttributeValue( "XMLFilePath" );
            if ( !_xml.StartsWith( "http" ) )
            {
                _xml = ResolveRockUrlIncludeRoot( _xml );
            }

            // this event gets fired after block settings are updated. it's nice to repaint the screen if these settings would alter it
            this.BlockUpdated += Block_BlockUpdated;
            this.AddConfigurationUpdateTrigger( upnlContent );
        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected void Page_Load( object sender, EventArgs e )
        {
            ProcessXslt();
        }

        /// <summary>
        /// Handles the BlockUpdated event of the control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void Block_BlockUpdated( object sender, EventArgs e )
        {
            ProcessXslt();
        }

        #endregion

        #region Internal Methods

        private void ProcessXslt()
        {
            litXml.Visible = false;
            pnlError.Visible = false;

            if ( !string.IsNullOrWhiteSpace( _xml ) && !string.IsNullOrWhiteSpace( _xslt ) )
            {
                try
                {
                    // Create the XslTransform object and load the style sheet.
                    XslCompiledTransform xslt = new XslCompiledTransform();
                    xslt.Load( _xslt );

                    // Load the file to transform.
                    XPathDocument doc = new XPathDocument( _xml );

                    // Create the writer.
                    StringBuilder sb = new StringBuilder();
                    XmlWriter writer = XmlWriter.Create( sb, xslt.OutputSettings );

                    // Transform the file and send the output to the console.
                    xslt.Transform( doc, writer );
                    writer.Close();

                    // Display Results
                    if ( GetAttributeValue( "RawXML" ).AsBoolean() )
                    {
                        Response.Clear();
                        Response.ContentType = "text/xml";
                        Response.Charset = "UTF-8";
                        Response.Write( sb.ToString() );
                        Response.End();
                    }
                    else
                    {
                        litXml.Visible = true;
                        litXml.Text = sb.ToString();
                    }
                }
                catch ( System.Exception ex )
                {
                    pnlError.Controls.Clear();
                    pnlError.Controls.Add( new LiteralControl( ex.Message ) );
                    pnlError.Visible = true;
                    litXml.Visible = false;
                }
            }
            else
            {
                pnlError.Controls.Clear();
                pnlError.Controls.Add( new LiteralControl( "Please verify settings are not blank." ) );
                pnlError.Visible = true;
                litXml.Visible = false;
            }
        }

        #endregion
    }
}
