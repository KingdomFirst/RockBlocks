<%@ Control Language="C#" AutoEventWireup="true" CodeFile="EventbriteSync.ascx.cs" Inherits="RockWeb.Plugins.rocks_kfs.Eventbrite.EventbriteSync" %>

<asp:UpdatePanel runat="server" ID="upnlEventbriteButtons">
    <ContentTemplate>
        <asp:Panel runat="server" ID="pnlEventbriteButtons" CssClass="pull-right" Visible="false">
            <asp:HiddenField ID="hfGroupId" runat="server" />
            &nbsp;<asp:LinkButton runat="server" ID="lbSyncButton" Visible="true" CssClass="btn btn-default btn-sync" OnClick="lbSyncButton_Click">Eventbrite Sync Attendees</asp:LinkButton>
            <asp:LinkButton runat="server" ID="lbUnlink" Visible="true" CssClass="btn btn-default btn-unlink" OnClick="lbUnlink_Click">Unlink Eventbrite Event</asp:LinkButton><br />
            &nbsp;
        </asp:Panel>
    </ContentTemplate>
</asp:UpdatePanel>
