<%@ Control Language="C#" AutoEventWireup="true" CodeFile="EditBusiness.ascx.cs" Inherits="RockWeb.Plugins.rocks_kfs.Crm.EditBusiness" %>

<asp:UpdatePanel ID="upEditBusiness" runat="server">
    <ContentTemplate>

        <div class="panel panel-block">
            <div class="panel-heading">
                <h1 class="panel-title"><i class="fa fa-user"></i>
                    <asp:Literal ID="lTitle" runat="server" /></h1>
            </div>

            <div class="panel-body">
                <asp:ValidationSummary ID="valValidation" runat="server" HeaderText="Please correct the following:" CssClass="alert alert-validation" />
                <asp:HiddenField ID="hfBusinessId" runat="server" />

                <div class="row">

                    <div class="col-md-3">
                        <div class="well form-well">
                            <Rock:DefinedValuePicker ID="dvpRecordStatus" runat="server" Label="Record Status" AutoPostBack="true" OnSelectedIndexChanged="ddlRecordStatus_SelectedIndexChanged" />
                            <Rock:DefinedValuePicker ID="dvpReason" runat="server" Label="Reason" Visible="false"></Rock:DefinedValuePicker>
                            <Rock:ImageEditor ID="imgPhoto" runat="server" Label="Photo" BinaryFileTypeGuid="03BD8476-8A9F-4078-B628-5B538F967AFC" />
                        </div>
                    </div>

                    <div class="col-md-9">

                        <div class="well form-well">
                            <Rock:DataTextBox ID="tbBusinessName" runat="server" Label="Name" SourceTypeName="Rock.Model.Person, Rock" PropertyName="LastName" />

                            <Rock:AddressControl ID="acAddress" runat="server" UseStateAbbreviation="true" UseCountryAbbreviation="false" />

                            <div class="row">
                                <div class="col-sm-6">
                                    <Rock:PhoneNumberBox ID="pnbPhone" runat="server" Label="Phone Number" CountryCode='<%# Eval("CountryCode") %>' Number='<%# Eval("NumberFormatted")  %>' />
                                </div>
                                <div class="col-sm-6">
                                    <div class="row">
                                        <div class="col-xs-6">
                                            <Rock:RockCheckBox ID="cbSms" runat="server" Text="SMS" Label="&nbsp;" Checked='<%# (bool)Eval("IsMessagingEnabled") %>' />
                                        </div>
                                        <div class="col-xs-6">
                                            <Rock:RockCheckBox ID="cbUnlisted" runat="server" Text="Unlisted" Label="&nbsp;" Checked='<%# (bool)Eval("IsUnlisted") %>' />
                                        </div>
                                    </div>
                                </div>
                            </div>

                            <div class="form-group emailgroup">
                                <div class="form-row">
                                    <div class="col-sm-6">
                                        <Rock:EmailBox ID="tbEmail" runat="server" SourceTypeName="Rock.Model.Person, Rock" PropertyName="Email" />
                                    </div>
                                    <div class="col-sm-3 form-align">
                                        <Rock:RockCheckBox ID="cbIsEmailActive" runat="server" Text="Email Is Active" DisplayInline="true" />
                                    </div>
                                </div>
                            </div>

                            <Rock:RockRadioButtonList ID="rblEmailPreference" runat="server" RepeatDirection="Horizontal" Label="Email Preference">
                                <asp:ListItem Text="Email Allowed" Value="EmailAllowed" />
                                <asp:ListItem Text="No Mass Emails" Value="NoMassEmails" />
                                <asp:ListItem Text="Do Not Email" Value="DoNotEmail" />
                            </Rock:RockRadioButtonList>

                            <Rock:RockRadioButtonList ID="rblCommunicationPreference" runat="server" RepeatDirection="Horizontal" Label="Communication Preference">
                                <asp:ListItem Text="Email" Value="1" />
                                <asp:ListItem Text="SMS" Value="2" />
                            </Rock:RockRadioButtonList>

                            <Rock:NotificationBox ID="nbCommunicationPreferenceWarning" runat="server" NotificationBoxType="Warning" Visible="false" />

                            <Rock:RockDropDownList ID="ddlCampus" runat="server" Label="Campus" />

                        </div>

                        <Rock:PanelWidget runat="server" ID="PanelWidget1" Title="Alternate Identifiers">
                            <div class="row">
                                <div class="col-md-12">
                                    <Rock:RockControlWrapper ID="rcwAlternateIds" runat="server" Label="Alternate Identifiers" Help="Alternate Ids are used by things like check-in to allow easily checking in. This may include a barcode id or a fingerprint id for example.">
                                        <Rock:Grid ID="gAlternateIds" runat="server" DisplayType="Light" DataKeyNames="Guid" RowItemText="Alternate Id" ShowConfirmDeleteDialog="false">
                                            <Columns>
                                                <Rock:RockBoundField DataField="SearchValue" HeaderText="Value" />
                                                <Rock:DeleteField OnClick="gAlternateIds_Delete" />
                                            </Columns>
                                        </Rock:Grid>
                                    </Rock:RockControlWrapper>
                                    <asp:CustomValidator ID="cvAlternateIds" runat="server" OnServerValidate="cvAlternateIds_ServerValidate" Display="None" />
                                </div>
                            </div>
                        </Rock:PanelWidget>

                        <Rock:PanelWidget runat="server" ID="pwAdvanced" Title="Advanced Settings">
                            <div class="row">
                                <div class="col-md-12">
                                    <Rock:RockControlWrapper ID="rcwSearchKeys" runat="server" Label="Search Keys" Help="Search keys provide alternate ways to search for an individual.">
                                        <Rock:Grid ID="gSearchKeys" runat="server" DisplayType="Light" DataKeyNames="Guid" RowItemText="Search Key" ShowConfirmDeleteDialog="false">
                                            <Columns>
                                                <Rock:DefinedValueField DataField="SearchTypeValueId" HeaderText="Search Type" />
                                                <Rock:RockBoundField DataField="SearchValue" HeaderText="Search Value" />
                                                <Rock:DeleteField OnClick="gSearchKeys_Delete" />
                                            </Columns>
                                        </Rock:Grid>
                                    </Rock:RockControlWrapper>
                                </div>
                            </div>
                        </Rock:PanelWidget>

                        <Rock:ModalDialog runat="server" ID="mdAlternateId" Title="Add Alternate Identifier" ValidationGroup="vgAlternateId" OnSaveClick="mdAlternateId_SaveClick">
                            <Content>
                                <Rock:RockTextBox ID="tbAlternateId" runat="server" Label="Alternate Id" Required="true" ValidationGroup="vgAlternateId" autocomplete="off" />
                            </Content>
                        </Rock:ModalDialog>

                        <Rock:ModalDialog runat="server" ID="mdSearchKey" Title="Add Search Key" ValidationGroup="vgSearchKey" OnSaveClick="mdSearchKey_SaveClick">
                            <Content>
                                <Rock:RockDropDownList ID="ddlSearchValueType" runat="server" Label="Search Type" Required="true" ValidationGroup="vgSearchKey" />
                                <Rock:RockTextBox ID="tbSearchValue" runat="server" Label="Search Value" Required="true" ValidationGroup="vgSearchKey" autocomplete="off" />
                            </Content>
                        </Rock:ModalDialog>

                        <div class="actions">
                            <asp:LinkButton ID="btnSave" runat="server" AccessKey="s" ToolTip="Alt+s" Text="Save" CssClass="btn btn-primary" OnClick="btnSave_Click" />
                            <asp:LinkButton ID="btnCancel" runat="server" AccessKey="c" ToolTip="Alt+c" Text="Cancel" CssClass="btn btn-link" CausesValidation="false" OnClick="btnCancel_Click" />
                        </div>

                    </div>

                </div>
            </div>
        </div>

    </ContentTemplate>
</asp:UpdatePanel>
