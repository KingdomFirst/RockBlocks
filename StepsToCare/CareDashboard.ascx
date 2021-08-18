﻿<%@ Control Language="C#" AutoEventWireup="true" CodeFile="CareDashboard.ascx.cs" Inherits="RockWeb.Plugins.rocks_kfs.StepsToCare.CareDashboard" %>
<style>
    .table > thead > tr > th > a:not(.btn)::after {
        display: none;
    }

    .table > thead > tr > th.ascending a:not(.btn)::after, .table > thead > tr > th.descending a:not(.btn)::after {
        display: inline;
    }
</style>
<asp:UpdatePanel runat="server" ID="upnlCareDashboard" UpdateMode="Always">
    <ContentTemplate>
        <Rock:ModalAlert ID="mdGridWarning" runat="server" />

        <asp:Panel ID="pnlView" runat="server" CssClass="care-dashboard">
            <div class="panel panel-block">
                <div class="panel-heading">
                    <h1 class="panel-title"><i class="fa fa-hand-holding-heart"></i> Care Needs</h1>
                    <div class="pull-right d-flex align-items-center">
                        <asp:LinkButton ID="lbCareConfigure" runat="server" CssClass="btn btn-xs btn-square btn-default pull-right" OnClick="lbCareConfigure_Click" CausesValidation="false"> <i title="Options" class="fa fa-gear"></i></asp:LinkButton>
                    </div>
                </div>
                <div class="panel-body">

                    <div class="list-as-blocks clearfix margin-b-lg">
                        <ul>
                            <li class="block-status care-count-touches">
                                <a href="#" class="bg-teal-400 text-white text-uppercase">
                                    <h1 class="mt-0 mb-3">
                                        <asp:Literal ID="lTouchesCount" runat="server"></asp:Literal></h1>
                                    Care Touches This Week
                                </a>
                            </li>
                            <li class="block-status care-count-care-needs">
                                <a href="#" class="bg-blue-400 text-white text-uppercase">
                                    <h1 class="mt-0 mb-3">
                                        <asp:Literal ID="lCareNeedsCount" runat="server"></asp:Literal></h1>
                                    Outstanding Care Needs
                                </a>
                            </li>
                            <li class="block-status care-count-total-needs">
                                <a href="#" class="bg-blue-600 text-white text-uppercase">
                                    <h1 class="mt-0 mb-3">
                                        <asp:Literal ID="lTotalNeedsCount" runat="server"></asp:Literal></h1>
                                    Total Care Needs
                                </a>
                            </li>
                            <li class="block-status care-enter-need">
                                <asp:LinkButton ID="btnAdd" runat="server" CssClass="btn btn-default" OnClick="gList_AddClick"><i class='fas fa-plus'></i><strong class="text-uppercase">Enter Care Need</strong></asp:LinkButton>
                            </li>
                            <li class="block-status care-categories">
                                <asp:Literal ID="lCategories" runat="server" />
                            </li>
                        </ul>
                    </div>

                    <div class="grid grid-panel">
                        <Rock:GridFilter ID="rFilter" runat="server" OnDisplayFilterValue="rFilter_DisplayFilterValue">
                            <Rock:DateRangePicker ID="drpDate" runat="server" Label="Date Range" />
                            <Rock:RockTextBox ID="tbFirstName" runat="server" Label="First Name" />
                            <Rock:RockTextBox ID="tbLastName" runat="server" Label="Last Name" />
                            <Rock:DefinedValuesPicker ID="dvpCategory" runat="server" Label="Category" DataTextField="Value" DataValueField="Id" />
                            <Rock:DefinedValuePicker ID="dvpStatus" runat="server" Label="Status" DataTextField="Value" DataValueField="Id" />
                            <Rock:RockDropDownList ID="ddlSubmitter" runat="server" Label="Submitted By" EnhanceForLongLists="true" />
                            <Rock:CampusPicker ID="cpCampus" runat="server" Label="Campus" />
                            <asp:PlaceHolder ID="phAttributeFilters" runat="server" />
                        </Rock:GridFilter>
                        <Rock:Grid ID="gList" runat="server" DisplayType="Full" AllowSorting="true" OnRowDataBound="gList_RowDataBound" OnRowSelected="gList_Edit" OnRowCreated="gList_RowCreated" ExportSource="DataSource">
                            <Columns>
                                <Rock:ColorField DataField="Category.AttributeValues.Color" ToolTipDataField="Category.Value" HeaderText="Cat" SortExpression="Category.Value" />

                                <Rock:RockTemplateField SortExpression="PersonAlias.Person.LastName, PersonAlias.Person.NickName, LastName, FirstName" HeaderText="Name">
                                    <ItemTemplate>
                                        <asp:Literal ID="lName" runat="server" />
                                    </ItemTemplate>
                                </Rock:RockTemplateField>
                                <Rock:RockBoundField DataField="Details" HeaderText="Details" SortExpression="Details" />
                                <Rock:RockBoundField DataField="DateEntered" HeaderText="Date" DataFormatString="{0:d}" SortExpression="DateEntered" />

                                <Rock:RockTemplateField SortExpression="Status.Value" HeaderText="Status">
                                    <ItemTemplate>
                                        <Rock:HighlightLabel ID="hlStatus" runat="server" />
                                    </ItemTemplate>
                                </Rock:RockTemplateField>
                                <Rock:RockBoundField DataField="Campus.Name" HeaderText="Campus" SortExpression="Campus.Name" />
                                <Rock:RockBoundField DataField="CareNotes.Count" HeaderText="Care Touches" SortExpression="CareNotes.Count" HeaderStyle-CssClass="text-center" ItemStyle-CssClass="text-center"></Rock:RockBoundField>
                                <Rock:RockTemplateField HeaderText="Assigned">
                                    <ItemTemplate>
                                        <asp:Literal ID="lAssigned" runat="server"></asp:Literal>
                                    </ItemTemplate>
                                </Rock:RockTemplateField>
                                <%-- Columns are dynamically added due to dynamic content --%>
                            </Columns>
                        </Rock:Grid>
                    </div>

                </div>
            </div>
            <script type="text/javascript">
                function initDashboard() {
                    $("div.photo-icon").lazyload({
                        effect: "fadeIn"
                    });

                    // person-link-popover
                    $('.js-person-popover').popover({
                        placement: 'right',
                        trigger: 'manual',
                        delay: 500,
                        html: true,
                        content: function () {
                            var dataUrl = Rock.settings.get('baseUrl') + 'api/People/PopupHtml/' + $(this).attr('personid') + '/false';

                            var result = $.ajax({
                                type: 'GET',
                                url: dataUrl,
                                dataType: 'json',
                                contentType: 'application/json; charset=utf-8',
                                async: false
                            }).responseText;

                            var resultObject = JSON.parse(result);
                            var resultHtml = resultObject.PickerItemDetailsHtml.replace(/<small>.*<\/small>/ig, "").replace(/<div class='body'>.*<\/div>$/ig, "").replace(/header/g,"div").replace(/65/g,'120');
                            return resultHtml;

                        }
                    }).on('mouseenter', function () {
                        var _this = this;
                        $(this).popover('show');
                        $(this).siblings('.popover').on('mouseleave', function () {
                            $(_this).popover('hide');
                        });
                    }).on('mouseleave', function () {
                        var _this = this;
                        setTimeout(function () {
                            if (!$('.popover:hover').length) {
                                $(_this).popover('hide')
                            }
                        }, 100);
                    });
                }
                Sys.Application.add_load(initDashboard);
            </script>
        </asp:Panel>
    </ContentTemplate>
</asp:UpdatePanel>
