<%@ Control Language="C#" AutoEventWireup="true" CodeFile="PagerEntry_Setup.ascx.cs" Inherits="RockWeb.Plugins.rocks_kfs.CheckIn.PagerEntrySetup" %>

<asp:UpdatePanel ID="upnlContent" runat="server">
    <ContentTemplate>
        <asp:Panel ID="pnlWrapper" runat="server" CssClass="panel panel-block">
            <div class="panel-heading">
                <h1 class="panel-title"><i class="fas fa-pager mr-2"></i>Pager Entry</h1>
            </div>
            <div class="panel-body">
                <p>This block is used to install pager entry into your check-in process. This will configure your check-in process to include a "Enter your Pager Number" field prior to the final step of printing labels.</p>
                <p>
                    <br />
                </p>
                <asp:LinkButton ID="lbInstallPagerEntry" runat="server" Text="Install Pager Entry" CssClass="btn btn-primary mr-3" OnClick="lbInstallPagerEntry_Click"></asp:LinkButton>
                <asp:LinkButton ID="lbRemovePagerEntry" runat="server" Text="Remove Pager Entry" CssClass="btn btn-danger" OnClick="lbRemovePagerEntry_Click"></asp:LinkButton>
                <Rock:NotificationBox ID="nbWarning" runat="server" NotificationBoxType="Warning" Dismissable="true" CssClass="mt-3" />
            </div>
        </asp:Panel>
    </ContentTemplate>
</asp:UpdatePanel>
