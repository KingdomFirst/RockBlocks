<%@ Control Language="C#" AutoEventWireup="true" CodeFile="CareDashboard.ascx.cs" Inherits="RockWeb.Plugins.rocks_kfs.StepsToCare.CareDashboard" %>
<style>
    .table > thead > tr > th > a:not(.btn)::after {
        display: none;
    }

    .table > thead > tr > th.ascending a:not(.btn)::after, .table > thead > tr > th.descending a:not(.btn)::after {
        display: inline;
    }

    .grid-table > tbody > tr > td:empty, .grid-table > tbody > tr > th:empty, .grid-table > thead > tr > td:empty, .grid-table > thead > tr > th:empty {
        padding: 0;
    }

    .table-striped > tbody > tr.assigned:nth-of-type(odd) {
        background-color: oldlace;
    }

    .table-striped > tbody > tr.assigned:nth-of-type(even) {
        background-color: #fff2da;
    }

    .care-dashboard .table-responsive {
        overflow-y: auto;
    }

    .js-person-popover-simple + .popover .popover-content {
        text-align: center;
    }

    .js-person-popover-stepstocare + .popover {
        max-width: 100%;
    }

        .js-person-popover-stepstocare + .popover .popover-content .person-image {
            float: left;
            width: 70px;
            height: 70px;
            margin-right: 8px;
            background-position: 50%;
            background-size: cover;
            border: 1px solid #dfe0e1;
        }

        .js-person-popover-stepstocare + .popover .popover-content .contents {
            float: left;
            width: calc(100% - 78px);
        }

        .js-person-popover-stepstocare + .popover .popover-content .email {
            text-overflow: ellipsis;
            white-space: nowrap;
            overflow: hidden;
            width: 100%;
            display: inline-block;
        }

    .hasParentNeed {
        background-color: #ececec !important;
    }

    .table-striped > tbody > tr.assigned.hasParentNeedAssigned:nth-of-type(odd) {
        background-color: oldlace !important;
    }

    .table-striped > tbody > tr.assigned.hasParentNeedAssigned:nth-of-type(even) {
        background-color: #fff2da !important;
    }

    .assigned.hasParentNeed {
        background-color: #feefd2 !important;
    }

    .grid-select-cell .photo-icon {
        margin-bottom: 8px;
    }

    .grid-select-cell.photo-icon-cell {
        padding-bottom: 8px;
    }

    .modal.container.kfs-modal-confirm {
        width: 584px;
        margin-left: -242px;
    }

    .modal.container.kfs-modal-snooze {
        width: 300px;
        margin-left: -150px;
    }

    .kfs-radiobuttons-btn .radio-inline {
        padding: 0;
        margin-left: 0
    }

        .kfs-radiobuttons-btn .radio-inline .label-text {
            display: inline-block;
            margin-bottom: 0;
            font-weight: 500;
            text-align: center;
            white-space: nowrap;
            vertical-align: middle;
            touch-action: manipulation;
            cursor: pointer;
            background-image: none;
            border: 1px solid transparent;
            padding: 6px 16px;
            font-size: 16px;
            line-height: 1.5;
            border-radius: 6px;
            color: #fff;
            background-color: var(--color-primary);
            border-color: var(--color-primary);
            box-shadow: 0 1px 2px 0 rgba(0,0,0,0.05)
        }

        .kfs-radiobuttons-btn .radio-inline input:checked ~ .label-text {
            opacity: .8;
        }

        .kfs-radiobuttons-btn .radio-inline input[type='radio'], .kfs-radiobuttons-btn .radio-inline .label-text::before, .kfs-radiobuttons-btn .radio-inline .label-text::after {
            display: none;
            margin: 0
        }
</style>
<asp:UpdatePanel runat="server" ID="upnlCareDashboard" UpdateMode="Always">
    <ContentTemplate>
        <Rock:ModalAlert ID="mdGridWarning" runat="server" />

        <asp:Panel ID="pnlView" runat="server" CssClass="care-dashboard">
            <div class="panel panel-block">
                <div class="panel-heading">
                    <h1 class="panel-title"><i class="fa fa-hand-holding-heart mr-2"></i>Care Needs</h1>
                    <div class="pull-right d-flex align-items-center">
                        <asp:Literal ID="lCategoriesHeader" runat="server" />
                        <asp:LinkButton ID="lbNotificationType" runat="server" CssClass="btn btn-xs btn-square btn-default pull-right" OnClick="lbNotificationType_Click" CausesValidation="false"> <i title="Choose Notification Type" class="fas fa-comment-alt"></i></asp:LinkButton>
                        <asp:LinkButton ID="lbCareConfigure" runat="server" CssClass="btn btn-xs btn-square btn-default pull-right" OnClick="lbCareConfigure_Click" CausesValidation="false"> <i title="Options" class="fa fa-gear"></i></asp:LinkButton>
                    </div>
                </div>
                <div class="panel-body">

                    <asp:Panel ID="pnlStepsToCareStats" runat="server" CssClass="list-as-blocks clearfix margin-b-lg">
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
                            <li class="block-status care-enter-need" id="liEnterNeed" runat="server">
                                <asp:LinkButton ID="btnAdd" runat="server" CssClass="btn btn-default" OnClick="gList_AddClick"><i class='fas fa-plus'></i><strong class="text-uppercase">Enter Care Need</strong></asp:LinkButton>
                            </li>
                            <li class="block-status care-categories">
                                <asp:Literal ID="lCategories" runat="server" />
                            </li>
                        </ul>
                    </asp:Panel>

                    <asp:Panel ID="pnlGrid" runat="server" CssClass="grid grid-panel">
                        <h5 class="pl-2" id="hDashboard" runat="server">Care Dashboard</h5>
                        <Rock:GridFilter ID="rFilter" runat="server" OnDisplayFilterValue="rFilter_DisplayFilterValue" OnClearFilterClick="rFilter_ClearFilterClick">
                            <Rock:DateRangePicker ID="drpDate" runat="server" Label="Date Range" />
                            <Rock:RockTextBox ID="tbFirstName" runat="server" Label="First Name" />
                            <Rock:RockTextBox ID="tbLastName" runat="server" Label="Last Name" />
                            <Rock:DefinedValuesPicker ID="dvpCategory" runat="server" Label="Category" DataTextField="Value" DataValueField="Id" />
                            <Rock:DefinedValuesPicker ID="dvpStatus" runat="server" Label="Status" DataTextField="Value" DataValueField="Id" />
                            <Rock:RockDropDownList ID="ddlSubmitter" runat="server" Label="Submitted By" EnhanceForLongLists="true" />
                            <Rock:CampusPicker ID="cpCampus" runat="server" Label="Campus" />
                            <Rock:RockCheckBox ID="cbAssignedToMe" runat="server" Label="Assigned to Me" />
                            <Rock:RockCheckBox ID="cbIncludeFutureNeeds" runat="server" Label="Include Scheduled Needs" />
                            <asp:PlaceHolder ID="phAttributeFilters" runat="server" />
                        </Rock:GridFilter>
                        <Rock:Grid ID="gList" runat="server" DisplayType="Full" AllowSorting="true" ExportSource="DataSource">
                            <Columns>
                                <Rock:ColorField DataField="Category.AttributeValues.Color" ToolTipDataField="Category.Value" HeaderText="Cat" SortExpression="Category.Value" />
                                <Rock:RockTemplateField SortExpression="PersonAlias.Person.LastName, PersonAlias.Person.NickName, LastName, FirstName" HeaderText="Name" ItemStyle-CssClass="text-nowrap">
                                    <ItemTemplate>
                                        <asp:Literal ID="lName" runat="server" />
                                    </ItemTemplate>
                                </Rock:RockTemplateField>
                                <Rock:RockBoundField DataField="Details" HeaderText="Details" SortExpression="Details" />
                                <Rock:RockBoundField DataField="DateEntered" HeaderText="Date" DataFormatString="{0:d}" SortExpression="DateEntered" />
                                <Rock:RockBoundField DataField="Campus.Name" HeaderText="Campus" SortExpression="Campus.Name" />
                                <Rock:RockTemplateField HeaderText="Care Touches" HeaderStyle-CssClass="text-center" ItemStyle-CssClass="text-center">
                                    <ItemTemplate>
                                        <asp:Literal ID="lCareTouches" runat="server"></asp:Literal>
                                    </ItemTemplate>
                                </Rock:RockTemplateField>
                                <Rock:RockTemplateField HeaderText="Assigned" SortExpression="AssignedPersons" ItemStyle-CssClass="photo-icon-cell align-middle">
                                    <ItemTemplate>
                                        <asp:Literal ID="lAssigned" runat="server"></asp:Literal>
                                    </ItemTemplate>
                                </Rock:RockTemplateField>
                                <%-- Columns are dynamically added due to dynamic content --%>
                            </Columns>
                        </Rock:Grid>
                    </asp:Panel>
                    <asp:Panel ID="pnlFollowUpGrid" runat="server" CssClass="grid grid-panel">
                        <h5 class="pl-2">Care Follow Up</h5>
                        <Rock:GridFilter ID="rFollowUpFilter" runat="server" OnDisplayFilterValue="rFollowUpFilter_DisplayFilterValue" OnApplyFilterClick="rFollowUpFilter_ApplyFilterClick" OnClearFilterClick="rFollowUpFilter_ClearFilterClick">
                            <Rock:DateRangePicker ID="drpFollowUpDate" runat="server" Label="Date Range" />
                            <Rock:RockTextBox ID="tbFollowUpFirstName" runat="server" Label="First Name" />
                            <Rock:RockTextBox ID="tbFollowUpLastName" runat="server" Label="Last Name" />
                            <Rock:DefinedValuesPicker ID="dvpFollowUpCategory" runat="server" Label="Category" DataTextField="Value" DataValueField="Id" />
                            <Rock:RockDropDownList ID="ddlFollowUpSubmitter" runat="server" Label="Submitted By" EnhanceForLongLists="true" />
                            <Rock:CampusPicker ID="cpFollowUpCampus" runat="server" Label="Campus" />
                            <Rock:RockCheckBox ID="cbFollowUpAssignedToMe" runat="server" Label="Assigned to Me" Checked="true" />
                            <asp:PlaceHolder ID="phFollowUpAttributeFilters" runat="server" />
                        </Rock:GridFilter>
                        <Rock:Grid ID="gFollowUp" runat="server" DisplayType="Full" AllowSorting="true" ExportSource="DataSource">
                            <Columns>
                                <Rock:ColorField DataField="Category.AttributeValues.Color" ToolTipDataField="Category.Value" HeaderText="Cat" SortExpression="Category.Value" />
                                <Rock:RockTemplateField SortExpression="PersonAlias.Person.LastName, PersonAlias.Person.NickName, LastName, FirstName" HeaderText="Name" ItemStyle-CssClass="text-nowrap">
                                    <ItemTemplate>
                                        <asp:Literal ID="lName" runat="server" />
                                    </ItemTemplate>
                                </Rock:RockTemplateField>
                                <Rock:RockBoundField DataField="Details" HeaderText="Details" SortExpression="Details" />
                                <Rock:RockBoundField DataField="DateEntered" HeaderText="Date" DataFormatString="{0:d}" SortExpression="DateEntered" />
                                <Rock:RockBoundField DataField="Campus.Name" HeaderText="Campus" SortExpression="Campus.Name" />
                                <Rock:RockTemplateField HeaderText="Care Touches" HeaderStyle-CssClass="text-center" ItemStyle-CssClass="text-center">
                                    <ItemTemplate>
                                        <asp:Literal ID="lCareTouches" runat="server"></asp:Literal>
                                    </ItemTemplate>
                                </Rock:RockTemplateField>
                                <Rock:RockTemplateField HeaderText="Assigned" SortExpression="AssignedPersons" ItemStyle-CssClass="photo-icon-cell align-middle">
                                    <ItemTemplate>
                                        <asp:Literal ID="lAssigned" runat="server"></asp:Literal>
                                    </ItemTemplate>
                                </Rock:RockTemplateField>
                                <%-- Columns are dynamically added due to dynamic content --%>
                            </Columns>
                        </Rock:Grid>
                    </asp:Panel>
                </div>
            </div>
            <script type="text/javascript">
                function initDashboard() {
                    $("div.photo-icon").lazyload({
                        effect: "fadeIn"
                    });

                    $('#<%=drpDate.ClientID%>').datepicker({
                        format: 'mm/dd/yyyy',
                        todayHighlight: true,
                        assumeNearbyYear: 10,
                        autoclose: true,
                        endDate: '+0d',
                        inputs: $('#<%=drpDate.ClientID%> .form-control'),
                        zIndexOffset: 1050
                    });

                    // person-link-popover
                    $('.js-person-popover-stepstocare').popover({
                        placement: 'right',
                        trigger: 'manual',
                        sanitize: false,
                        delay: 500,
                        html: true,
                        content: function ()
                        {
                            var dataUrl = Rock.settings.get( 'baseUrl' ) + 'api/People/GetSearchDetails?id=' + $( this ).attr( 'personid' ) + '';

                            var result = $.ajax( {
                                type: 'GET',
                                url: dataUrl,
                                dataType: 'text/html',
                                async: false
                            } ).responseText;

                            return result.replace( /^"/i, '' ).replace( /"$/i, '' ).replace( /\\"/ig, '"' ).replace( /<span class=['"]email['"]>(.*)<\/span>/ig, '<a href="/Communication?person=' + $( this ).attr( 'personid' ) + '" class="email">$1</a>' );

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
                    } );
                    $('.js-person-popover-simple').popover( {
                        placement: 'right',
                        trigger: 'manual',
                        sanitize: false,
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
                            var resultHtml = resultObject.PickerItemDetailsHtml.replace(/<small>.*<\/small>/ig, "").replace(/<div class='body'>.*<\/div>$/ig, "").replace(/header/g, "div").replace(/maxwidth=65/g, 'maxwidth=120').replace(/maxheight=65/g, 'maxheight=120');
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
                    $('.fa-flag[data-toggle="tooltip"]').tooltip({html: true, sanitize: false});
                }
                Sys.Application.add_load(initDashboard);

                function toggleNeeds(needId) {
                    $('.hasParentNeed' + needId).toggleClass('hide');
                    $('#toggleIcon' + needId).toggleClass('fa-plus').toggleClass('fa-minus');
                }
            </script>
        </asp:Panel>
        <Rock:ModalDialog ID="mdMakeNote" runat="server" Title="Add Note" ValidationGroup="MakeNote" ClickBackdropToClose="true" Content-CssClass="modal-kfsmakenote">
            <Content>
                <asp:HiddenField ID="hfCareNeedId" runat="server" />
                <asp:Literal ID="lQuickNoteStatus" runat="server" />

                <asp:ValidationSummary ID="vsMakeNote" runat="server" HeaderText="Please correct the following:" CssClass="alert alert-validation" ValidationGroup="MakeNote" />
                <asp:Panel ID="pnlQuickNote" runat="server" CssClass="note-editor note-editor-standard ">
                    <asp:ValidationSummary ID="vsQuickNoteMakeNote" runat="server" HeaderText="Please correct the following:" CssClass="alert alert-validation" ValidationGroup="QuickNoteMakeNote" />
                    <Rock:RockRadioButtonList ID="rrblQuickNotes" runat="server" Label="Quick Notes" CssClass="kfs-radiobuttons-btn" ValidationGroup="QuickNoteMakeNote" Required="true" RepeatDirection="Horizontal" OnSelectedIndexChanged="rrblQuickNotes_SelectedIndexChanged" AutoPostBack="true"></Rock:RockRadioButtonList>
                    <asp:Panel ID="pnlQuickNoteText" runat="server" CssClass="noteentry-control meta-body" Visible="false">
                        <Rock:RockTextBox ID="rtbNote" runat="server" Placeholder="Write an additional note..." Rows="3" TextMode="MultiLine" ValidationGroup="QuickNoteMakeNote"></Rock:RockTextBox>
                        <div class="settings clearfix">
                            <asp:Checkbox ID="cbAlert" runat="server" Text="Alert" CssClass="js-notealert" />
                            <asp:Checkbox ID="cbPrivate" runat="server" Text="Private" CssClass="js-noteprivate" />
                            <Rock:BootstrapButton ID="rbBtnQuickNoteSave" runat="server" DataLoadingText="Saving..." Text="Save Note" CssClass="commands btn btn-primary btn-xs" OnClick="rbBtnQuickNoteSave_Click" ValidationGroup="QuickNoteMakeNote"></Rock:BootstrapButton>
                        </div>
                    </asp:Panel>
                </asp:Panel>
                <Rock:NoteContainer ID="notesTimeline" runat="server" ShowHeading="false"></Rock:NoteContainer>
            </Content>
        </Rock:ModalDialog>
        <Rock:ModalDialog ID="mdConnectionRequest" runat="server" Title="Add Connection Request" ValidationGroup="ConnectionRequest" ClickBackdropToClose="true" OnSaveClick="mdConnectionRequest_SaveClick" SaveButtonCausesValidation="true" Content-CssClass="modal-kfsconnectionrequest">
            <Content>
                <asp:HiddenField ID="hfConnectionCareNeedId" runat="server" />

                <asp:ValidationSummary ID="vsConnectionRequest" runat="server" HeaderText="Please correct the following:" CssClass="alert alert-validation" ValidationGroup="ConnectionRequest" />
                <Rock:NotificationBox ID="nbDanger" NotificationBoxType="Danger" runat="server" />
                <Rock:NotificationBox ID="nbSuccess" NotificationBoxType="Success" runat="server" />

                <asp:Panel ID="pnlConnectionTypes" runat="server" CssClass="well">
                    <asp:Repeater ID="rptConnnectionTypes" runat="server" OnItemDataBound="rptConnnectionTypes_ItemDataBound">
                        <ItemTemplate>
                            <asp:Literal ID="lConnectionTypeName" runat="server" />
                            <Rock:RockCheckBoxList ID="cblOpportunities" runat="server" RepeatDirection="Horizontal" DataTextField="Name" DataValueField="Id" />
                            </br>
                        </ItemTemplate>
                    </asp:Repeater>
                </asp:Panel>

                <Rock:RockTextBox ID="tbComments" Label="Comments" runat="server" TextMode="MultiLine" Rows="4" ValidateRequestMode="Disabled" />
            </Content>
        </Rock:ModalDialog>
        <Rock:ModalDialog ID="mdNotificationType" runat="server" Title="Choose Notification Type" ValidationGroup="Notification" ClickBackdropToClose="true" OnSaveClick="mdNotificationType_SaveClick" SaveButtonCausesValidation="true" Content-CssClass="modal-kfsnotification">
            <Content>
                <asp:ValidationSummary ID="vsNotificationType" runat="server" HeaderText="Please correct the following:" CssClass="alert alert-validation" ValidationGroup="Notification" />
                <Rock:DynamicPlaceholder ID="phNotificationAttribute" runat="server" />

                <Rock:NotificationBox ID="nbNotificationWarning" runat="server" NotificationBoxType="Warning" Visible="false"></Rock:NotificationBox>
            </Content>
        </Rock:ModalDialog>
        <Rock:ModalDialog ID="mdConfirmNote" runat="server" Title="Confirm Quick Note" ValidationGroup="QuickNoteConfirm" ModalCssClass="kfs-modal-confirm" OnSaveClick="mdConfirmNote_SaveClick" CloseLinkVisible="true" SaveButtonText="Yes" SaveButtonCausesValidation="true" Content-CssClass="modal-kfsnotification">
            <Content>
                <asp:ValidationSummary ID="vsQuickNoteConfirm" runat="server" HeaderText="Please correct the following:" CssClass="alert alert-validation" ValidationGroup="QuickNoteConfirm" />
                <asp:HiddenField ID="hfQuickNote_CareNeedId" runat="server" />
                <asp:HiddenField ID="hfQuickNote_NoteId" runat="server" />
                <p>Are you sure you wish to add the Note "<asp:Literal ID="lQuickNote" runat="server" />" to Care Need (<asp:Literal ID="lCareNeedId" runat="server" />)?</p>
                <Rock:NotificationBox ID="nbQuickNoteConfirm" runat="server" NotificationBoxType="Warning" Visible="false"></Rock:NotificationBox>
            </Content>
        </Rock:ModalDialog>
        <Rock:ModalDialog ID="mdSnoozeNeed" runat="server" Title="Snooze Need" ValidationGroup="SnoozeNeed" ModalCssClass="kfs-modal-snooze" OnSaveClick="mdSnoozeNeed_SaveClick" CloseLinkVisible="true" SaveButtonText="Snooze" SaveButtonCausesValidation="true" Content-CssClass="modal-kfsnotification">
            <Content>
                <asp:ValidationSummary ID="vsSnoozeNeed" runat="server" HeaderText="Please correct the following:" CssClass="alert alert-validation" ValidationGroup="SnoozeNeed" />
                <asp:HiddenField ID="hfSnoozeNeed_CareNeedId" runat="server" />
                <Rock:DatePicker ID="dpSnoozeUntil" runat="server" Label="Snooze Until" ValidationGroup="SnoozeNeed" Required="true" AllowPastDateSelection="false" CssClass="w-100" />
            </Content>
        </Rock:ModalDialog>
    </ContentTemplate>
</asp:UpdatePanel>
