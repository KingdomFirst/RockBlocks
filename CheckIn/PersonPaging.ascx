<%@ Control Language="C#" AutoEventWireup="true" CodeFile="PersonPaging.ascx.cs" Inherits="RockWeb.Plugins.rocks_kfs.CheckIn.PersonPaging" %>

<asp:UpdatePanel ID="upnlContent" runat="server">
    <ContentTemplate>
        <Rock:NotificationBox ID="nbWarning" runat="server" NotificationBoxType="Warning" Dismissable="true" />
        <span class="input-group-btn">
            <asp:LinkButton ID="lbPersonPaging" runat="server" CssClass="btn btn-default" OnClick="lbPersonPaging_Click" Visible="false"></asp:LinkButton>
        </span>
    </ContentTemplate>
</asp:UpdatePanel>
