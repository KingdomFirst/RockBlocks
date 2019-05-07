<%@ Control Language="C#" AutoEventWireup="true" CodeFile="OptInFamily.ascx.cs" Inherits="RockWeb.Plugins.rocks_kfs.Cms.OptInFamily" %>
<asp:UpdatePanel ID="upContent" runat="server">
    <ContentTemplate>

        <asp:Literal ID="lIntroText" runat="server"></asp:Literal>

        <div class="panel panel-block">
            <div class="panel-body">
                <asp:HiddenField ID="hfPersonId" runat="server" />
                <asp:Panel ID="pnlNotAuthorizedMessage" runat="server" CssClass="alert alert-warning"></asp:Panel>

                <asp:Panel ID="pnlConfirmationMessage" runat="server" CssClass="alert alert-success"></asp:Panel>

                <asp:Panel ID="pnlView" runat="server">
                    <asp:Repeater ID="rptGroupMembers" runat="server" OnItemDataBound="rptGroupMembers_ItemDataBound" OnItemCommand="rptGroupMembers_ItemCommand">
                        <ItemTemplate>
                            <div class="row">
                                <div class="col-md-12" >
                                    <asp:CheckBox ID="cbSelectFamilyMember" runat="server" CommandArgument='<%# Eval("PersonId") %>'/>
                                    <asp:Literal ID="lGroupMemberFamilies" runat="server" />
                                </div>
                            </div>
                        </ItemTemplate>
                    </asp:Repeater>
                </asp:Panel>
                <div class="actions" style="margin-top: 10px;">
                    <asp:LinkButton ID="btnSave" runat="server" AccessKey="s" ToolTip="Alt+s" Text="Save" CssClass="btn btn-primary" OnClick="btnSave_Click" />
                    <asp:LinkButton ID="btnCancel" runat="server" AccessKey="c" ToolTip="Alt+c" Text="Cancel" CssClass="btn btn-link" CausesValidation="false" OnClick="btnCancel_Click" />
                </div>
            </div>
        </div>
    </ContentTemplate>
</asp:UpdatePanel>
