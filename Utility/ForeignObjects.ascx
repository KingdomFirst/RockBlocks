<%@ Control Language="C#" AutoEventWireup="true" CodeFile="ForeignObjects.ascx.cs" Inherits="RockWeb.Plugins.com_kfs.Utility.ForeignObjects" %>

<asp:UpdatePanel ID="upnlContent" runat="server">
    <ContentTemplate>

        <div id="pnlViewDetails" runat="server">
            <asp:Literal ID="lResults" runat="server" />

            <asp:Literal ID="lDebug" runat="server" />

            <div class="actions">
                <asp:LinkButton ID="lbEdit" runat="server" Text="Edit" CssClass="btn btn-link" CausesValidation="false" OnClick="lbEdit_Click" />
            </div>
        </div>

        <div id="pnlEditDetails" runat="server">
            <div>
                <Rock:RockTextBox ID="tbForeignKey" runat="server" Label="Foreign Key" />
                <Rock:RockTextBox ID="tbForeignGuid" runat="server" Label="Foreign Guid" />
                <Rock:NumberBox ID="tbForeignId" runat="server" Label="Foreign Id" NumberType="Integer" />
            </div>

            <div class="actions">
                <asp:LinkButton ID="lbSave" runat="server" Text="Save" AccessKey="s" CssClass="btn btn-primary" OnClick="lbSave_Click" />
                <asp:LinkButton ID="lbCancel" runat="server" Text="Cancel" AccessKey="c" CssClass="btn btn-link" CausesValidation="false" OnClick="lbCancel_Click" />
            </div>
        </div>

    </ContentTemplate>
</asp:UpdatePanel>
