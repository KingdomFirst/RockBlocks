<%@ Control Language="C#" AutoEventWireup="true" CodeFile="CareEntry.ascx.cs" Inherits="RockWeb.Plugins.rocks_kfs.StepsToCare.CareEntry" %>
<style type="text/css">
    fieldset[id*='pwDetails'] .col-md-6:nth-child(odd) {
        clear: left;
    }

    .modal.container.kfs-modal-snooze {
        width: 300px;
        margin-left: -150px;
    }
</style>
<asp:UpdatePanel runat="server" ID="upnlCareEntry">
    <ContentTemplate>
        <asp:Panel ID="pnlView" runat="server" CssClass="panel panel-block">
            <asp:HiddenField ID="hfCareNeedId" runat="server" />
            <div class="panel-heading">
                <h1 class="panel-title pull-left"><i class="fa fa-hand-holding mr-2"></i>Care Need</h1>

                <div class="panel-labels">
                    <Rock:HighlightLabel ID="hlStatus" runat="server" LabelType="Default" Text="Pending" />
                    <asp:LinkButton ID="btnSnooze" runat="server" CssClass="btn btn-primary btn-xs" OnClick="btnSnooze_Click" Visible="false">Snooze</asp:LinkButton>
                    <asp:LinkButton ID="btnComplete" runat="server" CssClass="btn btn-success btn-xs" OnClick="btnComplete_Click" Visible="false">Complete Need</asp:LinkButton>
                </div>
            </div>
            <Rock:PanelDrawer ID="pdAuditDetails" runat="server"></Rock:PanelDrawer>
            <div class="panel-body">
                <asp:ValidationSummary ID="valValidation" runat="server" HeaderText="Please correct the following:" CssClass="alert alert-validation" />
                <asp:CustomValidator ID="cvCareNeed" runat="server" Display="None" EnableClientScript="False"></asp:CustomValidator>

                <div class="">
                    <div class="row">
                        <div class="col-md-3">
                            <Rock:DateTimePicker ID="dpDate" runat="server" Label="Date Entered" SourceTypeName="rocks.kfs.StepsToCare.Model.CareNeed, rocks.kfs.StepsToCare" PropertyName="DateEntered" />
                        </div>
                        <div class="col-md-3">
                            <Rock:PersonPicker ID="ppSubmitter" runat="server" Label="Submitter" Visible="false" SourceTypeName="rocks.kfs.StepsToCare.Model.CareNeed, rocks.kfs.StepsToCare" PropertyName="SubmitterPersonAlias" />
                        </div>
                        <div class="col-md-3">
                            <Rock:CampusPicker ID="cpCampus" runat="server" Label="Campus" OnSelectedIndexChanged="cpCampus_SelectedIndexChanged" AutoPostBack="true" />
                        </div>
                        <div class="col-md-3">
                            <Rock:DefinedValuePicker ID="dvpStatus" runat="server" Label="Status" SourceTypeName="rocks.kfs.StepsToCare.Model.CareNeed, rocks.kfs.StepsToCare" PropertyName="StatusValueId" Required="true" OnSelectedIndexChanged="dvpStatus_SelectedIndexChanged" AutoPostBack="true" />
                        </div>
                    </div>
                </div>

                <Rock:PanelWidget ID="wpRequestor" runat="server" Title="Requestor" Expanded="true" CssClass="margin-t-md">
                    <div class="row">
                        <div class="col-md-8">
                            <Rock:PersonPicker ID="ppPerson" runat="server" Label="Person" OnSelectPerson="ppPerson_SelectPerson" SourceTypeName="rocks.kfs.StepsToCare.Model.CareNeed, rocks.kfs.StepsToCare" PropertyName="PersonAlias" />
                        </div>
                    </div>

                    <asp:Panel runat="server" ID="pnlNewPersonFields">
                        <div class="row">
                            <div class="col-md-4">
                                <Rock:RockTextBox ID="dtbFirstName" runat="server" Label="First Name" />
                            </div>
                            <div class="col-md-4">
                                <Rock:RockTextBox ID="dtbLastName" runat="server" Label="Last Name" />
                            </div>
                            <div class="col-md-4">
                                <Rock:EmailBox ID="ebEmail" runat="server" Label="Email" />
                            </div>
                        </div>

                        <div class="row">
                            <div class="col-md-4">
                                <Rock:PhoneNumberBox ID="pnbHomePhone" runat="server" Label="Home Phone" />
                            </div>
                            <div class="col-md-4">
                                <Rock:PhoneNumberBox ID="pnbCellPhone" runat="server" Label="Cell Phone" />
                            </div>
                            <div class="col-md-4">
                                <Rock:LocationAddressPicker ID="lapAddress" runat="server" Label="Address" />
                            </div>
                        </div>
                    </asp:Panel>
                    <asp:CustomValidator ID="cvPersonValidation" runat="server" Display="None" EnableClientScript="False"></asp:CustomValidator>
                </Rock:PanelWidget>

                <Rock:PanelWidget ID="pwDetails" runat="server" Title="Need Details" Expanded="true">
                    <Rock:DefinedValuePicker ID="dvpCategory" runat="server" Label="Category" SourceTypeName="rocks.kfs.StepsToCare.Model.CareNeed, rocks.kfs.StepsToCare" PropertyName="CategoryValueId" Required="true" OnSelectedIndexChanged="dvpCategory_SelectedIndexChanged" AutoPostBack="true" />

                    <Rock:DataTextBox ID="dtbDetailsText" runat="server" Label="Description of Need" TextMode="MultiLine" Rows="4" SourceTypeName="rocks.kfs.StepsToCare.Model.CareNeed, rocks.kfs.StepsToCare" PropertyName="Details" />

                    <Rock:DynamicPlaceholder ID="phAttributes" runat="server" />

                    <Rock:RockCheckBox ID="cbCustomFollowUp" runat="server" Text="Custom Follow Up" OnCheckedChanged="cbCustomFollowUp_CheckedChanged" AutoPostBack="true" />

                    <asp:Panel ID="pnlRecurrenceOptions" runat="server" CssClass="row" Visible="false">
                        <div class="col-sm-6 col-md-4 col-lg-3">
                            <Rock:NumberBox ID="numbRepeatDays" runat="server" Label="Follow Up After" Required="true" Help="Will change to follow up status and notify worker the provided number of days after the need is entered or snoozed." AppendText="days" />
                        </div>
                        <div class="col-sm-6 col-md-4 col-lg-3">
                            <Rock:NumberBox ID="numbRepeatTimes" runat="server" Label="Number of Times to Repeat" Help="The number of times to repeat.  Leave blank to repeat indefinitely." AppendText="times" />
                        </div>
                    </asp:Panel>
                </Rock:PanelWidget>

                <Rock:PanelWidget ID="pwAssigned" runat="server" Title="Assign Workers" Expanded="true">
                    <div class="pull-left mr-3">
                        <Rock:ButtonDropDownList
                            ID="bddlAddWorker"
                            runat="server"
                            Title="<i class='fa fa-briefcase'></i> Add Worker"
                            OnSelectionChanged="bddlAddWorker_SelectionChanged"
                            DataTextField="Label"
                            DataValueField="Value">
                        </Rock:ButtonDropDownList>
                    </div>
                    <div class="pull-left">
                        <Rock:PersonPicker
                            ID="ppAddPerson"
                            runat="server"
                            CssClass="picker-menu-right"
                            Label=""
                            PersonName="Add Person"
                            OnSelectPerson="ppAddPerson_SelectPerson"
                            EnableSelfSelection="true" />
                    </div>
                    <Rock:Grid
                        ID="gAssignedPersons"
                        runat="server"
                        DisplayType="Light"
                        HideDeleteButtonForIsSystem="false"
                        ShowConfirmDeleteDialog="false"
                        OnRowDataBound="gAssignedPersons_RowDataBound">
                        <Columns>
                            <Rock:SelectField></Rock:SelectField>
                            <asp:BoundField DataField="PersonAlias.Person.FullName" HeaderText="Name" SortExpression="PersonAlias.Person.LastName, PersonAlias.Person.NickName" />
                            <Rock:BoolField HeaderText="Follow Up Worker" DataField="FollowUpWorker"></Rock:BoolField>
                            <Rock:CheckBoxEditableField HeaderText="Follow Up Worker" DataField="FollowUpWorker" HeaderStyle-CssClass="text-center" ItemStyle-CssClass="w-auto"></Rock:CheckBoxEditableField>
                            <Rock:RockTemplateField HeaderText="Type (Need Count)">
                                <ItemTemplate>
                                    <asp:PlaceHolder runat="server" ID="phCountOrRole"></asp:PlaceHolder>
                                </ItemTemplate>
                            </Rock:RockTemplateField>
                            <Rock:DeleteField OnClick="gAssignedPersons_DeleteClick" />
                        </Columns>
                    </Rock:Grid>
                    <asp:LinkButton ID="btnDeleteSelectedAssignedPersons"
                        runat="server"
                        CssClass="btn btn-sm btn-outline-primary mt-3"
                        OnClick="btnDeleteSelectedAssignedPersons_Click"
                        Text="Remove Selected" />
                </Rock:PanelWidget>

                <asp:LinkButton ID="lbSave" runat="server" AccessKey="s" ToolTip="Alt+s" Text="Save" CssClass="btn btn-primary" OnClick="lbSave_Click" />
                <asp:LinkButton ID="lbCancel" runat="server" AccessKey="c" ToolTip="Alt+c" Text="Cancel" CssClass="btn btn-link" CausesValidation="false" OnClick="lbCancel_Click" />
                <Rock:RockCheckBox ID="cbIncludeFamily" runat="server" Text="Include Family" DisplayInline="true" FormGroupCssClass="d-inline-block" />
                <Rock:RockCheckBox ID="cbWorkersOnly" runat="server" Text="Workers Only" DisplayInline="true" FormGroupCssClass="d-inline-block" />
                <div class="pull-right">
                    <asp:LinkButton ID="btnSnoozeFtr" runat="server" CssClass="btn btn-warning btn-sm" OnClick="btnSnooze_Click" Visible="false">Snooze</asp:LinkButton>
                    <asp:LinkButton ID="btnCompleteFtr" runat="server" CssClass="btn btn-success btn-sm" OnClick="btnComplete_Click" Visible="false">Complete Need</asp:LinkButton>
                </div>
            </div>
        </asp:Panel>

        <Rock:ConfirmPageUnload ID="confirmExit" runat="server" ConfirmationMessage="Changes have been made to this care need that have not yet been saved." Enabled="false" />

        <Rock:ModalDialog ID="mdSnoozeNeed" runat="server" Title="Snooze Need" ValidationGroup="SnoozeNeed" ModalCssClass="kfs-modal-snooze" OnSaveClick="mdSnoozeNeed_SaveClick" CloseLinkVisible="true" SaveButtonText="Snooze" SaveButtonCausesValidation="true" Content-CssClass="modal-kfsnotification">
            <Content>
                <asp:ValidationSummary ID="vsSnoozeNeed" runat="server" HeaderText="Please correct the following:" CssClass="alert alert-validation" ValidationGroup="SnoozeNeed" />
                <Rock:DatePicker ID="dpSnoozeUntil" runat="server" Label="Snooze Until" ValidationGroup="SnoozeNeed" Required="true" AllowPastDateSelection="false" CssClass="w-100" />
            </Content>
        </Rock:ModalDialog>

    </ContentTemplate>
</asp:UpdatePanel>
