<%@ Control Language="C#" AutoEventWireup="true" CodeFile="ProfileBioEdit.ascx.cs" Inherits="RockWeb.Plugins.rocks_kfs.Cms.ProfileBioEdit" %>
<asp:UpdatePanel runat="server" ID="upnlCareEntry">
    <ContentTemplate>
        <asp:Panel ID="pnlView" runat="server" CssClass="panel panel-block">
            <asp:Panel ID="pnlProfilePanels" runat="server">

            </asp:Panel>
            <asp:HiddenField ID="hfGroupId" runat="server" />
            <asp:HiddenField ID="hfPrimaryPersonGuid" runat="server" />

            <div class="pull-right">
                <Rock:HighlightLabel ID="hlblSuccess" runat="server" LabelType="Success" Text="Saved" Visible="false" />
            </div>

            <asp:LinkButton ID="lbSave" runat="server" AccessKey="s" ToolTip="Alt+s" Text="Save" CssClass="btn btn-primary" OnClick="lbSave_Click" />
            <asp:LinkButton ID="lbCancel" runat="server" AccessKey="c" ToolTip="Alt+c" Text="Cancel" CssClass="btn btn-link" CausesValidation="false" OnClick="lbCancel_Click" />
        </asp:Panel>

        <Rock:ConfirmPageUnload ID="confirmExit" runat="server" ConfirmationMessage="Changes have been made to your profile that have not yet been saved." Enabled="false" />

    </ContentTemplate>
</asp:UpdatePanel>
