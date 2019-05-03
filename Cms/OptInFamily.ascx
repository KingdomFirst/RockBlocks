<%@ Control Language="C#" AutoEventWireup="true" CodeFile="OptInFamily.ascx.cs" Inherits="RockWeb.Plugins.rocks_kfs.Cms.OptInFamily" %>
<asp:UpdatePanel ID="upContent" runat="server">
    <ContentTemplate>

        <script>
            $(function () {
                $(".photo a").fluidbox();
            });
        </script>

        <asp:Literal ID="lIntroText" runat="server"></asp:Literal>

        <div class="panel panel-block">
            <div class="panel-body">
                <asp:HiddenField ID="hfPersonId" runat="server" />
                <asp:Panel ID="pnlView" runat="server">
                    <div class="row">
                        <div class="col-sm-9">
                            <h1 class="title name">
                                <asp:Literal ID="lName" runat="server" /><div class="pull-right">
                                    <Rock:RockDropDownList ID="ddlGroup" runat="server" DataTextField="Name" DataValueField="Id" OnSelectedIndexChanged="ddlGroup_SelectedIndexChanged" AutoPostBack="true" Visible="false" />
                                </div>
                            </h1>
                        </div>
                    </div>

                    <asp:Repeater ID="rptGroupMembers" runat="server" OnItemDataBound="rptGroupMembers_ItemDataBound" OnItemCommand="rptGroupMembers_ItemCommand">
                        <ItemTemplate>
                            <div class="row">
                                <div class="col-md-6" >
                                    <asp:CheckBox ID="cbSelectFamilyMember" runat="server" CommandArgument='<%# Eval("PersonId") %>' />
                                    <asp:Literal ID="lGroupMemberFamilies" runat="server" />
                                </div>
                                <div class="col-md-2">
                                    <%--<div class="photo">
                                        <asp:Literal ID="lGroupMemberImage" runat="server" />
                                    </div>--%>
                                    
                                </div>
                                <%--<div class="col-md-11">
                                    <div class="row">
                                        <div class="col-md-3">
                                            <b>
                                                <asp:Literal ID="lGroupMemberName" runat="server" /></b>
                                        </div>
                                    </div>
                                </div>--%>
                            </div>
                            <br />
                        </ItemTemplate>
                    </asp:Repeater>
                </asp:Panel>
            </div>
            <div class="actions">
                <asp:LinkButton ID="btnSave" runat="server" AccessKey="s" ToolTip="Alt+s" Text="Save" CssClass="btn btn-primary" OnClick="btnSave_Click" />
                <asp:LinkButton ID="btnCancel" runat="server" AccessKey="c" ToolTip="Alt+c" Text="Cancel" CssClass="btn btn-link" CausesValidation="false" OnClick="btnCancel_Click" />
            </div>
        </div>
    </ContentTemplate>
</asp:UpdatePanel>
