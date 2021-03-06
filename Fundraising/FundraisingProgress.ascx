﻿<%@ Control Language="C#" AutoEventWireup="true" CodeFile="FundraisingProgress.ascx.cs" Inherits="RockWeb.Plugins.rocks_kfs.Fundraising.FundraisingProgress" %>

<asp:UpdatePanel ID="upnlContent" runat="server">
    <ContentTemplate>
        <asp:Panel ID="pnlView" runat="server">
            <asp:HiddenField ID="hfGroupId" runat="server" />
            <asp:HiddenField ID="hfGroupMemberId" runat="server" />
            <asp:HiddenField ID="hfGroupGuid" runat="server" />
            <div class="panel panel-block">
                <div class="panel-heading" id="divPanelHeading" runat="server">
                    <h1 class="panel-title">
                        <i class="fa fa-certificate"></i>
                        <asp:Literal ID="lTitle" runat="server" />
                    </h1>
                </div>

                <div id="divTotalRaised" runat="server" class="text-center total-raised">
                    <h2 class="padding-top-lg total-raised-h2">Total raised year to date:</h2>
                    <div class="padding-h-xl padding-v-md alert alert-default h1 alert-total-raised">
                        $<%=this.GroupContributionTotal.ToString("N")%>
                    </div>
                </div>

                <asp:Panel ID="pnlHeader" runat="server" class="bg-color padding-t-md padding-l-md padding-r-md padding-b-sm">
                    <div class="clearfix">
                        <b>Total Individual Goals</b>
                        <p id="pTotalAmounts" runat="server" class="pull-right" style="margin-bottom: 0;"><strong>$<%=this.GroupContributionTotal.ToString("N")%>/$<%=this.GroupIndividualFundraisingGoal.ToString("N")%></strong></p>
                    </div>

                    <div class="progress" id="divTotalProgress" runat="server" style="margin-top: 12px;">
                        <div class="progress-bar progress-bar-<%=this.ProgressCssClass %>" role="progressbar" aria-valuenow="<%=this.PercentComplete%>" aria-valuemin="0" aria-valuemax="100" style="width: <%=this.PercentComplete%>%;">
                            <%=this.PercentComplete%>% Complete
                        </div>
                    </div>
                </asp:Panel>

                <ul class="list-group" id="ulGroupMembers" runat="server">
                    <asp:Repeater ID="rptFundingProgress" runat="server" OnItemDataBound="rptFundingProgress_ItemDataBound">
                        <ItemTemplate>
                            <li class="list-group-item">
                                <div class="row" style="margin-top: 12px;">
                                    <div class="col-xs-4" style="margin-bottom: 12px;">
                                        <%# Eval("FullName") %>
                                    </div>
                                    <div class="col-xs-8" id="divMemberGoalAmount" runat="server">
                                        <p class="pull-right">$<%#Eval("ContributionTotal") %>/$<%#Eval("IndividualFundraisingGoal") %></p>
                                    </div>

                                    <asp:Panel ID="pnlMemberGoalProgressBar" runat="server">
                                        <div class="progress">
                                            <div class="progress-bar progress-bar-<%#Eval("CssClass") %>" role="progressbar" aria-valuenow="<%#Eval( "Percentage" )%>" aria-valuemin="0" aria-valuemax="100" style="width: <%#Eval( "ProgressBarWidth" )%>%;">
                                                <%#Eval("Percentage") %>% Complete
                                            </div>
                                        </div>
                                    </asp:Panel>
                                </div>
                            </li>
                        </ItemTemplate>
                    </asp:Repeater>
                </ul>
            </div>
        </asp:Panel>
    </ContentTemplate>
</asp:UpdatePanel>
<asp:Panel runat="server" ID="pnlActions" CssClass="actions">
    <asp:Button ID="btnExport" runat="server" Text="Export to Excel" CssClass="btn btn-primary" OnClick="btnExport_Click" />
</asp:Panel>
