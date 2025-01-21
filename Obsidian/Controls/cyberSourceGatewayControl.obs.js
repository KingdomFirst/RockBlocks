System.register(['vue', '@Obsidian/Controls/loadingIndicator.obs', '@Obsidian/Controls/datePartsPicker.obs', '@Obsidian/Controls/addressControl.obs', '@Obsidian/Utility/guid', '@Obsidian/Core/Controls/financialGateway', '@Obsidian/Enums/Controls/gatewayEmitStrings'], (function (exports) {
  'use strict';
  var createElementVNode, createStaticVNode, defineComponent, ref, computed, onMounted, openBlock, createElementBlock, createVNode, unref, createCommentVNode, withDirectives, vShow, isRef, toDisplayString, createTextVNode, LoadingIndicator, DatePartsPicker, AddressControl, newGuid, onSubmitPayment, GatewayEmitStrings;
  return {
    setters: [function (module) {
      createElementVNode = module.createElementVNode;
      createStaticVNode = module.createStaticVNode;
      defineComponent = module.defineComponent;
      ref = module.ref;
      computed = module.computed;
      onMounted = module.onMounted;
      openBlock = module.openBlock;
      createElementBlock = module.createElementBlock;
      createVNode = module.createVNode;
      unref = module.unref;
      createCommentVNode = module.createCommentVNode;
      withDirectives = module.withDirectives;
      vShow = module.vShow;
      isRef = module.isRef;
      toDisplayString = module.toDisplayString;
      createTextVNode = module.createTextVNode;
    }, function (module) {
      LoadingIndicator = module.default;
    }, function (module) {
      DatePartsPicker = module.default;
    }, function (module) {
      AddressControl = module.default;
    }, function (module) {
      newGuid = module.newGuid;
    }, function (module) {
      onSubmitPayment = module.onSubmitPayment;
    }, function (module) {
      GatewayEmitStrings = module.GatewayEmitStrings;
    }],
    execute: (function () {

      function asyncGeneratorStep(gen, resolve, reject, _next, _throw, key, arg) {
        try {
          var info = gen[key](arg);
          var value = info.value;
        } catch (error) {
          reject(error);
          return;
        }
        if (info.done) {
          resolve(value);
        } else {
          Promise.resolve(value).then(_next, _throw);
        }
      }
      function _asyncToGenerator(fn) {
        return function () {
          var self = this,
            args = arguments;
          return new Promise(function (resolve, reject) {
            var gen = fn.apply(self, args);
            function _next(value) {
              asyncGeneratorStep(gen, resolve, reject, _next, _throw, "next", value);
            }
            function _throw(err) {
              asyncGeneratorStep(gen, resolve, reject, _next, _throw, "throw", err);
            }
            _next(undefined);
          });
        };
      }

      var _hoisted_1 = {
        key: 0,
        class: "text-center"
      };
      var _hoisted_2 = {
        style: {
          "max-width": "600px"
        }
      };
      var _hoisted_3 = {
        class: "gateway-address-container js-gateway-address-container",
        ref: "addressContainer"
      };
      var _hoisted_4 = createElementVNode("h4", null, "Billing", -1);
      var _hoisted_5 = {
        class: "gateway-creditcard-container gateway-payment-container js-gateway-creditcard-container"
      };
      var _hoisted_6 = createStaticVNode("<h4>Payment</h4><div class=\"form-group position-relative\"><label class=\"control-label\">Card Number</label><div class=\"form-control js-credit-card-input iframe-input credit-card-input\"></div><i id=\"cardDisplay\" style=\"position:absolute;right:10px;bottom:3px;\"></i></div><div class=\"break\"></div>", 3);
      var _hoisted_9 = {
        class: "row"
      };
      var _hoisted_10 = {
        class: "col-xs-6 exp-col"
      };
      var _hoisted_11 = createElementVNode("div", {
        class: "iframe-input credit-card-exp-input js-credit-card-exp-input"
      }, null, -1);
      var _hoisted_12 = {
        class: "col-xs-6 cvv-col"
      };
      var _hoisted_13 = {
        class: "form-group"
      };
      var _hoisted_14 = {
        class: "control-label credit-card-cvv-label"
      };
      var _hoisted_15 = createElementVNode("div", {
        id: "divCVV",
        class: "form-control js-credit-card-cvv-input iframe-input credit-card-cvv-input input-width-sm"
      }, null, -1);
      var _hoisted_16 = createElementVNode("button", {
        type: "button",
        style: {
          "display": "none"
        },
        class: "payment-button js-payment-button"
      }, null, -1);
      var _hoisted_17 = {
        class: "alert alert-validation js-payment-input-validation"
      };
      var _hoisted_18 = {
        class: "js-validation-message"
      };
      var _hoisted_19 = createElementVNode("a", {
        href: "javascript:location.reload();",
        class: "btn btn-warning mt-3",
        onclick: "Rock.controls.bootstrapButton.showLoading(this);",
        "data-loading-text": "Reloading..."
      }, "Reload Page", -1);
      var _hoisted_20 = [_hoisted_19];
      var FlexJS;
      var script = exports('default', defineComponent({
        __name: 'cyberSourceGatewayControl',
        props: {
          settings: {
            type: Object,
            required: true
          }
        },
        setup(__props, _ref) {
          var _props$settings$addre;
          var __emit = _ref.emit;
          var standardStyling = "";
          var isSaving = ref(false);
          var props = __props;
          var emit = __emit;
          var nowYear = computed(() => {
            return new Date().getFullYear();
          });
          var loading = ref(true);
          var failedToLoad = ref(false);
          var validationMessage = ref("");
          var validationMessageShowReload = ref(false);
          var tokenResponseSent = ref(false);
          var ccexpvalue = {
            month: 0,
            year: 0,
            day: 0
          };
          var address = ref((_props$settings$addre = props.settings.address) !== null && _props$settings$addre !== void 0 ? _props$settings$addre : undefined);
          var showAddress = ref(true);
          var addressRules = ref("");
          var controlId = "cyberSource_".concat(newGuid());
          var paymentInputs = ref(null);
          function loadMicroformJsAsync(_x) {
            return _loadMicroformJsAsync.apply(this, arguments);
          }
          function _loadMicroformJsAsync() {
            _loadMicroformJsAsync = _asyncToGenerator(function* (microFormJWK) {
              if (typeof Flex === "undefined") {
                var script = document.createElement("script");
                script.type = "text/javascript";
                script.src = props.settings.microFormJsPath;
                script.setAttribute("data-variant", "inline");
                document.getElementsByTagName("head")[0].appendChild(script);
                try {
                  yield new Promise((resolve, reject) => {
                    script.addEventListener("load", () => resolve());
                    script.addEventListener("error", () => reject());
                  });
                } catch (_unused2) {
                  return false;
                }
              }
              return typeof Flex !== "undefined";
            });
            return _loadMicroformJsAsync.apply(this, arguments);
          }
          function loadStandardStyleTagAsync() {
            return _loadStandardStyleTagAsync.apply(this, arguments);
          }
          function _loadStandardStyleTagAsync() {
            _loadStandardStyleTagAsync = _asyncToGenerator(function* () {
              var style = document.createElement("style");
              style.type = "text/css";
              style.innerText = standardStyling;
              yield new Promise((resolve, reject) => {
                style.addEventListener("load", () => resolve());
                style.addEventListener("error", () => reject());
                document.getElementsByTagName("head")[0].appendChild(style);
              });
            });
            return _loadStandardStyleTagAsync.apply(this, arguments);
          }
          function getFlexJSOptions(controlId) {
            var customStyles = {
              ':disabled': {
                'cursor': 'not-allowed'
              },
              'valid': {
                'color': '#3c763d'
              },
              'invalid': {
                'color': '#a94442'
              }
            };
            var cardIcons = {
              "visa": 'fab fa-cc-visa',
              "mastercard": 'fab fa-cc-mastercard',
              "amex": 'fab fa-cc-amex',
              "discover": 'fab fa-cc-discover',
              "dinersclub": 'fab fa-cc-diners-club',
              "jcb": 'fab fa-cc-jcb'
            };
            var options = {
              paymentSelector: "".concat(controlId, " .js-payment-button"),
              variant: "inline",
              fields: {
                ccnumber: {
                  selector: "#".concat(controlId, " .js-credit-card-input"),
                  title: "Card Number",
                  placeholder: "0000 0000 0000 0000"
                },
                ccexp: {
                  selector: "#".concat(controlId, " .js-credit-card-exp-input"),
                  title: "Expiration Date",
                  placeholder: "MM / YY"
                },
                cvv: {
                  display: "show",
                  selector: "#".concat(controlId, " .js-credit-card-cvv-input"),
                  title: "Security Code",
                  placeholder: "CVV"
                }
              },
              styleSniffer: false,
              customStyles,
              cardIcons,
              timeoutDuration: 10000,
              initialLoadTimeout: 30000,
              flexTimeout: 900000,
              flexTimeLoaded: props.settings.jwkGeneratedTime
            };
            return options;
          }
          function checkCybersourceFieldsLoaded() {
            var _ref2, _FlexJS, _FlexJS2, _FlexJS3;
            var clearTimer = arguments.length > 0 && arguments[0] !== undefined ? arguments[0] : true;
            var currentTime = new Date().getTime();
            var timeDiff = (_ref2 = currentTime - ((_FlexJS = FlexJS) === null || _FlexJS === void 0 || (_FlexJS = _FlexJS.config) === null || _FlexJS === void 0 ? void 0 : _FlexJS.flexTimeLoaded)) !== null && _ref2 !== void 0 ? _ref2 : 0;
            if ((_FlexJS2 = FlexJS) !== null && _FlexJS2 !== void 0 && (_FlexJS2 = _FlexJS2.config) !== null && _FlexJS2 !== void 0 && _FlexJS2.flexTimeout && timeDiff >= ((_FlexJS3 = FlexJS) === null || _FlexJS3 === void 0 || (_FlexJS3 = _FlexJS3.config) === null || _FlexJS3 === void 0 ? void 0 : _FlexJS3.flexTimeout)) {
              if (clearTimer) {
                var _FlexJS4;
                clearInterval((_FlexJS4 = FlexJS) === null || _FlexJS4 === void 0 ? void 0 : _FlexJS4.loadCheckInterval);
              }
              validationMessage.value = 'We\'re sorry your payment session has timed out. Please reload the page to try again.';
              validationMessageShowReload.value = true;
              var actionBtn = document.querySelector('.btn-give-now, .js-submit-hostedpaymentinfo, .navigation.actions .btn, .registration-entry .actions .btn-primary');
              if (actionBtn != null) {
                actionBtn.classList.add('disabled');
                actionBtn.removeAttribute('href');
                actionBtn.setAttribute('disabled', 'disabled');
              }
            }
          }
          function initCybersourceMicroFormFields() {
            var _FlexJS5, _FlexJS6, _FlexJS9, _FlexJS10, _FlexJS11, _FlexJS12, _FlexJS13, _FlexJS15;
            if (((_FlexJS5 = FlexJS) === null || _FlexJS5 === void 0 ? void 0 : _FlexJS5.number) == undefined && ((_FlexJS6 = FlexJS) === null || _FlexJS6 === void 0 ? void 0 : _FlexJS6.securityCode) == undefined) {
              var _FlexJS7, _FlexJS8;
              FlexJS.number = (_FlexJS7 = FlexJS) === null || _FlexJS7 === void 0 || (_FlexJS7 = _FlexJS7.microform) === null || _FlexJS7 === void 0 ? void 0 : _FlexJS7.createField('number', {
                placeholder: '0000 0000 0000 0000'
              });
              FlexJS.securityCode = (_FlexJS8 = FlexJS) === null || _FlexJS8 === void 0 || (_FlexJS8 = _FlexJS8.microform) === null || _FlexJS8 === void 0 ? void 0 : _FlexJS8.createField('securityCode');
            }
            (_FlexJS9 = FlexJS) === null || _FlexJS9 === void 0 || (_FlexJS9 = _FlexJS9.number) === null || _FlexJS9 === void 0 || _FlexJS9.load('.cybersource-payment-inputs .js-credit-card-input');
            (_FlexJS10 = FlexJS) === null || _FlexJS10 === void 0 || (_FlexJS10 = _FlexJS10.securityCode) === null || _FlexJS10 === void 0 || _FlexJS10.load('.cybersource-payment-inputs .js-credit-card-cvv-input');
            (_FlexJS11 = FlexJS) === null || _FlexJS11 === void 0 || (_FlexJS11 = _FlexJS11.number) === null || _FlexJS11 === void 0 || _FlexJS11.on('error', function (data) {
              console.error(data);
              loading.value = false;
              validationMessage.value = data.message;
            });
            (_FlexJS12 = FlexJS) === null || _FlexJS12 === void 0 || (_FlexJS12 = _FlexJS12.number) === null || _FlexJS12 === void 0 || _FlexJS12.on('load', function (data) {
              loading.value = false;
            });
            var cardIcon = document.querySelector('#cardDisplay');
            var cardSecurityCodeLabel = document.querySelector('label.credit-card-cvv-label');
            (_FlexJS13 = FlexJS) === null || _FlexJS13 === void 0 || (_FlexJS13 = _FlexJS13.number) === null || _FlexJS13 === void 0 || _FlexJS13.on('change', function (data) {
              if (data.card.length === 1) {
                var _FlexJS14;
                cardIcon.className = 'fa-2x ' + ((_FlexJS14 = FlexJS) === null || _FlexJS14 === void 0 || (_FlexJS14 = _FlexJS14.config) === null || _FlexJS14 === void 0 ? void 0 : _FlexJS14.cardIcons[data.card[0].name]);
                cardSecurityCodeLabel.textContent = data.card[0].securityCode.name;
                FlexJS.config.fields.cvv.title = data.card[0].securityCode.name;
              } else {
                cardIcon.className = 'fa-2x fas fa-credit-card';
              }
            });
            checkCybersourceFieldsLoaded(false);
            setTimeout(function () {
              if (loading.value) {
                loading.value = false;
                failedToLoad.value = true;
                FlexJS.config.flexTimeLoaded -= FlexJS.config.flexTimeout;
                checkCybersourceFieldsLoaded(false);
              }
            }, (_FlexJS15 = FlexJS) === null || _FlexJS15 === void 0 || (_FlexJS15 = _FlexJS15.config) === null || _FlexJS15 === void 0 ? void 0 : _FlexJS15.initialLoadTimeout);
          }
          function submitCybersourceMicroFormInfo() {
            var _FlexJS16;
            isSaving.value = true;
            var options = {
              expirationMonth: ('00' + ccexpvalue.month).slice(-2),
              expirationYear: ccexpvalue.year
            };
            FlexJS.inSubmission = true;
            (_FlexJS16 = FlexJS) === null || _FlexJS16 === void 0 || (_FlexJS16 = _FlexJS16.microform) === null || _FlexJS16 === void 0 || _FlexJS16.createToken(options, function (err, token) {
              if (err) {
                console.error(err);
                isSaving.value = false;
                emit(GatewayEmitStrings.Error, err.message);
              } else {
                var _JSON$stringify;
                var addressToken = {
                  billingAddress: address.value,
                  originalToken: token
                };
                emit(GatewayEmitStrings.Success, (_JSON$stringify = JSON.stringify(addressToken)) !== null && _JSON$stringify !== void 0 ? _JSON$stringify : "");
              }
            });
          }
          onSubmitPayment(() => {
            if (loading.value || failedToLoad.value) {
              return;
            }
            tokenResponseSent.value = false;
            setTimeout(() => {
              submitCybersourceMicroFormInfo();
            }, 0);
          });
          onMounted(_asyncToGenerator(function* () {
            var _props$settings$micro;
            yield loadStandardStyleTagAsync();
            if (!(yield loadMicroformJsAsync((_props$settings$micro = props.settings.microFormJWK) !== null && _props$settings$micro !== void 0 ? _props$settings$micro : ""))) {
              emit(GatewayEmitStrings.Error, "Error configuring hosted gateway. This could be due to an invalid or missing API Key. Please verify that API Key is configured correctly in gateway settings.");
              return;
            }
            if (paymentInputs.value) {
              paymentInputs.value.querySelectorAll(".iframe-input").forEach(el => {
                el.innerHTML = "";
              });
            }
            try {
              var _options = getFlexJSOptions(controlId);
              var flex = new Flex(props.settings.microFormJWK);
              FlexJS = {
                config: _options,
                captureContext: props.settings.microFormJWK,
                inSubmission: false,
                microform: flex.microform({
                  styles: _options.customStyles
                }),
                loadCheckInterval: setInterval(checkCybersourceFieldsLoaded, 1000),
                number: undefined,
                securityCode: undefined
              };
              showAddress.value = props.settings.addressMode != "Hide";
              if (props.settings.addressMode == "Required") {
                addressRules.value = "required";
              }
              initCybersourceMicroFormFields();
            } catch (_unused) {
              failedToLoad.value = true;
              emit(GatewayEmitStrings.Error, "Error configuring hosted gateway. This could be due to an invalid or missing API Key. Please verify that API Key is configured correctly in gateway settings.");
              return;
            }
          }));
          return (_ctx, _cache) => {
            var _unref2;
            return openBlock(), createElementBlock("div", null, [loading.value ? (openBlock(), createElementBlock("div", _hoisted_1, [createVNode(unref(LoadingIndicator))])) : createCommentVNode("v-if", true), withDirectives(createElementVNode("div", _hoisted_2, [createElementVNode("div", {
              id: controlId,
              class: "js-cybersource-payment-inputs cybersource-payment-inputs",
              ref_key: "paymentInputs",
              ref: paymentInputs
            }, [withDirectives(createElementVNode("div", _hoisted_3, [_hoisted_4, createVNode(unref(AddressControl), {
              label: "Address",
              modelValue: address.value,
              "onUpdate:modelValue": _cache[0] || (_cache[0] = $event => address.value = $event),
              disabled: isSaving.value,
              rules: addressRules.value
            }, null, 8, ["modelValue", "disabled", "rules"])], 512), [[vShow, showAddress.value]]), createElementVNode("div", _hoisted_5, [_hoisted_6, createElementVNode("div", _hoisted_9, [createElementVNode("div", _hoisted_10, [_hoisted_11, createVNode(unref(DatePartsPicker), {
              label: "Expiration Date",
              isRequired: true,
              startYear: nowYear.value,
              futureYearCount: 15,
              modelValue: unref(ccexpvalue),
              "onUpdate:modelValue": _cache[1] || (_cache[1] = $event => isRef(ccexpvalue) ? ccexpvalue.value = $event : ccexpvalue = $event),
              showBlankItem: false,
              multiple: false,
              hideDay: true,
              disabled: isSaving.value
            }, null, 8, ["startYear", "modelValue", "disabled"])]), createElementVNode("div", _hoisted_12, [createElementVNode("div", _hoisted_13, [createElementVNode("label", _hoisted_14, toDisplayString((_unref2 = unref(FlexJS)) === null || _unref2 === void 0 ? void 0 : _unref2.config.fields.cvv.title), 1), _hoisted_15])])])]), _hoisted_16], 512)], 512), [[vShow, !loading.value && !failedToLoad.value]]), withDirectives(createElementVNode("div", _hoisted_17, [createElementVNode("span", _hoisted_18, [createTextVNode(toDisplayString(validationMessage.value) + " ", 1), withDirectives(createElementVNode("p", null, [..._hoisted_20], 512), [[vShow, validationMessageShowReload.value]])])], 512), [[vShow, validationMessage.value]])]);
          };
        }
      }));

      script.__file = "KFSRockAssemblies/rocks.kfs.JavaScript.Obsidian/src/Controls/cyberSourceGatewayControl.obs";

    })
  };
}));
//# sourceMappingURL=cyberSourceGatewayControl.obs.js.map
