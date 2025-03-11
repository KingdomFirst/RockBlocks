<%@ Control Language="C#" AutoEventWireup="true" CodeFile="ProfileBioEdit.ascx.cs" Inherits="RockWeb.Plugins.rocks_kfs.Cms.ProfileBioEdit" %>
<style>
    @media (min-width: 992px) {
        .card-person fieldset, .family-member fieldset {
            padding-left: 15px;
            padding-right: 15px;
            width: 100%;
        }

        .card-person .col-md-6 ~ fieldset, .family-member .col-md-6 ~ fieldset {
            width: 50%;
        }
    }
</style>
<asp:UpdatePanel runat="server" ID="upnlCareEntry">
    <ContentTemplate>
        <asp:Panel ID="pnlView" runat="server" CssClass="panel panel-block">
            <asp:Panel ID="pnlProfilePanels" runat="server"></asp:Panel>
            <Rock:ModalAlert ID="maAlert" runat="server"></Rock:ModalAlert>

            <asp:HiddenField ID="hfGroupId" runat="server" />
            <asp:HiddenField ID="hfPrimaryPersonGuid" runat="server" />

            <asp:LinkButton ID="lbSave" runat="server" AccessKey="s" ToolTip="Alt+s" Text="Save" CssClass="btn btn-primary" OnClick="lbSave_Click" />
            <asp:LinkButton ID="lbCancel" runat="server" AccessKey="c" ToolTip="Alt+c" Text="Cancel" CssClass="btn btn-link" CausesValidation="false" OnClick="lbCancel_Click" />
        </asp:Panel>
    </ContentTemplate>
</asp:UpdatePanel>
