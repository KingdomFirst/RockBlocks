System.register(['vue', '@Obsidian/Controls/loadingIndicator', '@Obsidian/Controls/datePartsPicker', '@Obsidian/Utility/guid', '@Obsidian/Core/Controls/financialGateway', '@Obsidian/Enums/Controls/gatewayEmitStrings'], (function (exports) {
  'use strict';
  var createStaticVNode, createElementVNode, defineComponent, computed, ref, onMounted, openBlock, createElementBlock, createVNode, unref, createCommentVNode, withDirectives, isRef, toDisplayString, createTextVNode, vShow, LoadingIndicator, DatePartsPicker, newGuid, onSubmitPayment, GatewayEmitStrings;
  return {
    setters: [function (module) {
      createStaticVNode = module.createStaticVNode;
      createElementVNode = module.createElementVNode;
      defineComponent = module.defineComponent;
      computed = module.computed;
      ref = module.ref;
      onMounted = module.onMounted;
      openBlock = module.openBlock;
      createElementBlock = module.createElementBlock;
      createVNode = module.createVNode;
      unref = module.unref;
      createCommentVNode = module.createCommentVNode;
      withDirectives = module.withDirectives;
      isRef = module.isRef;
      toDisplayString = module.toDisplayString;
      createTextVNode = module.createTextVNode;
      vShow = module.vShow;
    }, function (module) {
      LoadingIndicator = module["default"];
    }, function (module) {
      DatePartsPicker = module["default"];
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
        class: "gateway-creditcard-container gateway-payment-container js-gateway-creditcard-container"
      };
      var _hoisted_4 = createStaticVNode("<div class=\"form-group position-relative\"><label class=\"control-label\">Card Number</label><div class=\"form-control js-credit-card-input iframe-input credit-card-input\"></div><i id=\"cardDisplay\" style=\"position:absolute;right:10px;bottom:3px;\"></i></div><div class=\"break\"></div>", 2);
      var _hoisted_6 = {
        class: "row"
      };
      var _hoisted_7 = {
        class: "col-xs-6 exp-col"
      };
      var _hoisted_8 = createElementVNode("div", {
        class: "iframe-input credit-card-exp-input js-credit-card-exp-input"
      }, null, -1);
      var _hoisted_9 = {
        class: "col-xs-6 cvv-col"
      };
      var _hoisted_10 = {
        class: "form-group"
      };
      var _hoisted_11 = {
        class: "control-label credit-card-cvv-label"
      };
      var _hoisted_12 = createElementVNode("div", {
        id: "divCVV",
        class: "form-control js-credit-card-cvv-input iframe-input credit-card-cvv-input input-width-sm"
      }, null, -1);
      var _hoisted_13 = createElementVNode("button", {
        type: "button",
        style: {
          "display": "none"
        },
        class: "payment-button js-payment-button"
      }, null, -1);
      var _hoisted_14 = {
        class: "alert alert-validation js-payment-input-validation"
      };
      var _hoisted_15 = {
        class: "js-validation-message"
      };
      var _hoisted_16 = createElementVNode("a", {
        href: "javascript:location.reload();",
        class: "btn btn-warning mt-3",
        onclick: "Rock.controls.bootstrapButton.showLoading(this);",
        "data-loading-text": "Reloading..."
      }, "Reload Page", -1);
      var _hoisted_17 = [_hoisted_16];
      var _hoisted_18 = {
        class: "form-group has-error",
        style: {
          "display": "none"
        }
      };
      var FlexJS;
      var script = exports('default', defineComponent({
        name: 'cyberSourceGatewayControl',
        props: {
          settings: {
            type: Object,
            required: true
          }
        },
        setup(__props, _ref) {
          var emit = _ref.emit;
          var props = __props;
          var standardStyling = "";
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
          function getFlexJSOptions(controlId, inputStyleHook, inputInvalidStyleHook) {
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
            var customCss = {
              "margin-bottom": "5px",
              "margin-top": "0"
            };
            if (inputStyleHook) {
              var inputStyles = getComputedStyle(inputStyleHook);
              customCss["color"] = inputStyles.color;
              customCss["border-bottom-color"] = inputStyles.borderBottomColor;
              customCss["border-bottom-left-radius"] = inputStyles.borderBottomLeftRadius;
              customCss["border-bottom-right-radius"] = inputStyles.borderBottomRightRadius;
              customCss["border-bottom-style"] = inputStyles.borderBottomStyle;
              customCss["border-bottom-width"] = inputStyles.borderBottomWidth;
              customCss["border-left-color"] = inputStyles.borderLeftColor;
              customCss["border-left-style"] = inputStyles.borderLeftStyle;
              customCss["border-left-width"] = inputStyles.borderLeftWidth;
              customCss["border-right-color"] = inputStyles.borderRightColor;
              customCss["border-right-style"] = inputStyles.borderRightStyle;
              customCss["border-right-width"] = inputStyles.borderRightWidth;
              customCss["border-top-color"] = inputStyles.borderTopColor;
              customCss["border-top-left-radius"] = inputStyles.borderTopLeftRadius;
              customCss["border-top-right-radius"] = inputStyles.borderTopRightRadius;
              customCss["border-top-style"] = inputStyles.borderTopStyle;
              customCss["border-top-width"] = inputStyles.borderTopWidth;
              customCss["border-width"] = inputStyles.borderWidth;
              customCss["border-style"] = inputStyles.borderStyle;
              customCss["border-radius"] = inputStyles.borderRadius;
              customCss["border-color"] = inputStyles.borderColor;
              customCss["background-color"] = inputStyles.backgroundColor;
              customCss["box-shadow"] = inputStyles.boxShadow;
              customCss["padding"] = inputStyles.padding;
              customCss["font-size"] = inputStyles.fontSize;
              customCss["height"] = inputStyles.height;
              customCss["font-family"] = inputStyles.fontFamily;
            }
            var focusCss = {
              "border-color": getComputedStyle(document.documentElement).getPropertyValue("--focus-state-border-color"),
              "outline-style": "none"
            };
            var invalidCss = {};
            if (inputInvalidStyleHook) {
              invalidCss["border-color"] = getComputedStyle(inputInvalidStyleHook).borderColor;
            }
            var placeholderCss = {
              "color": getComputedStyle(document.documentElement).getPropertyValue("--input-placeholder")
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
              customCss,
              focusCss,
              invalidCss,
              placeholderCss,
              cardIcons,
              timeoutDuration: 10000,
              flexTimeout: 900000,
              flexTimeLoaded: new Date().getTime(),
              callback: () => {}
            };
            return options;
          }
          function checkCybersourceFieldsLoaded() {
            var _ref2, _FlexJS, _FlexJS$config, _FlexJS2, _FlexJS2$config, _FlexJS3, _FlexJS3$config;
            var clearTimer = arguments.length > 0 && arguments[0] !== undefined ? arguments[0] : true;
            var currentTime = new Date().getTime();
            var timeDiff = (_ref2 = currentTime - ((_FlexJS = FlexJS) === null || _FlexJS === void 0 ? void 0 : (_FlexJS$config = _FlexJS.config) === null || _FlexJS$config === void 0 ? void 0 : _FlexJS$config.flexTimeLoaded)) !== null && _ref2 !== void 0 ? _ref2 : 0;
            if ((_FlexJS2 = FlexJS) !== null && _FlexJS2 !== void 0 && (_FlexJS2$config = _FlexJS2.config) !== null && _FlexJS2$config !== void 0 && _FlexJS2$config.flexTimeout && timeDiff >= ((_FlexJS3 = FlexJS) === null || _FlexJS3 === void 0 ? void 0 : (_FlexJS3$config = _FlexJS3.config) === null || _FlexJS3$config === void 0 ? void 0 : _FlexJS3$config.flexTimeout)) {
              if (clearTimer) {
                var _FlexJS4;
                clearInterval((_FlexJS4 = FlexJS) === null || _FlexJS4 === void 0 ? void 0 : _FlexJS4.loadCheckInterval);
              }
              validationMessage.value = 'We\'re sorry your session has timed out. Please reload the page to try again.';
              validationMessageShowReload.value = true;
              var actionBtn = document.querySelector('.btn-give-now, .js-submit-hostedpaymentinfo, .navigation.actions .btn, .registration-entry .actions .btn-primary');
              if (actionBtn != null) {
                actionBtn.classList.add('disabled');
                actionBtn.removeAttribute('href');
              }
            }
          }
          function initCyberSourceMicroFormFields() {
            var _FlexJS5, _FlexJS6, _FlexJS9, _FlexJS9$number, _FlexJS10, _FlexJS10$securityCod, _FlexJS11, _FlexJS11$number, _FlexJS12, _FlexJS12$number, _FlexJS13, _FlexJS13$number;
            if (((_FlexJS5 = FlexJS) === null || _FlexJS5 === void 0 ? void 0 : _FlexJS5.number) == undefined && ((_FlexJS6 = FlexJS) === null || _FlexJS6 === void 0 ? void 0 : _FlexJS6.securityCode) == undefined) {
              var _FlexJS7, _FlexJS7$microform, _FlexJS8, _FlexJS8$microform;
              FlexJS.number = (_FlexJS7 = FlexJS) === null || _FlexJS7 === void 0 ? void 0 : (_FlexJS7$microform = _FlexJS7.microform) === null || _FlexJS7$microform === void 0 ? void 0 : _FlexJS7$microform.createField('number', {
                placeholder: '0000 0000 0000 0000'
              });
              FlexJS.securityCode = (_FlexJS8 = FlexJS) === null || _FlexJS8 === void 0 ? void 0 : (_FlexJS8$microform = _FlexJS8.microform) === null || _FlexJS8$microform === void 0 ? void 0 : _FlexJS8$microform.createField('securityCode');
            }
            (_FlexJS9 = FlexJS) === null || _FlexJS9 === void 0 ? void 0 : (_FlexJS9$number = _FlexJS9.number) === null || _FlexJS9$number === void 0 ? void 0 : _FlexJS9$number.load('.cybersource-payment-inputs .js-credit-card-input');
            (_FlexJS10 = FlexJS) === null || _FlexJS10 === void 0 ? void 0 : (_FlexJS10$securityCod = _FlexJS10.securityCode) === null || _FlexJS10$securityCod === void 0 ? void 0 : _FlexJS10$securityCod.load('.cybersource-payment-inputs .js-credit-card-cvv-input');
            (_FlexJS11 = FlexJS) === null || _FlexJS11 === void 0 ? void 0 : (_FlexJS11$number = _FlexJS11.number) === null || _FlexJS11$number === void 0 ? void 0 : _FlexJS11$number.on('error', function (data) {
              console.error(data);
              validationMessage.value = data.message;
            });
            (_FlexJS12 = FlexJS) === null || _FlexJS12 === void 0 ? void 0 : (_FlexJS12$number = _FlexJS12.number) === null || _FlexJS12$number === void 0 ? void 0 : _FlexJS12$number.on('load', function (data) {
              loading.value = false;
            });
            var cardIcon = document.querySelector('#cardDisplay');
            var cardSecurityCodeLabel = document.querySelector('label.credit-card-cvv-label');
            (_FlexJS13 = FlexJS) === null || _FlexJS13 === void 0 ? void 0 : (_FlexJS13$number = _FlexJS13.number) === null || _FlexJS13$number === void 0 ? void 0 : _FlexJS13$number.on('change', function (data) {
              if (data.card.length === 1) {
                var _FlexJS14, _FlexJS14$config;
                cardIcon.className = 'fa-2x ' + ((_FlexJS14 = FlexJS) === null || _FlexJS14 === void 0 ? void 0 : (_FlexJS14$config = _FlexJS14.config) === null || _FlexJS14$config === void 0 ? void 0 : _FlexJS14$config.cardIcons[data.card[0].name]);
                cardSecurityCodeLabel.textContent = data.card[0].securityCode.name;
                FlexJS.config.fields.cvv.title = data.card[0].securityCode.name;
              } else {
                cardIcon.className = 'fa-2x fas fa-credit-card';
              }
            });
            checkCybersourceFieldsLoaded(false);
          }
          function submitCyberSourceMicroFormInfo() {
            var _FlexJS15, _FlexJS15$microform;
            var options = {
              expirationMonth: ('00' + ccexpvalue.month).slice(-2),
              expirationYear: ccexpvalue.year
            };
            FlexJS.inSubmission = true;
            (_FlexJS15 = FlexJS) === null || _FlexJS15 === void 0 ? void 0 : (_FlexJS15$microform = _FlexJS15.microform) === null || _FlexJS15$microform === void 0 ? void 0 : _FlexJS15$microform.createToken(options, function (err, token) {
              if (err) {
                console.error(err);
                emit(GatewayEmitStrings.Error, err.message);
              } else {
                emit(GatewayEmitStrings.Success, token !== null && token !== void 0 ? token : "");
              }
            });
          }
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
          var controlId = "cyberSource_".concat(newGuid());
          var inputStyleHook = ref(null);
          var inputInvalidStyleHook = ref(null);
          var paymentInputs = ref(null);
          onSubmitPayment(() => {
            if (loading.value || failedToLoad.value) {
              return;
            }
            tokenResponseSent.value = false;
            setTimeout(() => {
              submitCyberSourceMicroFormInfo();
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
              var _FlexJS17;
              var _options = getFlexJSOptions(controlId, inputStyleHook.value, inputInvalidStyleHook.value);
              var flex = new Flex(props.settings.microFormJWK);
              FlexJS = {
                config: _options,
                captureContext: props.settings.microFormJWK,
                inSubmission: false,
                microform: flex.microform({
                  styles: (_FlexJS17 = FlexJS) === null || _FlexJS17 === void 0 ? void 0 : _FlexJS17.config.customStyles
                }),
                loadCheckInterval: setInterval(checkCybersourceFieldsLoaded, 1000),
                number: undefined,
                securityCode: undefined
              };
              initCyberSourceMicroFormFields();
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
            }, [createElementVNode("div", _hoisted_3, [_hoisted_4, createElementVNode("div", _hoisted_6, [createElementVNode("div", _hoisted_7, [_hoisted_8, createVNode(unref(DatePartsPicker), {
              label: "Expiration Date",
              isRequired: true,
              startYear: unref(nowYear),
              futureYearCount: 15,
              modelValue: unref(ccexpvalue),
              "onUpdate:modelValue": _cache[0] || (_cache[0] = $event => isRef(ccexpvalue) ? ccexpvalue.value = $event : ccexpvalue = $event),
              showBlankItem: false,
              multiple: false,
              hideDay: true
            }, null, 8, ["startYear", "modelValue"])]), createElementVNode("div", _hoisted_9, [createElementVNode("div", _hoisted_10, [createElementVNode("label", _hoisted_11, toDisplayString((_unref2 = unref(FlexJS)) === null || _unref2 === void 0 ? void 0 : _unref2.config.fields.cvv.title), 1), _hoisted_12])])])]), _hoisted_13], 512), withDirectives(createElementVNode("div", _hoisted_14, [createElementVNode("span", _hoisted_15, [createTextVNode(toDisplayString(validationMessage.value) + " ", 1), withDirectives(createElementVNode("p", null, _hoisted_17, 512), [[vShow, validationMessageShowReload.value]])])], 512), [[vShow, validationMessage.value]])], 512), [[vShow, !loading.value && !failedToLoad.value]]), createElementVNode("input", {
              ref_key: "inputStyleHook",
              ref: inputStyleHook,
              class: "form-control cybersource-input-style-hook form-group",
              style: {
                "display": "none"
              }
            }, null, 512), createElementVNode("div", _hoisted_18, [createElementVNode("input", {
              ref_key: "inputInvalidStyleHook",
              ref: inputInvalidStyleHook,
              type: "text",
              class: "form-control"
            }, null, 512)])]);
          };
        }
      }));

      script.__file = "KFSRockAssemblies/rocks.kfs.JavaScript.Obsidian/src/Controls/cyberSourceGatewayControl.obs";

    })
  };
}));
//# sourceMappingURL=cyberSourceGatewayControl.obs.js.map
