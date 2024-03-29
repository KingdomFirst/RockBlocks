﻿<%@ Control Language="C#" AutoEventWireup="true" CodeFile="BusinessBio.ascx.cs" Inherits="RockWeb.Plugins.rocks_kfs.Crm.BusinessBio" %>

<Rock:NotificationBox ID="nbInvalidBusiness" runat="server" NotificationBoxType="Warning" Title="Business Not Found" Text="The requested business profile does not exist." Visible="false" />
<asp:UpdatePanel ID="pnlContent" runat="server">
    <ContentTemplate>

        <script>
            $(function () {
                $(".photo a").fluidbox();
                $('span.js-email-status').tooltip({ html: true, container: 'body', delay: { show: 100, hide: 100 } });
            });
        </script>

        <div id="divBio" runat="server" class="">
            <div class="action-wrapper">

                <ul id="ulActions" runat="server" class="nav nav-actions action action-extended">
                    <li class="dropdown">
                        <a class="persondetails-actions dropdown-toggle" data-toggle="dropdown" href="#" tabindex="0">
                            <span>Actions</span>
                            <b class="caret"></b>
                        </a>
                        <ul class="dropdown-menu dropdown-menu-right">
                            <li>
                                <asp:LinkButton ID="lbImpersonate" runat="server" Visible="false" OnClick="lbImpersonate_Click"><i class='fa-fw fa fa-unlock'></i>&nbsp;Impersonate</asp:LinkButton></li>
                            <li>
                                <asp:HyperLink ID="hlVCard" runat="server"><i class='fa fa-address-card'></i>&nbsp;Download vCard</asp:HyperLink></li>
                            <asp:Literal ID="lActions" runat="server" />
                        </ul>
                    </li>
                </ul>

                <asp:LinkButton ID="lbEditBusiness" runat="server" AccessKey="I" ToolTip="Alt+I" CssClass="action" OnClick="lbEditBusiness_Click"><i class="fa fa-pencil"></i></asp:LinkButton>
            </div>

            <div class="row">
                <div class="col-sm-3 col-md-2 xs-text-center">
                    <div class="photo">
                        <asp:Literal ID="lImage" runat="server" />
                        <asp:Panel ID="pnlFollow" runat="server" CssClass="following-status"><i class="fa fa-star"></i></asp:Panel>
                    </div>
                    <div class="social-icons margin-t-sm">
                        <asp:Repeater ID="rptSocial" runat="server">
                            <ItemTemplate>
                                <a href='<%# Eval("url") %>' class='btn btn-<%# Eval("name").ToString().ToLower() %> btn-sm btn-square' <%# !string.IsNullOrEmpty( Eval("color").ToString())? "style='background-color:"+Eval("color").ToString()+"'":"" %> target="_blank"><i class='<%# Eval("icon") %>'></i></a>
                            </ItemTemplate>
                        </asp:Repeater>
                    </div>
                </div>
                <div class="col-sm-9 col-md-10 xs-text-center">

                    <h1 class="title name">
                        <asp:Literal ID="lName" runat="server" /></h1>

                    <Rock:BadgeListControl ID="blStatus" runat="server" />
                    <Rock:HighlightLabel ID="hlStatus" runat="server" />

                    <Rock:TagList ID="taglPersonTags" runat="server" CssClass="clearfix" />

                    <div class="summary clearfix">
                        <div class="demographics">
                            <asp:Literal ID="lDetailsLeft" runat="server" />
                        </div>
                        <div class="personcontact">
                            <asp:Literal ID="lDetailsRight" runat="server" />

                            <ul class="list-unstyled phonenumbers">
                                <asp:Repeater ID="rptPhones" runat="server">
                                    <ItemTemplate>
                                        <li data-value="<%# Eval("Number") %>"><%# FormatPhoneNumber( (bool)Eval("IsUnlisted"), Eval("CountryCode"), Eval("Number"), (int?)Eval("NumberTypeValueId") ?? 0, (bool)Eval("IsMessagingEnabled") ) %></li>
                                    </ItemTemplate>
                                </asp:Repeater>
                            </ul>

                            <div class="email">
                                <asp:Literal ID="lEmail" runat="server" />
                            </div>
                        </div>
                    </div>

                    <asp:PlaceHolder ID="phCustomContent" runat="server" />

                </div>
            </div>

        </div>
    </ContentTemplate>
</asp:UpdatePanel>


