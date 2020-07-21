<%@ Control Language="C#" AutoEventWireup="true" CodeFile="EventbriteSync.ascx.cs" Inherits="RockWeb.Plugins.rocks_kfs.Eventbrite.EventbriteSync" %>

<asp:UpdatePanel runat="server" ID="upnlEventbriteButtons">
    <ContentTemplate>
        <asp:Panel runat="server" ID="pnlEventbriteButtons" CssClass="row" Visible="false">
            <asp:HiddenField ID="hfGroupId" runat="server" />
            <asp:Panel runat="server" ID="pnlSyncButton" Visible="true" CssClass="col-md-12 pull-right">
                <p>
                    &nbsp;<asp:LinkButton runat="server" ID="lbSyncButton" Visible="true" CssClass="btn btn-default btn-sync pull-right" OnClick="lbSyncButton_Click">Eventbrite Sync</asp:LinkButton><br />
                    &nbsp;
                </p>
            </asp:Panel>
            <div class="clearfix">
                <br />
            </div>
        </asp:Panel>
    </ContentTemplate>
</asp:UpdatePanel>
