<%@ Control Language="C#" AutoEventWireup="true" CodeFile="ProfileBioEdit.ascx.cs" Inherits="RockWeb.Plugins.rocks_kfs.Cms.ProfileBioEdit" EnableViewState="false" %>
<style>
    .rock-panel-widget.family-member .btn + .btn {
        margin-left: .5em;
    }

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
<asp:UpdatePanel runat="server" ID="upnlProfileBio">
    <ContentTemplate>
        <asp:Panel ID="pnlView" runat="server">
            <asp:ValidationSummary ID="valValidation" runat="server" HeaderText="Please correct the following:" CssClass="alert alert-validation" />
            <asp:Panel ID="pnlProfilePanels" runat="server"></asp:Panel>
            <Rock:ModalAlert ID="maAlert" runat="server"></Rock:ModalAlert>

            <asp:HiddenField ID="hfGroupId" runat="server" />
            <asp:HiddenField ID="hfPrimaryPersonGuid" runat="server" />

            <asp:LinkButton ID="lbSave" runat="server" AccessKey="s" ToolTip="Alt+s" Text="Save" CssClass="btn btn-primary btn-save" OnClick="lbSave_Click" />
            <asp:LinkButton ID="lbCancel" runat="server" AccessKey="c" ToolTip="Alt+c" Text="Cancel" CssClass="btn btn-link btn-cancel" CausesValidation="false" OnClick="lbCancel_Click" />
        </asp:Panel>
    </ContentTemplate>
</asp:UpdatePanel>
