<%@ Control Language="C#" AutoEventWireup="true" CodeFile="OptInFamily.ascx.cs" Inherits="RockWeb.Plugins.rocks_kfs.Cms.OptInFamily" %>
<asp:UpdatePanel ID="upContent" runat="server">
    <ContentTemplate>

        <script>
            $(function () {
                $(".photo a").fluidbox();
            });
        </script>

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
                    <hr />

                    <h3>
                        <asp:Literal ID="lGroupName" runat="server" />
                    </h3>
                    <asp:Repeater ID="rptGroupMembers" runat="server" OnItemDataBound="rptGroupMembers_ItemDataBound" OnItemCommand="rptGroupMembers_ItemCommand">
                        <ItemTemplate>
                            <div class="row">
                                <div class="col-md-1">
                                    <div class="photo">
                                        <asp:Literal ID="lGroupMemberImage" runat="server" />
                                    </div>
                                </div>
                                <div class="col-md-11">
                                    <div class="row">
                                        <div class="col-md-3">
                                            <b>
                                                <asp:Literal ID="lGroupMemberName" runat="server" /></b>
                                        </div>
                                    </div>
                                    <div class="row pull-right">
                                        <asp:LinkButton ID="lbEditGroupMember" runat="server" CssClass="btn btn-primary btn-xs" CommandArgument='<%# Eval("PersonId") %>' CommandName="Update"> Update</asp:LinkButton>
                                    </div>
                                </div>
                            </div>
                            <br />
                        </ItemTemplate>
                    </asp:Repeater>
                </asp:Panel>
            </div>
        </div>
    </ContentTemplate>
</asp:UpdatePanel>
